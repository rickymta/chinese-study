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
