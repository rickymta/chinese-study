# AntFarm — Nền tảng học ngoại ngữ đa service + Tiếng Trung MVP — Hợp đồng thực thi

- Ngày: 2026-09-16 (bản viết lại theo kiến trúc đa service, thay thế `2026-09-16-chinese-study-mvp-hop-dong-thuc-thi.md` đã xoá)
- Loại: **tạo mới** · Repo: `G:\Github\15.ant-farm-technology\chinese-study` (nhánh `develop`)
- Service/app: `gateway` · `identity-service` · `chinese-backend` · `frontend/apps/chinese` · (feature sau MVP) `frontend/apps/portal` · học liệu `content/chinese/`
- Module: khung monorepo, xác thực tập trung (JWKS), phân quyền cục bộ theo service, pinyin & thanh điệu, từ vựng HSK + tra từ, flashcard SRS, luyện viết chữ Hán, bài học + quiz, quản trị nội dung, tổng quan tiến độ
- Người soạn: agent business-analysis (Opus) · Mẫu kiến trúc: `H:\Work\Meddental\mdt-re-construct`
- Trạng thái: **F0 sẵn sàng làm ngay.** Đã chốt D1 (HSK 3.0), D2 (FSRS-6), domain `antfarms.xyz` + mỗi ngôn ngữ một subdomain, mọi truy cập đi qua nginx/YARP, mọi service đóng Docker (giai đoạn đầu chạy thẳng trên máy local). Các quyết định còn mở ở §10.2 không chặn F0–F5.

> Tài liệu là **điểm tựa chống mất bộ nhớ**. Khi context bị nén: đọc §3 (quy tắc), §7 (feature đang làm), rồi mục §5/§6 mà feature trỏ tới. **F0–F1 chi tiết mức làm ngay; F2–F3 chi tiết mức thiết kế đầy đủ; F4 trở đi mức thiết kế** — BA phải bổ sung chi tiết (ghi ngày) trước khi agent thực thi bắt đầu các feature đó.

---

## 1. Bối cảnh & mục tiêu

### 1.1 Bối cảnh

Repo hiện chỉ có `CLAUDE.md`, `.gitignore`, `.claude/agents/*.md`, `docs/agents/AGENT-WORKFLOW.md`. Chủ dự án vừa phát triển vừa là **học viên đầu tiên, trình độ số 0**. Người dùng chốt (16/09/2026): dựng **nền tảng AntFarm** theo monorepo giống MedDental vì sau này sẽ học thêm các ngôn ngữ khác, **mỗi ngôn ngữ là một service riêng**; tiếng Trung là service đầu tiên.

Máy dev: Windows 11, .NET SDK 10.0.103/200/302, Node 22.18.0, Yarn 1.22.22, PostgreSQL 18 (Windows service `postgresql-x64-18`, cổng 5432). **Không có Docker.**

### 1.2 Mục tiêu phần mềm

- Triển khai: domain **`antfarms.xyz`**, mỗi ngôn ngữ một subdomain (`chinese.antfarms.xyz`, sau này `english.`, `japanese.`, `vietnamese.`). Production: **nginx biên** (TLS + phân theo host) → **gateway YARP** (định tuyến API) → service; **không service nào mở cổng ra host**. Mọi service là Docker container; giai đoạn đầu chạy thẳng trên máy local (chưa có Docker) nhưng vẫn đi qua gateway.
- Nền tảng: **gateway YARP** làm cửa vào duy nhất của API; **identity-service** chỉ lo xác thực (tài khoản email + mật khẩu, refresh token, ký access token RS256, công bố JWKS); mỗi **service ngôn ngữ** tự kiểm JWT và **tự phân quyền trên DB riêng**; frontend monorepo có package dùng chung `@af/*`, mỗi ngôn ngữ một app.
- Tiếng Trung MVP: người dùng đăng ký/đăng nhập, học pinyin, từ vựng HSK 1 bằng SRS, luyện viết, bài học + quiz, admin soạn nội dung, theo dõi tiến độ.

### 1.3 Mục tiêu học tập (nghiệp vụ sư phạm)

Người học đi **từ số 0 → hoàn thành từ vựng HSK 3.0 cấp 1** với nền phát âm đúng.

| Giai đoạn | Nội dung | Vì sao ở vị trí này | Đo tiến bộ |
|---|---|---|---|
| **G0 — Âm & thanh** (≈ 1–2 tuần, 15–20 phút/ngày) | Thanh mẫu, vận mẫu, 4 thanh + thanh nhẹ, quy tắc đặt dấu, biến điệu thanh 3 / 不 / 一 | Mọi thứ sau ghi bằng pinyin; sai thanh từ đầu rất khó sửa. Người Việt quen ngôn ngữ có thanh nhưng hay **nhầm thanh 2 ↔ 3**, nhầm `z/c/s ↔ zh/ch/sh`, `j/q/x`, `ü`, `-n ↔ -ng` | Độ chính xác nhận diện theo từng thanh + ma trận nhầm (F5) |
| **G1 — Từ vựng HSK 1 qua SRS** (từ tuần 2) | ~10 thẻ mới/ngày + ôn đến hạn | Lặp lại ngắt quãng là cách rẻ nhất để nhớ lâu; bắt đầu sớm để cộng dồn | Thẻ đã ôn, số từ "vững" (độ ổn định ≥ 21 ngày) (F7, F11) |
| **G1 — Bài học theo chủ đề** | Chào hỏi, số đếm, gia đình, thời gian, mua sắm... ≤ 10–15 từ mới/bài, hội thoại + ngữ pháp + quiz | Đặt từ vào ngữ cảnh; quiz kiểm hiểu chứ không chỉ nhận mặt chữ | Bài hoàn thành, điểm quiz (F9) |
| **G1 — Viết chữ Hán** | Thứ tự nét các chữ trong bài đang học | Viết giúp nhớ mặt chữ, nhận ra bộ thủ; **âm Hán Việt** là cầu nối riêng của người Việt | Số chữ đã luyện, số lỗi giảm (F8) |
| **Xuyên suốt** | Chuỗi ngày học, thẻ đến hạn | Thói quen hằng ngày quan trọng hơn cường độ | Streak theo múi giờ người dùng (F11) |

---

## 2. Phạm vi

### 2.1 In-scope (MVP)

1. Khung monorepo: shared libs `AntFarm.*`, gateway, identity-service, chinese-backend, packages `@af/*`, app `apps/chinese`, README, build/test sạch.
1b. Đóng gói triển khai **ngay từ F0/F1**: Dockerfile mọi service + app, `deploy/docker-compose.yml`, `deploy/.env.example`, cấu hình nginx biên theo subdomain (§5.6). Chỉ **viết đúng** — chưa nghiệm thu bằng Docker vì máy dev chưa có.
2. identity-service: đăng ký/đăng nhập, refresh token xoay vòng (cookie HttpOnly + phát hiện dùng lại), đăng xuất, RS256 + JWKS, hồ sơ tài khoản (tên hiển thị, múi giờ), đổi mật khẩu.
3. chinese-backend: kiểm JWT qua JWKS, provision danh tính lần đầu, phân quyền cục bộ, bootstrap admin, `/api/me`, màn quản lý vai trò; frontend trang 401/403/404.
4. Pinyin & thanh điệu; HSK 3.0 cấp 1 + tra từ; flashcard FSRS; luyện viết chữ Hán; bài học + quiz; quản trị nội dung + duyệt nghĩa; tổng quan tiến độ.
5. Checklist "thêm một ngôn ngữ mới" (§5.5).

### 2.2 Out-of-scope (MVP)

- OIDC đầy đủ (authorization code/PKCE, consent), SSO bên thứ ba, xác minh email, quên mật khẩu qua email.
- Nhận dạng giọng nói; file âm thanh người thật (chỉ TTS trình duyệt).
- HSK 2 trở lên; ngôn ngữ thứ hai (chỉ chuẩn bị khung + checklist).
- PWA offline, app di động gốc, thông báo đẩy, gamification.
- Optimizer tham số FSRS cá nhân.
- CI/CD, registry ảnh, cấp chứng chỉ TLS thật và đưa lên server — **F12** (sau MVP).
- **Portal `antfarms.xyz`** (trang chọn ngôn ngữ) + quản trị tài khoản identity — **F13, feature sau cùng, ngoài MVP** (người dùng chốt 16/09/2026). F0 không tạo `apps/portal`, không có server block nginx thật cho `antfarms.xyz`.

---

## 3. Quy tắc nghiệp vụ (đã chốt)

> **[BA-mặc định]** = BA chọn để không chặn việc, người dùng có thể phản đối (§10.3). **[MỞ — Dx]** = chờ chốt (§10.2).

### 3.1 Nền tảng & ranh giới service

- R-N1. **identity-service chỉ XÁC THỰC**: sở hữu tài khoản (email, mật khẩu, tên hiển thị, múi giờ, trạng thái khoá), refresh token, khoá ký. **Không** chứa vai trò/quyền của service ngôn ngữ nào.
- R-N2. **Mỗi service ngôn ngữ tự PHÂN QUYỀN** trên DB riêng (`users → user_roles → roles → role_permissions → permissions`). JWT chỉ dùng để nhận diện (`sub`, `email`, `name`, `zoneinfo`). **Không** đặt claim `role`/`permission` vào token.
- R-N3. **Mỗi service một database** (`af_identity`, `af_chinese`, sau này `af_<ngôn-ngữ>`). Service không đọc/ghi DB của service khác; cần dữ liệu thì qua token hoặc HTTP API.
- R-N4. Trình duyệt chỉ nói chuyện với **gateway** (dev: qua Vite proxy). Gateway định tuyến `/identity/**` → identity-service, `/chinese/**` → chinese-backend, bỏ tiền tố.
- R-N5. Service ngôn ngữ kiểm token **trực tiếp tại service** (JWKS + issuer + audience), không tin gateway đã kiểm.
- R-N6. **Mọi truy cập từ ngoài đi qua reverse proxy** — production: nginx biên → gateway YARP; dev local: Vite proxy → gateway YARP. **Không service nào publish cổng ra host** trong compose (chỉ nginx biên mở 80/443; Postgres không mở, hoặc chỉ `127.0.0.1` khi cần quản trị). Không gọi thẳng cổng service từ trình duyệt, kể cả ở dev.
- R-N7. **Bảng domain** (người dùng chốt 16/09/2026):

| Host | Đích | Ghi chú |
|---|---|---|
| `antfarms.xyz` | `portal-frontend` — **feature sau (F13)** | Chưa triển khai; nginx chỉ có file mẫu `.example` |
| `id.antfarms.xyz` | **mọi đường dẫn** → gateway (thêm tiền tố `/identity`) → identity-service | **identity-service có subdomain riêng (người dùng chốt 16/09/2026).** Trình duyệt gọi đăng ký/đăng nhập/làm mới/tài khoản trực tiếp ở đây (CORS có credentials); JWKS `https://id.antfarms.xyz/.well-known/jwks.json`; là `iss` của token |
| `chinese.antfarms.xyz` | `/` → `chinese-frontend`; `/chinese/*` → gateway → chinese-backend | App tiếng Trung |
| `english.antfarms.xyz` · `japanese.antfarms.xyz` · `vietnamese.antfarms.xyz` | Mẫu giống `chinese.` (đổi slug) | Chưa bật; server block để dạng mẫu comment trong nginx |

- R-N8. **Mọi service là Docker container** (Dockerfile multi-stage, nghe cổng **8080** trong container, `HEALTHCHECK` bằng `wget`), app frontend là container nginx phục vụ file tĩnh. Giai đoạn đầu chạy local bằng `dotnet run` + Vite nhưng file Docker phải luôn được cập nhật cùng feature.
- R-N8b. **HTTPS Let's Encrypt cho mọi tên miền `antfarms.xyz`** (người dùng chốt 16/09/2026), theo đúng khuôn MedDental `deploy/app-core`: một chứng chỉ SAN/máy, HTTP-01 webroot, `--key-type rsa`, certbot renew 12 giờ + cron `renew-cert.sh`, cổng 80 chỉ ACME + 301, TLS 1.2/1.3, HSTS không `includeSubDomains`. DNS quản lý ở **Cloudflare** — bật proxy thì SSL mode **Full (strict)**. Dev local vẫn HTTP `localhost`.
- R-N9. API **của ngôn ngữ** gọi **cùng origin** với trang đang mở (`chinese.antfarms.xyz/chinese/api/...`; dev `localhost:3280/chinese/api/...` — giống hệt, không cần CORS). **[BA-mặc định — D19]**
- R-N10. API **identity** ở production là **khác origin nhưng cùng site** (`chinese.antfarms.xyz` → `id.antfarms.xyz`): axios `withCredentials: true`, identity-service bật CORS **có credentials** với danh sách origin tường minh `Auth:AllowedOrigins` (không `*`). Ở dev không có subdomain nên app proxy `/identity/*` qua Vite → gateway (cùng origin). URL gốc identity là biến build `VITE_IDENTITY_API_URL` (dev mặc định `/identity/api`; production `https://id.antfarms.xyz/api`) — **biến nướng vào bundle lúc build**, sai giá trị thì ảnh vẫn build/chạy, chỉ hỏng khi bấm đăng nhập.

### 3.2 Tài khoản & xác thực (identity-service)

- R-A1. Email so khớp không phân biệt hoa thường (`email_normalized = lower(trim(email))`, unique).
- R-A2. Mật khẩu 8–128 ký tự, không bắt buộc ký tự đặc biệt (NIST SP 800-63B). Băm bằng `PasswordHasher<T>` của ASP.NET Core Identity (PBKDF2, có sẵn trong shared framework). **[BA-mặc định]**
- R-A3. Access token JWT **RS256**, sống **15 phút**, header có `kid`. Claims: `iss`, `aud` (mảng), `sub` (uuid tài khoản), `email`, `name`, `zoneinfo`, `jti`, `iat`, `nbf`, `exp`. `iss` = `Jwt:Issuer`: production `https://id.antfarms.xyz`, dev `http://localhost:5280/identity`. JWKS công khai: `https://id.antfarms.xyz/.well-known/jwks.json` (dev `http://localhost:5280/identity/.well-known/jwks.json`). Service ngôn ngữ **tải JWKS qua mạng nội bộ** (`http://identity-service:8080/.well-known/jwks.json` trong Docker, `http://localhost:5281/...` ở dev) — không đi vòng ra Internet.
- R-A4. `aud` = danh sách cấu hình `Jwt:Audiences` (MVP: `["af-identity", "af-chinese"]`). Thêm ngôn ngữ ⇒ thêm audience (§5.5).
- R-A5. Refresh token ngẫu nhiên 32 byte, sống **30 ngày trượt**, **xoay vòng mỗi lần dùng**; DB chỉ lưu SHA-256.
- R-A6. Dùng lại refresh token đã xoay: **ngoài** cửa sổ ân hạn 30 giây ⇒ thu hồi **cả họ** (family) + 401; **trong** cửa sổ (hai tab cùng làm mới) ⇒ cấp token mới cùng họ, không thu hồi. **[BA-mặc định]**
- R-A7. Refresh token gửi trình duyệt bằng cookie HttpOnly `af_rt`, `SameSite=Strict`, `Path` = **đường dẫn phía trình duyệt** của `/api/auth` (`Auth:RefreshCookiePath`). **Production (chốt 16/09/2026):** `Domain=.antfarms.xyz`, `Secure`, `Path=/api/auth` (vì gọi thẳng `id.antfarms.xyz/api/auth/...`). `chinese.` và `id.` cùng site `antfarms.xyz` nên cookie `Strict` vẫn đi kèm lời gọi CORS có credentials. **Dev:** **không** đặt `Domain` (cookie host-only `localhost` — trình duyệt từ chối `Domain=.localhost`; cookie không phân biệt cổng nên 3280/3281 dùng chung), `Secure=false`, `Path=/identity/api/auth` (vì đi qua Vite proxy có tiền tố). Cấu hình: `Auth:RefreshCookieDomain`, `Auth:RefreshCookiePath`, `Auth:RefreshCookieSecure`. Access token chỉ giữ trong bộ nhớ JS. **[ĐÃ CHỐT phần domain; phần còn lại BA-mặc định]**
- R-A7b. **CORS + chống CSRF** ở identity-service, một danh sách duy nhất `Auth:AllowedOrigins` (prod: `https://antfarms.xyz`, `https://chinese.antfarms.xyz`, sau này `https://english.` ...; dev: `http://localhost:3280`, `http://localhost:3281`):
  - CORS policy của identity: `WithOrigins(AllowedOrigins)`, `AllowCredentials()`, `AllowAnyHeader()`, method `GET, POST, PUT, OPTIONS`, `SetPreflightMaxAge(10 phút)`. **Không** `AllowAnyOrigin` (trình duyệt cấm kết hợp với credentials). CORS đặt ở **identity-service**, không ở gateway (gateway chỉ chuyển tiếp, kể cả preflight `OPTIONS`).
  - Mọi `POST` của `/api/auth/*` và `/api/account/*`: header `Origin` (hoặc `Referer` khi thiếu) phải thuộc danh sách, sai ⇒ `403 ORIGIN_NOT_ALLOWED` (log Warning kèm origin). Lý do: cookie `Domain=.antfarms.xyz` đi kèm request từ **mọi** subdomain — subdomain bị XSS/tiếp quản không được làm mới phiên hộ; CORS chỉ chặn đọc kết quả, không chặn request được gửi.
- R-A8. Sai mật khẩu 10 lần liên tiếp ⇒ khoá 15 phút (`423`). Thông báo không phân biệt sai email/sai mật khẩu.
- R-A9. Rate limit `login|register|refresh`: 20 yêu cầu/phút/IP (IP thật lấy từ `X-Forwarded-For` do gateway đặt; identity chỉ tin header này từ proxy loopback).
- R-A10. Đăng ký mở hay cần mời — **[MỞ — D4]**; cờ `Auth:AllowRegistration` (mặc định `true`).
- R-A11. Tài khoản bị khoá quản trị (`is_active=false`) ⇒ không đăng nhập/làm mới được; mọi refresh token bị thu hồi.
- R-A12. Đổi mật khẩu ⇒ thu hồi mọi họ refresh token **khác** họ hiện tại.
- R-A13. Khoá ký RSA 2048 bit, lưu **file PEM** trong thư mục cấu hình `Jwt:KeysPath` (dev mặc định `.secrets/identity/keys/`, đã gitignore). Development: thiếu khoá ⇒ tự sinh. Môi trường khác: thiếu khoá ⇒ **dừng khởi động với lỗi rõ ràng**. JWKS công bố public key của **mọi** file khoá còn trong thư mục; ký bằng khoá `Jwt:ActiveKeyId` (trống ⇒ file mới nhất). **[BA-mặc định — D16]**

### 3.3 Phân quyền cục bộ (mỗi service ngôn ngữ; áp cho chinese-backend)

- R-P1. Quyền hiệu lực chỉ từ DB `af_chinese` (schema `access`).
- R-P2. Danh mục quyền:

| Mã quyền | Ý nghĩa |
|---|---|
| `study.use` | Dùng mọi chức năng học của service |
| `content.manage` | Soạn/sửa/xuất bản bài học, quiz, duyệt nghĩa từ vựng |
| `users.manage` | Xem người dùng của service, gán vai trò |

| Vai trò | Quyền |
|---|---|
| `admin` | `study.use`, `content.manage`, `users.manage` |
| `learner` | `study.use` |

- R-P3. Vai trò/quyền là danh mục hệ thống ⇒ seed "chèn bù mã thiếu", idempotent, không ném lỗi.
- R-P4. **Provision danh tính**: request có token hợp lệ mà `sub` chưa có trong `access.users` ⇒ tạo dòng (id = `sub`, email, tên, múi giờ); đã có ⇒ đồng bộ email/tên/múi giờ nếu khác (tối đa 1 lần/5 phút mỗi user, cache bộ nhớ).
- R-P5. Người dùng mới provision nhận vai trò theo `ChineseAccess:DefaultRoles` (mặc định `["learner"]`; để rỗng ⇒ 0 quyền, fail-closed như MedDental). **[BA-mặc định — D17]**
- R-P6. Bootstrap admin: email trong `ChineseAdmin:BootstrapEmails` được gán `admin` **lúc provision lần đầu**; lúc khởi động, user đã tồn tại có email trong danh sách mà thiếu `admin` ⇒ gán một lần (không gỡ khi email bị xoá khỏi cấu hình).
- R-P7. Không gỡ được vai trò `admin` của **admin cuối cùng** (`422 LAST_ADMIN`).
- R-P8. Tài khoản 0 quyền đăng nhập được, frontend đưa thẳng tới `/403` kèm nút Đăng xuất.
- R-P9. `[RequirePermission("...")]` gán `Policy` của lớp cơ sở trong constructor; có test chứng minh learner nhận 403.

### 3.4 Thời gian & ngày học

- R-T1. Mốc thời gian lưu `timestamptz` UTC (`Kind=Utc`); ngày lịch lưu `date` (`DateOnly`).
- R-T2. Múi giờ là ID IANA, **thuộc hồ sơ identity** (mặc định `Asia/Ho_Chi_Minh`), đi vào token qua claim `zoneinfo`, service ngôn ngữ chép vào `access.users.time_zone` khi provision/đồng bộ. Frontend gửi `Intl.DateTimeFormat().resolvedOptions().timeZone` lúc đăng ký. Kiểm hợp lệ bằng `TimeZoneInfo.TryFindSystemTimeZoneById`.
- R-T3. "Hôm nay" = ngày lịch theo múi giờ người dùng tại thời điểm tính. Sự kiện học ghi `local_date` **lúc ghi**; đổi múi giờ sau không viết lại lịch sử.
- R-T4. Truy vấn khoảng ngày dùng nửa hở `[from 00:00 local → UTC, to+1 00:00 local → UTC)`.

### 3.5 Học liệu & ngôn ngữ (tiếng Trung)

- R-C1. Giản thể mặc định; phồn thể là trường phụ.
- R-C2. Pinyin lưu **số thanh**, âm tiết cách nhau một dấu cách, thanh nhẹ `5`, `ü` viết **`v`**, giữ hoa đầu danh từ riêng (`Bei3 jing1`), 儿化 là âm tiết `r5`. Hiển thị dạng dấu qua `@af/utils`. **[BA-mặc định — D10]**
- R-C3. Lưu **thanh gốc từ điển** (不 `bu4`, 一 `yi1`); biến điệu chỉ là gợi ý hiển thị. **[BA-mặc định — D11]**
- R-C4. Nghĩa Việt có `meaningViStatus: "machine" | "reviewed"` + `meaningViSource: "cvdict" | "machine" | "manual"`. Chưa được người dùng duyệt ⇒ `machine` (kể cả lấy từ CVDICT). Frontend gắn nhãn "chưa duyệt".
- R-C5. Hán Việt cấp chữ từ Unihan `kVietnamese`; cấp từ = nối âm đầu từng chữ, `hanVietStatus: "derived" | "reviewed"`.
- R-C6. Chỉ nguồn có giấy phép rõ ràng, ghi vào `content/chinese/SOURCES.md`. **Không** chép giáo trình có bản quyền — hướng dẫn, bài học, quiz do content-implement tự soạn.
- R-C7. Phần tử chứa chữ Hán đặt `lang="zh-CN"` + phông fallback CJK.
- R-C8. Import học liệu lúc khởi động, idempotent theo khoá tự nhiên, **không ghi đè** trường đã duyệt/sửa tay (`*_status = reviewed` hoặc `edited_at IS NOT NULL`).
- R-C9. **Chuẩn từ vựng: HSK 3.0** (chuẩn GF0025-2021). Kho từ đầu tiên là **HSK 3.0 cấp 1**; giữ nhãn cấp HSK 2.0 nếu nguồn có. **[ĐÃ CHỐT — D1, 16/09/2026]** Thứ tự đưa vào SRS (`path_order`): từ thuộc **cả** HSK 3.0–1 và HSK 2.0–1 lên trước, còn lại theo tần suất. **[BA-mặc định]**

### 3.6 Học tập

- R-L1. Luyện thanh chỉ dùng âm tiết–thanh có **chữ minh hoạ đơn âm** (một cách đọc) để TTS đọc đúng; thanh nhẹ không đưa vào bài nghe đơn âm tiết.
- R-L2. **SRS: FSRS-6, tham số mặc định, độ nhớ mục tiêu 0,9, không tối ưu tham số trong MVP.** **[ĐÃ CHỐT — D2, 16/09/2026]** Mặc định 10 thẻ mới/ngày, tối đa 200 lượt ôn/ngày. Một từ = một thẻ `hanzi_to_meaning`.
- R-L3. "Thẻ đến hạn hôm nay" = thẻ khác `new`, không tạm dừng, `due_at < 00:00 ngày mai theo múi giờ người dùng`. "Thẻ mới còn học được hôm nay" = `min(daily_new_cards − thẻ mới đã ôn lần đầu hôm nay, số từ chưa có thẻ trong lộ trình)`.
- R-L4. Hoàn thành bài học: quiz **≥ 80%**, không khoá tuần tự. **[MỞ — D7]**
- R-L5. Ngày tính vào streak khi có **≥ 1 hoạt động học có kết quả** (ôn ≥ 1 thẻ, nộp 1 phiên luyện thanh, viết xong ≥ 1 chữ, nộp 1 quiz). **[MỞ — D8]**

---

## 4. Hiện trạng liên quan

### 4.1 Repo đích

```
chinese-study/                 # tên thư mục repo giữ nguyên; tên nền tảng trong code là AntFarm
  .claude/agents/*.md  .gitignore  CLAUDE.md  docs/agents/AGENT-WORKFLOW.md
```

### 4.2 Mẫu ở `H:\Work\Meddental\mdt-re-construct` (kiểm chứng 16/09/2026)

| Cần | File mẫu | Rút gọn/khác biệt |
|---|---|---|
| SDK pin | `backend/global.json` | `10.0.100` + `rollForward: latestFeature` |
| Props | `backend/Directory.Build.props` | net10.0, ImplicitUsings, Nullable, LangVersion latest |
| Version tập trung | `backend/Directory.Packages.props` | Bê version §5.2.0.3. `Microsoft.OpenApi` **giữ 2.x (2.7.5)**; `FluentAssertions` **giữ 7.x** (8.x thương mại); `Yarp.ReverseProxy` 2.3.0 |
| Gateway | `backend/services/gateway/{Gateway.csproj,Program.cs,appsettings.json}` | YARP `LoadFromConfig("ReverseProxy")`, route `PathRemovePrefix`; bỏ OpenTelemetry + middleware chặn đường máy-máy |
| Service DDD | `backend/services/lms-backend/src/MedDental.Lms.Api/Program.cs` | Bỏ OpenTelemetry, Swagger UI, DevRoleBypass, HttpClient auth-service |
| Shared | `backend/shared/MedDental.{Logging,Security,HealthChecks,Auth,Core}` | Serilog chỉ `Enrich.FromLogContext`; CORS bỏ nhánh `*.meddental.vn`; Auth đổi từ OIDC Authority sang JWKS URL (§5.2.2) |
| Health | `backend/shared/MedDental.HealthChecks/HealthCheckExtensions.cs` | `/health/live`, `/health/ready`, `/health` — **AllowAnonymous** |
| Test API | `backend/services/wiki-backend/MedDental.Wiki.Tests/*.csproj` | xUnit + FluentAssertions + Moq + Mvc.Testing |
| Frontend root | `mdt-frontend/{package.json,turbo.json}` | Turbo ^2.5.4, `yarn@1.22.22` |
| tsconfig | `mdt-frontend/packages/tsconfig/{base,react-app,node}.json` | Bê nguyên |
| App mẫu | `mdt-frontend/apps/lms/*` | React ^19.2.6, MUI ^9.0.1, TanStack Query ^5.100.10, react-router-dom ^7.15.0, RHF ^7.75.0, zod ^4.4.3, axios ^1.16.1, TS ~6.0.2, Vite ^8.0.12, @vitejs/plugin-react ^6.0.1 |
| API client | `mdt-frontend/packages/api/src/createApiClient.ts` | Bỏ `ApiCryptoAdapter`; giữ refresh single-flight + điều hướng 4xx cho GET + `skipErrorRedirect` |
| Trang lỗi | `mdt-frontend/packages/ui/src/components/errors/ErrorPage.tsx` | Bê ý tưởng |
| Lint UI | `mdt-frontend/scripts/check-ui-conventions.mjs` | Bê luật `raw-dialog` (FAIL), `tabs-no-url` (WARN) + 2 luật mới |
| Auth FE | `mdt-frontend/packages/auth/src/*` | OIDC PKCE ⇒ **không bê**; viết mới cho identity-service |

---

## 5. Thiết kế giải pháp

### 5.0 Quy ước chung

#### 5.0.1 Đặt tên

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| Nền tảng | AntFarm / tiền tố `af` | — |
| Shared .NET | `backend/shared/AntFarm.<Lib>/`, namespace `AntFarm.<Lib>` | `AntFarm.Logging` |
| Extension shared | `AddAf*` / `UseAf*` / `MapAf*` | `AddAfSerilog`, `UseAfCorrelationId`, `AddAfJwtBearer` |
| Thư mục service | `backend/services/<ten>-service` (hạ tầng), `backend/services/<ngon-ngu>-backend` (ngôn ngữ) | `identity-service`, `chinese-backend` |
| Project service | `AntFarm.<Service>.{Domain,Application,Infrastructure,Api}` + `tests/AntFarm.<Service>.{UnitTests,ApiTests}` | `AntFarm.Chinese.Api` |
| Gateway | `backend/services/gateway/AntFarm.Gateway.csproj` | — |
| Database | `af_<service>` (dev), `af_<service>_test` (test) | `af_identity`, `af_chinese` |
| Route gateway | `/<service-slug>/**` | `/identity/**`, `/chinese/**` |
| Audience JWT | `af-<service-slug>` | `af-chinese` |
| Package FE | `@af/<pkg>` | `@af/ui` |
| App FE | `frontend/apps/<ngon-ngu>` → package `@af/<ngon-ngu>` | `apps/chinese` → `@af/chinese` |
| Học liệu | `content/<ngon-ngu>/` | `content/chinese/` |
| Cấu hình phân quyền service | `<Service>Admin:BootstrapEmails`, `<Service>Access:DefaultRoles` | `ChineseAdmin:BootstrapEmails` |

#### 5.0.2 Cổng dev (tránh dải MedDental 3000–3014, 5000–5960, 7001–7960, 8080, và 5200/7200 của EMR)

| Thành phần | URL dev |
|---|---|
| gateway | **http://localhost:5280** |
| identity-service | **http://localhost:5281** |
| chinese-backend | **http://localhost:5282** |
| apps/chinese | **http://localhost:3280** |
| apps/portal (F13 — feature sau) | http://localhost:3281 (dành sẵn) |
| Ngôn ngữ kế tiếp | backend 5283, app 3282, ... (tăng dần) |

Không dùng HTTPS ở dev. Vite `strictPort: true`.

#### 5.0.3 Quy ước kỹ thuật khác

| Chủ đề | Quy ước |
|---|---|
| DB | Mỗi service một `DbContext`, schema theo module, snake_case (`EFCore.NamingConventions`), PK `uuid` (`Guid.CreateVersion7()` ở ứng dụng), bảng lịch sử `public.__ef_migrations_history` |
| Migration | Một migration mỗi feature có đổi schema **trong mỗi service**, tên `F<n>_<TenNgan>` |
| API | Mỗi service tiền tố `/api` (gateway thêm `/identity`, `/chinese` phía trước), JSON camelCase, enum chuỗi snake_case, thời điểm ISO-8601 UTC `Z`, ngày `YYYY-MM-DD` |
| Lỗi | `{ "error": "Thông điệp tiếng Việt", "code": "MA_LOI", "details"?: {...} }` (§6.0) |
| Phân trang | `page` (từ 1), `pageSize` (mặc định 20, tối đa 100) → `{ items, page, pageSize, totalCount }` |
| Thời gian | `TimeProvider` có sẵn của .NET (đăng ký `TimeProvider.System`), không tự viết `IClock` |
| Route FE | Slug tiếng Việt không dấu: `/dang-nhap`, `/dang-ky`, `/pinyin`, `/tu-dien`, `/on-tap`, `/luyen-viet`, `/bai-hoc`, `/ho-so`, `/quan-tri/...` |
| Cấu trúc app | `src/features/<module>/{api.ts,hooks.ts,types.ts,pages/,components/}` |

### 5.1 Database

#### 5.1.0 F0 — tạo database (chưa có bảng)

Người dùng chạy một lần (README ghi lại):

```powershell
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
& $psql -U postgres -c "CREATE DATABASE af_identity ENCODING 'UTF8' TEMPLATE template0;"
& $psql -U postgres -c "CREATE DATABASE af_chinese  ENCODING 'UTF8' TEMPLATE template0;"
```

Dùng tài khoản `postgres` ở dev cho đơn giản; Docker dùng role riêng mỗi service (`deploy/postgres/init`, §5.6.5).

F0 đăng ký `DbContext` **rỗng** cho identity-service và chinese-backend. Khối AutoMigrate chỉ gọi `MigrateAsync()` khi `db.Database.GetMigrations().Any()`. ⚠️ EF Core 9+ ném lỗi `PendingModelChangesWarning` nếu model đổi mà chưa sinh migration ⇒ mọi feature đổi entity sinh migration trong cùng commit.

Lệnh sinh migration (dotnet-ef là local tool, `.config/dotnet-tools.json`):

```powershell
dotnet tool restore
dotnet ef migrations add F2_Accounts `
  --project backend/services/identity-service/src/AntFarm.Identity.Infrastructure `
  --startup-project backend/services/identity-service/src/AntFarm.Identity.Api `
  --output-dir Persistence/Migrations
```

#### 5.1.1 F2 — `af_identity`, schema `identity` (migration `F2_Accounts`)

```
identity.accounts
  id                  uuid PK                  -- = claim sub
  email               varchar(254) NOT NULL
  email_normalized    varchar(254) NOT NULL UNIQUE
  display_name        varchar(100) NOT NULL
  password_hash       text NOT NULL
  time_zone           varchar(64) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh'
  is_active           boolean NOT NULL DEFAULT true
  failed_login_count  int NOT NULL DEFAULT 0
  lockout_until       timestamptz NULL
  last_login_at       timestamptz NULL
  password_changed_at timestamptz NOT NULL
  created_at, updated_at timestamptz NOT NULL

identity.refresh_tokens
  id                  uuid PK
  account_id          uuid FK → accounts(id) ON DELETE CASCADE
  family_id           uuid NOT NULL
  token_hash          char(64) NOT NULL UNIQUE   -- hex SHA-256
  created_at          timestamptz NOT NULL
  expires_at          timestamptz NOT NULL
  rotated_at          timestamptz NULL
  replaced_by_id      uuid NULL
  revoked_at          timestamptz NULL
  revoke_reason       varchar(32) NULL           -- logout|reuse_detected|password_changed|account_disabled
  user_agent          varchar(300) NULL
  created_ip          varchar(45) NULL
  INDEX (account_id), INDEX (family_id)
```

Khoá ký không vào DB (R-A13). Quản trị tài khoản (khoá, đặt lại mật khẩu) cần quyền ở identity ⇒ thiết kế ở F13 (bảng `identity.account_roles` tối thiểu) — **không** làm ở F2.

#### 5.1.2 F3 — `af_chinese`, schema `access` (migration `F3_Access`)

```
access.users
  id                  uuid PK                  -- = sub từ identity
  email               varchar(254) NOT NULL
  display_name        varchar(100) NOT NULL
  time_zone           varchar(64) NOT NULL
  first_seen_at       timestamptz NOT NULL
  last_seen_at        timestamptz NOT NULL
  INDEX (lower(email))

access.roles              id uuid PK · code varchar(32) UNIQUE · name varchar(100)
access.permissions        code varchar(64) PK · description varchar(200)
access.role_permissions   role_id FK CASCADE · permission_code FK CASCADE · PK(role_id, permission_code)
access.user_roles         user_id FK → users CASCADE · role_id FK RESTRICT · assigned_at timestamptz · PK(user_id, role_id)
```

Không có mật khẩu, không có refresh token ở đây.

#### 5.1.3 F5 — `af_chinese` schema `learning` phần nền (migration `F5_ToneDrill`)

```
learning.study_events            -- SỔ HOẠT ĐỘNG HỌC DÙNG CHUNG (F5, F7, F8, F9 ghi; F11 đọc)
  id uuid PK · user_id uuid FK → access.users CASCADE
  kind varchar(32)               -- tone_drill|srs_review|writing|quiz_submit|lesson_complete
  occurred_at timestamptz · local_date date (tính lúc ghi, R-T3)
  quantity int DEFAULT 1 · correct int NULL · ref_id uuid NULL
  INDEX (user_id, local_date)

learning.tone_drill_sessions
  id uuid PK · user_id uuid FK CASCADE · mode varchar(16) (listen_tone|tone_pair)
  started_at, finished_at timestamptz · total int · correct int · INDEX (user_id, finished_at)

learning.tone_drill_answers
  id uuid PK · session_id uuid FK CASCADE · user_id uuid
  syllable varchar(8) (không thanh, 'ma','nv') · expected_tone smallint CHECK 1..4 · answered_tone smallint CHECK 1..4
  response_ms int NULL · INDEX (user_id, expected_tone)
```

Dịch vụ Application `IStudyActivityRecorder.RecordAsync(userId, kind, quantity, correct, refId, ct)` ghi **cùng transaction** với dữ liệu nghiệp vụ, tự tính `local_date` từ `access.users.time_zone`. Pinyin chart **không** vào DB (singleton `PinyinCatalog` đọc JSON).

#### 5.1.4 F6 — schema `content` từ vựng (migration `F6_Vocabulary`)

```
content.words
  id uuid PK · simplified varchar(32) · traditional varchar(32) NULL
  pinyin varchar(128)                 -- số thanh
  pinyin_search varchar(128)          -- bỏ thanh + cách, lower: 'nihao'
  hsk3_level smallint NULL (1..7; 7 = "7-9") · hsk2_level smallint NULL (1..6)
  path_order int NULL · frequency_rank int NULL
  pos text[] · meanings_en text[] · meanings_vi text[]
  meaning_vi_status varchar(16) · meaning_vi_source varchar(16)
  han_viet varchar(64) NULL · han_viet_status varchar(16) NULL
  search_vi text                      -- nghĩa Việt + Hán Việt, lower, bỏ dấu (tính ở C#)
  edited_at timestamptz NULL · created_at, updated_at timestamptz
  UNIQUE (simplified, pinyin) · INDEX (pinyin_search) · INDEX (hsk3_level, path_order) · INDEX (hsk2_level)

content.characters
  id uuid PK · hanzi varchar(4) UNIQUE · traditional varchar(4) NULL
  pinyin_readings text[] · han_viet text[] · stroke_count smallint NULL · radical varchar(4) NULL
  created_at, updated_at timestamptz

content.word_characters    word_id FK CASCADE · position smallint · character_id FK RESTRICT · PK(word_id, position)

content.import_runs
  id uuid PK · dataset varchar(64) · file_hash char(64) · imported_at timestamptz
  inserted, updated, skipped int · INDEX (dataset, imported_at DESC)
```

Bỏ dấu tiếng Việt ở C# (`VietnameseText.RemoveDiacritics`, test `đ/Đ`) — không cần extension `unaccent`.

#### 5.1.5 F7 — SRS (migration `F7_Srs`)

```
learning.learner_settings
  user_id uuid PK FK CASCADE · daily_new_cards smallint DEFAULT 10 (0..50) · daily_review_limit smallint DEFAULT 200
  desired_retention numeric(3,2) DEFAULT 0.90 (0.80..0.97) · tts_rate numeric(3,2) DEFAULT 0.80
  auto_play_audio boolean DEFAULT true · updated_at timestamptz

learning.srs_cards
  id uuid PK · user_id uuid FK CASCADE · word_id uuid FK → content.words CASCADE
  card_type varchar(24) DEFAULT 'hanzi_to_meaning'
  state varchar(12) (new|learning|review|relearning) · step smallint NULL · due_at timestamptz
  stability double NULL · difficulty double NULL · reps int · lapses int
  last_review_at timestamptz NULL · first_reviewed_at timestamptz NULL · first_reviewed_local_date date NULL
  is_suspended boolean DEFAULT false · source varchar(12) (path|manual|lesson) · created_at timestamptz
  UNIQUE (user_id, word_id, card_type) · INDEX (user_id, state, due_at)

learning.srs_review_logs
  id uuid PK · client_review_id uuid UNIQUE · card_id uuid FK CASCADE · user_id uuid
  rating smallint (1 again, 2 hard, 3 good, 4 easy) · reviewed_at timestamptz
  state_before varchar(12) · stability_before, difficulty_before double NULL
  elapsed_days double · scheduled_days double · duration_ms int NULL · INDEX (user_id, reviewed_at)
```

#### 5.1.6 F8–F11 — mức thiết kế

- **F8 `F8_Writing`**: `learning.writing_attempts` (id, user_id, hanzi, mode `guided|recall`, total_strokes, total_mistakes, hints_used, duration_ms, completed_at; INDEX (user_id, hanzi), (user_id, completed_at)); `learning.character_writing_stats` (PK user_id+hanzi, attempts, recall_attempts, last_mistakes, best_recall_mistakes, last_practiced_at).
- **F9 `F9_Lessons`**: `content.lessons` (id, slug UNIQUE, title, topic, level `hsk1`, order_index, summary, status `draft|published`, published_at, created_by, created_at, updated_at, `xmin` làm concurrency token); `content.lesson_blocks` (id, lesson_id, order_index, type `text|dialogue|grammar|tip`, payload **jsonb**); `content.lesson_words` (lesson_id, word_id, order_index); `content.quiz_questions` (id, lesson_id, order_index, type `single_choice|listen_choice`, prompt, prompt_lang, audio_text, options **jsonb**, correct_option_id, explanation); `learning.lesson_progress` (PK user_id+lesson_id, status, started_at, completed_at, best_score_percent); `learning.quiz_attempts` (id, user_id, lesson_id, submitted_at, total, correct, score_percent, answers **jsonb**). `jsonb` vì cấu trúc lồng theo loại, không bao giờ lọc SQL theo trường con. Seed bài học **chỉ khi `content.lessons` trống**.
- **F10**: không bảng mới (dùng `edited_at`, `xmin`). **F11**: không bảng mới.

### 5.2 Backend

#### 5.2.0 F0 — khung backend (CHI TIẾT LÀM NGAY)

##### 5.2.0.1 Cây thư mục phải tạo

```
chinese-study/
  .config/dotnet-tools.json                       # { "version": 1, "isRoot": true, "tools": { "dotnet-ef": { "version": "10.0.7", "commands": ["dotnet-ef"] } } }
  .editorconfig                                   # utf-8; *.cs indent 4; *.{ts,tsx,json,md,yml} indent 2; end_of_line lf
  .gitattributes                                  # * text=auto eol=lf ; *.png *.jpg *.ico *.woff2 binary
  .gitignore                                      # bổ sung: .secrets/  content/**/.raw/  content/node_modules/  deploy/.env  deploy/certs/  deploy/conf/nginx.conf  deploy/conf/cloudflare-realip.conf  deploy/secrets/
  README.md                                       # §5.2.0.9
  deploy/                                         # §5.6 — viết ở F0, F1 bổ sung chinese-frontend
    docker-compose.yml
    .env.example
    conf/nginx.conf.example                       # COMMIT — chép thành conf/nginx.conf (gitignore) rồi thay <...>; mẫu MedDental deploy/app-core
    conf/cloudflare-realip.conf.example           # set_real_ip_from dải IP Cloudflare + real_ip_header CF-Connecting-IP (bật khi đám mây cam)
    scripts/self-signed.sh                        # chứng chỉ tự ký tạm để nginx khởi động được trước lần cấp đầu
    scripts/get-cert.sh                           # certbot HTTP-01 webroot, --key-type rsa, MỘT chứng chỉ SAN cho mọi tên miền của máy
    scripts/renew-cert.sh                         # cron: chép bản gia hạn sang certs/live + nginx -t + reload
    certs/                                        # gitignore — live/{fullchain,privkey}.pem, letsencrypt/, acme/
    postgres/init/01-create-databases.sql         # CREATE DATABASE af_identity; af_chinese; (role riêng mỗi service)
    VERIFY-DOCKER.md                              # checklist verify khi có máy Docker (§5.6.6)
  backend/
    global.json
    Directory.Build.props
    Directory.Packages.props
    backend.slnx
    .dockerignore                                 # **/bin **/obj **/TestResults **/appsettings.Development.json **/.vs
    shared/
      AntFarm.Core/
        AntFarm.Core.csproj
        Errors/AppException.cs                    # abstract: string Code, int StatusCode, object? Details
        Errors/NotFoundException.cs               # 404 NOT_FOUND
        Errors/ConflictException.cs               # 409
        Errors/BusinessRuleException.cs           # 422
        Errors/ForbiddenException.cs              # 403
        Pagination/PagedResult.cs                 # record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
        Pagination/PageQuery.cs                   # Page/PageSize chặn biên (page<1⇒1; pageSize<1⇒20; >100⇒100)
      AntFarm.Logging/
        AntFarm.Logging.csproj
        SerilogExtensions.cs                      # AddAfSerilog(this WebApplicationBuilder, string serviceName)
      AntFarm.Security/
        AntFarm.Security.csproj
        Middleware/SecurityHeadersMiddleware.cs   # X-Content-Type-Options nosniff, X-Frame-Options DENY, Referrer-Policy no-referrer
        Middleware/CorrelationIdMiddleware.cs     # đọc/sinh X-Correlation-ID, ghi response, LogContext.PushProperty("CorrelationId")
        Middleware/MiddlewareExtensions.cs        # UseAfSecurityHeaders, UseAfCorrelationId
        Cors/CorsExtensions.cs                    # AddAfCors(config) đọc Cors:AllowedOrigins; const PolicyName = "AfCors"; UseAfCors()
        Errors/ExceptionHandlingExtensions.cs     # UseAfExceptionHandler(); AddAfInvalidModelStateResponse() — dùng chung mọi service
        Errors/ErrorResponseMapper.cs             # hàm thuần: Exception → (status, { error, code, details }) — unit test được
      AntFarm.HealthChecks/
        AntFarm.HealthChecks.csproj
        HealthCheckExtensions.cs                  # AddAfHealthChecks(), MapAfHealthChecks() — 3 endpoint AllowAnonymous
      # AntFarm.Auth/ — TẠO Ở F2 (không tạo ở F0)
    services/
      gateway/
        AntFarm.Gateway.csproj
        Program.cs
        appsettings.json                          # routes + clusters (§5.2.0.7) — không có bí mật
        Properties/launchSettings.json            # profile "http": http://localhost:5280, Development
        Dockerfile                                # §5.6.3 — build context ./backend
      identity-service/
        src/
          AntFarm.Identity.Domain/                AntFarm.Identity.Domain.csproj · AssemblyMarker.cs
          AntFarm.Identity.Application/           AntFarm.Identity.Application.csproj · DependencyInjection.cs (AddApplication)
                                                  Common/Abstractions/IIdentityDbContext.cs (chỉ SaveChangesAsync ở F0)
          AntFarm.Identity.Infrastructure/        AntFarm.Identity.Infrastructure.csproj · DependencyInjection.cs (AddInfrastructure)
                                                  Persistence/IdentityDbContext.cs · Persistence/Configurations/.gitkeep
          AntFarm.Identity.Api/                   AntFarm.Identity.Api.csproj · Program.cs · appsettings.json
                                                  appsettings.Development.json.example · Properties/launchSettings.json (5281)
                                                  Features/System/SystemController.cs     # GET /api/system/info
                                                  Dockerfile                              # §5.6.3 — build context ./backend
        tests/
          AntFarm.Identity.UnitTests/             *.csproj · Common/PlaceholderSanityTests.cs (xem §5.2.0.10)
          AntFarm.Identity.ApiTests/              *.csproj · Infrastructure/IdentityApiFactory.cs · System/HealthAndInfoTests.cs
      chinese-backend/
        src/
          AntFarm.Chinese.Domain/                 như identity (đổi tên)
          AntFarm.Chinese.Application/            Common/Abstractions/IChineseDbContext.cs
          AntFarm.Chinese.Infrastructure/         Persistence/ChineseDbContext.cs
          AntFarm.Chinese.Api/                    Program.cs · appsettings*.json · launchSettings (5282) · Features/System/SystemController.cs · Dockerfile
        tests/
          AntFarm.Chinese.UnitTests/
          AntFarm.Chinese.ApiTests/
    shared/                                       # (cùng thư mục backend/shared/ ở trên — tách dòng chỉ để dễ đọc)
      AntFarm.Testing/                            # THƯ VIỆN tiện ích test dùng chung — không phải project test
        AntFarm.Testing.csproj
        DbFactAttribute.cs                        # Skip khi thiếu biến AF_TEST_PG
        TestDatabase.cs                           # BuildConnectionString(dbName) = AF_TEST_PG + ";Database=" + dbName
    tests/
      AntFarm.Shared.UnitTests/                   # PROJECT test cho các thư viện backend/shared/
        AntFarm.Shared.UnitTests.csproj
        Core/PageQueryTests.cs
        Security/ErrorResponseMapperTests.cs
```

> `backend/shared/AntFarm.Testing` là **thư viện** tiện ích test (DbFact; `TestTokenFactory` từ F2) được project test của mọi service tham chiếu. `backend/tests/AntFarm.Shared.UnitTests` là **project test** cho các thư viện `backend/shared/`.

##### 5.2.0.2 `global.json`, `Directory.Build.props`

```json
{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }
```

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <!-- cần ICU: TimeZoneInfo nhận ID IANA trên Windows + so sánh chuỗi tiếng Việt -->
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>
</Project>
```

##### 5.2.0.3 `Directory.Packages.props`

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup Label="Gateway">
    <PackageVersion Include="Yarp.ReverseProxy" Version="2.3.0" />
  </ItemGroup>
  <ItemGroup Label="EF Core">
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.7" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.7" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
    <PackageVersion Include="EFCore.NamingConventions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="10.0.7" />
  </ItemGroup>
  <ItemGroup Label="Auth (dùng từ F2)">
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup Label="Logging">
    <!-- kéo theo Serilog.Sinks.Console + Serilog.Settings.Configuration -->
    <PackageVersion Include="Serilog.AspNetCore" Version="9.0.0" />
  </ItemGroup>
  <ItemGroup Label="OpenAPI">
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.7" />
    <!-- GIỮ 2.x: Microsoft.AspNetCore.OpenApi 10.0.7 viết theo API 2.x; lên 3.x là vỡ source generator -->
    <PackageVersion Include="Microsoft.OpenApi" Version="2.7.5" />
    <PackageVersion Include="Scalar.AspNetCore" Version="2.5.0" />
  </ItemGroup>
  <ItemGroup Label="Validation">
    <PackageVersion Include="FluentValidation" Version="11.11.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
    <PackageVersion Include="FluentValidation.AspNetCore" Version="11.3.0" />
  </ItemGroup>
  <ItemGroup Label="Testing">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    <!-- GIỮ 7.x: FluentAssertions 8.x đổi sang giấy phép thương mại -->
    <PackageVersion Include="FluentAssertions" Version="7.2.0" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
  </ItemGroup>
</Project>
```

Thêm gói ở feature sau ⇒ thêm vào file này, kiểm version có thật trên nuget.org, ghi vào báo cáo. **Không** thêm Testcontainers (không có Docker).

##### 5.2.0.4 csproj & tham chiếu

| Project | SDK | PackageReference | ProjectReference |
|---|---|---|---|
| `AntFarm.Core` | `Microsoft.NET.Sdk` | — | — |
| `AntFarm.Logging` | `Microsoft.NET.Sdk` + `<FrameworkReference Include="Microsoft.AspNetCore.App" />` | `Serilog.AspNetCore` | — |
| `AntFarm.Security` | `Microsoft.NET.Sdk` + FrameworkReference AspNetCore.App | `Serilog.AspNetCore` | Core |
| `AntFarm.HealthChecks` | `Microsoft.NET.Sdk` + FrameworkReference AspNetCore.App | — | — |
| `AntFarm.Gateway` | `Microsoft.NET.Sdk.Web` | `Yarp.ReverseProxy` | Logging, Security, HealthChecks |
| `AntFarm.<Svc>.Domain` | `Microsoft.NET.Sdk` | — | Core |
| `AntFarm.<Svc>.Application` | `Microsoft.NET.Sdk` | `Microsoft.EntityFrameworkCore`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions` | Domain, Core |
| `AntFarm.<Svc>.Infrastructure` | `Microsoft.NET.Sdk` + FrameworkReference AspNetCore.App | `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | Application |
| `AntFarm.<Svc>.Api` | `Microsoft.NET.Sdk.Web` | `Microsoft.EntityFrameworkCore.Design` (PrivateAssets all), `Microsoft.AspNetCore.OpenApi`, `Microsoft.OpenApi`, `Scalar.AspNetCore`, `FluentValidation.AspNetCore` | Application, Infrastructure, Logging, Security, HealthChecks |
| `AntFarm.Testing` | `Microsoft.NET.Sdk`, `IsPackable=false` | `xunit` | — |
| `AntFarm.Shared.UnitTests` | `Microsoft.NET.Sdk`, `IsPackable=false` | Test.Sdk, xunit, xunit.runner.visualstudio (PrivateAssets all), coverlet.collector (PrivateAssets all), FluentAssertions | Core, Security |
| `AntFarm.<Svc>.UnitTests` | như trên + Moq | | Domain, Application, Core |
| `AntFarm.<Svc>.ApiTests` | như trên + `Microsoft.AspNetCore.Mvc.Testing` | | Api, AntFarm.Testing |

`<Svc>` ∈ {`Identity`, `Chinese`}. Đường dẫn tới shared từ `services/<svc>/src/<project>/`: `../../../../shared/AntFarm.X/AntFarm.X.csproj`. Mỗi `Program.cs` của service kết thúc bằng `public partial class Program;`. Hai project Api cùng tên lớp `Program` ở namespace toàn cục — ApiTests mỗi service chỉ tham chiếu Api của **chính** service đó nên không đụng nhau.

`backend.slnx`:

```xml
<Solution>
  <Folder Name="/shared/">
    <Project Path="shared/AntFarm.Core/AntFarm.Core.csproj" />
    <Project Path="shared/AntFarm.Logging/AntFarm.Logging.csproj" />
    <Project Path="shared/AntFarm.Security/AntFarm.Security.csproj" />
    <Project Path="shared/AntFarm.HealthChecks/AntFarm.HealthChecks.csproj" />
    <Project Path="shared/AntFarm.Testing/AntFarm.Testing.csproj" />
    <Project Path="tests/AntFarm.Shared.UnitTests/AntFarm.Shared.UnitTests.csproj" />
  </Folder>
  <Folder Name="/services/gateway/">
    <Project Path="services/gateway/AntFarm.Gateway.csproj" />
  </Folder>
  <Folder Name="/services/identity-service/">
    <Project Path="services/identity-service/src/AntFarm.Identity.Domain/AntFarm.Identity.Domain.csproj" />
    <Project Path="services/identity-service/src/AntFarm.Identity.Application/AntFarm.Identity.Application.csproj" />
    <Project Path="services/identity-service/src/AntFarm.Identity.Infrastructure/AntFarm.Identity.Infrastructure.csproj" />
    <Project Path="services/identity-service/src/AntFarm.Identity.Api/AntFarm.Identity.Api.csproj" />
    <Project Path="services/identity-service/tests/AntFarm.Identity.UnitTests/AntFarm.Identity.UnitTests.csproj" />
    <Project Path="services/identity-service/tests/AntFarm.Identity.ApiTests/AntFarm.Identity.ApiTests.csproj" />
  </Folder>
  <Folder Name="/services/chinese-backend/">
    <Project Path="services/chinese-backend/src/AntFarm.Chinese.Domain/AntFarm.Chinese.Domain.csproj" />
    <Project Path="services/chinese-backend/src/AntFarm.Chinese.Application/AntFarm.Chinese.Application.csproj" />
    <Project Path="services/chinese-backend/src/AntFarm.Chinese.Infrastructure/AntFarm.Chinese.Infrastructure.csproj" />
    <Project Path="services/chinese-backend/src/AntFarm.Chinese.Api/AntFarm.Chinese.Api.csproj" />
    <Project Path="services/chinese-backend/tests/AntFarm.Chinese.UnitTests/AntFarm.Chinese.UnitTests.csproj" />
    <Project Path="services/chinese-backend/tests/AntFarm.Chinese.ApiTests/AntFarm.Chinese.ApiTests.csproj" />
  </Folder>
</Solution>
```

Tổng **19 project**. ⚠️ Gõ sai tên file solution vẫn có thể "thành công" mà không build gì (bài học MedDental) — kiểm output liệt kê đủ project.

##### 5.2.0.5 `Program.cs` của service (identity-service và chinese-backend giống nhau ở F0, chỉ khác tên)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddAfSerilog("chinese-backend");                           // identity: "identity-service"

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(builder.Configuration);        // DbContext; ném InvalidOperationException rõ ràng nếu ConnectionStrings:Default rỗng
builder.Services.AddApplication();
builder.Services.AddAfHealthChecks();                             // "self" [live]; Infrastructure thêm "postgres" [ready]
builder.Services.AddAfCors(builder.Configuration);

// F2 (identity) / F3 (chinese) chèn ở đây: AddAfJwtBearer, AddAuthorization, AddRateLimiter (identity)

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAfInvalidModelStateResponse();                // 400 → { error, code: "VALIDATION", details: { field: [..] } }
builder.Services.AddOpenApi("v1");                                // document transformer: Title "AntFarm Chinese API" / "AntFarm Identity API"

var app = builder.Build();

app.UseAfSecurityHeaders();
app.UseAfCorrelationId();
app.UseAfExceptionHandler();                                      // AppException → status+code; còn lại 500 "Đã xảy ra lỗi nội bộ." + log Error
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                             // /openapi/v1.json
    app.MapScalarApiReference();                                  // /scalar/v1
}
app.UseAfCors();
// F2/F3: app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllers();
app.MapAfHealthChecks();

if (app.Configuration.GetValue<bool>("AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ChineseDbContext>();
    if (db.Database.GetMigrations().Any())
        await db.Database.MigrateAsync();
    // F2+/F3+: seeder chạy ở đây — bắt mọi exception, log Error, KHÔNG ném
}

Log.Information("Khởi động chinese-backend ({Env})", app.Environment.EnvironmentName);
try { await app.RunAsync(); }
catch (Exception ex) { Log.Fatal(ex, "chinese-backend dừng bất thường"); }
finally { await Log.CloseAndFlushAsync(); }

public partial class Program;
```

`AddInfrastructure`:

```csharp
var cs = configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException("Thiếu ConnectionStrings:Default — copy appsettings.Development.json.example thành appsettings.Development.json và điền mật khẩu.");
services.AddDbContext<ChineseDbContext>(o => o
    .UseNpgsql(cs, npg => npg.MigrationsHistoryTable("__ef_migrations_history", "public"))
    .UseSnakeCaseNamingConvention());
services.AddScoped<IChineseDbContext>(sp => sp.GetRequiredService<ChineseDbContext>());
services.AddHealthChecks().AddDbContextCheck<ChineseDbContext>("postgres", tags: ["ready"]);
```

`GET /api/system/info` (AllowAnonymous tường minh — F2/F3 sẽ đặt FallbackPolicy yêu cầu đăng nhập):

```json
{ "service": "chinese-backend", "version": "0.1.0", "environment": "Development", "serverTimeUtc": "2026-09-16T08:00:00Z" }
```

`/health/live` · `/health/ready` · `/health` như MedDental, **AllowAnonymous**. `/health/ready` trả 503 khi Postgres không kết nối được.

##### 5.2.0.6 Cấu hình service

`appsettings.json` (commit — không bí mật), chinese-backend:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": { "Microsoft": "Warning", "Microsoft.Hosting.Lifetime": "Information", "Microsoft.EntityFrameworkCore": "Warning" }
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": { "Default": "" },
  "AutoMigrate": true,
  "Cors": { "AllowedOrigins": [] }
}
```

identity-service giống hệt. `Cors:AllowedOrigins` rỗng ở F0 cho cả hai service. Từ F2, identity-service **không dùng `Cors:AllowedOrigins`** mà dựng CORS có credentials từ `Auth:AllowedOrigins` (R-A7b); chinese-backend giữ rỗng (luôn cùng origin).

`appsettings.Development.json.example` (commit; copy thành `appsettings.Development.json` — đã gitignore):

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=af_chinese;Username=postgres;Password=<điền>"
  },
  "Serilog": { "MinimumLevel": { "Default": "Debug" } }
}
```

identity-service: `Database=af_identity`. Các khối thêm ở feature sau (ghi sẵn để agent biết đích):

- identity (F2), trong `appsettings.json`: `"Jwt": { "Issuer": "http://localhost:5280/identity", "Audiences": ["af-identity","af-chinese"], "AccessTokenMinutes": 15, "RefreshTokenDays": 30, "KeysPath": ".secrets/identity/keys", "ActiveKeyId": "" }`, `"Auth": { "AllowRegistration": true, "RefreshCookieName": "af_rt", "RefreshCookiePath": "/api/auth", "RefreshCookieDomain": ".antfarms.xyz", "RefreshCookieSecure": true, "RefreshReuseGraceSeconds": 30, "MaxFailedLogins": 10, "LockoutMinutes": 15, "AllowedOrigins": [] }`, `"ForwardedHeaders": { "KnownNetworks": ["127.0.0.1/32", "::1/128"], "ForwardLimit": 2 }` (Docker: thêm dải mạng `af-net` qua biến môi trường). Development example: `"Auth": { "RefreshCookieDomain": "", "RefreshCookiePath": "/identity/api/auth", "RefreshCookieSecure": false, "AllowedOrigins": ["http://localhost:3280", "http://localhost:3281"] }`, `"Jwt": { "KeysPath": "../../../../../.secrets/identity/keys" }` (tương đối thư mục chạy — agent chốt đường dẫn chạy được và ghi README).
- chinese (F3): `"Auth": { "Issuer": "http://localhost:5280/identity", "Audience": "af-chinese", "JwksUrl": "http://localhost:5281/.well-known/jwks.json", "RequireHttpsMetadata": false }`, `"ChineseAdmin": { "BootstrapEmails": [] }`, `"ChineseAccess": { "DefaultRoles": ["learner"] }`; example điền `"BootstrapEmails": ["<email-cua-ban>"]`.

`launchSettings.json` mỗi Api: một profile `"http"`, `commandName: Project`, `applicationUrl` theo §5.0.2, `ASPNETCORE_ENVIRONMENT: Development`, `launchBrowser: false`.

##### 5.2.0.7 Gateway

`Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddAfSerilog("gateway");
builder.Services.AddAfHealthChecks();
// KHÔNG AddAfCors/UseAfCors: middleware CORS tự trả lời preflight OPTIONS ⇒ identity-service không nhận được (R-A7b). Sửa 17/09/2026 theo review F0.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseAfCorrelationId();
app.UseAfSecurityHeaders();
app.MapAfHealthChecks();
app.MapReverseProxy();
app.Run();
```

`appsettings.json`:

```json
{
  "Serilog": { "MinimumLevel": { "Default": "Information", "Override": { "Microsoft": "Warning", "Yarp": "Warning" } } },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "identity": {
        "ClusterId": "identity",
        "Match": { "Path": "/identity/{**catch-all}" },
        "Transforms": [ { "PathRemovePrefix": "/identity" }, { "X-Forwarded": "Append" } ]
      },
      "chinese": {
        "ClusterId": "chinese",
        "Match": { "Path": "/chinese/{**catch-all}" },
        "Transforms": [ { "PathRemovePrefix": "/chinese" }, { "X-Forwarded": "Append" } ]
      }
    },
    "Clusters": {
      "identity": { "Destinations": { "primary": { "Address": "http://localhost:5281" } } },
      "chinese":  { "Destinations": { "primary": { "Address": "http://localhost:5282" } } }
    }
  }
}
```

`X-Forwarded: Append` (không `Set`): ở production nginx biên đã **ghi đè** `X-Forwarded-For $remote_addr` (bỏ giá trị client tự gửi), gateway nối thêm IP của nginx ⇒ identity đọc được IP thật với `ForwardLimit: 2`. Dùng `Set` thì IP thật bị thay bằng IP nginx ⇒ rate limit gom mọi người dùng thành một.

Cấu hình cluster bằng biến môi trường trong Docker: `ReverseProxy__Clusters__identity__Destinations__primary__Address=http://identity-service:8080` (tương tự `chinese`). File `appsettings.json` giữ địa chỉ dev.

Gateway **không** kiểm JWT (R-N5). Gateway **không** có database. `/health/*` của gateway chỉ báo gateway sống, không gộp health của service phía sau.

##### 5.2.0.8 Test F0

- `AntFarm.Shared.UnitTests`: `PageQueryTests` (3 ca biên); `ErrorResponseMapperTests` (`NotFoundException` → 404 + `NOT_FOUND`; `BusinessRuleException("LAST_ADMIN", ...)` → 422; exception lạ → 500 với thông điệp chung, không lộ `ex.Message`).
- `AntFarm.<Svc>.UnitTests`: một test thật tối thiểu cho mỗi service — kiểm `AddInfrastructure` ném `InvalidOperationException` khi connection string rỗng (gọi trên `ServiceCollection` + `ConfigurationBuilder` in-memory). Không viết test "assert true".
- `AntFarm.<Svc>.ApiTests` (không cần DB): factory đặt môi trường `Testing`, cấu hình in-memory `ConnectionStrings:Default = "Host=localhost;Database=unused"`, `AutoMigrate=false`. Test: `GET /health/live` 200; `GET /api/system/info` 200 và `service` đúng tên; `GET /api/khong-ton-tai` 404.
- `AntFarm.Testing/DbFactAttribute`: `public DbFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AF_TEST_PG"))) Skip = "Chưa đặt AF_TEST_PG — bỏ qua test cần PostgreSQL"; }`. `AF_TEST_PG` = chuỗi kết nối **không có `Database=`**, ví dụ `Host=localhost;Port=5432;Username=postgres;Password=...`; test mỗi service tự gắn `af_identity_test` / `af_chinese_test`. F0 chưa dùng.
- Gateway: không có project test ở F0; kiểm thủ công (§7 F0).

##### 5.2.0.9 README.md (gốc repo) — mục bắt buộc

1. Giới thiệu ngắn: AntFarm là nền tảng học ngoại ngữ; sơ đồ `trình duyệt → app (Vite) → gateway 5280 → identity 5281 / chinese 5282 → PostgreSQL (af_identity, af_chinese)`.
2. Yêu cầu: .NET SDK 10 (≥ 10.0.100), Node ≥ 22.12, Yarn 1.22, PostgreSQL 18 (`Get-Service postgresql-x64-18`).
3. Tạo database (§5.1.0).
4. Copy `appsettings.Development.json.example` → `appsettings.Development.json` cho **cả hai** service, điền mật khẩu.
5. Backend: `dotnet tool restore` · `dotnet build backend/backend.slnx -v q` · `dotnet test backend/backend.slnx` · chạy 3 cửa sổ:
   `dotnet run --project backend/services/identity-service/src/AntFarm.Identity.Api --launch-profile http`
   `dotnet run --project backend/services/chinese-backend/src/AntFarm.Chinese.Api --launch-profile http`
   `dotnet run --project backend/services/gateway --launch-profile http`
6. Kiểm: `http://localhost:5280/identity/api/system/info`, `http://localhost:5280/chinese/api/system/info`, `http://localhost:5282/scalar/v1`.
7. Test tích hợp DB: đặt `$env:AF_TEST_PG = "Host=localhost;Port=5432;Username=postgres;Password=..."` rồi `dotnet test`.
8. Frontend (bổ sung ở F1): `cd frontend` · `yarn install` · `yarn workspace @af/chinese dev` → http://localhost:3280.
9. Khắc phục: cổng bị chiếm (đổi `launchSettings.json` + cluster gateway + `vite.config.ts`); `/health/ready` 503 (Postgres chưa chạy / sai mật khẩu); gateway 502 (service phía sau chưa chạy).

#### 5.2.1 F1 — không đổi backend

F1 là frontend thuần. Ngoại lệ duy nhất cho phép: sửa lỗi F0 phát hiện khi nối frontend (ghi rõ trong commit).

#### 5.2.2 F2 — identity-service xác thực + `AntFarm.Auth` (thiết kế đầy đủ)

**Thư viện `backend/shared/AntFarm.Auth/`** (dùng bởi mọi service ngôn ngữ, và identity-service cho endpoint tài khoản của chính nó):

| File | Nội dung |
|---|---|
| `AfAuthOptions.cs` | `Issuer`, `Audience`, `JwksUrl`, `RequireHttpsMetadata` |
| `JwksConfigurationRetriever.cs` | `IConfigurationRetriever<OpenIdConnectConfiguration>`: tải JSON JWKS từ `JwksUrl`, trả `OpenIdConnectConfiguration { Issuer = options.Issuer }` với `SigningKeys` từ `JsonWebKeySet`. Dùng với `ConfigurationManager<OpenIdConnectConfiguration>` có sẵn (tự làm mới định kỳ; `RefreshOnIssuerKeyNotFound = true` ⇒ gặp `kid` lạ thì tải lại — hỗ trợ xoay khoá) |
| `JwtBearerExtensions.cs` | `AddAfJwtBearer(IConfiguration)`: `MapInboundClaims = false`; `TokenValidationParameters { ValidIssuer, ValidAudience, ValidateLifetime, ValidAlgorithms = ["RS256"], ClockSkew = 30s, NameClaimType = "name" }`; `options.ConfigurationManager = new ConfigurationManager<...>(JwksUrl, new JwksConfigurationRetriever(...), new HttpDocumentRetriever { RequireHttps = RequireHttpsMetadata })`. Overload `AddAfJwtBearer(IConfiguration, Func<IEnumerable<SecurityKey>> localKeys)` cho identity-service tự kiểm token của mình không cần gọi HTTP. `OnChallenge`/`OnForbidden` ghi body JSON `{ error, code }` |
| `ClaimsPrincipalExtensions.cs` | `GetAccountId()` (Guid từ `sub`), `GetEmail()`, `GetDisplayName()`, `GetTimeZone()` (`zoneinfo`) |
| `Authorization/RequirePermissionAttribute.cs` | `public RequirePermissionAttribute(string permission) => Policy = $"Permission:{permission}";` |
| `Authorization/PermissionPolicyProvider.cs`, `PermissionRequirement.cs`, `PermissionAuthorizationHandler.cs` | Policy `Permission:*` → requirement; handler gọi `IPermissionResolver` (interface khai trong AntFarm.Auth, **mỗi service hiện thực** trên DB của mình) |
| `Authorization/IPermissionResolver.cs` | `Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken)` |

Test cho service ngôn ngữ không cần identity chạy: factory `PostConfigure<JwtBearerOptions>` gán `options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(new OpenIdConnectConfiguration { Issuer = ... } + SigningKeys.Add(testRsaKey))` (**sửa 17/09/2026**: cách cũ `Configuration = ...` + `ConfigurationManager = null` KHÔNG chạy trên ASP.NET Core 10 — handler chỉ đọc `ConfigurationManager`; mẫu đúng ở `ChineseDbApiFactory.cs`); tiện ích `TestTokenFactory` đặt trong `AntFarm.Testing`.

**identity-service**

| Lớp | Thành phần |
|---|---|
| Domain | `Accounts/Account.cs` (`RegisterFailedLogin(now, max, lockoutMinutes)`, `IsLockedOut(now)`), `Accounts/RefreshToken.cs` (`IsActive(now)`) |
| Application | `Accounts/AuthService` (`RegisterAsync`, `LoginAsync`, `RefreshAsync`, `LogoutAsync` → `AuthResult(AccessToken, ExpiresAt, RefreshTokenPlain, AccountDto)`), `Accounts/AccountService` (`GetAsync`, `UpdateProfileAsync`, `ChangePasswordAsync`), validators, `IPasswordHasherService`, `ITokenIssuer`, `ISigningKeyStore` |
| Infrastructure | `Security/PasswordHasherService` (bọc `PasswordHasher<Account>`, `SuccessRehashNeeded` ⇒ băm lại), `Security/FileSigningKeyStore` (R-A13: đọc `*.pem` trong `KeysPath`, `kid` = tên file không đuôi dạng `yyyyMMdd-<8 hex>`, Development tự sinh bằng `RSA.Create(2048)` + `ExportPkcs8PrivateKeyPem`), `Security/TokenIssuer` (`JsonWebTokenHandler` + `SigningCredentials(RsaSecurityKey{KeyId}, RS256)`), cấu hình EF `identity.*` |
| Api | `Features/Auth/AuthController` (register/login/refresh/logout, set/xoá cookie theo `Auth:RefreshCookieDomain/Path/Secure` — xoá cookie phải dùng **đúng** Domain + Path lúc đặt, lệch là cookie cũ còn nguyên), CORS có credentials từ `Auth:AllowedOrigins` + filter kiểm `Origin` (R-A7b), `Features/Account/AccountController` (`GET/PUT /api/account`, `POST /api/account/password`), `Features/WellKnown/WellKnownController` (`GET /.well-known/jwks.json` — `Cache-Control: public, max-age=300`; `GET /.well-known/openid-configuration` tối thiểu `{ issuer, jwks_uri }` cho công cụ ngoài), `UseForwardedHeaders` tin loopback, `AddRateLimiter` policy `auth` |

Luồng refresh (R-A5, R-A6) — toàn bộ trong một transaction, khoá dòng `SELECT ... FOR UPDATE`:

```
hash = sha256(cookie af_rt)
t = refresh_tokens WHERE token_hash = hash FOR UPDATE
không có t / t.revoked_at != null / t.expires_at <= now  ⇒ xoá cookie, 401 REFRESH_INVALID
t.rotated_at != null:
    now − t.rotated_at ≤ grace (30s) ⇒ phát token mới cùng family, không thu hồi (ca hai tab)
    ngược lại ⇒ thu hồi mọi token active cùng family (reuse_detected), log Warning (accountId, familyId), 401
account không active ⇒ thu hồi family (account_disabled), 403 ACCOUNT_DISABLED
bình thường ⇒ t.rotated_at = now; t2 cùng family, expires now+30d; t.replaced_by_id = t2.id; phát access token mới
```

Đăng nhập: dọn lười token hết hạn > 7 ngày của chính tài khoản đó.

Test F2 bắt buộc: UnitTests — `TokenIssuer` (đủ claim, `kid` có trong header, `aud` là mảng, hết hạn 15 phút theo `TimeProvider` giả), `Account` lockout, validator, `FileSigningKeyStore` sinh khoá khi thư mục trống (thư mục tạm). ApiTests `[DbFact]` (`af_identity_test`, fixture `EnsureDeleted` + `MigrateAsync` một lần mỗi lượt): đăng ký → 201 + cookie đúng `Domain/Path/Secure` theo cấu hình (chạy cả hai bộ cấu hình dev và prod); `POST` với `Origin` lạ → 403 `ORIGIN_NOT_ALLOWED`; preflight `OPTIONS` từ origin hợp lệ có `Access-Control-Allow-Credentials: true` và **không** có `*`; trùng email khác hoa thường → 409; sai mật khẩu → 401; 10 lần → 423; refresh xoay token; dùng lại sau 31 giây (TimeProvider giả) → 401 và cả family bị thu hồi; trong 30 giây → 200; JWKS trả khoá có `kid` khớp header token; token phát ra được `AddAfJwtBearer` (cấu hình khoá từ JWKS vừa lấy) chấp nhận với audience `af-chinese`.

#### 5.2.3 F3 — chinese-backend nhận JWT + phân quyền cục bộ (thiết kế đầy đủ)

| Lớp | Thành phần |
|---|---|
| Domain | `Access/User.cs`, `Role.cs`, `Permission.cs`, `UserRole.cs`, `RolePermission.cs`, `Access/PermissionCodes.cs`, `RoleCodes.cs` |
| Application | `Access/UserProvisioningService.EnsureAsync(ClaimsPrincipal)` (R-P4/R-P5/R-P6; cache 5 phút theo `sub`), `Access/PermissionResolver : IPermissionResolver` (cache 60 giây, `Invalidate(userId)`), `Access/MeService`, `Access/UserAdminService` (list, `SetRolesAsync` kiểm R-P7 trong transaction) |
| Infrastructure | Cấu hình EF `access.*`, `Seeding/AccessSeeder` (chèn bù quyền/vai trò; bootstrap admin cho user đã tồn tại; không ném lỗi) |
| Api | `Program.cs`: `AddAfJwtBearer`, `AddAuthorization(o => o.FallbackPolicy = RequireAuthenticatedUser)`, `IAuthorizationPolicyProvider = PermissionPolicyProvider`; middleware `UserProvisioningMiddleware` sau `UseAuthentication` (chỉ khi `User.Identity.IsAuthenticated`). `Features/Me/MeController` (`GET /api/me`, chỉ `[Authorize]`), `Features/Admin/UsersController` (`users.manage`), `Features/Admin/RolesController` (đọc), `Features/Admin/PingController` (`GET /api/admin/ping`, `users.manage` — canh gác phân quyền). `SystemController` + health AllowAnonymous tường minh |

Test F3: UnitTests provisioning (vai trò mặc định, bootstrap chỉ gán một lần), `LAST_ADMIN`. ApiTests `[DbFact]` (`af_chinese_test`, token do `TestTokenFactory` ký): lần đầu gọi `/api/me` ⇒ tạo user + `learner`; email bootstrap ⇒ `admin`; learner gọi `/api/admin/ping` ⇒ 403 JSON `FORBIDDEN`; token sai audience (`af-identity` only) ⇒ 401; token hết hạn ⇒ 401; `grep -rn "new string Policy" backend` rỗng.

#### 5.2.4 F4 trở đi — mức thiết kế (BA bổ sung khi tới lượt)

- **F4 (hồ sơ & quản trị vai trò)**: backend đã có ở F2 (`/api/account`) + F3 (`/api/admin/users`); F4 chủ yếu frontend. Đổi hồ sơ ở identity ⇒ frontend gọi refresh ngay để token mang `name`/`zoneinfo` mới ⇒ chinese-backend đồng bộ ở request kế (bỏ qua cache 5 phút khi claim khác bản ghi).
- **F5 pinyin**: Domain `Pinyin/PinyinSyllable`, `Pinyin/PinyinText` (`NormalizeNumbered` nhận `ü|u:|v` ⇒ `v`; `FromToneMarks`; `ToSearchKey`; ≥ 25 unit test). Infrastructure `Content/PinyinCatalogLoader` đọc `content/chinese/data/pinyin/*.json` (lỗi ⇒ log Error + catalog rỗng, endpoint 503 `CONTENT_UNAVAILABLE`). `ToneDrillService.SubmitAsync` (syllable có thật, tone 1..4, 1–100 mục, `finishedAt ≥ startedAt`, không vượt hiện tại > 5 phút) ghi session + answers + `study_events`. `ToneStatsService` tính trên 200 câu gần nhất mỗi thanh; `recommendedFocus` = thanh có accuracy < 0,8 với ≥ 10 câu. Csproj Api: `<None Include="../../../../../content/chinese/data/**/*.json" LinkBase="content/chinese/data" CopyToOutputDirectory="PreserveNewest" />`; `Content:RootPath = "content/chinese"` tương đối `AppContext.BaseDirectory`.
- **F6 từ vựng**: `ContentImporter` (hash SHA-256 file ⇒ trùng lần trước thì bỏ qua; upsert theo `(simplified, pinyin)`/`hanzi`; không ghi đè trường đã duyệt; không xoá từ đã có mà file không còn — log Warning; dòng pinyin sai bỏ qua + log; ghi `import_runs`; không ném). `DictionaryService.SearchAsync`: `q` rỗng ⇒ liệt kê theo `hsk3_level`, `path_order`; có CJK ⇒ `LIKE` trên simplified/traditional; Latin có thanh ⇒ chuẩn hoá số ⇒ khớp/tiền tố `pinyin`; Latin không thanh ⇒ tiền tố `pinyin_search` hoặc chứa trong `search_vi` (bỏ dấu). Xếp hạng: chữ Hán chính xác > pinyin chính xác > tiền tố pinyin > Hán Việt > nghĩa; rồi `path_order`, `frequency_rank`.
- **F7 SRS (FSRS-6)**: Domain `Srs/ISrsScheduler` + `Srs/FsrsScheduler` — 21 tham số mặc định FSRS-6 chép từ `open-spaced-repetition/py-fsrs` (MIT, ghi nguồn + commit trong comment), bước học `[1m, 10m]`, học lại `[10m]`, `maximum_interval = 36500`, **tắt fuzz**. Unit test bắt buộc: `R(S,S) ≈ 0,9`; khoảng lần đầu `Again < Hard < Good < Easy`; `Again` ở review ⇒ `lapses+1`, sang `relearning`; **vector vàng** chép từ bộ test py-fsrs (ghi commit) sai số ≤ 1e-4; ranh giới "hôm nay" ở `Asia/Ho_Chi_Minh` lúc 23:30 và 00:30. `SrsQueueService` (đến hạn trước, thẻ mới theo `path_order` tới hạn mức R-L3), `SrsReviewService` (idempotent `clientReviewId`, khoá dòng thẻ, ghi log + `study_events`). Gói test `Microsoft.Extensions.TimeProvider.Testing` (kiểm version thật).
- **F8 viết**: `WritingController` nhận kết quả (không phục vụ dữ liệu nét), kiểm `hanzi` có trong `content.characters`, ghi `study_events(writing)`.
- **F9 bài học**: `LessonsController` (danh sách published + tiến độ; chi tiết không kèm đáp án; nộp quiz chấm ở server; `lesson_complete` lần đầu đạt). Validator payload `jsonb` theo `type` dùng chung seed + F10.
- **F10 quản trị nội dung**: `Admin/LessonsController` (`content.manage`, PUT theo lô blocks/words/quiz, concurrency `xmin` ⇒ 409, chặn xuất bản bài thiếu quiz/đáp án ⇒ 422), `Admin/WordsController` (duyệt nghĩa, `edited_at`).
- **F11 tổng quan**: `Progress/StreakCalculator.Calculate(IReadOnlySet<DateOnly>, DateOnly today)` ⇒ `(current, longest, studiedToday)` — hôm nay chưa học thì đếm lùi từ hôm qua. `ProgressController.GetOverview` tổng hợp 90 ngày từ `study_events`.
- **F13 Portal `antfarms.xyz` (feature sau MVP)**: identity-service thêm vai trò quản trị tài khoản (`identity.account_roles`, `IdentityAdmin:BootstrapEmails`, quyền `accounts.manage`), API khoá/mở, đặt lại mật khẩu; `GET /api/platform/languages` (danh mục ngôn ngữ đọc từ cấu hình gateway hoặc identity). Chi tiết khi tới lượt.

### 5.3 Frontend

#### 5.3.0 F1 — khung frontend (CHI TIẾT LÀM NGAY)

##### 5.3.0.1 Cây thư mục

```
frontend/
  package.json
  turbo.json
  yarn.lock                                   # sinh bởi yarn install — COMMIT
  .prettierrc.json                            # { "semi": false, "singleQuote": true, "printWidth": 110, "trailingComma": "all" }
  scripts/check-ui-conventions.mjs
  packages/
    tsconfig/
      package.json                            # "@af/tsconfig", "files": ["*.json"]
      base.json  react-app.json  node.json    # bê nguyên mdt (§4.2)
    ui/
      package.json  tsconfig.json
      src/index.ts
      src/theme/buildTheme.ts                 # palette sáng/tối; biến CSS --af-font-cjk
      src/theme/ThemeProvider.tsx             # CssBaseline + chế độ sáng/tối lưu localStorage 'af.themeMode' (try/catch)
      src/components/layout/AppLayout.tsx     # md+: Drawer trái cố định; xs–sm: AppBar + BottomNavigation (≤ 5 mục)
      src/components/layout/PageContainer.tsx
      src/components/errors/ErrorPage.tsx
      src/components/errors/NotFoundPage.tsx
      src/components/text/LangText.tsx        # <Box component="span" lang={lang}> + phông theo lang (zh-* ⇒ --af-font-cjk)
    api/
      package.json  tsconfig.json
      src/index.ts
      src/createApiClient.ts                  # F1: baseURL + điều hướng GET 403/404 + skipErrorRedirect; F2 thêm token/refresh
  apps/
    chinese/
      package.json  index.html  vite.config.ts
      Dockerfile  nginx.conf                  # §5.6.4 — build context ./frontend; nginx CHỈ phục vụ tĩnh (proxy API do nginx biên lo)
      tsconfig.json  tsconfig.app.json  tsconfig.node.json
      public/favicon.svg
      src/main.tsx
      src/App.tsx                             # ThemeProvider → QueryClientProvider → RouterProvider
      src/router.tsx                          # "/" (AppShell) + index HomePage; "/404"; "*" → Navigate /404
      src/vite-env.d.ts
      src/api/clients.ts                      # chineseApi = createApiClient({ baseURL: '/chinese/api' }); identityApi = createApiClient({ baseURL: import.meta.env.VITE_IDENTITY_API_URL ?? '/identity/api', withCredentials: true })
      .env.example                            # VITE_IDENTITY_API_URL=/identity/api  (production build: https://id.antfarms.xyz/api)
      src/layout/AppShell.tsx                 # AppLayout, title "AntFarm · Tiếng Trung", nav [Trang chủ]
      src/features/system/api.ts              # getSystemInfo(client)
      src/features/system/hooks.ts            # useSystemInfo('chinese' | 'identity') — useQuery, skipErrorRedirect
      src/pages/HomePage.tsx                  # Lời chào + 2 chip trạng thái: "Tiếng Trung: đang chạy" / "Tài khoản: đang chạy" hoặc Alert lỗi
```

`@af/auth` và `@af/utils` **tạo ở F2**. F1 không tạo package rỗng.

`ui` gọi là `LangText` (không `Hanzi`) vì package dùng chung cho mọi ngôn ngữ; `apps/chinese` có thể bọc thành `Hanzi` riêng.

##### 5.3.0.2 `frontend/package.json` và `turbo.json`

```json
{
  "name": "antfarm-frontend",
  "private": true,
  "version": "0.0.0",
  "workspaces": ["apps/*", "packages/*"],
  "scripts": {
    "dev": "turbo dev",
    "build": "turbo build",
    "typecheck": "turbo typecheck",
    "lint:ui": "node scripts/check-ui-conventions.mjs"
  },
  "devDependencies": { "prettier": "^3.8.3", "turbo": "^2.5.4" },
  "packageManager": "yarn@1.22.22",
  "engines": { "node": ">=22.12", "yarn": ">=1.22" }
}
```

`turbo.json`: `$schema` turbo; tasks `build` (`dependsOn: ["^build"]`, `outputs: ["dist/**"]`), `dev` (`cache: false, persistent: true`), `typecheck` (`cache: false`), `lint:ui` (`cache: false`).

##### 5.3.0.3 `apps/chinese/package.json`

```json
{
  "name": "@af/chinese",
  "private": true,
  "version": "0.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "typecheck": "tsc -b",
    "lint:ui": "node ../../scripts/check-ui-conventions.mjs src",
    "preview": "vite preview"
  },
  "dependencies": {
    "@af/api": "*",
    "@af/ui": "*",
    "@emotion/react": "^11.14.0",
    "@emotion/styled": "^11.14.1",
    "@mui/icons-material": "^9.0.1",
    "@mui/material": "^9.0.1",
    "@tanstack/react-query": "^5.100.10",
    "axios": "^1.16.1",
    "react": "^19.2.6",
    "react-dom": "^19.2.6",
    "react-is": "^19.2.6",
    "react-router-dom": "^7.15.0"
  },
  "devDependencies": {
    "@af/tsconfig": "*",
    "@types/node": "^24.12.4",
    "@types/react": "^19.2.14",
    "@types/react-dom": "^19.2.3",
    "@vitejs/plugin-react": "^6.0.1",
    "typescript": "~6.0.2",
    "vite": "^8.0.12"
  }
}
```

Package dùng chung (`exports: { ".": "./src/index.ts" }`, `private: true`):
- `@af/ui` peerDependencies: `@emotion/react`, `@emotion/styled`, `@mui/icons-material`, `@mui/material`, `react`, `react-dom`, `react-router-dom` (F2 thêm `react-hook-form`). devDependencies: `@af/tsconfig`, `@types/react`, `@types/react-dom`.
- `@af/api` peerDependencies: `axios`.
- App phải khai **đủ** peer trong `dependencies` (quy tắc CLAUDE.md).

ESLint không đưa vào MVP (cổng: `tsc -b` + `lint:ui`).

##### 5.3.0.4 `vite.config.ts` + tsconfig

```ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

// Trình duyệt chỉ nói chuyện với gateway (R-N4). Dev không có subdomain id.* nên identity cũng đi cùng origin qua /identity (R-N10);
// production identity nằm ở https://id.antfarms.xyz (VITE_IDENTITY_API_URL), /chinese vẫn cùng origin.
const GATEWAY = 'http://localhost:5280'

export default defineConfig({
  plugins: [react()],
  resolve: { alias: { '@': path.resolve(__dirname, 'src') } },
  server: {
    port: 3280,
    strictPort: true,
    proxy: {
      '/identity': { target: GATEWAY, changeOrigin: false },
      '/chinese': { target: GATEWAY, changeOrigin: false },
    },
  },
})
```

⚠️ Route SPA của app **không được** bắt đầu bằng `/identity` hoặc `/chinese` (sẽ bị proxy nuốt). `src/vite-env.d.ts` khai kiểu `ImportMetaEnv { readonly VITE_IDENTITY_API_URL?: string }`. Quy ước route tiếng Việt ở §5.0.3 đã tránh được.

- `tsconfig.json`: `{ "files": [], "references": [{ "path": "./tsconfig.app.json" }, { "path": "./tsconfig.node.json" }] }`
- `tsconfig.app.json`: `extends "@af/tsconfig/react-app.json"`, `tsBuildInfoFile: "./node_modules/.tmp/tsconfig.app.tsbuildinfo"`, `"paths": { "@/*": ["./src/*"] }` (**không `baseUrl`**), `include: ["src"]`.
- `tsconfig.node.json`: `extends "@af/tsconfig/node.json"`, `tsBuildInfoFile: "./node_modules/.tmp/tsconfig.node.tsbuildinfo"`, `include: ["vite.config.ts"]`.
- `index.html`: `lang="vi"`, `<title>AntFarm · Tiếng Trung</title>`, meta viewport, `theme-color`.

##### 5.3.0.5 Giao diện

- Màu nền tảng (dùng chung mọi ngôn ngữ): primary xanh lục đậm `#2E7D32`, nền sáng `#FAFAF7`. Mỗi app ngôn ngữ truyền **màu nhấn riêng** vào `buildTheme({ accent })`: tiếng Trung đỏ son `#C62828`. Frontend-implement được tinh chỉnh.
- `--af-font-cjk`: `"Noto Sans SC", "Microsoft YaHei", "PingFang SC", "Hiragino Sans GB", sans-serif` (không tải webfont trong MVP).
- `AppLayout` props: `title`, `navItems: { label, to, icon, requiredPermission? }[]`, `userMenu?`. Bottom nav ≤ 5 mục, thừa gom vào "Thêm". Kiểm ở 375×812.

##### 5.3.0.6 `scripts/check-ui-conventions.mjs` — có ngay từ F1

Quét `apps/*/src/**/*.{ts,tsx}` (hoặc đường dẫn truyền vào), chỉ dùng Node chuẩn.

| Luật | Mức | Phát hiện |
|---|---|---|
| `raw-dialog` | FAIL | import `Dialog`/`Drawer`/`SwipeableDrawer` từ `@mui/material` trong `apps/` (`packages/ui` được miễn) |
| `uuid-import` | FAIL | `from 'uuid'` |
| `autocomplete-slotprops-override` | FAIL | trong `renderInput` có `{...params}` rồi `slotProps={{` mà khối không mở đầu bằng `...params.slotProps` (regex nhiều dòng) |
| `tabs-no-url` | WARN | `<Tabs` trong file không có `useTabParam` và không nằm trong `AppDialog/AppDrawer` |

Exit 1 khi có FAIL.

#### 5.3.1 F2 — `@af/auth`, `@af/utils`, đăng nhập/đăng ký (thiết kế đầy đủ)

**`@af/api` bổ sung:**

```ts
interface ApiClientOptions {
  baseURL: string
  getAccessToken?: () => string | null
  refresh?: () => Promise<string>     // trả access token mới; ném nếu mất phiên
  onAuthLost?: () => void             // → /dang-nhap?returnTo=...&reason=expired
}
```

Gắn `Authorization: Bearer`; `withCredentials: true`; 401 (trừ URL `/auth/*`) ⇒ single-flight `refresh()` rồi thử lại **một** lần, hỏng ⇒ `onAuthLost()`. 403/404 cho GET ⇒ `window.location.assign('/403'|'/404')` trừ `skipErrorRedirect` hoặc đang đứng ở đó; request ghi giữ lỗi.

**`@af/utils`**: `parseApiError(err) → { message, code?, fieldErrors? }`; zod 4 `emailSchema`, `passwordSchema`, `displayNameSchema` (thông điệp tiếng Việt).

**`@af/auth`** (dùng chung mọi app ngôn ngữ):

| Export | Hành vi |
|---|---|
| `createIdentityClient(identityApi)` | `register`, `login`, `refresh`, `logout`, `getAccount`, `updateAccount`, `changePassword` |
| `AuthProvider({ identity, loadMe, children })` | `loadMe: () => Promise<{ permissions: string[]; ... }>` do **app** truyền (app tiếng Trung gọi `GET /chinese/api/me`). Khi mount: `refresh` (bọc `navigator.locks.request('af-auth-refresh', …)` nếu có) ⇒ `loadMe`. Token trong biến module; làm mới chủ động 60 giây trước hết hạn. `BroadcastChannel('af-auth')` đồng bộ đăng xuất giữa tab |
| `useAuth()` | `{ status: 'loading'|'authenticated'|'anonymous', account, me, permissions: Set<string>, hasPermission, login, register, logout, reloadMe }` |
| `RequireAuth` | `anonymous` ⇒ `/dang-nhap?returnTo=`; `authenticated` mà 0 quyền ⇒ `/403` |
| `RequirePermission({ permission })` | thiếu ⇒ `/403` |
| `LoginPage`, `RegisterPage` | Trang dùng chung (MUI + RHF + zod), nhận `brand` (tên app) và `afterLogin`. `RegisterPage` gửi `timeZone` từ `Intl`. Lỗi 401/423/403/409 hiện `Alert` tại chỗ |

Ở F2 (trước khi chinese-backend có `/api/me`), app truyền tạm `loadMe` = `{ permissions: ['study.use'] }` **chỉ trong nhánh F2**, và F3 thay bằng lời gọi thật — ghi TODO có mã feature, review F3 kiểm đã gỡ.

**Màn hình apps/chinese (F2):** `/dang-nhap`, `/dang-ky` (dùng trang của `@af/auth`), menu người dùng (tên hiển thị, Đăng xuất).

#### 5.3.2 F3 — phân quyền & trang 4xx

- `loadMe` gọi `GET /chinese/api/me`.
- Route `/401` (nút "Đăng nhập lại"), `/403` (nút "Đăng xuất" khi đang đăng nhập), `/404`, `*` → `/404`; tất cả có "Về trang chủ" (`ErrorPage` của `@af/ui`).
- Mọi route học bọc `RequireAuth`; mục menu có `requiredPermission` chỉ hiện khi có quyền.

#### 5.3.3 F4 trở đi — mức thiết kế (BA bổ sung khi tới lượt)

- **F4**: `/ho-so` (tên hiển thị, múi giờ bằng `AppAutocomplete` từ `Intl.supportedValuesOf('timeZone')` — **đúng quy tắc renderInput**; đổi mật khẩu) gọi identity; lưu xong ⇒ `refresh()` + `reloadMe()`. `/quan-tri/nguoi-dung` (`users.manage`): danh sách + `?q=` trên URL, `AppDialog` đổi vai trò. Thêm `AppDialog`, `AppDrawer`, `ConfirmProvider/useConfirm`, `ToastProvider` vào `@af/ui`.
- **F5 pinyin**: `@af/utils/pinyin` **không** đặt ở package chung (đặc thù tiếng Trung) ⇒ đặt `apps/chinese/src/lib/pinyin.ts` + `vitest` trong app (`yarn workspace @af/chinese test`, ≥ 30 ca: `lve4 → lüè`, `gui4 → guì`, `liu2 → liú`, `er2 → ér`, `r5 → r`; đặt dấu: có `a`/`e` ⇒ trên đó; `ou` ⇒ trên `o`; còn lại ⇒ nguyên âm cuối). Tiện ích TTS **dùng chung** `@af/ui/speech` (`listVoices(langPrefix)`, `speak(text, { lang, rate })`, hook `useSpeech(lang)` trả `unsupported` khi không có giọng) — ngôn ngữ khác dùng lại. Route `/pinyin?tab=bang|huong-dan|luyen` (`useTabParam` thêm vào `@af/ui`): bảng thanh mẫu × vận mẫu (cuộn ngang trong vùng riêng trên mobile, bấm ô ⇒ `AppDrawer` 4 thanh + chữ minh hoạ + nghe); hướng dẫn từ `guide.json`; luyện `listen_tone` / `tone_pair` 20 câu, trọng số theo `recommendedFocus`, phím `1–4` + `Space`. Không có giọng `zh*` ⇒ `Alert` hướng dẫn cài gói tiếng Trung trên Windows hoặc dùng Chrome/Edge.
- **F6 tra từ**: `/tu-dien?q=&hsk=1&page=` (debounce 300 ms, `useScrollRestore`), `/tu-dien/:id` (`useBackTo`), `/tu-dien/chu/:hanzi`.
- **F7 ôn tập**: `/on-tap`, `/on-tap/phien` (mặt trước chữ Hán + tự phát âm; `Space` hiện đáp án; 4 nút Quên/Khó/Được/Dễ + khoảng dự kiến; phím `1–4`; tải 20, còn 5 thì tải thêm; mất mạng giữ đánh giá và thử lại cùng `clientReviewId` sinh bằng `crypto.randomUUID()`), `/ho-so?tab=hoc-tap`, nút "Thêm vào ôn tập" ở tra từ.
- **F8 luyện viết**: `hanzi-writer` 3.x (ghi version thật); `/luyen-viet`, `/luyen-viet/:hanzi` 3 bước Xem/Tô theo/Tự viết, canvas `min(90vw, 360px)`, lưới 米字格, hiện Hán Việt + pinyin.
- **F9 bài học**: `/bai-hoc`, `/bai-hoc/:slug?tab=noi-dung|tu-vung|quiz`.
- **F10 quản trị**: `/quan-tri/bai-hoc`, `/quan-tri/bai-hoc/:id?tab=thong-tin|noi-dung|tu-vung|quiz` (thêm/xoá/lên/xuống khối, `AppAutocomplete` chọn từ, hỏi xác nhận khi rời trang chưa lưu), `/quan-tri/tu-vung?trang-thai=machine`.
- **F11 tổng quan**: `/` thay HomePage: streak, việc hôm nay, thẻ đến hạn, lịch 90 ngày tự vẽ bằng `Box`, độ chính xác thanh.
- **F13 Portal `antfarms.xyz` (feature sau MVP)**: `apps/portal` (`@af/portal`, cổng 3281): chọn ngôn ngữ đang học, hồ sơ tài khoản, quản trị tài khoản identity; dùng trang đăng nhập chung của `@af/auth` (cookie `Domain=.antfarms.xyz` nên đăng nhập một nơi dùng mọi subdomain). Chi tiết khi tới lượt.

### 5.4 Học liệu (tiếng Trung — `content/chinese/`)

#### 5.4.1 Cấu trúc

```
content/
  package.json                   # dự án yarn RIÊNG cho công cụ học liệu mọi ngôn ngữ (không thuộc workspace frontend)
                                 # devDeps: ajv ^8, ajv-formats ^3 · scripts: "validate:chinese": "node chinese/scripts/validate.mjs"
  yarn.lock
  README.md                      # quy ước chung học liệu đa ngôn ngữ (tạo ở F5)
  chinese/
    SOURCES.md                   # BẮT BUỘC
    LICENSES/                    # toàn văn giấy phép nguồn đã dùng
    schemas/  pinyin-initials|pinyin-finals|pinyin-syllables|pinyin-guide|hsk-words|characters|lesson .schema.json
    data/
      pinyin/initials.json  finals.json  syllables.json  guide.json       # F5
      vocabulary/hsk-words.json                                            # F6
      characters/characters.json                                           # F6
      lessons/<order>-<slug>.json                                          # F9
    scripts/validate.mjs  build-hsk.mjs
    .raw/                        # gitignore — dữ liệu nguồn thô
```

Frontend **không** import JSON trong `content/`; mọi thứ qua API, trừ dữ liệu nét hanzi-writer (D3).

#### 5.4.2 Định dạng & schema

**Pinyin số thanh** (R-C2): âm tiết khớp `^[a-zA-Z]+[1-5]$` sau khi đổi `ü|u:` → `v`; phần chữ (lower) phải thuộc `syllables.json`, trừ `r` (chỉ thanh 5). Nhiều âm tiết cách nhau đúng một dấu cách; không dấu câu trong trường `pinyin`.

`pinyin/initials.json`: `[{ "code": "zh", "group": "uon-luoi", "ipa": "ʈʂ", "noteVi": "Cong lưỡi; đừng đọc thành 'tr' tiếng Việt", "examples": ["zhong1"] }]`
`pinyin/finals.json`: `[{ "code": "ian", "group": "i", "display": "ian", "standaloneSpelling": "yan", "noteVi": "đọc gần 'iên'" }]`
`pinyin/syllables.json`:

```json
[
  { "syllable": "ma", "initial": "m", "final": "a",
    "tones": { "1": { "hanzi": "妈", "meaningVi": "mẹ" }, "2": { "hanzi": "麻", "meaningVi": "gai, tê" },
               "3": { "hanzi": "马", "meaningVi": "ngựa" }, "4": { "hanzi": "骂", "meaningVi": "mắng" } } },
  { "syllable": "nv", "initial": "n", "final": "v", "tones": { "3": { "hanzi": "女", "meaningVi": "nữ" } } }
]
```

`tones` chỉ chứa thanh có chữ minh hoạ **đơn âm** (R-L1); `validate.mjs` kiểm dựa trên bảng đọc CC-CEDICT trong `.raw/` (thiếu `.raw/` ⇒ WARN).

`pinyin/guide.json`: `[{ "id": "bon-thanh", "title": "Bốn thanh điệu", "blocks": [{ "type": "paragraph", "text": "..." }, { "type": "examples", "items": [{ "pinyin": "ma1", "hanzi": "妈", "meaningVi": "mẹ" }] }] }]`. Chủ đề bắt buộc: bốn thanh + thanh nhẹ (so sánh với thanh tiếng Việt, ghi rõ "gần đúng"), quy tắc đặt dấu, `ü` và j/q/x/y, cặp dễ nhầm với người Việt, biến điệu thanh 3 / 不 / 一. **Tự soạn.**

`vocabulary/hsk-words.json`:

```json
{
  "dataset": "hsk-words",
  "version": "2026-09-xx",
  "words": [
    { "simplified": "爱", "traditional": "愛", "pinyin": "ai4", "hsk3Level": 1, "hsk2Level": 1,
      "pathOrder": 12, "frequencyRank": 311, "pos": ["v"],
      "meaningsEn": ["to love", "to be fond of"], "meaningsVi": ["yêu", "thích"],
      "meaningViStatus": "machine", "meaningViSource": "machine",
      "hanViet": "ái", "hanVietStatus": "derived",
      "sources": ["complete-hsk-vocabulary", "cc-cedict", "unihan"] }
  ]
}
```

Luật: khoá `(simplified, pinyin)` duy nhất; số âm tiết = số chữ Hán (trừ `r5`); `hsk3Level ∈ 1..7`, `hsk2Level ∈ 1..6 | null`; `meaningsVi` không rỗng; `pathOrder` duy nhất; mọi `sources[]` có trong `SOURCES.md`; **số từ HSK 3.0 cấp 1 khớp nguồn** (con số kỳ vọng ghi trong script sau khi content-implement xác minh — theo hiểu biết là 500).

`characters/characters.json`: `[{ "hanzi": "爱", "traditional": "愛", "pinyinReadings": ["ai4"], "hanViet": ["ái"], "strokeCount": 10, "radical": "爫", "sources": ["unihan", "cc-cedict"] }]` — đủ mọi chữ xuất hiện trong từ vựng.

`lessons/*.json` (F9): `{ slug, title, topic, level: "hsk1", orderIndex, summary, words: [{ simplified, pinyin }], blocks: [{ type, payload }], quiz: [{ type, prompt, promptLang, audioText?, options: [{ id, text, lang }], correctOptionId, explanation }] }`. Payload: `text {paragraphs[]}` · `dialogue {lines[{speaker,hanzi,pinyin,vi}]}` · `grammar {title,explanation,examples[{hanzi,pinyin,vi,note?}]}` · `tip {text}`. Sư phạm: ≤ 10–15 từ mới/bài, chỉ dùng từ đã học + từ mới, 5–10 câu quiz, ≥ 30% `listen_choice`.

#### 5.4.3 Nguồn ứng viên (content-implement xác minh trước khi dùng)

| Khoá | Nguồn | Dùng cho | Giấy phép (theo hiểu biết — **phải xác minh**) | Nghĩa vụ | Feature |
|---|---|---|---|---|---|
| `pinyin-table` | Bảng âm tiết Hán ngữ Pinyin (dữ kiện), đối chiếu CC-CEDICT | `syllables.json` | Dữ kiện không bảo hộ | Ghi nguồn đối chiếu | F5 |
| `complete-hsk-vocabulary` | github.com/drkameleon/complete-hsk-vocabulary | Danh sách HSK 3.0 & 2.0, cấp, pinyin, từ loại, tần suất | MIT (kiểm LICENSE + commit). Nghĩa Anh có thể lấy từ CC-CEDICT ⇒ khi đó chịu CC BY-SA 4.0 | Ghi MIT; áp CC BY-SA cho **tệp dữ liệu** nếu có | F6 |
| `cc-cedict` | cc-cedict.org / MDBG | Nghĩa Anh, cách đọc, phồn thể | CC BY-SA 4.0 | Ghi công, ghi thay đổi; dữ liệu dẫn xuất (kể cả nghĩa Việt dịch từ nghĩa Anh) cùng giấy phép — chỉ áp cho `content/chinese/data/` | F5, F6 |
| `cvdict` | CVDICT (Trung–Việt dựng trên CC-CEDICT) | Nghĩa Việt | **Chưa rõ** — không rõ ⇒ không dùng | Nếu CC BY-SA: như trên | F6 (D5) |
| `unihan` | Unicode Unihan (`kVietnamese`, `kTotalStrokes`, `kRSUnicode`) | Hán Việt, số nét, bộ | Unicode License v3 | Kèm thông báo bản quyền | F6 |
| `hanzi-writer` | github.com/chanind/hanzi-writer | Thư viện vẽ/quiz nét | MIT | LICENSE trong bundle | F8 |
| `hanzi-writer-data` | github.com/chanind/hanzi-writer-data (từ Make Me a Hanzi) | Dữ liệu nét | Arphic Public License — xác minh | Nếu đóng gói: kèm toàn văn giấy phép | F8 (D3) |
| `tatoeba` | tatoeba.org (tuỳ chọn) | Câu mẫu | CC BY 2.0 FR | Ghi công từng câu | F9 |
| `machine` | Dịch máy do content-implement | Nghĩa Việt khi không có nguồn | Theo giấy phép nguồn Anh | Đánh dấu `machine` | F6 |
| `original` | Tự soạn | Hướng dẫn pinyin, bài học, quiz | Thuộc dự án | — | F5, F9 |

`SOURCES.md` mỗi dòng: khoá, tên, URL, giấy phép, ngày lấy, commit/phiên bản, phần đã dùng, nghĩa vụ, file bị ảnh hưởng. **Không dùng**: giáo trình có bản quyền, âm thanh không rõ giấy phép, dữ liệu cào từ từ điển thương mại (Hanzii, Mazii, Pleco...).

#### 5.4.4 Cách nạp

Pinyin: `PinyinCatalogLoader` vào bộ nhớ (F5) · Từ vựng + chữ: `ContentImporter` upsert `content.*` (F6) · Bài học: import khi `content.lessons` trống (F9).

### 5.5 Nền tảng đa ngôn ngữ — checklist "thêm một ngôn ngữ mới"

**Dùng chung, KHÔNG làm lại:** gateway (chỉ thêm route), identity-service (tài khoản dùng chung mọi ngôn ngữ; chỉ thêm audience), `backend/shared/AntFarm.*` (Core, Logging, Security, HealthChecks, Auth — gồm `RequirePermissionAttribute`, policy provider), `backend/shared/AntFarm.Testing`, `@af/tsconfig|ui|api|auth|utils` (layout, dialog, trang lỗi, trang đăng nhập/đăng ký, TTS `speech`), `scripts/check-ui-conventions.mjs`, `content/package.json` + ajv, quy ước streak/`study_events`/FSRS (chép pattern, tách thành thư viện chung khi có ngôn ngữ thứ hai dùng thật — không tách trước).

**Phải thêm (ví dụ ngôn ngữ `japanese`):**

1. [ ] Hợp đồng thực thi riêng `docs/agent-workflow/YYYY-MM-DD-<ngon-ngu>-...-hop-dong-thuc-thi.md` (nghiệp vụ sư phạm riêng: hệ chữ, chuẩn trình độ, quy ước phiên âm).
2. [ ] Service `backend/services/japanese-backend/` với `AntFarm.Japanese.{Domain,Application,Infrastructure,Api}` + `tests/AntFarm.Japanese.{UnitTests,ApiTests}`; thêm vào `backend.slnx`; cổng kế tiếp (§5.0.2).
3. [ ] Database `af_japanese` (+ `af_japanese_test`), schema `access`/`content`/`learning`; migration đầu `F<n>_Access` (bảng quyền giống §5.1.2).
4. [ ] Phân quyền cục bộ: seed vai trò/quyền, `JapaneseAdmin:BootstrapEmails`, `JapaneseAccess:DefaultRoles`, `IPermissionResolver` riêng, `/api/me`, endpoint canh gác `/api/admin/ping`.
5. [ ] identity-service: thêm `af-japanese` vào `Jwt:Audiences`; service mới cấu hình `Auth:Audience = af-japanese`, `Auth:JwksUrl`, `Auth:Issuer`.
6. [ ] Gateway: route `/japanese/{**catch-all}` + cluster.
7. [ ] Frontend `frontend/apps/japanese` (`@af/japanese`), cổng kế tiếp, Vite proxy `/identity` + `/japanese` → gateway, `AuthProvider` với `loadMe` gọi `/japanese/api/me`, font/`lang` riêng (`LangText lang="ja"`), màu nhấn riêng.
8. [ ] Học liệu `content/japanese/` (`SOURCES.md`, `LICENSES/`, `schemas/`, `data/`, `scripts/validate.mjs`) + script `validate:japanese`.
9. [ ] Portal (khi đã có F13): thêm ngôn ngữ vào danh mục.
10. [ ] Cập nhật `CLAUDE.md` (bảng service/app/cổng/database), README.
11. [ ] **Subdomain + chứng chỉ**: bản ghi DNS `japanese.antfarms.xyz` trên Cloudflare (lần đầu để DNS-only nếu HTTP-01 lỗi); chạy lại `./scripts/get-cert.sh <email> id.antfarms.xyz chinese.antfarms.xyz japanese.antfarms.xyz` với **ĐỦ danh sách tên miền cũ + mới** (thiếu tên cũ là đè mất chứng chỉ của chúng). Nếu đã chuyển sang wildcard (D20) thì bỏ bước chứng chỉ.
12. [ ] **nginx biên**: trong `deploy/conf/nginx.conf.example` (và bản thật `conf/nginx.conf` trên server) chép khối `chinese` thành khối `japanese` (đổi `server_name`, `location ^~ /japanese/`, upstream `japanese-frontend:80`); `nginx -t` + reload.
13. [ ] **identity CORS**: thêm `https://japanese.antfarms.xyz` vào `Auth:AllowedOrigins` (prod, compose) và `http://localhost:<cổng app>` (dev) — thiếu là trình duyệt chặn CORS khi gọi `id.antfarms.xyz` / 403 `ORIGIN_NOT_ALLOWED`. App mới build với `VITE_IDENTITY_API_URL=https://id.antfarms.xyz/api`.
14. [ ] **Docker**: `Dockerfile` cho `japanese-backend` (mẫu §5.6.3) và `japanese-frontend` (mẫu §5.6.4); thêm 2 service vào `deploy/docker-compose.yml` (không `ports:`), biến cluster gateway, chuỗi kết nối DB.
15. [ ] **Database**: thêm `CREATE DATABASE af_japanese` + role vào `deploy/postgres/init/01-create-databases.sql` (chỉ chạy khi volume trống ⇒ trên server đã chạy phải tạo tay — ghi vào `VERIFY-DOCKER.md`).
16. [ ] Cập nhật bảng domain R-N7 trong hợp đồng mới và `CLAUDE.md`.

### 5.6 Triển khai — nginx biên + YARP + Docker

#### 5.6.1 Topology

```
Production (một server, Docker, mạng nội bộ af-net)

Internet ─(Cloudflare DNS, tuỳ chọn proxy)─443/80──► nginx (container DUY NHẤT publish cổng; certbot gia hạn bên cạnh)
                        │  TLS Let's Encrypt (1 chứng chỉ SAN), HTTP→HTTPS 301, phân theo Host, ghi đè X-Forwarded-For/Proto
                        ├── chinese.antfarms.xyz  /            ──► chinese-frontend:80 (nginx tĩnh)
                        │                          /chinese/    ──► gateway:8080 ──► chinese-backend:8080
                        ├── id.antfarms.xyz       /*           ──► gateway:8080 (/identity/*) ──► identity-service:8080
                        │     ▲ trình duyệt ở chinese.antfarms.xyz gọi thẳng (CORS có credentials, cookie Domain=.antfarms.xyz)
                        └── antfarms.xyz          (feature sau — F13 portal; chưa có server block)
                                                                         │
                                                  chinese-backend ──JWKS nội bộ──► identity-service:8080
                                                  identity-service, chinese-backend ──► postgres:5432 (af_identity, af_chinese)

Dev local (không Docker)

Trình duyệt ──► Vite apps/chinese :3280 ──proxy /identity, /chinese──► gateway :5280 ──► identity :5281 / chinese :5282 ──► PostgreSQL :5432
```

**Vì sao cả nginx lẫn YARP** (thay vì chỉ một): nginx lo việc biên mà nó làm tốt và phổ biến, đúng khuôn đã chạy thật ở MedDental (TLS + Let's Encrypt, phân theo host, phục vụ/nén file tĩnh, giới hạn kích thước request); YARP giữ **bảng định tuyến API bằng cấu hình .NET dùng y hệt ở dev và production** — dev không có nginx mà vẫn "đi qua proxy" đúng đường đi thật. Phương án gọn hơn (chỉ YARP + Kestrel tự lo TLS) bị loại vì tự quản chứng chỉ trong .NET kém phổ biến và khó vận hành hơn; phương án chỉ nginx bị loại vì mất đường đi thống nhất ở dev.

#### 5.6.2 Bảng biến môi trường và cấu hình theo môi trường

| Khoá | Dev local | Production (Docker) |
|---|---|---|
| Cổng service | 5280/5281/5282 (launchSettings) | tất cả `8080` trong container (`ASPNETCORE_URLS=http://+:8080`), không publish |
| Gateway cluster | `http://localhost:5281`, `:5282` | `http://identity-service:8080`, `http://chinese-backend:8080` |
| `Jwt:Issuer` (identity) / `Auth:Issuer` (service) | `http://localhost:5280/identity` | `https://id.antfarms.xyz` |
| `Auth:JwksUrl` (service) | `http://localhost:5281/.well-known/jwks.json` | `http://identity-service:8080/.well-known/jwks.json` |
| `Auth:Audience` (service) / `Jwt:Audiences` (identity) | `af-chinese` / `["af-identity","af-chinese"]` | như dev |
| `Auth:RefreshCookieDomain` | *(rỗng)* | `.antfarms.xyz` |
| `Auth:RefreshCookiePath` | `/identity/api/auth` | `/api/auth` |
| `Auth:RefreshCookieSecure` | `false` | `true` |
| `Auth:AllowedOrigins` (CORS có credentials + kiểm `Origin`) | `http://localhost:3280`, `http://localhost:3281` | `https://chinese.antfarms.xyz` (+ `https://antfarms.xyz` khi có portal F13) |
| `Jwt:KeysPath` | `.secrets/identity/keys` (tự sinh) | `/keys` (volume `identity-keys`, **không** tự sinh) |
| `ForwardedHeaders:KnownNetworks` | loopback | loopback + dải `af-net` (vd `172.28.0.0/16`, cố định trong compose) |
| `ConnectionStrings:Default` | `appsettings.Development.json` | biến `ConnectionStrings__Default` từ `deploy/.env` |
| Frontend baseURL ngôn ngữ | `/chinese/api` (Vite proxy) | `/chinese/api` (nginx biên) — giống hệt |
| Frontend baseURL identity (`VITE_IDENTITY_API_URL`, nướng lúc build) | `/identity/api` (Vite proxy) | `https://id.antfarms.xyz/api` (build arg) |

Không cần `*.localhost` ở dev: cookie host-only `localhost` dùng chung mọi cổng, app gọi identity cùng origin qua Vite. Hệ quả: **luồng CORS có credentials chỉ kiểm được thật ở môi trường có subdomain** (checklist §5.6.6); dev chỉ kiểm được bằng ApiTests F2 (preflight + header). Không đặt `Domain=.localhost` (trình duyệt từ chối).

#### 5.6.3 Dockerfile .NET (mẫu cho gateway, identity-service, chinese-backend)

Theo mẫu MedDental `backend/services/lms-backend/src/MedDental.Lms.Api/Dockerfile`. Build context `./backend`.

```dockerfile
# Build context: ./backend
# KHÔNG khai "# syntax=docker/dockerfile:1": directive đó bắt BuildKit tải frontend từ Docker Hub trước MỖI lần build
# (tag di động) — Docker Hub tắc là build chết ngay bước resolve. Docker Engine 23+ đã hỗ trợ sẵn --mount=type=cache.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# --- Layer restore: chỉ copy thứ restore cần ---
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY shared/ shared/
COPY services/chinese-backend/src/AntFarm.Chinese.Domain/AntFarm.Chinese.Domain.csproj                 services/chinese-backend/src/AntFarm.Chinese.Domain/
COPY services/chinese-backend/src/AntFarm.Chinese.Application/AntFarm.Chinese.Application.csproj       services/chinese-backend/src/AntFarm.Chinese.Application/
COPY services/chinese-backend/src/AntFarm.Chinese.Infrastructure/AntFarm.Chinese.Infrastructure.csproj services/chinese-backend/src/AntFarm.Chinese.Infrastructure/
COPY services/chinese-backend/src/AntFarm.Chinese.Api/AntFarm.Chinese.Api.csproj                       services/chinese-backend/src/AntFarm.Chinese.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore services/chinese-backend/src/AntFarm.Chinese.Api/AntFarm.Chinese.Api.csproj

# --- Layer publish ---
COPY services/chinese-backend/src/ services/chinese-backend/src/
# /m:2: không để MSBuild chiếm hết core khi build trên máy đang chạy production
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish services/chinese-backend/src/AntFarm.Chinese.Api/AntFarm.Chinese.Api.csproj \
    -c Release -o /app/publish --no-restore /m:2

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# aspnet image không có wget/curl — cài wget cho HEALTHCHECK. Không cài curl.
RUN apt-get update -qq \
 && apt-get install -y --no-install-recommends wget \
 && rm -rf /var/lib/apt/lists/* \
 && groupadd --gid 1001 app && useradd --uid 1001 --gid app --shell /bin/false --no-create-home app
COPY --from=build --chown=app:app /app/publish .
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "AntFarm.Chinese.Api.dll"]
```

- identity-service: đổi đường dẫn/tên; thêm `RUN mkdir -p /keys && chown app:app /keys`.
- gateway: chỉ một csproj `services/gateway/AntFarm.Gateway.csproj`; `COPY services/gateway/ services/gateway/`; entrypoint `AntFarm.Gateway.dll`.
- ⚠️ `.dockerignore` ở `backend/` phải loại `**/bin`, `**/obj`, `**/appsettings.Development.json` — thiếu là ảnh chứa mật khẩu dev.
- ⚠️ Từ F5: chinese-backend cần học liệu `content/chinese/data` ⇒ build context phải đổi thành **gốc repo** (hoặc copy vào ảnh qua `additional_contexts` của compose). Ghi chú này để F5 xử lý; F0 giữ context `./backend`.

#### 5.6.4 Dockerfile frontend (F1)

```dockerfile
# Build context: ./frontend
FROM node:22-alpine AS builder
WORKDIR /app
COPY package.json yarn.lock turbo.json ./
COPY packages/tsconfig/package.json ./packages/tsconfig/
COPY packages/ui/package.json       ./packages/ui/
COPY packages/api/package.json      ./packages/api/
# F2 thêm: packages/auth, packages/utils
COPY apps/chinese/package.json      ./apps/chinese/
RUN yarn install --frozen-lockfile --network-timeout 300000
COPY packages ./packages
COPY apps/chinese ./apps/chinese
# Vite nướng biến vào bundle LÚC BUILD — sai giá trị thì ảnh vẫn build/chạy, chỉ hỏng khi bấm đăng nhập
ARG VITE_IDENTITY_API_URL=https://id.antfarms.xyz/api
ENV VITE_IDENTITY_API_URL=$VITE_IDENTITY_API_URL
RUN yarn workspace @af/chinese build

FROM nginx:1.27-alpine AS runtime
COPY --from=builder /app/apps/chinese/dist /usr/share/nginx/html
COPY apps/chinese/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s CMD wget -qO- http://127.0.0.1/ || exit 1
```

⚠️ Dockerfile chỉ copy `package.json` của đúng app + các package dùng chung ⇒ peer dependency thiếu ở app sẽ **chỉ lộ ở đây** (quy tắc CLAUDE.md).

`apps/chinese/nginx.conf`:

```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;
    absolute_redirect off;

    gzip on;
    gzip_types text/plain text/css application/json application/javascript image/svg+xml;

    # mime.types của nginx KHÔNG khai .mjs ⇒ octet-stream ⇒ trình duyệt từ chối module ("Failed to fetch dynamically imported module").
    # Khối riêng — KHÔNG dùng `types {}` ở server level (ghi đè toàn bộ bảng MIME).
    location ~* \.mjs$ {
        default_type application/javascript;
        expires 1y;
        add_header Cache-Control "public, immutable";
        try_files $uri =404;
    }

    location /assets/ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        try_files $uri =404;
    }

    location = /index.html {
        add_header Cache-Control "no-cache";
    }

    # SPA fallback. KHÔNG proxy API ở đây — nginx biên đã tách /chinese/ trước khi tới container này; identity ở id.antfarms.xyz.
    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

#### 5.6.5 `deploy/docker-compose.yml`, `.env.example`, nginx biên

`deploy/docker-compose.yml` (F0 có postgres, identity-service, chinese-backend, gateway, nginx, certbot; F1 thêm chinese-frontend):

```yaml
name: antfarm

networks:
  af-net:
    ipam:
      config: [{ subnet: 172.28.0.0/16 }]

volumes:
  pg-data:
  identity-keys:

x-dotnet-env: &dotnet-env
  ASPNETCORE_ENVIRONMENT: Production
  ForwardedHeaders__KnownNetworks__0: 172.28.0.0/16

services:
  postgres:
    image: postgres:18
    restart: unless-stopped
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - pg-data:/var/lib/postgresql
      - ./postgres/init:/docker-entrypoint-initdb.d:ro
    networks: [af-net]
    # KHÔNG ports: — muốn quản trị thì tạm thêm "127.0.0.1:5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 10s
      retries: 10

  identity-service:
    image: ${REGISTRY:-antfarm}/identity-service:${IDENTITY_SERVICE_TAG:-latest}
    build: { context: ../backend, dockerfile: services/identity-service/src/AntFarm.Identity.Api/Dockerfile }
    restart: unless-stopped
    environment:
      <<: *dotnet-env
      ConnectionStrings__Default: Host=postgres;Port=5432;Database=af_identity;Username=af_identity;Password=${AF_IDENTITY_DB_PASSWORD}
      Jwt__Issuer: https://id.${APP_DOMAIN}
      Jwt__KeysPath: /keys
      Auth__RefreshCookieDomain: .${APP_DOMAIN}
      Auth__RefreshCookiePath: /api/auth
      Auth__RefreshCookieSecure: "true"
      Auth__AllowRegistration: ${AUTH_ALLOW_REGISTRATION:-false}
      Auth__AllowedOrigins__0: https://chinese.${APP_DOMAIN}
      # Auth__AllowedOrigins__1: https://${APP_DOMAIN}   # bật khi có portal (F13)
    volumes: [identity-keys:/keys]
    depends_on: { postgres: { condition: service_healthy } }
    networks: [af-net]

  chinese-backend:
    image: ${REGISTRY:-antfarm}/chinese-backend:${CHINESE_BACKEND_TAG:-latest}
    build: { context: ../backend, dockerfile: services/chinese-backend/src/AntFarm.Chinese.Api/Dockerfile }
    restart: unless-stopped
    environment:
      <<: *dotnet-env
      ConnectionStrings__Default: Host=postgres;Port=5432;Database=af_chinese;Username=af_chinese;Password=${AF_CHINESE_DB_PASSWORD}
      Auth__Issuer: https://id.${APP_DOMAIN}
      Auth__Audience: af-chinese
      Auth__JwksUrl: http://identity-service:8080/.well-known/jwks.json
      ChineseAdmin__BootstrapEmails__0: ${CHINESE_ADMIN_EMAIL}
    depends_on: { postgres: { condition: service_healthy } }
    networks: [af-net]

  gateway:
    image: ${REGISTRY:-antfarm}/gateway:${GATEWAY_TAG:-latest}
    build: { context: ../backend, dockerfile: services/gateway/Dockerfile }
    restart: unless-stopped
    environment:
      <<: *dotnet-env
      ReverseProxy__Clusters__identity__Destinations__primary__Address: http://identity-service:8080
      ReverseProxy__Clusters__chinese__Destinations__primary__Address: http://chinese-backend:8080
    networks: [af-net]

  # F1 thêm:
  # chinese-frontend:
  #   image: ${REGISTRY:-antfarm}/chinese-frontend:${CHINESE_FRONTEND_TAG:-latest}
  #   build:
  #     context: ../frontend
  #     dockerfile: apps/chinese/Dockerfile
  #     args: { VITE_IDENTITY_API_URL: "https://id.${APP_DOMAIN}/api" }
  #   restart: unless-stopped
  #   networks: [af-net]

  nginx:                                  # nginx biên — container DUY NHẤT publish cổng
    image: nginx:1.27-alpine
    restart: unless-stopped
    ports:
      # Mặc định bind 127.0.0.1 để máy chưa cấu hình tường lửa không lộ ra ngoài; server thật đặt PUBLIC_BIND=0.0.0.0
      - "${PUBLIC_BIND:-127.0.0.1}:80:80"
      - "${PUBLIC_BIND:-127.0.0.1}:443:443"
    volumes:
      - ./conf/nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - ./certs:/etc/nginx/certs:ro           # nginx đọc CỐ ĐỊNH certs/live/fullchain.pem + privkey.pem
      - ./certs/acme:/var/www/certbot          # webroot HTTP-01
    depends_on: [gateway]
    networks: [af-net]

  certbot:                                 # chỉ GIA HẠN; cấp lần đầu bằng scripts/get-cert.sh
    image: certbot/certbot:latest
    restart: unless-stopped
    volumes:
      - ./certs/letsencrypt:/etc/letsencrypt
      - ./certs/acme:/var/www/certbot
    entrypoint: /bin/sh -c 'trap exit TERM; while :; do certbot renew --webroot -w /var/www/certbot --quiet; sleep 12h & wait $${!}; done'
    networks: [af-net]
```

Ghi chú bắt buộc trong compose (comment): `image:` đặt trên `build:` và giữ cả hai (registry hỏng vẫn tự build được); triển khai luôn `docker compose pull <svc> && docker compose up -d <svc>` (`up -d` không tự kéo ảnh mới cùng tag); trên server chỉ build **từng service một**.

`deploy/.env.example`: `APP_DOMAIN=antfarms.xyz`, `PUBLIC_BIND=127.0.0.1` (server thật: `0.0.0.0`), `LETSENCRYPT_EMAIL=`, `REGISTRY=`, `POSTGRES_USER=postgres`, `POSTGRES_PASSWORD=`, `AF_IDENTITY_DB_PASSWORD=`, `AF_CHINESE_DB_PASSWORD=`, `CHINESE_ADMIN_EMAIL=`, `AUTH_ALLOW_REGISTRATION=false`, các `*_TAG=` để trống (mặc định `latest`).

`deploy/postgres/init/01-create-databases.sql`: tạo role `af_identity`, `af_chinese` (mật khẩu đặt tay sau hoặc qua script `.sh` đọc biến môi trường — **agent chọn `.sh` nếu cần đọc biến**, vì `.sql` không đọc được env), `CREATE DATABASE af_identity OWNER af_identity`, tương tự `af_chinese`. Chỉ chạy khi volume trống.

##### nginx biên + HTTPS Let's Encrypt — **bám đúng khuôn MedDental `deploy/app-core`** (người dùng chốt 16/09/2026; DNS ở Cloudflare)

Tham chiếu phải đọc khi viết: `H:\Work\Meddental\mdt-re-construct\deploy\app-core\{scripts/get-cert.sh, scripts/renew-cert.sh, scripts/self-signed.sh, conf/nginx.conf.example, docker-compose.yml (service nginx + certbot), README.md}`. Chép cấu trúc + lời giải thích, chỉ đổi tên miền/service.

**Nguyên tắc cốt lõi (không được đổi):**

1. **Một máy = MỘT chứng chỉ SAN** chứa đủ mọi tên miền (`id.antfarms.xyz`, `chinese.antfarms.xyz`, sau này thêm `english.`/`japanese.`/`vietnamese.`, `antfarms.xyz` khi có portal). nginx **mọi khối server** đọc cùng đường dẫn cố định `/etc/nginx/certs/live/fullchain.pem` + `privkey.pem`. `get-cert.sh <email> <domain...>` phải truyền **ĐỦ tên miền cũ + mới** — chỉ truyền tên mới là **đè mất** chứng chỉ của tên cũ, trình duyệt báo lỗi ngay. Tên miền đầu tiên là `--cert-name` (đề xuất `id.antfarms.xyz`, giữ cố định mãi).
2. **HTTP-01 webroot**: khối `listen 80` **chỉ** phục vụ `/.well-known/acme-challenge/` (root `/var/www/certbot`) và `return 301 https://$host$request_uri`. Điều kiện: DNS mọi tên đã trỏ về máy + cổng 80 mở; Let's Encrypt giới hạn 5 lần hỏng/giờ mỗi tên — đừng thử liên tục.
3. **Vòng luẩn quẩn lần đầu** (nginx không lên nếu thiếu file chứng chỉ, mà cấp chứng chỉ cần nginx đang chạy): `scripts/self-signed.sh id.antfarms.xyz` tạo chứng chỉ tự ký 90 ngày vào `certs/live/` ⇒ `docker compose up -d nginx` ⇒ `get-cert.sh` ghi đè.
4. **Ép `--key-type rsa`** — chép nguyên khối giải thích của MedDental: certbot ≥ 2.0 mặc định cấp ECDSA (chuỗi ISRG Root X2); tường lửa doanh nghiệp bật SSL deep inspection (vd FortiGate) dùng kho CA cũ không có gốc ECDSA đời mới ⇒ coi chứng chỉ không hợp lệ, chặn toàn bộ website. RSA đi qua ISRG Root X1 có trong gần như mọi kho tin cậy. Cờ chỉ áp cho lần cấp mới; đổi loại khoá của chứng chỉ đã có phải `FORCE=1` (`--force-renewal`, cẩn thận giới hạn 5 chứng chỉ trùng bộ tên/tuần).
5. **Gia hạn hai tầng**: container `certbot` chạy `certbot renew` mỗi 12 giờ vào `certs/letsencrypt/`; **cron** trên host chạy `renew-cert.sh <id.antfarms.xyz>` mỗi đêm (vd `17 3 * * *`) để chép sang `certs/live/` (chỉ khi nội dung khác, `cmp -s`) + `nginx -t` + `nginx -s reload`. **Thiếu cron là hỏng im lặng**: certbot gia hạn xong nhưng nginx vẫn phục vụ bản cũ tới lúc hết hạn.
6. **Phân giải upstream lúc chạy**: `resolver 127.0.0.11 valid=10s ipv6=off;` + `set $upstream <service>:<port>;` + `proxy_pass http://$upstream$request_uri;` — nginx không chết khi backend chưa lên (vẫn phục vụ ACME), và `$request_uri` bắt buộc để không mất query string.
7. `conf/nginx.conf.example` được **commit**; `conf/nginx.conf` (bản thật) **gitignore**. Cổng publish qua `${PUBLIC_BIND:-127.0.0.1}`.

`deploy/conf/nginx.conf.example` (khung — agent viết đủ comment như bản MedDental):

```nginx
# Chép thành conf/nginx.conf rồi thay <...>. Bản đã sửa KHÔNG được git theo dõi.
# CỔNG DUY NHẤT vào máy. Tường lửa chỉ mở 80/443.
resolver 127.0.0.11 valid=10s ipv6=off;

# Khi bản ghi Cloudflare bật proxy (đám mây cam): bỏ comment dòng dưới, nếu không IP thật = IP Cloudflare
# include /etc/nginx/cloudflare-realip.conf;   # (mount thêm ./conf/cloudflare-realip.conf:/etc/nginx/cloudflare-realip.conf:ro — KHÔNG mount vào conf.d) set_real_ip_from <dải CF> ...; real_ip_header CF-Connecting-IP;

server {
    listen 80;
    server_name _;
    location /.well-known/acme-challenge/ { root /var/www/certbot; }   # ACME — ./certs/acme
    location / { return 301 https://$host$request_uri; }
}

# ── Identity — id.antfarms.xyz (người dùng chốt subdomain riêng) ─────────────
server {
    listen 443 ssl;
    http2 on;
    server_name <ID_DOMAIN>;                 # id.antfarms.xyz

    ssl_certificate     /etc/nginx/certs/live/fullchain.pem;
    ssl_certificate_key /etc/nginx/certs/live/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_session_cache   shared:SSL:10m;
    # HSTS thận trọng: KHÔNG includeSubDomains/preload cho tới khi MỌI subdomain đã có HTTPS (lý do §10 RK27)
    add_header Strict-Transport-Security "max-age=31536000" always;
    client_max_body_size 1m;

    # Mọi đường dẫn → gateway với tiền tố /identity (gateway bỏ tiền tố). CORS do identity-service trả —
    # nginx KHÔNG thêm Access-Control-* (trùng header ⇒ trình duyệt từ chối).
    location / {
        set $upstream gateway:8080;
        proxy_pass        http://$upstream/identity$request_uri;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $remote_addr;      # GHI ĐÈ (không $proxy_add_x_forwarded_for) — chống giả IP; §5.2.0.7
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host  $host;
    }
}

# ── Tiếng Trung — chinese.antfarms.xyz ────────────────────────────────────────
server {
    listen 443 ssl;
    http2 on;
    server_name <CHINESE_DOMAIN>;            # chinese.antfarms.xyz
    ssl_certificate     /etc/nginx/certs/live/fullchain.pem;
    ssl_certificate_key /etc/nginx/certs/live/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_session_cache   shared:SSL:10m;
    add_header Strict-Transport-Security "max-age=31536000" always;
    client_max_body_size 5m;

    # ^~ BẮT BUỘC: không để location regex (nếu thêm sau) cướp request API
    location ^~ /chinese/ {
        set $upstream gateway:8080;
        proxy_pass        http://$upstream$request_uri;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host  $host;
    }
    location / {
        set $upstream chinese-frontend:80;
        proxy_pass        http://$upstream$request_uri;
        proxy_set_header Host              $host;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

# ── MẪU ngôn ngữ mới (english./japanese./vietnamese.) — chép khối chinese, đổi <LANG> ──
# server { listen 443 ssl; http2 on; server_name <LANG>.antfarms.xyz; ... location ^~ /<LANG>/ {...} location / { set $upstream <LANG>-frontend:80; ... } }

# ── MẪU portal antfarms.xyz — FEATURE SAU (F13), để comment ──
# server { listen 443 ssl; http2 on; server_name antfarms.xyz; ... location / { set $upstream portal-frontend:80; ... } }
```

`scripts/get-cert.sh`, `renew-cert.sh`, `self-signed.sh`: chép logic MedDental, đổi đường dẫn log cron thành `/var/log/af-renew-cert.log`. Lệnh cấp lần đầu (ghi README):

```bash
cd deploy
./scripts/self-signed.sh id.antfarms.xyz
docker compose up -d nginx
./scripts/get-cert.sh "$LETSENCRYPT_EMAIL" id.antfarms.xyz chinese.antfarms.xyz
(crontab -l 2>/dev/null; echo "17 3 * * * cd $PWD && ./scripts/renew-cert.sh id.antfarms.xyz >> /var/log/af-renew-cert.log 2>&1") | crontab -
```

**Cloudflare** (DNS của `antfarms.xyz` do Cloudflare quản lý) — ghi vào `deploy/README` phần triển khai:

- Bật proxy (đám mây cam) thì SSL/TLS mode **bắt buộc Full (strict)**. **Flexible** ⇒ Cloudflare gọi origin bằng HTTP ⇒ nginx `return 301 https` ⇒ **vòng lặp chuyển hướng vô hạn**. **Full** (không strict) chạy được nhưng không kiểm chứng chỉ origin — không dùng.
- HTTP-01 sau proxy Cloudflare thường vẫn qua; nếu `get-cert.sh` báo lỗi xác minh (vd do rule "Always Use HTTPS"/WAF), chuyển bản ghi sang **DNS-only (đám mây xám)** trong lúc cấp lần đầu rồi bật lại.
- Proxied ⇒ `$remote_addr` là IP Cloudflare: bật `conf/cloudflare-realip.conf` (`set_real_ip_from` toàn bộ dải IPv4/IPv6 công bố tại cloudflare.com/ips + `real_ip_header CF-Connecting-IP;`), nếu không rate limit/khoá đăng nhập/log theo IP **gom mọi người thành vài IP Cloudflare**. Danh sách dải IP phải cập nhật định kỳ.
- Chỉ tin `CF-Connecting-IP` khi request thật sự đến từ dải Cloudflare (đó là việc của `set_real_ip_from`); không đọc header này ở tầng .NET.

**ForwardedHeaders ở tầng .NET** (scheme/host/IP thật sau proxy): gateway **không** gọi `UseForwardedHeaders` (chỉ chuyển tiếp, `X-Forwarded: Append`); identity-service và chinese-backend gọi `UseForwardedHeaders` với `ForwardedHeaders = XForwardedFor | XForwardedProto | XForwardedHost`, `ForwardLimit = 2` (nginx + gateway), **xoá mặc định rồi khai** `KnownNetworks` = loopback + dải `af-net` (`172.28.0.0/16`) từ cấu hình `ForwardedHeaders:KnownNetworks`. Issuer/JWKS/cookie **không** suy từ scheme request mà lấy từ cấu hình (`https://id.antfarms.xyz`, `Secure=true`) — ForwardedHeaders chỉ phục vụ IP thật, log và `Request.IsHttps`.

**Dev local**: HTTP `localhost`, không chứng chỉ, không nginx, không certbot. Khác production ở: `Jwt:Issuer`, `Auth:RefreshCookieDomain/Path/Secure`, `Auth:AllowedOrigins`, `VITE_IDENTITY_API_URL`, `ForwardedHeaders:KnownNetworks` (bảng §5.6.2).

#### 5.6.6 Verify Docker — CHƯA làm được trên máy dev (không Docker)

F0/F1 chỉ **viết đúng** các file trên và kiểm bằng mắt khi review. `deploy/VERIFY-DOCKER.md` ghi checklist chạy trên máy có Docker Engine ≥ 23 (Compose v2):

1. [ ] `docker compose -f deploy/docker-compose.yml config` không lỗi.
2. [ ] `docker compose build identity-service` / `chinese-backend` / `gateway` / `chinese-frontend` **từng cái một**; build lần 2 không đổi `.csproj` ⇒ bước restore dùng cache.
3. [ ] `docker run --rm --entrypoint sh <ảnh> -c 'ls /app | grep -i appsettings.Development'` rỗng.
4. [ ] `docker compose up -d` ⇒ mọi container `healthy` (`docker compose ps`).
5. [ ] `docker compose ps` chỉ `nginx` có cột PORTS ra host.
6. [ ] Trên máy verify, thêm vào file hosts `127.0.0.1 chinese.antfarms.xyz id.antfarms.xyz`, dùng chứng chỉ tự ký (`deploy/certs/`, gitignore) ⇒ `https://chinese.antfarms.xyz/chinese/api/system/info` = 200; `https://id.antfarms.xyz/.well-known/jwks.json` = 200 (từ F2); `https://chinese.antfarms.xyz/` trả trang app (từ F1); một file `.mjs` trả `Content-Type: application/javascript`.
7. [ ] `curl -H "X-Forwarded-For: 1.2.3.4" ...` ⇒ log identity ghi IP thật của máy gọi, không phải `1.2.3.4` (từ F2).
8. [ ] Đăng nhập ở `chinese.antfarms.xyz` (bundle build với `VITE_IDENTITY_API_URL=https://id.antfarms.xyz/api`) ⇒ request tới `id.antfarms.xyz` qua CORS thành công; cookie `af_rt` có `Domain=.antfarms.xyz; Secure; HttpOnly; SameSite=Strict; Path=/api/auth`; F5 vẫn đăng nhập (từ F2).
8b. [ ] `curl -i -X OPTIONS https://id.antfarms.xyz/api/auth/refresh -H 'Origin: https://chinese.antfarms.xyz' -H 'Access-Control-Request-Method: POST'` ⇒ `Access-Control-Allow-Origin: https://chinese.antfarms.xyz` (không `*`), `Access-Control-Allow-Credentials: true`; đổi `Origin` thành `https://evil.example` ⇒ không có header CORS, POST trả 403 (từ F2).
8c. [ ] `grep -r "id.antfarms.xyz" ` trong `/usr/share/nginx/html` của ảnh `chinese-frontend` có kết quả (biến build đã nướng đúng) (từ F2).
9. [ ] Khởi động lại `identity-service` ⇒ phiên cũ vẫn làm mới được (khoá ký trên volume) (từ F2).
10. [ ] `docker compose down && docker compose up -d` ⇒ dữ liệu còn (volume `pg-data`).

**HTTPS — cần server có DNS `id.`/`chinese.antfarms.xyz` trỏ về + Docker + cổng 80/443 mở** (chưa verify được ở F0):

11. [ ] `./scripts/self-signed.sh id.antfarms.xyz` ⇒ `docker compose up -d nginx` lên được.
12. [ ] `./scripts/get-cert.sh <email> id.antfarms.xyz chinese.antfarms.xyz` thành công; `openssl x509 -in certs/live/fullchain.pem -noout -text | grep -A1 "Subject Alternative Name"` có đủ 2 tên; `Public Key Algorithm: rsaEncryption`.
13. [ ] `curl -I http://chinese.antfarms.xyz/abc` ⇒ `301` sang `https://chinese.antfarms.xyz/abc`; `curl -I http://id.antfarms.xyz/.well-known/acme-challenge/x` ⇒ 404 (không bị chuyển hướng).
14. [ ] `curl -I https://id.antfarms.xyz/.well-known/jwks.json` ⇒ 200 + `Strict-Transport-Security: max-age=31536000` (không `includeSubDomains`).
15. [ ] SSL Labs (ssllabs.com/ssltest) cho `id.` và `chinese.` ⇒ hạng A trở lên, chỉ TLS 1.2/1.3.
16. [ ] `docker compose exec certbot certbot renew --dry-run` thành công; cron `renew-cert.sh` đã cài (`crontab -l`); chạy tay `./scripts/renew-cert.sh id.antfarms.xyz` ⇒ "không đổi".
17. [ ] Nếu bật proxy Cloudflare: SSL/TLS mode = Full (strict); truy cập không lặp chuyển hướng; log identity ghi IP thật của máy gọi (không phải IP Cloudflare).

Mỗi feature sau có đụng Docker thì bổ sung dòng vào checklist này.

---

## 6. Hợp đồng API

Bảng dưới ghi **đường dẫn service**. Trình duyệt gọi: ngôn ngữ `/chinese/api/...` (cùng origin, mọi môi trường); identity dev `/identity/api/...` (qua Vite), production `https://id.antfarms.xyz/api/...` (CORS có credentials).

### 6.0 Quy ước lỗi

| HTTP | `code` | Khi nào |
|---|---|---|
| 400 | `VALIDATION` | validator/model binding; `details` = `{ "email": ["Email không hợp lệ"] }` |
| 401 | `UNAUTHENTICATED` · `INVALID_CREDENTIALS` · `REFRESH_INVALID` | thiếu/hỏng token · sai đăng nhập · refresh hỏng |
| 403 | `FORBIDDEN` · `ACCOUNT_DISABLED` · `REGISTRATION_CLOSED` | |
| 404 | `NOT_FOUND` | |
| 409 | `EMAIL_TAKEN` · `CONCURRENCY_CONFLICT` | |
| 422 | `LAST_ADMIN` · `INVALID_TIME_ZONE` · `WRONG_PASSWORD` · `UNKNOWN_ROLE` · `LESSON_NOT_PUBLISHABLE` · ... | quy tắc nghiệp vụ |
| 423 | `ACCOUNT_LOCKED` | `details.lockedUntil` |
| 429 | `RATE_LIMITED` | |
| 503 | `CONTENT_UNAVAILABLE` | học liệu nạp lỗi |

401/403 do middleware sinh cũng phải có body JSON (`JwtBearerEvents.OnChallenge/OnForbidden` + `IAuthorizationMiddlewareResultHandler`).

### 6.1 F0 — mọi service

`GET /health/live` · `GET /health/ready` · `GET /api/system/info` → `{ "service", "version", "environment", "serverTimeUtc" }`.

### 6.2 F2 — identity-service

`POST /api/auth/register` (ẩn danh, rate limit)

```json
// request
{ "email": "ban@vidu.com", "password": "matkhau-dai-it-nhat-8", "displayName": "Quân", "timeZone": "Asia/Ho_Chi_Minh" }
// 201 + Set-Cookie (prod): af_rt=<token>; Domain=.antfarms.xyz; Path=/api/auth; Secure; HttpOnly; SameSite=Strict; Max-Age=2592000
//       (dev): af_rt=<token>; Path=/identity/api/auth; HttpOnly; SameSite=Strict; Max-Age=2592000
{ "accessToken": "eyJ...", "accessTokenExpiresAt": "2026-09-16T08:15:00Z",
  "account": { "id": "0192...", "email": "ban@vidu.com", "displayName": "Quân", "timeZone": "Asia/Ho_Chi_Minh", "createdAt": "2026-09-16T08:00:00Z" } }
```

Lỗi: 400 `VALIDATION` · 403 `REGISTRATION_CLOSED` · 409 `EMAIL_TAKEN` · 422 `INVALID_TIME_ZONE` · 429.

`POST /api/auth/login` `{ email, password }` → 200 (như register) · 401 `INVALID_CREDENTIALS` · 403 `ACCOUNT_DISABLED` · 423 `ACCOUNT_LOCKED` · 429.
`POST /api/auth/refresh` (cookie) → 200 `{ accessToken, accessTokenExpiresAt }` + cookie mới · 401 `REFRESH_INVALID` (xoá cookie) · 403 `ACCOUNT_DISABLED`.
`POST /api/auth/logout` (ẩn danh được) → 204, thu hồi token của cookie, xoá cookie.
`GET /api/account` (Bearer, audience `af-identity`) → `account`.
`PUT /api/account` `{ displayName, timeZone }` → 200 `account` · 422 `INVALID_TIME_ZONE`.
`POST /api/account/password` `{ currentPassword, newPassword }` → 204 · 422 `WRONG_PASSWORD`.
`GET /.well-known/jwks.json` (ẩn danh) → `{ "keys": [ { "kty": "RSA", "use": "sig", "alg": "RS256", "kid": "20260916-a1b2c3d4", "n": "...", "e": "AQAB" } ] }`.
`GET /.well-known/openid-configuration` → prod `{ "issuer": "https://id.antfarms.xyz", "jwks_uri": "https://id.antfarms.xyz/.well-known/jwks.json" }`; dev `{ "issuer": "http://localhost:5280/identity", "jwks_uri": "http://localhost:5280/identity/.well-known/jwks.json" }` (dựng từ `Jwt:Issuer`).

Payload access token (ví dụ):

```json
{ "iss": "https://id.antfarms.xyz", "aud": ["af-identity", "af-chinese"], "sub": "0192...", "email": "ban@vidu.com",
  "name": "Quân", "zoneinfo": "Asia/Ho_Chi_Minh", "jti": "...", "iat": 1789545600, "nbf": 1789545600, "exp": 1789546500 }
```

### 6.3 F3/F4 — chinese-backend truy cập

`GET /api/me` (`[Authorize]`) → `{ "id", "email", "displayName", "timeZone", "roles": ["learner"], "permissions": ["study.use"], "firstSeenAt" }`.
`GET /api/admin/ping` (`users.manage`) → `{ "ok": true }`.
`GET /api/admin/users?q=&page=1&pageSize=20` (`users.manage`) → `{ items: [{ id, email, displayName, roles, firstSeenAt, lastSeenAt }], page, pageSize, totalCount }`.
`PUT /api/admin/users/{id}/roles` `{ "roles": ["learner"] }` → 200 · 422 `LAST_ADMIN` | `UNKNOWN_ROLE` · 404.
`GET /api/admin/roles` → `[{ "code": "admin", "name": "Quản trị viên", "permissions": [...] }]`.

### 6.4 F5 — pinyin (chinese-backend, `study.use`)

`GET /api/pinyin/chart` → `{ initials: [...], finals: [...], syllables: [...] }` (định dạng §5.4.2). `GET /api/pinyin/guide` → nội dung `guide.json`.

`POST /api/pinyin/tone-drills`

```json
{ "mode": "listen_tone", "startedAt": "2026-09-16T08:00:00Z", "finishedAt": "2026-09-16T08:04:10Z",
  "items": [ { "syllable": "ma", "expectedTone": 3, "answeredTone": 2, "responseMs": 1800 } ] }
// 201
{ "id": "...", "total": 20, "correct": 16, "byTone": { "1": { "total": 5, "correct": 5 }, "2": { "total": 5, "correct": 3 } } }
```

`GET /api/pinyin/tone-stats` → `{ "totalAnswered": 240, "accuracy": 0.82, "byTone": { "2": { "total": 60, "correct": 41, "accuracy": 0.68 } }, "confusions": [ { "expected": 2, "answered": 3, "count": 14 } ], "recommendedFocus": [2, 3] }`.

### 6.5 F6 — từ điển

`GET /api/dictionary/search?q=ni3hao3&hsk=1&page=1&pageSize=20` → `{ items: [{ id, simplified, traditional, pinyin, hsk3Level, hsk2Level, hanViet, meaningsVi, meaningViStatus }], page, pageSize, totalCount }`.
`GET /api/dictionary/words/{id}` → như trên + `pos`, `meaningsEn`, `meaningViSource`, `hanVietStatus`, `characters: [{ hanzi, pinyinReadings, hanViet, strokeCount }]` (F7 thêm `inSrs`).
`GET /api/dictionary/characters/{hanzi}` → `{ hanzi, traditional, pinyinReadings, hanViet, strokeCount, radical, words: [≤ 20] }`.

### 6.6 F7 — SRS

`GET /api/srs/summary` → `{ localDate, dueToday, dueNow, newAvailableToday, newIntroducedToday, reviewedToday, nextDueAt }`.
`GET /api/srs/queue?limit=20` → `{ cards: [{ cardId, state, word: { id, simplified, pinyin, hanViet, meaningsVi, meaningViStatus }, intervals: { again: "PT10M", hard: "P2D", good: "P5D", easy: "P12D" } }] }`.
`POST /api/srs/cards/{cardId}/reviews` `{ clientReviewId, rating: "again|hard|good|easy", durationMs }` → 200 `{ card: { state, dueAt, reps, lapses }, summary }` · 404 · 422 `CARD_SUSPENDED`.
`POST /api/srs/cards` `{ wordIds }` → 201 `{ added, skipped }`.
`GET|PUT /api/me/learning-settings` `{ dailyNewCards, dailyReviewLimit, desiredRetention, ttsRate, autoPlayAudio }`.

### 6.7 F8–F10 — phác thảo

`POST /api/writing/attempts` `{ hanzi, mode, totalStrokes, totalMistakes, hintsUsed, durationMs }` → 201 · `GET /api/writing/characters?set=hsk1|lesson:{slug}|weak` · `GET /api/lessons` · `GET /api/lessons/{slug}` (quiz không có `correctOptionId`) · `POST /api/lessons/{id}/quiz-attempts` `{ answers: [{ questionId, optionId }] }` → `{ total, correct, scorePercent, passed, results: [{ questionId, correct, correctOptionId, explanation }] }` · Admin: `GET/POST /api/admin/lessons`, `GET/PUT/DELETE /api/admin/lessons/{id}` (kèm `version`), `PUT /api/admin/lessons/{id}/blocks|words|quiz`, `POST .../publish|unpublish`, `GET /api/admin/words?meaningViStatus=machine`, `PUT /api/admin/words/{id}/meaning`, `POST /api/admin/words/review`.

### 6.8 F11 — tổng quan

`GET /api/progress/overview` → `{ localDate, timeZone, streak: { current, longest, studiedToday }, today: { srsReviews, newCards, toneDrillItems, writingChars, quizzes }, srs: { dueToday, newAvailableToday }, vocabulary: { totalInPath, learning, mature }, lessons: { completed, published, next: { slug, title } }, tone: { accuracy }, activity: [{ date, count }] }` — `mature` = thẻ `review` có `stability ≥ 21`.

---

## 7. Phân rã feature

> Mỗi feature: code → build/test sạch → review → integration → **commit local riêng** → DỪNG chờ người dùng OK. "Học thử ngay" = việc học viên tự làm để nghiệm thu.

### Feature F0: Khung backend nền tảng
- Mục tiêu: 19 project build/test sạch; gateway + identity-service + chinese-backend chạy local, định tuyến qua gateway; đóng gói Docker + nginx biên sẵn sàng (chưa verify); README phần backend + triển khai.
- Phạm vi BE: §5.2.0 toàn bộ. Triển khai: §5.6.1–§5.6.3, §5.6.5 (không có chinese-frontend), §5.6.6. FE: không. DB: tạo tay `af_identity`, `af_chinese` (§5.1.0), chưa migration. Học liệu: không. Gốc repo: `.config/dotnet-tools.json`, `.editorconfig`, `.gitattributes`, bổ sung `.gitignore`, `README.md`.
- Phụ thuộc: không.
- Tiêu chí hoàn thành + cách tự test:
  1. `dotnet build backend/backend.slnx -v q` 0 error, output có đủ 19 project; `dotnet test backend/backend.slnx` xanh.
  2. Chạy 3 tiến trình theo README: `http://localhost:5280/identity/api/system/info` ⇒ `service = identity-service`; `http://localhost:5280/chinese/api/system/info` ⇒ `service = chinese-backend`.
  3. `http://localhost:5281/health/ready` và `5282/health/ready` = 200 khi Postgres chạy; dừng `postgresql-x64-18` ⇒ 503; `/health/live` vẫn 200.
  4. Tắt chinese-backend ⇒ `5280/chinese/api/system/info` trả 502, `5280/identity/...` vẫn 200.
  5. Xoá `appsettings.Development.json` của chinese ⇒ khởi động báo lỗi tiếng Việt chỉ rõ cần copy file example.
  6. `http://localhost:5282/scalar/v1` mở được.
  6b. File triển khai có đủ theo §5.6: 3 Dockerfile .NET (không dòng `# syntax=`, có tách layer restore, cache mount NuGet, `--no-restore /m:2`, cổng 8080, `HEALTHCHECK` bằng `wget`), `backend/.dockerignore`, `deploy/docker-compose.yml` (chỉ `nginx` có `ports:`, có service `certbot`), `deploy/.env.example`, `deploy/conf/nginx.conf.example` (khối 80 ACME + 301, `id.`, `chinese.`, mẫu ngôn ngữ + portal để comment, HSTS không `includeSubDomains`), service `certbot`, `deploy/scripts/{self-signed,get-cert,renew-cert}.sh` (chép khuôn MedDental, `--key-type rsa`), `conf/cloudflare-realip.conf.example`, script init Postgres, `deploy/VERIFY-DOCKER.md` (gồm mục HTTPS 11–17). **Trạng thái: CHƯA VERIFY — cần server có DNS trỏ về + Docker** (checklist §5.6.6); review kiểm bằng mắt, đối chiếu từng dòng với `mdt-re-construct/deploy/app-core`.
  7. `git status` không có `appsettings.Development.json`, `bin/`, `obj/`, `.secrets/`.

### Feature F1: Khung frontend
- Mục tiêu: `apps/chinese` chạy, đi qua gateway tới cả hai service; packages `@af/tsconfig|ui|api`; `lint:ui`.
- Phạm vi FE: §5.3.0 toàn bộ. BE/DB/Học liệu: không. README phần frontend. Cập nhật `CLAUDE.md` nếu thực tế khác hợp đồng.
- Phụ thuộc: F0.
- Tiêu chí hoàn thành + cách tự test:
  1. `cd frontend && yarn install && yarn workspace @af/chinese tsc -b && yarn workspace @af/chinese build` sạch; `yarn lint:ui` exit 0.
  2. Thêm tạm `import { Dialog } from '@mui/material'` vào `apps/chinese/src` ⇒ `lint:ui` exit 1 (rồi gỡ).
  3. `yarn workspace @af/chinese dev` + 3 backend ⇒ `http://localhost:3280` hiện layout + 2 chip "đang chạy"; tắt chinese-backend ⇒ chip đó thành lỗi, trang không trắng.
  4. `/abc` ⇒ trang 404 có "Về trang chủ"; ở 375px có bottom nav; chế độ tối bật/tắt được.
  5. `apps/chinese/Dockerfile` + `nginx.conf` (khối `.mjs`, SPA fallback, không proxy API) theo §5.6.4; bỏ comment `chinese-frontend` trong compose; bổ sung checklist §5.6.6. **CHƯA VERIFY bằng Docker — cần máy có Docker.**

### Feature F2: identity-service — xác thực + JWKS + `@af/auth`
- Mục tiêu: đăng ký/đăng nhập/làm mới/đăng xuất/hồ sơ tài khoản, khoá ký RS256 persist, JWKS; frontend có trang đăng nhập/đăng ký dùng chung.
- Phạm vi BE: §5.2.2 (identity-service + `backend/shared/AntFarm.Auth` + `TestTokenFactory` trong `AntFarm.Testing`), migration `F2_Accounts`. FE: §5.3.1 (`@af/auth`, `@af/utils`, bổ sung `@af/api`). DB: §5.1.1. Học liệu: không.
- Phụ thuộc: F1.
- Tiêu chí hoàn thành + cách tự test: build/test sạch; test §5.2.2 xanh với `AF_TEST_PG`; đăng ký trên `http://localhost:3280/dang-ky` ⇒ vào trang chủ, menu hiện tên; F5 trang vẫn đăng nhập; DevTools: cookie `af_rt` HttpOnly, Path `/identity/api/auth`, không có token trong `localStorage`; mở 2 tab F5 gần như cùng lúc ⇒ không tab nào bị đăng xuất; restart identity-service ⇒ phiên cũ vẫn làm mới được (khoá ký đã persist); `.secrets/` không vào git. Học thử ngay: tạo tài khoản học viên của chính mình.

### Feature F3: chinese-backend — nhận JWT + phân quyền cục bộ + trang 4xx
- Mục tiêu: service tiếng Trung kiểm token qua JWKS, provision người dùng, quyền từ DB `af_chinese`, `/api/me`; frontend chặn theo quyền.
- Phạm vi BE: §5.2.3, migration `F3_Access`. FE: §5.3.2. DB: §5.1.2. Học liệu: không.
- Phụ thuộc: F2.
- Tiêu chí hoàn thành + cách tự test: build/test sạch (test §5.2.3); đăng nhập bằng email bootstrap ⇒ `/chinese/api/me` có `admin`; tài khoản khác ⇒ `learner`; learner gọi `/chinese/api/admin/ping` ⇒ 403 JSON; gỡ hết vai trò một user trong DB ⇒ user đó vào app thấy `/403` có nút Đăng xuất; tắt identity-service khi chinese-backend đã cache JWKS ⇒ token còn hạn vẫn được chấp nhận; `loadMe` tạm của F2 đã bị gỡ.

### Feature F4: Hồ sơ & quản trị vai trò
- Mục tiêu: người học đổi tên/múi giờ/mật khẩu; admin tiếng Trung gán vai trò.
- Phạm vi: §5.2.4 (F4), §5.3.3 (F4). DB: không.
- Phụ thuộc: F3.
- Tiêu chí: đổi múi giờ ⇒ `/chinese/api/me` phản ánh ngay sau khi lưu; không gỡ được admin cuối (422 hiện tại chỗ); learner không thấy menu Quản trị, vào URL ⇒ `/403`; ô múi giờ gõ "Ho_Chi" ra gợi ý (quy tắc renderInput); `lint:ui` sạch.

### Feature F5: Pinyin & thanh điệu
- Mục tiêu: người số 0 học âm & thanh, biết mình yếu thanh nào.
- Phạm vi BE: §5.2.4 (F5), migration `F5_ToneDrill` (gồm `study_events`). FE: §5.3.3 (F5). DB: §5.1.3. Học liệu: khung `content/` + `content/chinese/` (schemas, validate, SOURCES, LICENSES) + `data/pinyin/*.json` tự soạn.
- Phụ thuộc: F3 (làm được trước F4 nếu người dùng muốn học sớm). *Mức thiết kế — BA bổ sung chi tiết trước khi làm.*
- Tiêu chí (dự kiến): `yarn --cwd content validate:chinese` sạch; test pinyin FE + BE xanh; bấm ô `ma` nghe đủ 4 thanh trên máy người dùng (hoặc hiện hướng dẫn cài giọng); luyện 20 câu lúc 06:30 sáng giờ VN ⇒ `study_events.local_date` đúng ngày VN. Học thử ngay: 3 phiên luyện thanh.

### Feature F6: Học liệu HSK 3.0 cấp 1 + tra từ
- Mục tiêu: kho từ HSK 3.0 cấp 1 có pinyin, Hán Việt, nghĩa Việt; tra bằng mọi cách gõ.
- Phạm vi: §5.2.4 (F6), migration `F6_Vocabulary`, §5.3.3 (F6), §5.1.4, học liệu `hsk-words.json`, `characters.json`, `build-hsk.mjs`.
- Phụ thuộc: F5. **Bị chặn bởi D5, D6.** *Mức thiết kế.*
- Tiêu chí (dự kiến): số từ cấp 1 khớp nguồn; restart 2 lần không nhân đôi; tìm `爱`, `ai4`, `ài`, `ai`, `yêu`, `yeu`, `ái` đều ra 爱; dòng đã duyệt không bị ghi đè.

### Feature F7: Flashcard SRS (FSRS-6)
- Phạm vi: §5.2.4 (F7), migration `F7_Srs`, §5.3.3 (F7), §5.1.5.
- Phụ thuộc: F6. *Mức thiết kế.*
- Tiêu chí (dự kiến): unit test scheduler xanh gồm vector vàng py-fsrs; trùng `clientReviewId` không tạo 2 log; thẻ mới dừng ở 10/ngày; số "đến hạn hôm nay" đổi đúng qua 00:00 giờ VN; ôn 20 thẻ ở 375px. Học thử ngay: ôn 3 ngày liên tiếp.

### Feature F8: Luyện viết chữ Hán
- Phạm vi: §5.2.4 (F8), migration `F8_Writing`, §5.3.3 (F8), §5.1.6, dữ liệu nét theo D3.
- Phụ thuộc: F6. **Bị chặn bởi D3.** *Mức thiết kế.*

### Feature F9: Bài học + quiz (học viên)
- Phạm vi: §5.2.4 (F9), migration `F9_Lessons`, §5.3.3 (F9), §5.1.6, 5 bài tự soạn (`chao-hoi`, `ban-than`, `so-dem`, `gia-dinh`, `thoi-gian`).
- Phụ thuộc: F6, F5. **Bị chặn nhẹ bởi D7** (có mặc định). *Mức thiết kế.*

### Feature F10: Quản trị nội dung & duyệt nghĩa
- Phạm vi: §5.2.4 (F10), §5.3.3 (F10).
- Phụ thuộc: F9. *Mức thiết kế.*

### Feature F11: Tổng quan tiến độ & streak
- Phạm vi: §5.2.4 (F11), §6.8, §5.3.3 (F11).
- Phụ thuộc: F5, F7, F8, F9 (khối chưa có thì ẩn). **Bị chặn bởi D8** (có mặc định). *Mức thiết kế.*

### Feature F12 (sau MVP): Đưa lên server thật
- Mục tiêu: chạy hết checklist `deploy/VERIFY-DOCKER.md` trên máy có Docker, sửa lỗi phát sinh; DNS Cloudflare `id.`, `chinese.`; cấp chứng chỉ Let's Encrypt theo §5.6.5 (mặc định HTTP-01 + SAN; wildcard nếu chốt D20) + cron gia hạn; chạy mục 11–17 checklist HTTPS; registry ảnh (tuỳ chọn); sao lưu volume `pg-data` + `identity-keys`; tắt `Auth:AllowRegistration` hoặc theo D4.
- Phụ thuộc: F3 trở lên (có đăng nhập thật). Dockerfile/compose/nginx đã có từ F0–F1 và được cập nhật ở mỗi feature.

### Feature F13 (feature sau cùng, ngoài MVP): Portal antfarms.xyz
- Mục tiêu: `apps/portal` (`@af/portal`) ở `antfarms.xyz` — trang chọn ngôn ngữ, hồ sơ tài khoản; identity có quyền quản trị tài khoản (khoá/mở, đặt lại mật khẩu).
- Phạm vi: §5.2.4 (F13), §5.3.3 (F13); server block thật từ `_template-portal-antfarms.xyz.conf.example`; thêm `https://antfarms.xyz` vào `Auth:AllowedOrigins`; Dockerfile + mục compose `portal-frontend`.
- Phụ thuộc: F4 (và F12 nếu làm trên server). Người dùng chốt 16/09/2026: **không** thuộc F0 và **không** thuộc MVP học tiếng Trung. *Mức thiết kế.*

---

## 8. Thứ tự thực thi & phụ thuộc

```
F0 ─► F1 ─► F2 ─► F3 ─┬─► F4 ─────────────────────► F13 Portal (feature sau cùng)
                      └─► F5 ─► F6 ─┬─► F7 ─┐
                                    ├─► F8 ─┼─► F11
                                    └─► F9 ─┴─► F10
F12 (đưa lên server) làm được bất kỳ lúc nào sau F3.
```

- **Phân công agent (người dùng chốt 16/09/2026):** mọi phần **FRONTEND** (packages `@af/*`, `frontend/apps/*`, `frontend/scripts/check-ui-conventions.mjs`) do agent `frontend-implement` chạy model **Fable** làm — Orchestrator gọi công cụ `Agent` với `subagent_type: "frontend-implement"` và **luôn truyền `model: "fable"`**. Backend/Database/Content: Sonnet. Review/Integration: Opus. **F1 toàn bộ là việc của agent Fable**; F0 là backend (Sonnet).
- Thứ tự đề xuất theo lộ trình học: **F0 → F1 → F2 → F3 → F5 → F4 → F6 → F7 → F9 → F8 → F10 → F11**, rồi F12 (lên server) khi cần; F13 (Portal) sau cùng. F5 trước F4 để học pinyin sớm; F9 trước F8 vì bài học quyết định chữ cần viết.
- Trong một feature: DB chốt tên bảng/cột theo §5.1 trước, rồi **Backend ‖ Frontend ‖ Content** song song; Frontend dựng theo §6.
- F0 và F1 có thể giao hai agent song song **chỉ khi** người dùng chấp nhận F1 commit sau F0 (F1 cần gateway của F0 để nghiệm thu mục 3).
- Trước F6–F13: kiểm quyết định mở liên quan đã chốt; BA bổ sung chi tiết.

---

## 9. Tiêu chí hoàn thành + cách kiểm thử

### 9.1 Cổng bắt buộc mọi feature

```powershell
dotnet build backend/backend.slnx -v q          # 0 error, đủ project
dotnet test backend/backend.slnx                # xanh; test [DbFact] tự skip khi thiếu AF_TEST_PG
cd frontend
yarn workspace @af/chinese tsc -b               # bắt buộc -b
yarn workspace @af/chinese build                # khi đụng dependency hoặc packages @af/*
yarn lint:ui                                    # exit 0
yarn workspace @af/chinese test                 # từ F5
yarn --cwd ../content validate:chinese          # từ F5, khi đụng học liệu
```

Kèm: `git status` sạch bí mật; migration mới chạy được trên DB dev trống **và** DB dev đang có dữ liệu feature trước.

### 9.2 Test tích hợp PostgreSQL local (không Docker)

- `AF_TEST_PG` = chuỗi kết nối **không** có `Database=`; mỗi service dùng `af_<service>_test`.
- `[DbFact]` tự skip khi thiếu biến. Fixture `ICollectionFixture`: `EnsureDeletedAsync` + `MigrateAsync` **một lần mỗi lượt**; dữ liệu test dùng email/khoá ngẫu nhiên.
- Service ngôn ngữ test với token do `TestTokenFactory` ký — **không** cần identity-service chạy.
- Integration agent **phải** đặt `AF_TEST_PG` và báo số test chạy/skip.

### 9.3 Nghiệm thu end-to-end

Chạy gateway + identity + chinese + app thật, thao tác trình duyệt ở 1366px và 375px, đối chiếu tiêu chí §7; kèm "Học thử ngay".

---

## 10. Rủi ro / quyết định mở / ràng buộc

### 10.1 Rủi ro

| # | Rủi ro | Ảnh hưởng | Giảm thiểu |
|---|---|---|---|
| RK1 | Máy không có giọng TTS `zh-CN` | F5, F7, F9 không nghe được | Phát hiện + hướng dẫn cài; nghiệm thu trên máy người dùng |
| RK2 | TTS đọc sai chữ đa âm | Học sai thanh | R-L1 chữ đơn âm |
| RK3 | Giấy phép nguồn khác hiểu biết (CVDICT, nghĩa Anh trong complete-hsk-vocabulary, hanzi-writer-data) | Bỏ nguồn, dịch máy | content-implement xác minh trước; không rõ ⇒ không dùng |
| RK4 | Nghĩa Việt dịch máy sai | Học viên số 0 không tự phát hiện | Nhãn "chưa duyệt", màn duyệt F10, ưu tiên duyệt từ đầu lộ trình |
| RK5 | Cài FSRS sai công thức mà test tự bịa vẫn xanh | Lịch ôn lệch | Vector vàng từ py-fsrs kèm commit; review đối chiếu công thức |
| RK6 | Cookie refresh sai `Path` (dev `/identity/api/auth` ≠ prod `/api/auth`), sai `Domain`, hoặc `Secure` trên http | Đăng nhập xong F5 là mất phiên | Bảng §5.6.2; ApiTests F2 chạy cả hai bộ cấu hình; verify mục 8 |
| RK7 | Hai tab xoay refresh cùng lúc | Đăng xuất oan | Web Locks + ân hạn 30 giây + `FOR UPDATE` |
| RK8 | Mất thư mục khoá ký (xoá `.secrets/`) | Mọi access token cũ vô hiệu, người dùng phải đăng nhập lại (refresh token trong DB vẫn dùng được nên thực tế chỉ làm mới lại) | README cảnh báo; Docker dùng volume `identity-keys`; không fallback khoá trong code |
| RK9 | Service ngôn ngữ không tải được JWKS lúc khởi động (identity chưa chạy) | 401 tới khi tải được | `ConfigurationManager` tải lười + thử lại; README thứ tự khởi động identity → chinese → gateway |
| RK10 | Đồng bộ hồ sơ trễ (tên/múi giờ đổi ở identity, token cũ còn 15 phút) | Streak tính theo múi giờ cũ tối đa 15 phút | F4: lưu hồ sơ xong frontend refresh ngay; provisioning bỏ qua cache khi claim khác |
| RK11 | Test tích hợp bị skip âm thầm (không Docker) | Lỗi DB lọt | Integration bắt buộc đặt `AF_TEST_PG` và báo số skip |
| RK12 | Npgsql `timestamptz` + `DateTime` `Unspecified` (tham số ngày từ query string) | 500 lúc chạy, build sạch | Quy tắc CLAUDE.md; review soát |
| RK13 | EF `PendingModelChangesWarning` khi quên migration | Service chết lúc khởi động | Migration cùng commit; ApiTests `[DbFact]` bắt |
| RK14 | Unihan `kVietnamese` thiếu/lẫn cách đọc không phải Hán Việt | Hán Việt sai | `derived`, để trống khi không có, duyệt ở F10 |
| RK15 | HSK 3.0 cấp 1 (~500 từ) nặng với người số 0 | Nản | `path_order` ưu tiên giao HSK 2.0–1, 10 thẻ mới/ngày |
| RK16 | Nhiều service nhỏ làm chạy dev phức tạp (4 tiến trình) | Dễ quên chạy một service, báo lỗi 502 khó hiểu | README thứ tự chạy + chip trạng thái ở trang chủ F1; cân nhắc script `scripts/dev.ps1` mở 3 backend (không bắt buộc) |
| RK17 | **Cookie `Domain=.antfarms.xyz`** đặt nhầm ở dev (`localhost` từ chối cookie có Domain) hoặc quên đặt ở production | Dev: đăng nhập xong F5 là mất phiên. Prod: mỗi subdomain đăng nhập riêng | `Auth:RefreshCookieDomain` rỗng ở dev, `.antfarms.xyz` ở prod (§5.6.2); verify mục 8 §5.6.6 |
| RK18 | **Cookie refresh gửi được từ MỌI subdomain** — subdomain nào bị XSS/bị tiếp quản (DNS trỏ tới dịch vụ đã huỷ) sẽ gửi được request tới `id.antfarms.xyz/api/auth/refresh` kèm cookie | Chiếm phiên | Kiểm `Origin` theo `Auth:AllowedOrigins` (R-A7b); không tạo subdomain trỏ ra dịch vụ bên thứ ba; xoá bản ghi DNS khi gỡ ngôn ngữ |
| RK19 | Quên thêm subdomain mới vào `Auth:AllowedOrigins` | Trình duyệt chặn CORS khi gọi `id.antfarms.xyz` (lỗi chỉ thấy trong Console) / 403 `ORIGIN_NOT_ALLOWED` | Checklist §5.5 mục 13; thông điệp lỗi nêu rõ origin bị từ chối (log Warning) |
| RK20 | `Jwt:Issuer` (identity) khác `Auth:Issuer` (service) — vd `https://id.antfarms.xyz` vs `https://id.antfarms.xyz/` | Mọi request 401, build/test đều sạch | Một biến `APP_DOMAIN` dựng cả hai trong compose; test F3 có ca issuer sai |
| RK21 | nginx biên dùng `$proxy_add_x_forwarded_for` hoặc gateway dùng `X-Forwarded: Set` | Rate limit sai (gom mọi người thành một IP) hoặc bị giả IP để né khoá | Snippet §5.6.5 ghi đè `X-Forwarded-For $remote_addr`; gateway `Append`; verify mục 7 |
| RK22 | nginx phân giải tên upstream một lần lúc khởi động | Rebuild một container ⇒ nginx trỏ IP cũ ⇒ 502 | `resolver 127.0.0.11` + `proxy_pass` qua biến (§5.6.5) |
| RK23 | Dockerfile viết trên máy không có Docker, **chưa từng build** | Lỗi đường dẫn COPY/peer dependency chỉ lộ lúc lên server | Review soát bằng mắt theo §5.6.3–§5.6.4; `VERIFY-DOCKER.md` là việc đầu tiên của F13; mỗi feature đổi `.csproj`/package phải sửa Dockerfile cùng commit |
| RK25 | `VITE_IDENTITY_API_URL` nướng sai lúc build (quên build arg ⇒ `/identity/api` trên production, nơi `chinese.` không proxy `/identity`) | Ảnh build/chạy bình thường, bấm đăng nhập nhận `index.html` hoặc 404 | Dockerfile đặt mặc định `https://id.antfarms.xyz/api`; verify 8c |
| RK26 | nginx biên hoặc gateway tự thêm header `Access-Control-*` chồng lên header của identity-service | Trình duyệt từ chối (header trùng) — đăng nhập hỏng chỉ trên production | CORS chỉ ở identity-service; comment trong `10-id.antfarms.xyz.conf`; verify 8b |
| RK27 | HSTS bật `includeSubDomains`/`preload` quá sớm | Một subdomain chưa có HTTPS (vd mới tạo, đang chờ cấp chứng chỉ) **không truy cập được suốt max-age** (1 năm), không gỡ nhanh được — preload còn phải xin gỡ khỏi danh sách trình duyệt | Chỉ `max-age=31536000` từng host; cân nhắc `includeSubDomains` khi mọi subdomain đã HTTPS ổn định (ghi quyết định mới) |
| RK28 | Chạy `get-cert.sh` chỉ với tên miền mới | Chứng chỉ bị cấp lại thiếu tên cũ ⇒ các subdomain cũ lỗi chứng chỉ ngay | Checklist §5.5 mục 11; comment đầu script; kiểm SAN sau mỗi lần cấp |
| RK29 | Thiếu cron `renew-cert.sh` | certbot gia hạn vào `certs/letsencrypt` nhưng nginx đọc `certs/live` ⇒ hết hạn sau 90 ngày, **không log nào báo** | Cron cài ngay trong bước cấp lần đầu; verify mục 16 |
| RK30 | Cloudflare SSL mode Flexible khi bật proxy | Vòng lặp chuyển hướng vô hạn (Cloudflare→origin HTTP→301 https→...) | Full (strict) — ghi README deploy + verify mục 17 |
| RK31 | Proxy Cloudflare bật mà chưa `set_real_ip_from` + `CF-Connecting-IP` | IP thật = IP Cloudflare ⇒ khoá đăng nhập/rate limit gom người dùng khác nhau, một người sai mật khẩu làm khoá lây | `conf/cloudflare-realip.conf`; cập nhật dải IP định kỳ |
| RK32 | Thiếu `KnownNetworks` (ASP.NET chỉ tin loopback mặc định) hoặc khai quá rộng (`0.0.0.0/0`) | Thiếu: IP = IP gateway, `IsHttps=false`. Quá rộng: client tự gửi `X-Forwarded-For` giả để né khoá | Chỉ loopback + `172.28.0.0/16`, `ForwardLimit=2`; verify mục 7 |
| RK33 | Chứng chỉ ECDSA (mặc định certbot) | Người dùng sau tường lửa doanh nghiệp SSL-inspection bị chặn cả website | `--key-type rsa` (bài học MedDental 23/08/2026) |
| RK24 | Từ F5, chinese-backend cần `content/chinese/data` mà build context đang là `./backend` | Ảnh thiếu học liệu ⇒ endpoint 503 | Ghi chú §5.6.3; F5 đổi context/`additional_contexts` |

### 10.2 QUYẾT ĐỊNH MỞ — cần người dùng chốt

> **F0–F5 không bị chặn** bởi quyết định nào dưới đây (D19 nên chốt trước F1 nhưng có mặc định). D1 (HSK 3.0) và D2 (FSRS-6) **đã chốt 16/09/2026**, chuyển vào R-C9 và R-L2.

| Mã | Câu hỏi | Đề xuất mặc định + lý do | Chặn |
|---|---|---|---|
| **D3** | Dữ liệu nét `hanzi-writer-data`: CDN jsdelivr hay đóng gói? | **Đóng gói tập con** (chỉ chữ có trong `characters.json`, vài trăm KB) vào `apps/chinese/public/hanzi-data/` bằng script, kèm toàn văn giấy phép Arphic. Lý do: không phụ thuộc CDN lúc học, ghim phiên bản, nghĩa vụ giấy phép rõ ràng | **F8** |
| **D4** | Đăng ký mở hay cần mời? | **Mở ở dev**, đóng khi deploy công khai (`Auth:AllowRegistration`). Mời bằng mã để sau MVP | Không chặn (cờ cấu hình) |
| **D5** | Nguồn nghĩa tiếng Việt | CVDICT nếu xác minh được giấy phép, vẫn `machine` tới khi duyệt; không được ⇒ dịch máy từ nghĩa Anh, `machine` | **F6** |
| **D6** | Chấp nhận nghĩa vụ CC BY-SA 4.0 cho dữ liệu dẫn xuất CC-CEDICT? | **Chấp nhận** — chỉ áp cho `content/chinese/data/`, không lan sang mã nguồn | **F6** |
| **D7** | Luật hoàn thành bài học | Quiz ≥ 80%, không khoá tuần tự; hoàn thành ⇒ tự thêm từ của bài vào SRS (`source='lesson'`) | **F9** (có mặc định) |
| **D8** | "Ngày có học" cho streak | ≥ 1 hoạt động có kết quả; mục tiêu ngày hiển thị riêng; không "đóng băng streak" | **F11** (có mặc định) |

| **D19** (mới) | API trên subdomain dùng tiền tố tên service (`chinese.antfarms.xyz/chinese/api/...`) hay `/api` trơn (`chinese.antfarms.xyz/api/...`)? | **Giữ tiền tố `/chinese/api`** (đang thiết kế). Lý do: đường dẫn giống hệt dev (Vite proxy) nên không có biến cấu hình URL nào khác nhau giữa môi trường; gateway không phải định tuyến theo Host; một app vẫn gọi được `/identity/api` cùng origin. `/api` trơn đẹp hơn nhưng buộc gateway khớp theo Host (dev phải dùng `chinese.localhost`) hoặc nginx rewrite riêng từng subdomain | Không chặn F0 (chỉ đổi cấu hình nginx + baseURL nếu chọn `/api`); nên chốt trước **F1** |
| **D20** | Chuyển sang chứng chỉ **wildcard** `*.antfarms.xyz` (+ `antfarms.xyz`) qua **DNS-01** với plugin `certbot/dns-cloudflare` và API token Cloudflare phạm vi **Zone:DNS:Edit** (token là secret, lưu `deploy/secrets/cloudflare.ini`, gitignore)? | **Chưa — mặc định giữ khuôn MedDental (HTTP-01 + một chứng chỉ SAN)**. Ưu điểm wildcard: thêm ngôn ngữ mới **không phải cấp lại/không lo đè tên cũ**, không cần cổng 80 mở cho xác minh, chạy được khi bật proxy Cloudflare. Nhược: thêm một secret quyền sửa DNS nằm trên server; wildcard **không** phủ apex `antfarms.xyz` (phải khai thêm). Nên cân nhắc khi bắt đầu ngôn ngữ thứ hai | Không chặn F0–F11; ảnh hưởng **F12** |

### 10.3 Mặc định BA đã chọn (không chặn — người dùng có thể phản đối)

- D9 — Refresh token trong cookie HttpOnly (Path theo tiền tố gateway), access token trong bộ nhớ; ân hạn 30 giây.
- D10 — Pinyin số thanh, `ü` = `v`, 儿化 = `r5`.
- D11 — Lưu thanh gốc, biến điệu là gợi ý hiển thị.
- D12 — Cổng dev: gateway 5280, identity 5281, chinese 5282, app 3280, portal 3281. Trong Docker mọi service nghe 8080, frontend 80, không publish.
- nginx biên + YARP (không chỉ một trong hai) — lý do §5.6.1.
- D13 — Không "quên mật khẩu qua email" trong MVP; quản trị tài khoản identity (khoá, đặt lại mật khẩu) dời sang F13 (Portal).
- Portal (F13) dùng trang đăng nhập chung của `@af/auth`, không làm "đăng nhập tập trung có chuyển hướng" — cookie `Domain=.antfarms.xyz` đã cho đăng nhập một nơi dùng mọi subdomain.
- D14 — Không ESLint trong MVP.
- D15 — 5 bài học seed do content-implement tự soạn, người dùng duyệt ở F10.
- **D16** (mới) — Khoá ký RSA lưu file PEM trong `.secrets/identity/keys/` (gitignore), Development tự sinh, môi trường khác thiếu khoá thì dừng. Phương án thay thế: lưu DB mã hoá bằng Data Protection — phức tạp hơn (lại phải persist key ring Data Protection).
- **D17** (mới) — Người dùng mới vào service ngôn ngữ tự nhận `learner` (`ChineseAccess:DefaultRoles`). Khác MedDental (0 quyền) vì đây là ứng dụng tự học; muốn fail-closed chỉ cần để mảng rỗng.
- Thứ tự `path_order`: từ thuộc cả HSK 3.0–1 và HSK 2.0–1 lên trước (R-C9).
- Tên thư mục: `identity-service`, `chinese-backend`, `apps/chinese`; tên repo giữ `chinese-study`.

### 10.4 Ràng buộc dự án phải nhắc agent thực thi

- Build/test sạch theo §9.1; commit local riêng từng feature, **không push**, nhánh `develop`.
- identity-service chỉ xác thực; service ngôn ngữ tự phân quyền trên DB riêng; JWT chỉ nhận diện; frontend đọc quyền từ `/api/me` của service ngôn ngữ; `RequirePermissionAttribute` gán `Policy` trong constructor.
- Mỗi service một database; không đọc chéo DB.
- MUI v9 (`slotProps`, shorthand trong `sx`); `renderInput` trải `params.slotProps` trước; `AppDialog`/`AppDrawer`; `useTabParam`; không `uuid`; `tsconfig.app.json` không `baseUrl`; `@af/*` import thẳng TS source, peer dependency khai ở app.
- Npgsql `timestamptz` chỉ nhận `Kind=Utc`; tham số ngày từ query string ⇒ `SpecifyKind` + nửa hở; "hôm nay" theo múi giờ người dùng.
- Seed/import idempotent, không ném lỗi; danh mục người dùng xoá được chỉ gieo khi bảng trống.
- Học liệu: nguồn có giấy phép rõ, ghi `content/<ngôn-ngữ>/SOURCES.md` + `LICENSES/`; dịch máy đánh dấu `machine`; `lang` đúng ngôn ngữ; mobile-first 375px.
- Không commit bí mật: `appsettings.Development.json`, `.secrets/`.
