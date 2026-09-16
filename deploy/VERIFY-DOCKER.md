# Verify Docker — checklist chạy trên máy CÓ Docker (F0/F1 chỉ viết đúng, chưa chạy được)

> Máy dev khi viết F0 (Windows) KHÔNG có Docker. Các mục dưới đây phải chạy trên máy có
> Docker Engine ≥ 23 (Compose v2) — dự kiến là MacBook người dùng chuyển sang. Đánh dấu
> `[x]` khi đã verify thật, ghi ngày + máy đã chạy.

## 0. Docker Compose dev (`deploy/dev/docker-compose.dev.yml`) — CHƯA VERIFY

> Bổ sung cùng đợt F0 (16/09/2026) để phát triển trên máy có Docker (MacBook) thay vì cài
> PostgreSQL/MinIO trực tiếp lên máy. Chưa verify vì máy viết code không có Docker.

1. [ ] `docker compose -f deploy/dev/docker-compose.dev.yml --env-file deploy/dev/.env config` không lỗi (sau khi `cp deploy/dev/.env.example deploy/dev/.env`).
2. [ ] `docker compose -f deploy/dev/docker-compose.dev.yml --env-file deploy/dev/.env up -d` ⇒ `postgres`, `minio`, `minio-init` lên; `minio-init` chạy xong rồi tự thoát (exit 0), không phải "unhealthy".
3. [ ] `docker compose -f deploy/dev/docker-compose.dev.yml ps` ⇒ `postgres` và `minio` đều `healthy`.
4. [ ] `psql -h localhost -p 5432 -U postgres -l` (mật khẩu trong `deploy/dev/.env`) liệt kê đủ `af_identity`, `af_chinese`.
5. [ ] Mở `http://localhost:9001` (MinIO Console), đăng nhập bằng `MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD` trong `.env` ⇒ thấy bucket `af-chinese` đã được tạo sẵn.
6. [ ] Copy `appsettings.Development.json.example` → `appsettings.Development.json` cho cả hai service backend, điền đúng mật khẩu trong `deploy/dev/.env` ⇒ `dotnet run` cả ba service (§ README) ⇒ `/health/ready` = 200 cho cả identity-service và chinese-backend.
7. [ ] `docker compose -f deploy/dev/docker-compose.dev.yml down` rồi `up -d` lại ⇒ dữ liệu Postgres/MinIO còn nguyên (volume đặt tên).

## 1. Bundle production (`deploy/docker-compose.yml`)

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

## 2. HTTPS — cần server có DNS `id.`/`chinese.antfarms.xyz` trỏ về + Docker + cổng 80/443 mở (chưa verify được ở F0)

11. [ ] `./scripts/self-signed.sh id.antfarms.xyz` ⇒ `docker compose up -d nginx` lên được.
12. [ ] `./scripts/get-cert.sh <email> id.antfarms.xyz chinese.antfarms.xyz` thành công; `openssl x509 -in certs/live/fullchain.pem -noout -text | grep -A1 "Subject Alternative Name"` có đủ 2 tên; `Public Key Algorithm: rsaEncryption`.
13. [ ] `curl -I http://chinese.antfarms.xyz/abc` ⇒ `301` sang `https://chinese.antfarms.xyz/abc`; `curl -I http://id.antfarms.xyz/.well-known/acme-challenge/x` ⇒ 404 (không bị chuyển hướng).
14. [ ] `curl -I https://id.antfarms.xyz/.well-known/jwks.json` ⇒ 200 + `Strict-Transport-Security: max-age=31536000` (không `includeSubDomains`).
15. [ ] SSL Labs (ssllabs.com/ssltest) cho `id.` và `chinese.` ⇒ hạng A trở lên, chỉ TLS 1.2/1.3.
16. [ ] `docker compose exec certbot certbot renew --dry-run` thành công; cron `renew-cert.sh` đã cài (`crontab -l`); chạy tay `./scripts/renew-cert.sh id.antfarms.xyz` ⇒ "không đổi".
17. [ ] Nếu bật proxy Cloudflare: SSL/TLS mode = Full (strict); truy cập không lặp chuyển hướng; log identity ghi IP thật của máy gọi (không phải IP Cloudflare).

Mỗi feature sau có đụng Docker thì bổ sung dòng vào checklist này.

## 3. Frontend `chinese-frontend` (từ F1) — CHƯA VERIFY

- [ ] `docker compose build chinese-frontend` thành công; bước `yarn install --frozen-lockfile` không báo thiếu module (peer dependency đủ).
- [ ] `https://chinese.antfarms.xyz/` trả `index.html` (có `<div id="root">`); `curl -I .../assets/<file>.js` ⇒ `application/javascript`.
- [ ] Tạo thử `x.mjs` trong `/usr/share/nginx/html` rồi `curl -I` ⇒ `Content-Type: application/javascript` (khối `.mjs`).
- [ ] `https://chinese.antfarms.xyz/abc` ⇒ 200 `index.html` (SPA fallback), trình duyệt hiện trang 404 có "Về trang chủ".
- [ ] `docker run --rm --entrypoint sh <ảnh chinese-frontend> -c 'grep -rl "id.antfarms.xyz" /usr/share/nginx/html | head -1'` có kết quả (biến `VITE_IDENTITY_API_URL` nướng đúng lúc build).
