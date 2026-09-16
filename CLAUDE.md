# CLAUDE.md — Chinese Study

> Ứng dụng học tiếng Trung trực tuyến cho người Việt. Chủ dự án vừa phát triển vừa là học viên đầu tiên (bắt đầu từ số 0).
> File này giữ **quy tắc bắt buộc** + **tổng quan ngắn** + **mục lục**. Kiến trúc và pattern bê từ dự án MedDental (`mdt-re-construct`), bản rút gọn.

## Mục lục

- [Quy trình đa Agent](#quy-trình-đa-agent) → chi tiết [`docs/agents/AGENT-WORKFLOW.md`](docs/agents/AGENT-WORKFLOW.md)
- [Quy tắc bắt buộc](#quy-tắc-bắt-buộc)
- [Tổng quan kiến trúc](#tổng-quan-kiến-trúc)
- Hợp đồng thực thi các đợt: `docs/agent-workflow/`

---

## Quy trình đa Agent

```
Người dùng → ORCHESTRATOR (phiên chính, Opus): phân loại
  • Câu hỏi → tự trả lời
  • Nâng cấp / Tạo mới → INVESTIGATION (Haiku) → BUSINESS ANALYSIS (Opus, hợp đồng + phân rã feature)
      → VÒNG LẶP từng feature: BACKEND ‖ FRONTEND ‖ DATABASE ‖ CONTENT (Sonnet)
        → REVIEW (Opus) → INTEGRATION (Opus, commit local riêng) → DỪNG chờ người dùng OK
```

**Feature-by-feature:** mỗi feature là đơn vị commit & test độc lập. Xong → kiểm tra → commit local → dừng cho người dùng review → mới sang feature kế. Không gộp nhiều feature vào một commit.

---

## Quy tắc bắt buộc

**Luôn trả lời bằng tiếng Việt có dấu** — kể cả giải thích kỹ thuật, comment code, tài liệu.

**Kiểm tra build sau khi sửa code** (chưa sạch thì chưa được báo hoàn thành):
- Backend: `dotnet build backend/backend.slnx -v q` — 0 error; `dotnet test backend/backend.slnx` — xanh.
- Frontend: `yarn workspace @cs/<app> tsc -b` (**bắt buộc `-b`** — root tsconfig có `files: []` nên `--noEmit` không kiểm gì). Đụng dependency → thêm `yarn workspace @cs/<app> build`.

**Commit local sau mỗi feature. Tuyệt đối không push.** Branch phát triển: `develop`; branch chính: `master`.

**Không commit secrets:** `appsettings.Development.json`, `appsettings.Production.json`, `.env`, `.env.local`, `*.pfx`, `*.pem` bị gitignore. Chỉ commit `appsettings.json` (mặc định không nhạy cảm) và `.env.example`.

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

**Peer dependency của package dùng chung:** `@cs/ui`, `@cs/api`, `@cs/auth`, `@cs/utils` khai thư viện ở `peerDependencies` ⇒ app tiêu thụ **phải** khai trong `dependencies`. Máy dev không báo lỗi (hoisted), chỉ Docker/CI lộ. Trước khi gỡ dependency khỏi app: `grep -l "<thu-vien>" frontend/packages/*/package.json`.

**Dialog/Drawer dùng `AppDialog`/`AppDrawer` của `@cs/ui`** — chặn đóng ngoài ý muốn khi bấm ra ngoài/ESC. Hộp thoại chỉ đọc cần đóng nhanh thì khai `closeOnBackdrop` tường minh kèm bình luận lý do.

**Tab cấp trang dùng `useTabParam` của `@cs/ui`** (tab lên URL, `replace` chứ không `push`).

**Npgsql + `timestamptz` — CHỈ nhận `DateTime` `Kind = Utc`** (nổ lúc runtime, build vẫn sạch):

| ❌ Sinh `Unspecified` | ✅ Viết đúng |
|---|---|
| `DateTime.Parse` chuỗi không `Z`/offset | parse sang `DateTimeOffset`, hoặc `SpecifyKind(..., Utc)` |
| `new DateTime(y,m,d)` · `DateTime.Today` · `DateTime.Now` | `DateTime.UtcNow` / `SpecifyKind(..., Utc)` |
| `dto.SomeOffset.Date` / `.DateTime` | `SpecifyKind(x.Date, Utc)` hoặc `x.UtcDateTime` |
| **Tham số `DateTime` bind từ query string** (`?from=2026-09-01`) | `SpecifyKind(q.From.Value.Date, Utc)`; cận trên nửa hở `< to.Date.AddDays(1)` |
| `default(DateTime)` chưa gán | gán tường minh trước `SaveChanges` |

**Ngày học theo múi giờ người dùng:** "hôm nay" (thẻ đến hạn, chuỗi ngày học liên tiếp, mục tiêu ngày) tính theo `users.time_zone` (mặc định `Asia/Ho_Chi_Minh`); mốc thời gian lưu UTC. Dùng UTC để cắt ngày sẽ làm chuỗi ngày học đứt oan lúc 0h–7h sáng.

**Dapper + `DateOnly`/`TimeOnly`** (nếu dùng Dapper): bắt buộc đăng ký TypeHandler lúc khởi động; đọc cột `date` phải khai `DateOnly?` chứ không `DateTime?`.

**PHÂN QUYỀN CỤC BỘ:** quyền hiệu lực chỉ từ bảng `users → user_roles → role_permissions → permissions` trong DB. JWT chỉ để nhận diện (`sub`, `email`, `name`) — **không** suy quyền từ claim `role`. Frontend đọc quyền từ `GET /api/me`, không tự derive. Seed MỘT tài khoản quản trị theo cấu hình `Bootstrap:AdminEmails`; tài khoản mới đăng ký nhận vai trò `learner`.

**`RequirePermissionAttribute` gán `Policy` của lớp cơ sở trong constructor**, KHÔNG khai `public new string Policy` — `new` chỉ che thuộc tính kiểu tĩnh, ASP.NET Core đọc `.Policy` qua `IAuthorizeData` nên nhận `null` ⇒ mọi `[RequirePermission]` thoái hoá thành `[Authorize]` trơn, không log, không lỗi. Mẫu đúng: `public RequirePermissionAttribute(string p) => Policy = $"Permission:{p}";`.

**Nút ẩn theo quyền phải kèm lời giải thích** (dải `Alert` chế độ chỉ xem) — không thì người dùng tưởng hệ thống hỏng.

**Trang lỗi 4xx thống nhất:** `ErrorPage` của `@cs/ui`; route `/401`, `/403`, `/404`, `*` → `/404`; lời gọi GET trả 401/403/404 qua `createApiClient` tự điều hướng, lời gọi ghi giữ lỗi để báo tại chỗ.

**Seed:** idempotent, không được ném lỗi (seed hỏng ⇒ tiến trình thoát ⇒ service chết). Danh mục người dùng tự thêm/xoá → chỉ gieo khi bảng TRỐNG. Học liệu nạp từ `content/` theo khoá tự nhiên, chạy lại không nhân đôi.

**Migration tự chạy lúc backend khởi động** theo cờ `AutoMigrate` — không dùng `dotnet ef database update` thủ công ngoài DB dev.

**Học liệu — bản quyền trước tiên:** chỉ dùng nguồn có giấy phép cho phép tái sử dụng; ghi nguồn + giấy phép + phần đã dùng vào `content/SOURCES.md`. Không rõ giấy phép ⇒ không dùng. Nghĩa tiếng Việt dịch máy phải đánh dấu `machine` cho tới khi được duyệt.

**Quy ước ngôn ngữ:** giản thể là mặc định; pinyin **lưu dạng số thanh** (`ni3 hao3`, thanh nhẹ `5`) làm nguồn sự thật, **hiển thị dạng dấu** (`nǐ hǎo`) qua tiện ích `@cs/utils`. Phần tử chứa chữ Hán đặt `lang="zh-CN"` + phông fallback CJK.

**Mobile-first:** mọi màn học phải dùng tốt ở ~375px — người học ôn thẻ trên điện thoại là chính.

---

## Tổng quan kiến trúc

Bản rút gọn của MedDental: **một** backend service + **một** frontend app, nhưng giữ nguyên khung monorepo để tách service/app về sau không phải đập đi làm lại.

### Backend — `backend/` (.NET 10, `backend.slnx`, Central Package Management)

- `Directory.Build.props` (net10.0, nullable, implicit usings) · `Directory.Packages.props` (version tập trung) · `global.json`.
- `shared/ChineseStudy.*` — thư viện dùng chung (Core, Logging, Security, HealthChecks, Auth).
- `services/learning-backend/` — DDD 4 lớp: `Domain → Application → Infrastructure → Api` + `tests/`.
- PostgreSQL (`chinese_study_dev` ở local), EF Core + Npgsql + snake_case, schema theo module.
- Xác thực: JWT tự phát (access 15 phút + refresh token xoay vòng) — **chưa** có auth-service OIDC/gateway; tách ra khi có app thứ hai.

### Frontend — `frontend/` (Turborepo + Yarn Classic Workspaces + React 19 + MUI v9 + TypeScript + Vite)

- `packages/@cs/tsconfig`, `@cs/ui` (theme, AppLayout, AppDialog, ErrorPage, useTabParam...), `@cs/api` (`createApiClient`), `@cs/auth` (AuthProvider, RequireAuth, RequirePermission), `@cs/utils` (pinyin, format, zod). Import thẳng TS source — không build/dist.
- `apps/web` — ứng dụng học viên + màn quản trị nội dung (ẩn theo quyền).

### Học liệu — `content/`

Dữ liệu JSON có schema (`content/schemas/`), script kiểm tra (`content/scripts/`), nguồn + giấy phép (`content/SOURCES.md`).

### Lệnh thường dùng

```bash
# Backend
dotnet build backend/backend.slnx -v q
dotnet test backend/backend.slnx
dotnet run --project backend/services/learning-backend/src/ChineseStudy.Learning.Api

# Frontend (chạy trong frontend/)
yarn install
yarn workspace @cs/web dev
yarn workspace @cs/web tsc -b
```

> Chi tiết cổng, cấu trúc thư mục, lệnh chạy chính xác được cập nhật bởi hợp đồng F0 trong `docs/agent-workflow/`.
