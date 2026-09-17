# CLAUDE.md — AntFarm (repo `chinese-study`)

> **AntFarm**: nền tảng học ngoại ngữ trực tuyến cho người Việt, kiến trúc monorepo đa service giống MedDental (`mdt-re-construct`). **Mỗi ngôn ngữ là một service + một app riêng**; tiếng Trung là ngôn ngữ đầu tiên. Chủ dự án vừa phát triển vừa là học viên đầu tiên (bắt đầu từ số 0).
> File này giữ **quy tắc bắt buộc** + **tổng quan ngắn** + **mục lục**.

## Mục lục

- [Quy trình đa Agent](#quy-trình-đa-agent) → chi tiết [`docs/agents/AGENT-WORKFLOW.md`](docs/agents/AGENT-WORKFLOW.md)
- [Quy tắc bắt buộc](#quy-tắc-bắt-buộc)
- [Tổng quan kiến trúc](#tổng-quan-kiến-trúc)
- Hợp đồng thực thi các đợt: `docs/agent-workflow/`
- **Bàn giao mới nhất:** [`docs/HANDOFF-2026-09-17-W1-W2-W12.md`](docs/HANDOFF-2026-09-17-W1-W2-W12.md) — đọc trước khi làm tiếp (bản trước: `docs/HANDOFF-2026-09-17-MVP.md`)

---

## Quy trình đa Agent

```
Người dùng → ORCHESTRATOR (phiên chính, Opus): phân loại
  • Câu hỏi → tự trả lời
  • Nâng cấp / Tạo mới → INVESTIGATION (Haiku) → BUSINESS ANALYSIS (Opus, hợp đồng + phân rã feature)
      → VÒNG LẶP từng feature: BACKEND ‖ DATABASE ‖ CONTENT (Sonnet) ‖ FRONTEND (Fable)
        → REVIEW (Opus) → INTEGRATION (Opus, commit local riêng) → DỪNG chờ người dùng OK
```

**Agent frontend chạy model Fable:** khi gọi `frontend-implement` qua công cụ `Agent`, **luôn truyền `model: "fable"`**.

**Feature-by-feature:** mỗi feature là đơn vị commit & test độc lập. Xong → kiểm tra → commit local → dừng cho người dùng review → mới sang feature kế. Không gộp nhiều feature vào một commit.

---

## Quy tắc bắt buộc

**Luôn trả lời bằng tiếng Việt có dấu** — kể cả giải thích kỹ thuật, comment code, tài liệu.

**Kiểm tra build sau khi sửa code** (chưa sạch thì chưa được báo hoàn thành):
- Backend: `dotnet build backend/backend.slnx -v q` — 0 error; `dotnet test backend/backend.slnx` — xanh.
- Frontend: `yarn workspace @af/<app> tsc -b` (**bắt buộc `-b`** — root tsconfig có `files: []` nên `--noEmit` không kiểm gì). Đụng dependency → thêm `yarn workspace @af/<app> build`.

**Commit local sau mỗi feature. Tuyệt đối không push.** Branch phát triển: `develop`; branch chính: `master`.

**Không commit secrets:** `appsettings.Development.json`, `appsettings.Production.json`, `.env`, `.env.local`, `*.pfx`, `*.pem`, `.secrets/` (khoá ký JWT của identity-service) bị gitignore. Chỉ commit `appsettings.json` (mặc định không nhạy cảm) và `.env.example`.

**Package manager frontend: yarn** (Classic 1.22) — không dùng npm/pnpm.

**MUI v9 — không dùng cú pháp cũ:**

| ❌ MUI v5 | ✅ MUI v9 |
|---|---|
| `<Stack justifyContent="center">` | `<Stack sx={{ justifyContent: 'center' }}>` |
| `<Typography sx={{ noWrap: true }}>` | prop `noWrap` hoặc `sx={{ whiteSpace: 'nowrap' }}` |
| `<ListItemText slotProps={{ primary: { fontWeight: 700 } }}>` | `slotProps={{ primary: { sx: { fontWeight: 700 } } }}` |
| `<Tooltip PopperProps={...}>` | `<Tooltip slotProps={{ popper: {...} }}>` |
| `<Tabs TabIndicatorProps={...}>` | `<Tabs slotProps={{ indicator: {...} }}>` |
| `<Dialog disableEscapeKeyDown>` | lọc `reason` trong `onClose` |

Nguyên tắc: **mọi prop `*Props` cũ → `slotProps`**; shorthand sx không còn là direct prop.

**Autocomplete `renderInput` — đè `slotProps` sau `{...params}` làm CHẾT ô, không lỗi nào hiện** (mất ref ⇒ `Cannot read properties of null (reading 'focus')` ⇒ không bao giờ hiện gợi ý). Luôn trải `params.slotProps` trước rồi mới ghi đè slot con.

**Không dùng package `uuid`** — dùng `crypto.randomUUID()`.

**tsconfig.app.json:** không dùng `baseUrl` (deprecated TS 6); dùng `"paths": { "@/*": ["./src/*"] }`.

**Peer dependency của package dùng chung:** `@af/ui`, `@af/api`, `@af/auth`, `@af/utils` khai thư viện ở `peerDependencies` ⇒ app tiêu thụ **phải** khai trong `dependencies`. Máy dev không báo lỗi (hoisted), chỉ Docker/CI lộ. Trước khi gỡ dependency khỏi app: `grep -l "<thu-vien>" frontend/packages/*/package.json`.

**Dialog/Drawer dùng `AppDialog`/`AppDrawer` của `@af/ui`** — chặn đóng ngoài ý muốn khi bấm ra ngoài/ESC. Hộp thoại chỉ đọc cần đóng nhanh thì khai `closeOnBackdrop` tường minh kèm bình luận lý do.

**Tab cấp trang dùng `useTabParam` của `@af/ui`** (tab lên URL, `replace` chứ không `push`).

**Npgsql + `timestamptz` — CHỈ nhận `DateTime` `Kind = Utc`** (nổ lúc runtime, build vẫn sạch):

| ❌ Sinh `Unspecified` | ✅ Viết đúng |
|---|---|
| `DateTime.Parse` chuỗi không `Z`/offset | parse sang `DateTimeOffset`, hoặc `SpecifyKind(..., Utc)` |
| `new DateTime(y,m,d)` · `DateTime.Today` · `DateTime.Now` | `DateTime.UtcNow` / `SpecifyKind(..., Utc)` |
| `dto.SomeOffset.Date` / `.DateTime` | `SpecifyKind(x.Date, Utc)` hoặc `x.UtcDateTime` |
| **Tham số `DateTime` bind từ query string** (`?from=2026-09-01`) | `SpecifyKind(q.From.Value.Date, Utc)`; cận trên nửa hở `< to.Date.AddDays(1)` |
| `default(DateTime)` chưa gán | gán tường minh trước `SaveChanges` |

**Ngày học theo múi giờ người dùng:** "hôm nay" (thẻ đến hạn, chuỗi ngày học liên tiếp, mục tiêu ngày) tính theo `users.time_zone` của service ngôn ngữ (chép từ claim `zoneinfo` của identity, mặc định `Asia/Ho_Chi_Minh`); mốc thời gian lưu UTC. Dùng UTC để cắt ngày sẽ làm chuỗi ngày học đứt oan lúc 0h–7h sáng.

**Dapper + `DateOnly`/`TimeOnly`** (nếu dùng Dapper): bắt buộc đăng ký TypeHandler lúc khởi động; đọc cột `date` phải khai `DateOnly?` chứ không `DateTime?`.

**XÁC THỰC TẬP TRUNG, PHÂN QUYỀN CỤC BỘ:** `identity-service` **chỉ xác thực** (tài khoản, mật khẩu, refresh token, ký JWT RS256, công bố JWKS) — không chứa vai trò/quyền của ngôn ngữ nào. **Mỗi service ngôn ngữ** kiểm JWT qua `AntFarm.Auth` (JWKS + issuer + audience riêng `af-<ngôn-ngữ>`) và tự phân quyền trên DB riêng: `users → user_roles → role_permissions → permissions`. JWT chỉ để nhận diện (`sub`, `email`, `name`, `zoneinfo`) — **không** có/không suy quyền từ claim `role`. Người dùng được provision danh tính lần đầu gọi service; vai trò mặc định theo `<Service>Access:DefaultRoles` (tiếng Trung: `learner`); admin bootstrap theo `<Service>Admin:BootstrapEmails` (vd `ChineseAdmin:BootstrapEmails`). Frontend đọc quyền từ `GET /<ngôn-ngữ>/api/me` của chính service đó, không tự derive.

**MỖI SERVICE MỘT DATABASE** (`af_identity`, `af_chinese`, ...) — không đọc/ghi chéo DB service khác; cần dữ liệu thì qua token hoặc HTTP API.

**`RequirePermissionAttribute` gán `Policy` của lớp cơ sở trong constructor**, KHÔNG khai `public new string Policy` — `new` chỉ che thuộc tính kiểu tĩnh, ASP.NET Core đọc `.Policy` qua `IAuthorizeData` nên nhận `null` ⇒ mọi `[RequirePermission]` thoái hoá thành `[Authorize]` trơn, không log, không lỗi. Mẫu đúng: `public RequirePermissionAttribute(string p) => Policy = $"Permission:{p}";`.

**Nút ẩn theo quyền phải kèm lời giải thích** (dải `Alert` chế độ chỉ xem) — không thì người dùng tưởng hệ thống hỏng.

**Trang lỗi 4xx thống nhất:** `ErrorPage` của `@af/ui`; route `/401`, `/403`, `/404`, `*` → `/404`; lời gọi GET trả 403/404 qua `createApiClient` tự điều hướng (401 ở client có `@af/auth` thì làm mới token, thất bại ⇒ về `/dang-nhap?returnTo=`; client không quản lý phiên mới nhảy `/401`), lời gọi ghi giữ lỗi để báo tại chỗ; lời gọi nền lúc mở app (vd `loadMe`) dùng `skipErrorRedirect`.

**MỌI TRUY CẬP ĐI QUA REVERSE PROXY — service KHÔNG public cổng:** production: **nginx biên** (TLS + phân theo host) → **gateway YARP** → service; trong `deploy/docker-compose.yml` **chỉ `nginx` có `ports:`**, bind qua `${PUBLIC_BIND:-127.0.0.1}` (Postgres không mở ra ngoài). Dev local: trình duyệt → Vite proxy → gateway `:5280` → service — không gọi thẳng cổng service từ trình duyệt kể cả ở dev. API ngôn ngữ luôn gọi **cùng origin** (`/<ngôn-ngữ>/api/...`, dev và production giống hệt). API identity: production gọi thẳng `https://id.antfarms.xyz/api/...` (CORS có credentials, danh sách origin `Auth:AllowedOrigins`, **CORS chỉ đặt ở identity-service** — nginx/gateway không thêm `Access-Control-*`); dev đi `/identity/api/...` qua Vite. URL identity là biến build `VITE_IDENTITY_API_URL` (nướng vào bundle — sai là chỉ hỏng khi bấm đăng nhập). nginx biên **ghi đè** `X-Forwarded-For $remote_addr` (không `$proxy_add_x_forwarded_for`), gateway `X-Forwarded: Append`; `proxy_pass` qua biến + `resolver 127.0.0.11`.

**Domain** (chốt 16/09/2026):

| Host | Đích |
|---|---|
| `id.antfarms.xyz` | identity-service (mọi đường dẫn → gateway `/identity/*`): đăng nhập/refresh/tài khoản, JWKS `https://id.antfarms.xyz/.well-known/jwks.json`, là `iss` của token |
| `admin.antfarms.xyz` | app admin kiêm CMS chung (W2) + `/cms/*`, `/chinese/*` → gateway; `X-Robots-Tag: noindex` |
| `chinese.antfarms.xyz` | app tiếng Trung + `/chinese/*` → gateway → chinese-backend |
| `antfarms.xyz` | portal / trang chọn ngôn ngữ — **feature sau (F13), ngoài MVP**; chưa có server block thật |
| `english.` · `japanese.` · `vietnamese.antfarms.xyz` | dành sẵn, cùng mẫu `chinese.` |

**Cookie refresh đa subdomain:** `af_rt`, HttpOnly, `SameSite=Strict`; production `Domain=.antfarms.xyz` + `Secure` + `Path=/api/auth`; dev **không** đặt `Domain` (localhost từ chối) + `Secure=false` + `Path=/identity/api/auth`. Mọi POST xác thực kiểm `Origin` thuộc `Auth:AllowedOrigins` — thêm subdomain mới mà quên khai là CORS chặn / đăng nhập trả 403. Đổi mật khẩu là **`POST /api/auth/password`** (D21), không phải `/api/account/password` — cookie chỉ gửi trong `Path` `/api/auth`, route ngoài nhánh này không biết phiên hiện tại để giữ lại khi thu hồi phiên khác.

**HTTPS Let's Encrypt cho mọi tên miền — bám ĐÚNG khuôn MedDental `mdt-re-construct/deploy/app-core`:** một máy = **một chứng chỉ SAN**, nginx mọi khối đọc cố định `certs/live/fullchain.pem` + `privkey.pem`; cấp bằng `deploy/scripts/get-cert.sh <email> <domain...>` (HTTP-01 webroot, **`--key-type rsa`** — ECDSA bị tường lửa SSL-inspection doanh nghiệp chặn) và **luôn truyền ĐỦ tên miền cũ + mới** (thiếu là đè mất chứng chỉ tên cũ); lần đầu dùng `self-signed.sh` để nginx lên được; gia hạn = container `certbot renew` 12 giờ **+ cron `renew-cert.sh`** chép sang `certs/live` + `nginx -t` + reload (thiếu cron là hết hạn im lặng). Cổng 80 chỉ ACME + `301 https`; TLS 1.2/1.3; HSTS `max-age` **không** `includeSubDomains`/`preload` cho tới khi mọi subdomain có HTTPS. `conf/nginx.conf.example` commit, `conf/nginx.conf` gitignore. nginx: `resolver 127.0.0.11 valid=10s ipv6=off` + `set $upstream` + `proxy_pass http://$upstream$request_uri`. **DNS ở Cloudflare:** bật proxy (đám mây cam) thì SSL mode **Full (strict)** (Flexible ⇒ vòng lặp chuyển hướng); proxied thì bật `set_real_ip_from` dải Cloudflare + `real_ip_header CF-Connecting-IP` (không thì IP thật = IP Cloudflare); HTTP-01 lỗi thì tạm để DNS-only lúc cấp. Service .NET sau proxy: `UseForwardedHeaders` với `KnownNetworks` = loopback + dải `af-net`, `ForwardLimit=2` (thiếu ⇒ IP = gateway; quá rộng ⇒ giả IP được). Dev local vẫn HTTP `localhost`, không chứng chỉ.

**Mọi service là Docker container:** Dockerfile multi-stage, cache-friendly (tách layer restore — copy props + `shared/` + riêng `.csproj` rồi `dotnet restore` trước khi copy source; `RUN --mount=type=cache,target=/root/.nuget/packages`; publish `--no-restore /m:2`), **KHÔNG khai `# syntax=docker/dockerfile:1`** (bắt BuildKit gọi Docker Hub trước mỗi lần build), nghe **8080** trong container, `HEALTHCHECK` bằng **`wget`** (ảnh aspnet không có curl). Frontend: node build → `nginx:1.27-alpine`, có khối `location ~* \.mjs$ { default_type application/javascript; }` (không dùng `types {}` ở server level). Giai đoạn đầu **chạy local không Docker** — file Docker vẫn phải cập nhật cùng feature và **ghi rõ "chưa verify"**; checklist verify ở `deploy/VERIFY-DOCKER.md`. Triển khai: `docker compose pull <svc> && docker compose up -d <svc>`; build trên server từng service một.

**Seed:** idempotent, không được ném lỗi (seed hỏng ⇒ tiến trình thoát ⇒ service chết). Danh mục người dùng tự thêm/xoá → chỉ gieo khi bảng TRỐNG. Học liệu nạp từ `content/<ngôn-ngữ>/` theo khoá tự nhiên, chạy lại không nhân đôi.

**Migration tự chạy lúc backend khởi động** theo cờ `AutoMigrate` — không dùng `dotnet ef database update` thủ công ngoài DB dev.

**Học liệu — bản quyền trước tiên:** chỉ dùng nguồn có giấy phép cho phép tái sử dụng; ghi nguồn + giấy phép + phần đã dùng vào `content/<ngôn-ngữ>/SOURCES.md`. Không rõ giấy phép ⇒ không dùng. Nghĩa tiếng Việt dịch máy phải đánh dấu `machine` cho tới khi được duyệt.

**Quy ước tiếng Trung:** giản thể là mặc định; pinyin **lưu dạng số thanh** (`ni3 hao3`, thanh nhẹ `5`, `ü` = `v`) làm nguồn sự thật, **hiển thị dạng dấu** (`nǐ hǎo`) qua `numberedToMarked` của `@af/chinese-kit` (W12 — trước đó ở `apps/chinese/src/lib/pinyin`). Phần tử chứa chữ Hán đặt `lang="zh-CN"` + phông fallback CJK. Chuẩn từ vựng **HSK 3.0**; SRS **FSRS-6** (chốt 16/09/2026).

**Mobile-first:** mọi màn học phải dùng tốt ở ~375px — người học ôn thẻ trên điện thoại là chính.

---

## Tổng quan kiến trúc

Monorepo đa service theo mẫu MedDental: **một gateway + một identity-service dùng chung + mỗi ngôn ngữ một backend service + một app frontend**. Tiếng Trung là ngôn ngữ đầu tiên.

```
Trình duyệt ─► [prod] nginx biên (TLS, host) / [dev] Vite proxy
             ─► gateway YARP ─┬─► identity-service ─► af_identity
                              ├─► chinese-backend  ─► af_chinese   (JWKS nội bộ từ identity-service)
                              └─► cms-backend      ─► af_cms       (W1 — nền tảng admin/website chung, JWKS nội bộ)
```

**Đợt W1–W15 (từ 17/09/2026):** thêm `cms-backend` (nền tảng quản trị chung) + `apps/admin` +
`apps/website` (Next.js, `antfarms.xyz`) — hợp đồng
`docs/agent-workflow/2026-09-17-antfarm-website-admin-cms-hop-dong-thuc-thi.md`. **W1, W2, W12 xong**
(cms-backend khung + phân quyền cục bộ fail-closed + `/api/me` + audience `af-cms` + route
gateway `/cms/**`; `apps/admin` khung gộp `/me` nhiều service; tách `@af/chinese-kit`); W3–W11, W13–W15 chưa làm.

### Backend — `backend/` (.NET 10, `backend.slnx`, Central Package Management)

- `global.json` · `Directory.Build.props` · `Directory.Packages.props` · `.dockerignore`.
- `shared/AntFarm.{Core,Logging,Security,HealthChecks,Auth,Testing}` — extension tiền tố `AddAf*`/`UseAf*`/`MapAf*`. `AntFarm.Testing` là thư viện tiện ích test (`[DbFact]`, `TestTokenFactory`). `tests/AntFarm.Shared.UnitTests` test cho shared.
- `services/gateway/` — `AntFarm.Gateway` (YARP): `/identity/**` → identity-service, `/chinese/**` → chinese-backend, `/cms/**` → cms-backend (W1).
- `services/identity-service/` — `AntFarm.Identity.{Domain,Application,Infrastructure,Api}` + `tests/AntFarm.Identity.{UnitTests,ApiTests}`. Khoá ký RSA: file PEM trong `.secrets/identity/keys/` (dev, gitignore, tự sinh) / volume `/keys` (Docker). `Jwt:Audiences` = `["af-identity","af-chinese","af-cms"]` (mảng tĩnh, một token dùng cho mọi audience — R-W6 hợp đồng W1–W15).
- `services/chinese-backend/` — `AntFarm.Chinese.{Domain,Application,Infrastructure,Api}` + `tests/AntFarm.Chinese.{UnitTests,ApiTests}`. Schema DB: `access` (người dùng/quyền cục bộ), `content`, `learning`.
- `services/cms-backend/` — **(W1, 17/09/2026)** `AntFarm.Cms.{Domain,Application,Infrastructure,Api}` + `tests/AntFarm.Cms.{UnitTests,ApiTests}` — nền tảng quản trị/website dùng chung (không riêng ngôn ngữ nào), DB `af_cms`, schema `access` (chép khuôn chinese-backend, thêm cột `user_roles.assigned_by`) + `site` (nội dung website, ảnh, hộp thư — từ W3+), audience `af-cms`. Phân quyền **fail-closed**: `CmsAccess:DefaultRoles` mặc định RỖNG (khác chinese `["learner"]`) — người mới vào admin KHÔNG có quyền nào cho tới khi admin gán tay. 3 vai trò `admin`/`editor`/`support`, nguồn duy nhất `AntFarm.Cms.Application.Access.RoleCatalog`.
- PostgreSQL 18 local, mỗi service một DB (`af_identity`, `af_chinese`, `af_cms`); test tích hợp dùng `af_<service>_test` qua biến `AF_TEST_PG` (chuỗi kết nối không có `Database=`) — thiếu biến thì `[DbFact]` tự skip. snake_case; một migration mỗi feature mỗi service, tên `F<n>_<Ten>` (đợt W1–W15 đổi tiền tố thành `W<n>_<Ten>`, ngoại lệ có chủ đích).
- Cấu hình dev: copy `appsettings.Development.json.example` → `appsettings.Development.json` (gitignore) cho từng service.
- **Migration không cần kết nối Postgres thật lúc sinh** (`dotnet ef migrations add`) — chỉ cần `ConnectionStrings:Default` ĐÚNG ĐỊNH DẠNG (không cần đúng mật khẩu, Npgsql không mở kết nối ở bước design-time); hữu ích trên máy chưa có Docker/mật khẩu Postgres local chưa biết.

| Thành phần | Dev local | Docker |
|---|---|---|
| gateway | http://localhost:5280 | `gateway:8080` |
| identity-service | http://localhost:5281 | `identity-service:8080` |
| chinese-backend | http://localhost:5282 (Scalar `/scalar/v1`) | `chinese-backend:8080` |
| cms-backend (W1) | http://localhost:5290 (Scalar `/scalar/v1`) | `cms-backend:8080` |
| apps/chinese | http://localhost:3280 (Vite proxy `/identity`, `/chinese` → 5280) | `chinese-frontend:80` |
| apps/admin (W2) | http://localhost:3290 (Vite proxy `/identity`, `/cms`, `/chinese` → 5280) | `admin-frontend:80` |
| apps/website (W7 — chưa làm, Next.js `antfarms.xyz`) | http://localhost:3281 (dành sẵn) | `website:3000` |
| Ngôn ngữ kế tiếp | backend 5283, app 3282, ... | `<ngon-ngu>-backend:8080` |

### Frontend — `frontend/` (Turborepo + Yarn Classic Workspaces + React 19 + MUI v9 + TypeScript + Vite)

- `packages/tsconfig|ui|api|auth|utils` (tên `@af/*`): `@af/ui` (theme, AppLayout, AppDialog, ErrorPage, useTabParam, LangText, TTS `speech`...), `@af/api` (`createApiClient`), `@af/auth` (AuthProvider, RequireAuth, RequirePermission, LoginPage/RegisterPage dùng chung — gọi identity-service), `@af/utils` (parseApiError, zod). Import thẳng TS source — không build/dist. Package chỉ được tạo ở feature đầu tiên cần nó; tiện ích đặc thù một ngôn ngữ đặt trong app của ngôn ngữ đó **cho tới khi có app thứ hai cần** — tiền lệ: `packages/chinese-kit` (`@af/chinese-kit`, W12 17/09/2026) tách từ `apps/chinese` để module Tiếng Trung của admin (W13) dùng chung: pinyin số⇄dấu, `Hanzi`/`Pinyin`, `ChineseSpeechProvider`/`SpeakButton`, `LessonContent` + khối/quiz, kiểu bài học/từ điển. Kit **không gọi API**: hook gọi máy chủ (`useTtsRate`) ở lại app và tiêm qua props; test riêng `yarn workspace @af/chinese-kit test`.
- `apps/admin` (`@af/admin`, W2) — admin kiêm CMS chung: `loadMe` gộp `GET /cms/api/me` + `/<ngôn-ngữ>/api/me` (Promise.allSettled), quyền gắn tiền tố service (`cms:users.manage`, `chinese:content.manage`), service không phản hồi chỉ ẩn phần của nó (`RequireAuth allowNoPermissions`), module ngôn ngữ khai ở `src/modules/registry.ts`. Origin `http://localhost:3290` / `https://admin.antfarms.xyz` phải có trong `Auth:AllowedOrigins` của identity.
- `apps/chinese` (`@af/chinese`) — học viên + quản trị nội dung tiếng Trung (ẩn theo quyền). `src/features/<module>/`; route slug tiếng Việt không dấu. Mỗi app có `Dockerfile` + `nginx.conf` (chỉ phục vụ tĩnh).
- `scripts/check-ui-conventions.mjs` (`yarn lint:ui`): FAIL `raw-dialog`, `uuid-import`, `autocomplete-slotprops-override`; WARN `tabs-no-url`.

### Học liệu — `content/`

`content/package.json` (dự án yarn riêng, công cụ kiểm tra chung) + mỗi ngôn ngữ một thư mục `content/<ngôn-ngữ>/` gồm `schemas/`, `data/`, `scripts/validate.mjs`, `SOURCES.md`, `LICENSES/`. Backend của ngôn ngữ nạp lúc khởi động — frontend không import thẳng JSON.

### Triển khai — `deploy/`

`docker-compose.yml` (postgres, identity-service, chinese-backend, **cms-backend (W1)**, gateway, chinese-frontend, **admin-frontend (W2)**, nginx, certbot — chỉ nginx publish cổng), `.env.example`, `conf/nginx.conf.example` (khối 80 ACME + server `id.`/`chinese.`/`admin.` + mẫu ngôn ngữ/portal để comment — khối `antfarms.xyz` thêm ở W7), `conf/cloudflare-realip.conf.example`, `scripts/{self-signed,get-cert,renew-cert}.sh`, `certs/` (gitignore), `postgres/init/` (tạo role+DB `af_identity`/`af_chinese`/`af_cms`, chỉ chạy khi volume trống), `VERIFY-DOCKER.md` (checklist verify Docker + HTTPS khi có server).

### Thêm một ngôn ngữ mới (tóm tắt — checklist đầy đủ ở hợp đồng §5.5)

Hợp đồng riêng → service `backend/services/<ngon-ngu>-backend` + DB `af_<ngon-ngu>` + phân quyền cục bộ + audience `af-<ngon-ngu>` trong identity → route gateway → app `frontend/apps/<ngon-ngu>` → học liệu `content/<ngon-ngu>/` → bản ghi DNS Cloudflare + chạy lại `get-cert.sh` với ĐỦ tên miền cũ + mới + khối server nginx + `Auth:AllowedOrigins` → Dockerfile + mục compose + init DB → cập nhật bảng domain/cổng trong file này. **Dùng lại, không làm mới:** gateway, identity-service, `AntFarm.*`, `@af/*`, lint UI, công cụ học liệu.

### Lệnh thường dùng

```bash
# Backend (từ gốc repo)
dotnet tool restore                                   # dotnet-ef cục bộ
dotnet build backend/backend.slnx -v q
dotnet test backend/backend.slnx                      # đặt AF_TEST_PG để chạy cả test tích hợp DB
dotnet run --project backend/services/identity-service/src/AntFarm.Identity.Api --launch-profile http
dotnet run --project backend/services/chinese-backend/src/AntFarm.Chinese.Api --launch-profile http
dotnet run --project backend/services/cms-backend/src/AntFarm.Cms.Api --launch-profile http   # W1
dotnet run --project backend/services/gateway --launch-profile http
dotnet ef migrations add F<n>_<Ten> --project backend/services/<svc>/src/AntFarm.<Svc>.Infrastructure --startup-project backend/services/<svc>/src/AntFarm.<Svc>.Api --output-dir Persistence/Migrations

# Frontend (chạy trong frontend/)
yarn install
yarn workspace @af/chinese dev
yarn workspace @af/chinese tsc -b
yarn workspace @af/chinese build
yarn lint:ui

# Học liệu
yarn --cwd content validate:chinese

# Docker (máy có Docker — xem deploy/VERIFY-DOCKER.md)
docker compose -f deploy/docker-compose.yml build <service>
docker compose -f deploy/docker-compose.yml pull <service> && docker compose -f deploy/docker-compose.yml up -d <service>
```

> Hợp đồng nền tảng + tiếng Trung MVP (F0–F11 MVP; F12 lên server; F13 portal — **F13 đã bị thay thế bởi đợt W1–W15**): `docs/agent-workflow/2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md`.
> Hợp đồng website `antfarms.xyz` + admin kiêm CMS chung (W1–W15, từ 17/09/2026 — **W1, W2, W12 xong**): `docs/agent-workflow/2026-09-17-antfarm-website-admin-cms-hop-dong-thuc-thi.md`.
