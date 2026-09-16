# AntFarm

Nền tảng học ngoại ngữ trực tuyến cho người Việt, kiến trúc monorepo đa service (giống
`mdt-re-construct`). **Mỗi ngôn ngữ là một service + một app riêng**; tiếng Trung là
ngôn ngữ đầu tiên.

```
Trình duyệt ─► app (Vite) :3280 ─proxy /identity,/chinese─► gateway :5280
                                                                 ├─► identity-service :5281 ─► af_identity
                                                                 └─► chinese-backend  :5282 ─► af_chinese
```

Production đi qua nginx biên + HTTPS Let's Encrypt trước gateway — xem `deploy/`.

## 1. Yêu cầu

- .NET SDK 10 (≥ 10.0.100)
- Node ≥ 22.12, Yarn 1.22 (frontend — bổ sung ở F1)
- PostgreSQL 18 — **hai cách chạy** (chọn một):
  - **Docker** (khuyến nghị, đặc biệt trên macOS): xem mục 2a.
  - **Cài trực tiếp trên máy** (vd Windows chưa có Docker): xem mục 2b.

## 2a. Chạy PostgreSQL + MinIO bằng Docker (macOS/Linux trước, Windows sau)

> Dùng khi máy dev có Docker Desktop/Engine. Đây là đường khuyến nghị — không phải cài
> PostgreSQL trực tiếp lên máy, không phải tự tạo database bằng tay.

**macOS / Linux (bash, zsh):**

```bash
cp deploy/dev/.env.example deploy/dev/.env
docker compose -f deploy/dev/docker-compose.dev.yml --env-file deploy/dev/.env up -d
docker compose -f deploy/dev/docker-compose.dev.yml ps        # đợi postgres + minio "healthy"
```

**Windows (PowerShell):**

```powershell
Copy-Item deploy\dev\.env.example deploy\dev\.env
docker compose -f deploy/dev/docker-compose.dev.yml --env-file deploy/dev/.env up -d
docker compose -f deploy/dev/docker-compose.dev.yml ps
```

Compose này dựng:
- **PostgreSQL 18** (cổng 5432) — script `deploy/postgres/init/01-create-databases.sh` tự tạo
  role + database `af_identity`, `af_chinese` (mật khẩu trong `deploy/dev/.env`) **chỉ khi
  volume còn trống**.
- **MinIO** (API 9000, Console 9001, tài khoản `minioadmin`/`minioadmin` mặc định) + bucket
  `af-chinese` tạo sẵn qua `minio-init`. **F0 chưa có code nào dùng MinIO** — chỉ dựng sẵn hạ
  tầng cho feature sau (vd F8 luyện viết, F10 quản trị nội dung).

⚠️ **Chưa verify bằng Docker thật** — máy viết F0 không có Docker. Verify khi có máy Docker
(dự kiến MacBook): checklist đầy đủ ở `deploy/VERIFY-DOCKER.md` mục 0.

Dừng: `docker compose -f deploy/dev/docker-compose.dev.yml down` (giữ dữ liệu) hoặc thêm `-v`
để xoá sạch volume.

## 2b. Cách phụ — PostgreSQL cài trực tiếp trên máy (không Docker, vd Windows)

```powershell
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
& $psql -U postgres -c "CREATE DATABASE af_identity ENCODING 'UTF8' TEMPLATE template0;"
& $psql -U postgres -c "CREATE DATABASE af_chinese  ENCODING 'UTF8' TEMPLATE template0;"
```

Dùng tài khoản `postgres` cho đơn giản (khác Docker dev — ở đó mỗi service có role
riêng `af_identity`/`af_chinese`, đúng mô hình production).

## 3. Cấu hình `appsettings.Development.json`

Copy file mẫu cho **cả hai** service backend, điền mật khẩu đúng với cách bạn chọn ở
bước 2 (Docker dev: giữ nguyên giá trị mặc định trong file `.example`; PostgreSQL cài
trực tiếp: đổi `Username=postgres;Password=<mật khẩu của bạn>`):

```bash
cp backend/services/identity-service/src/AntFarm.Identity.Api/appsettings.Development.json.example \
   backend/services/identity-service/src/AntFarm.Identity.Api/appsettings.Development.json

cp backend/services/chinese-backend/src/AntFarm.Chinese.Api/appsettings.Development.json.example \
   backend/services/chinese-backend/src/AntFarm.Chinese.Api/appsettings.Development.json
```

(PowerShell: dùng `Copy-Item` thay `cp`.)

## 4. Backend

```bash
dotnet tool restore
dotnet build backend/backend.slnx -v q
dotnet test backend/backend.slnx
```

Chạy 3 tiến trình (3 cửa sổ terminal riêng):

```bash
dotnet run --project backend/services/identity-service/src/AntFarm.Identity.Api --launch-profile http
dotnet run --project backend/services/chinese-backend/src/AntFarm.Chinese.Api --launch-profile http
dotnet run --project backend/services/gateway --launch-profile http
```

## 5. Kiểm tra

```
http://localhost:5280/identity/api/system/info   → { "service": "identity-service", ... }
http://localhost:5280/chinese/api/system/info    → { "service": "chinese-backend", ... }
http://localhost:5282/scalar/v1                  → tài liệu API chinese-backend
http://localhost:5281/health/ready               → 200 khi PostgreSQL kết nối được
```

## 5b. Xác thực (F2 — identity-service)

Chạy `identity-service` (mục 4) rồi thử nhanh bằng `curl`:

```bash
curl -i -X POST http://localhost:5281/api/auth/register \
  -H "Content-Type: application/json" -H "Origin: http://localhost:3280" \
  -d '{"email":"ban@vidu.com","password":"mat-khau-du-dai","displayName":"Bạn","timeZone":"Asia/Ho_Chi_Minh"}'
```

- Trả `201` + `Set-Cookie: af_rt=...; path=/identity/api/auth; httponly; samesite=strict` (dev
  không có `domain=`, không có `secure` — đúng bảng §5.6.2 của hợp đồng) + JSON có `accessToken`.
- `POST /api/auth/login`, `POST /api/auth/refresh` (đọc cookie `af_rt`), `POST /api/auth/logout`,
  **`POST /api/auth/password`** (đổi mật khẩu — **không phải** `/api/account/password`: cookie
  `af_rt` chỉ có `Path=/api/auth` nên route đổi mật khẩu phải nằm trong nhánh này để giữ được
  phiên đăng nhập hiện tại khi thu hồi các phiên khác, quyết định D21 ngày 17/09/2026).
- `GET/PUT /api/account` cần header `Authorization: Bearer <accessToken>`.
- `GET http://localhost:5281/.well-known/jwks.json` — khoá ký công khai (RS256), dùng bởi
  `chinese-backend` từ F3 để kiểm token.
- Thiếu `Origin` hợp lệ ở mọi `POST /api/auth/*` ⇒ `403 ORIGIN_NOT_ALLOWED` (R-A7b, chống CSRF
  qua cookie liên-subdomain).

⚠️ **Khoá ký RSA** (`Jwt:KeysPath`, mặc định `.secrets/identity/keys/` ở gốc repo, đã gitignore):
Development tự sinh khi thư mục trống; **mất thư mục này ⇒ mọi access token cũ hết hiệu lực**
(phải đăng nhập lại — refresh token trong DB không mất nên chỉ cần làm mới phiên, không mất dữ
liệu học).

## 5c. Phân quyền cục bộ (F3 — chinese-backend)

`chinese-backend` kiểm access token qua JWKS của `identity-service` (mạng nội bộ, `Auth:JwksUrl`
trong `appsettings.json`) — **chạy `identity-service` TRƯỚC** `chinese-backend` (RK9: JWKS tải
lười + tự thử lại nên chạy sai thứ tự không crash, chỉ 401 tới khi tải được).

Điền email của bạn vào `ChineseAdmin:BootstrapEmails` trong
`appsettings.Development.json` của chinese-backend (đã có sẵn khoá mẫu trong `.example`) để
được gán vai trò `admin` ngay lần đăng nhập đầu tiên (R-P6) — không điền thì tài khoản mới chỉ
nhận `learner` (`ChineseAccess:DefaultRoles`).

```bash
TOKEN=$(curl -s -X POST http://localhost:5281/api/auth/login \
  -H "Content-Type: application/json" -H "Origin: http://localhost:3280" \
  -d '{"email":"ban@vidu.com","password":"mat-khau-du-dai"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['accessToken'])")

curl -s http://localhost:5282/api/me -H "Authorization: Bearer $TOKEN"
curl -i http://localhost:5282/api/admin/ping -H "Authorization: Bearer $TOKEN"
```

- `GET /api/me` (`[Authorize]`, mọi người dùng đã đăng nhập) →
  `{ "id", "email", "displayName", "timeZone", "roles": ["learner"], "permissions": ["study.use"], "firstSeenAt" }`
  — lần đầu gọi tự **provision** dòng trong `access.users` + gán vai trò mặc định (R-P4/R-P5).
- `GET /api/admin/ping` (`users.manage`) → `{ "ok": true }` cho admin; learner ⇒ `403`
  `{ "error", "code": "FORBIDDEN" }`.
- Không có/hỏng Bearer token ⇒ `401 { "error", "code": "UNAUTHENTICATED" }` (kể cả đường dẫn
  không tồn tại — `FallbackPolicy = RequireAuthenticatedUser` áp cho MỌI endpoint không có
  `[AllowAnonymous]`, xem test `HealthAndInfoTests.DuongDanKhongTonTai_ChuaXacThuc_TraVe401`).
- Gỡ hết vai trò một người dùng trong DB (`access.user_roles`) ⇒ `/api/me` của người đó trả
  `roles: [], permissions: []` ngay lập tức (MeService đọc thẳng DB, không qua cache 60 giây của
  `PermissionResolver` — R-P8).
- **Danh mục vai trò/quyền đầy đủ** (`GET /api/admin/roles`, `GET /api/admin/users`,
  `PUT /api/admin/users/{id}/roles`) là **F4**, chưa có ở F3 — xem mục bàn giao.

## 6. Test tích hợp DB

```bash
export AF_TEST_PG="Host=localhost;Port=5432;Username=postgres;Password=<mật khẩu>"   # Docker dev: dùng POSTGRES_PASSWORD trong deploy/dev/.env
dotnet test backend/backend.slnx
```

(PowerShell: `$env:AF_TEST_PG = "..."`.) Thiếu biến này thì test cần DB tự `Skip`, không fail.

## 7. Frontend

**(F1 bổ sung)**

## 8. Triển khai production

Xem `deploy/` — `docker-compose.yml`, nginx biên + HTTPS Let's Encrypt, checklist verify ở
`deploy/VERIFY-DOCKER.md`. Tóm tắt kiến trúc + quy ước đầy đủ: `CLAUDE.md` mục "Triển khai —
`deploy/`" và hợp đồng
`docs/agent-workflow/2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md`.

## 9. Khắc phục sự cố

- **Cổng bị chiếm**: đổi `applicationUrl` trong `Properties/launchSettings.json` của service
  liên quan + cluster tương ứng trong `backend/services/gateway/appsettings.json` (frontend:
  `vite.config.ts`, bổ sung F1).
- **`/health/ready` trả 503**: PostgreSQL chưa chạy, hoặc sai mật khẩu trong
  `appsettings.Development.json` — xem `checks.postgres.description` trong response JSON.
- **Gateway trả 502**: service phía sau (`identity-service`/`chinese-backend`) chưa chạy hoặc
  đã crash — xem log cửa sổ terminal tương ứng.
- **`AddInfrastructure` ném lỗi lúc khởi động**: thiếu `appsettings.Development.json` (chưa
  copy từ `.example`) — thông điệp lỗi tiếng Việt đã ghi rõ đường sửa.
- **`identity-service` ném "Không tìm thấy khoá ký RS256..." lúc khởi động**: chỉ tự sinh khoá ở
  môi trường `Development` (biến `ASPNETCORE_ENVIRONMENT`) — môi trường khác thiếu file `.pem`
  trong `Jwt:KeysPath` là dừng hẳn theo chủ đích (R-A13), không fallback.
- **Đăng ký/đăng nhập trả `403 ORIGIN_NOT_ALLOWED`**: header `Origin` (hoặc `Referer`) của
  request không nằm trong `Auth:AllowedOrigins` — dev mặc định `http://localhost:3280`/`:3281`.
