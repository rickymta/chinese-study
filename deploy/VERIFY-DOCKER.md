# Verify Docker — checklist chạy trên máy CÓ Docker (F0/F1 chỉ viết đúng, chưa chạy được)

> Máy dev khi viết F0 (Windows) KHÔNG có Docker. Các mục dưới đây phải chạy trên máy có
> Docker Engine ≥ 23 (Compose v2) — dự kiến là MacBook người dùng chuyển sang. Đánh dấu
> `[x]` khi đã verify thật, ghi ngày + máy đã chạy.

## 0. Docker Compose dev (`deploy/dev/docker-compose.dev.yml`) — ĐÃ VERIFY 17/09/2026 (MacBook, Docker 29.7.2)

> Bổ sung cùng đợt F0 (16/09/2026) để phát triển trên máy có Docker (MacBook) thay vì cài
> PostgreSQL/MinIO trực tiếp lên máy. Verify lần đầu lộ lỗi mount volume của PG 18 (phải mount
> `/var/lib/postgresql`, không phải `.../data`) — đã sửa.
>
> Lưu ý MacBook: nếu `docker pull`/`compose up` treo im lặng ở bước tải image, nguyên nhân thường là
> credential helper `desktop` của Docker Desktop — thử `DOCKER_CONFIG=<thư mục có config.json không có "credsStore">`.

1. [x] `docker compose -f deploy/dev/docker-compose.dev.yml --env-file deploy/dev/.env config` không lỗi (sau khi `cp deploy/dev/.env.example deploy/dev/.env`).
2. [x] `docker compose -f deploy/dev/docker-compose.dev.yml --env-file deploy/dev/.env up -d` ⇒ `postgres`, `minio`, `minio-init` lên; `minio-init` chạy xong rồi tự thoát (exit 0), không phải "unhealthy".
3. [x] `docker compose -f deploy/dev/docker-compose.dev.yml ps` ⇒ `postgres` và `minio` đều `healthy`.
4. [x] `psql -h localhost -p 5432 -U postgres -l` (mật khẩu trong `deploy/dev/.env`) liệt kê đủ `af_identity`, `af_chinese`.
5. [x] (kiểm bằng log `minio-init` — `mc ls` thấy `af-chinese/`; chưa mở Console bằng trình duyệt) Mở `http://localhost:9001` (MinIO Console), đăng nhập bằng `MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD` trong `.env` ⇒ thấy bucket `af-chinese` đã được tạo sẵn.
6. [x] Copy `appsettings.Development.json.example` → `appsettings.Development.json` cho cả hai service backend, điền đúng mật khẩu trong `deploy/dev/.env` ⇒ `dotnet run` cả ba service (§ README) ⇒ `/health/ready` = 200 cho cả identity-service và chinese-backend.
7. [x] `docker compose -f deploy/dev/docker-compose.dev.yml down` rồi `up -d` lại ⇒ dữ liệu Postgres/MinIO còn nguyên (volume đặt tên).

## 1. Bundle production (`deploy/docker-compose.yml`)

1. [ ] `docker compose -f deploy/docker-compose.yml config` không lỗi.
2. [~] (17/09/2026 MacBook: `docker build` tay từng ảnh identity-service, gateway, chinese-frontend OK sau khi bỏ `groupadd`; chinese-backend OK qua compose ở F5 — chưa kiểm cache lần 2) `docker compose build identity-service` / `chinese-backend` / `gateway` / `chinese-frontend` **từng cái một**; build lần 2 không đổi `.csproj` ⇒ bước restore dùng cache.
3. [x] (17/09/2026: identity-service, gateway, chinese-backend — rỗng; chạy bằng user `app`) `docker run --rm --entrypoint sh <ảnh> -c 'ls /app | grep -i appsettings.Development'` rỗng.
4. [ ] `docker compose up -d` ⇒ mọi container `healthy` (`docker compose ps`).
5. [ ] `docker compose ps` chỉ `nginx` có cột PORTS ra host.
6. [ ] Trên máy verify, thêm vào file hosts `127.0.0.1 chinese.antfarms.xyz id.antfarms.xyz`, dùng chứng chỉ tự ký (`deploy/certs/`, gitignore) ⇒ `https://chinese.antfarms.xyz/chinese/api/system/info` = 200; `https://id.antfarms.xyz/.well-known/jwks.json` = 200 (từ F2); `https://chinese.antfarms.xyz/` trả trang app (từ F1); một file `.mjs` trả `Content-Type: application/javascript`.
7. [ ] `curl -H "X-Forwarded-For: 1.2.3.4" ...` ⇒ log identity ghi IP thật của máy gọi, không phải `1.2.3.4` (từ F2).
8. [ ] Đăng nhập ở `chinese.antfarms.xyz` (bundle build với `VITE_IDENTITY_API_URL=https://id.antfarms.xyz/api`) ⇒ request tới `id.antfarms.xyz` qua CORS thành công; cookie `af_rt` có `Domain=.antfarms.xyz; Secure; HttpOnly; SameSite=Strict; Path=/api/auth`; F5 vẫn đăng nhập (từ F2).
8b. [ ] `curl -i -X OPTIONS https://id.antfarms.xyz/api/auth/refresh -H 'Origin: https://chinese.antfarms.xyz' -H 'Access-Control-Request-Method: POST'` ⇒ `Access-Control-Allow-Origin: https://chinese.antfarms.xyz` (không `*`), `Access-Control-Allow-Credentials: true`; đổi `Origin` thành `https://evil.example` ⇒ không có header CORS, POST trả 403 (từ F2).
8c. [x] (17/09/2026: build tay với `--build-arg VITE_IDENTITY_API_URL=https://id.antfarms.xyz/api` ⇒ có trong `assets/index-*.js`; `nginx -t` OK) `grep -r "id.antfarms.xyz" ` trong `/usr/share/nginx/html` của ảnh `chinese-frontend` có kết quả (biến build đã nướng đúng) (từ F2).
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

## 4. Học liệu pinyin trong ảnh `chinese-backend` (F5, RK24/RK38) — VERIFY MỘT PHẦN (17/09/2026, MacBook, Docker Desktop)

> Đã kiểm (đánh `[x]`): build qua compose (`additional_contexts`), 4 file trong `/content/chinese/data/pinyin`, `/app` không có
> học liệu, chạy `docker run` (DB dev qua `host.docker.internal`, `Content__RootPath=/content/chinese`) ⇒ `/health/live` 200 +
> log "Nạp học liệu pinyin thành công", user chạy là `app` (UID 1654). Lần build đầu **hỏng** vì `groupadd app` trùng user có
> sẵn của ảnh .NET 8+ ⇒ đã bỏ `groupadd/useradd` khỏi cả 3 Dockerfile .NET (chinese-backend, identity-service, gateway — soát
> lại 17/09/2026, không service nào còn dòng này). Các mục `[ ]` còn lại chưa chạy.

- [x] `docker compose -f deploy/docker-compose.yml config` không lỗi với `additional_contexts` (cần Docker Compose ≥ 2.17 — RK38; `docker compose version` kiểm trước).
- [ ] Build tay (không qua compose): từ `backend/`, `docker build -f services/chinese-backend/src/AntFarm.Chinese.Api/Dockerfile --build-context content=../content .` thành công.
- [x] `docker compose build chinese-backend` thành công (Compose 5.3.1 trên máy dev). [ ] `docker compose up -d chinese-backend` (mới thử bằng `docker run`).
- [x] (kiểm bằng `docker run --entrypoint sh`) `docker compose exec chinese-backend ls /content/chinese/data/pinyin` ⇒ đủ 4 file (`initials.json`, `finals.json`, `syllables.json`, `guide.json`).
- [x] (kiểm bằng `docker run --entrypoint sh`) `docker compose exec chinese-backend ls /app | grep -i content` ⇒ RỖNG (đường dẫn học liệu Docker là `/content/chinese`, KHÔNG copy vào `/app` như dev local — tránh nhầm `Content:RootPath` mặc định của `appsettings.json`).
- [ ] `curl http://localhost:<port hoặc qua gateway>/chinese/api/pinyin/chart` (đăng nhập trước) ⇒ 200, KHÔNG 503 `CONTENT_UNAVAILABLE`.
- [x] Log khởi động có dòng "Nạp học liệu pinyin thành công" (Serilog Information) — không có "Nạp học liệu pinyin ... thất bại" (Error).
- [ ] Xoá tạm `content/chinese/data/pinyin/guide.json`, build lại ⇒ log Error rõ ràng lúc khởi động, `/health/live` vẫn 200, `GET /chinese/api/pinyin/chart` = 503 `CONTENT_UNAVAILABLE` JSON; hoàn tác.

## 5. Múi giờ (`tzdata`) trong ảnh identity-service/chinese-backend — RK36

> RK36 cảnh báo ảnh `mcr.microsoft.com/dotnet/aspnet` có thể THIẾU `tzdata`, khiến
> `TimeZoneInfo.TryFindSystemTimeZoneById`/`TryConvertIanaIdToWindowsId` từ chối MỌI múi giờ IANA
> (kể cả `Asia/Ho_Chi_Minh`) khi chạy trong container — người dùng không đổi được múi giờ ở `/ho-so`,
> `UserLocalDate`/`TimeZoneCatalog` phải fallback mặc định. Kiểm bằng ảnh có sẵn (không cần build lại):

- [x] (17/09/2026, MacBook, Docker Desktop 4.86.0 — `DOCKER_CONFIG` trỏ scratchpad, ảnh `af-identity:verify`) `docker run --rm --entrypoint ls af-identity:verify /usr/share/zoneinfo/Asia/Ho_Chi_Minh` ⇒ in ra đường dẫn (exit 0) — **CÓ `tzdata`**, không cần sửa Dockerfile identity-service.
- [x] (17/09/2026, MacBook — ảnh `antfarm/chinese-backend:latest`) `docker run --rm --entrypoint ls antfarm/chinese-backend:latest /usr/share/zoneinfo/Asia/Ho_Chi_Minh` ⇒ in ra đường dẫn (exit 0) — **CÓ `tzdata`**, không cần sửa Dockerfile chinese-backend.
- Kết luận: `mcr.microsoft.com/dotnet/aspnet:10.0` (base image runtime hiện dùng) đã kèm sẵn `tzdata` — KHÔNG cần thêm `apt-get install tzdata` vào Dockerfile của hai service. Nếu sau này đổi base image (vd `-alpine`, hay hạ xuống .NET 8 runtime `-noble-chiseled`...) phải chạy lại đúng 2 lệnh trên trước khi tin múi giờ hoạt động — ảnh chiselled/distroless thường CẮT `tzdata` để giảm kích thước.
- [ ] Kiểm thêm bằng ứng dụng THẬT (chưa làm — cần chạy container với DB thật): đăng nhập, đổi múi giờ ở `/ho-so` sang một múi giờ không phải `Asia/Ho_Chi_Minh` (vd `Europe/Berlin`) ⇒ `PUT /identity/api/account` trả 200 (không 422 `INVALID_TIME_ZONE`) khi chạy TỪ CONTAINER (không phải `dotnet run` trên máy host).

Mỗi feature sau có đụng Docker thì bổ sung dòng vào checklist này.

> **F2 — phần tương đương đã kiểm ở dev local (không Docker, không HTTPS), 17/09/2026 MacBook:**
> qua gateway `:5280` — `/identity/.well-known/jwks.json` = 200; preflight `OPTIONS` với
> `Origin: http://localhost:3280` có `Access-Control-Allow-Origin` đúng origin + `Allow-Credentials: true`;
> `Origin` lạ ⇒ không header CORS, `POST` ⇒ 403 `ORIGIN_NOT_ALLOWED`; cookie `af_rt` HttpOnly,
> `SameSite=Strict`, `Path=/identity/api/auth`, không `Domain`; restart identity-service ⇒ refresh vẫn 200
> (khoá `.secrets/identity/keys` giữ nguyên `kid`). Các mục (từ F2) ở mục 2 vẫn **CHƯA VERIFY** vì cần
> Docker + tên miền thật (`Domain=.antfarms.xyz; Secure`, CORS cross-subdomain, volume `/keys`).

## 3. Frontend `chinese-frontend` (từ F1) — CHƯA VERIFY

- [ ] `docker compose build chinese-frontend` thành công; bước `yarn install --frozen-lockfile` không báo thiếu module (peer dependency đủ).
- [ ] `https://chinese.antfarms.xyz/` trả `index.html` (có `<div id="root">`); `curl -I .../assets/<file>.js` ⇒ `application/javascript`.
- [ ] Tạo thử `x.mjs` trong `/usr/share/nginx/html` rồi `curl -I` ⇒ `Content-Type: application/javascript` (khối `.mjs`).
- [ ] `https://chinese.antfarms.xyz/abc` ⇒ 200 `index.html` (SPA fallback), trình duyệt hiện trang 404 có "Về trang chủ".
- [ ] `docker run --rm --entrypoint sh <ảnh chinese-frontend> -c 'grep -rl "id.antfarms.xyz" /usr/share/nginx/html | head -1'` có kết quả (biến `VITE_IDENTITY_API_URL` nướng đúng lúc build).

## 6. Bundle triển khai — bổ sung 17/09/2026 (F13 devops, soát theo khuôn MedDental) — CHƯA VERIFY bằng Docker thật

> Đợt này chỉnh sửa `deploy/docker-compose.yml`/`.env.example`/`conf/nginx.conf.example`, thêm
> `deploy/README.md` (runbook), `deploy/scripts/{preflight,backup-db}.sh`. Đã kiểm bằng
> `docker compose config -q`, `bash -n`, `nginx -t` (container `nginx:1.27-alpine`, xem dưới) —
> **CHƯA chạy `up -d` thật trên server có DNS + Docker.**

- [x] `docker compose -f deploy/docker-compose.yml --env-file deploy/.env.example config -q` không lỗi (REGISTRY mặc định đổi sang `localhost/antfarm`, thêm `x-logging`, `shm_size`, `logging:` mỗi service).
- [x] `frontend/apps/chinese/nginx.conf` (header bảo mật thêm) ⇒ `nginx -t` qua container `nginx:1.27-alpine` thành công.
- [x] `deploy/conf/nginx.conf.example` (thay `<ID_DOMAIN>`/`<CHINESE_DOMAIN>` bằng giá trị thật + chứng chỉ tự ký tạm trong scratchpad) ⇒ `nginx -t` thành công.
- [x] `bash -n` sạch cho toàn bộ `deploy/scripts/*.sh` + `deploy/postgres/init/*.sh`.
- [ ] `./scripts/preflight.sh` chạy thật trên máy đã `cp .env.example .env` + `cp conf/nginx.conf.example conf/nginx.conf` ⇒ báo đúng các mục thiếu, không báo nhầm.
- [ ] `REGISTRY` để trống ⇒ `docker compose pull <svc>` báo lỗi RÕ RÀNG (không kéo nhầm ảnh Docker Hub công khai) — xác nhận `docker compose config` in ra `image: localhost/antfarm/...`.
- [ ] Tạo khoá ký RSA vào volume `identity-keys` theo `deploy/README.md` mục 7 (`openssl genpkey`, UID 1654) ⇒ `identity-service` (Production) khởi động được, không ném "Không tìm thấy khoá ký RS256...".
- [ ] `deploy/README.md` mục 9 (tạo tài khoản đầu tiên qua bật/tắt `AUTH_ALLOW_REGISTRATION`) ⇒ đăng nhập ở `chinese.antfarms.xyz` thấy vai trò `admin` (`GET /api/me`).

### Sao lưu / phục hồi (B3) — CHƯA diễn tập trên máy thật

- [ ] `./scripts/backup-db.sh` chạy thành công trên server thật (compose dev không đại diện đúng — cần volume `identity-keys` + role `af_identity`/`af_chinese` như production) ⇒ tạo đủ `globals.sql`, `af_identity.dump`, `af_chinese.dump`, `identity-keys.tar.gz` trong `deploy/backups/<ts>/`.
- [ ] Cron `backup-db.sh` đã cài (`crontab -l`), chạy được ít nhất một đêm không lỗi (xem `/var/log/af-backup-db.log`).
- [ ] **Diễn tập phục hồi đầy đủ trên máy/VM KHÁC** (theo `deploy/README.md` mục 11): `pg_restore` cả hai DB + giải nén `identity-keys.tar.gz` ⇒ đăng nhập lại được, `/api/me` + tra từ điển hoạt động bình thường. Đánh dấu `[x]` kèm ngày + máy đã diễn tập — bản sao lưu chưa từng phục hồi thử coi như không tồn tại.

## 7. Dữ liệu nét chữ `/hanzi-data/` trong ảnh `chinese-frontend` (F8) — đã verify ảnh trên máy dev, CHƯA verify trên server

> F8 đóng gói tập con `hanzi-writer-data@2.0.1` vào `frontend/apps/chinese/public/hanzi-data/` (commit vào git,
> `COPY apps/chinese` của Dockerfile đã gồm `public/`) và thêm `location /hanzi-data/` + `/licenses/` vào
> `apps/chinese/nginx.conf` (`try_files $uri =404` — KHÔNG rơi về `index.html`). Mới kiểm `nginx -t` qua container
> `nginx:1.27-alpine` trên máy dev (17/09/2026). Ảnh thật đã build + chạy thử trên máy dev (17/09/2026, không qua
> nginx biên/HTTPS); các mục `https://chinese.antfarms.xyz/...` bên dưới vẫn chờ server.

- [x] (17/09/2026, MacBook) `docker run --rm -v "$PWD/apps/chinese/nginx.conf":/etc/nginx/conf.d/default.conf:ro nginx:1.27-alpine nginx -t` (chạy trong `frontend/`) ⇒ "syntax is ok / test is successful".
- [x] (17/09/2026, MacBook) `docker build -f apps/chinese/Dockerfile --build-arg VITE_IDENTITY_API_URL=https://id.antfarms.xyz/api frontend` + `docker run -p 127.0.0.1:18080:80` ⇒ `hanzi-data` 303 file (300 chữ + 3), có `licenses/hanzi-writer.LICENSE.txt`; `curl -I /hanzi-data/7231.json` ⇒ 200 `application/json`, đúng MỘT dòng `Cache-Control: public, max-age=604800`, không `Expires`, có `nosniff`; `/hanzi-data/ffff.json` ⇒ 404 (không phải `index.html`); `ARPHICPL.TXT` và `/licenses/hanzi-writer.LICENSE.txt` ⇒ 200 `text/plain`; `/assets/*.js` ⇒ một dòng `Cache-Control: public, max-age=31536000, immutable`. Container/ảnh tạm đã xoá.
- [ ] `docker compose build chinese-frontend` (trên server) ⇒ `docker run --rm --entrypoint sh <ảnh> -c 'ls /usr/share/nginx/html/hanzi-data | wc -l'` ≈ số chữ + 3 (`index.json`, `ARPHICPL.TXT`, `NOTICE.md`); có `/usr/share/nginx/html/licenses/hanzi-writer.LICENSE.txt`.
- [ ] `curl -I https://chinese.antfarms.xyz/hanzi-data/7231.json` ⇒ `200`, `Content-Type: application/json`, `Cache-Control: public, max-age=604800` (không `immutable`), có `X-Content-Type-Options: nosniff`.
- [ ] `curl -I https://chinese.antfarms.xyz/hanzi-data/ffff.json` ⇒ `404` (KHÔNG phải 200 `text/html` của `index.html`).
- [ ] (F10 bổ sung, mới kiểm `nginx -t` 17/09/2026) `curl -I https://chinese.antfarms.xyz/hanzi-data/index.json` ⇒ `200`, đúng MỘT dòng `Cache-Control: no-cache` (khối `location =` riêng — danh mục chữ đổi khi thêm chữ, không được cache 7 ngày), vẫn có `nosniff`/`X-Frame-Options`/`Referrer-Policy`; các file nét `/hanzi-data/<mã>.json` vẫn `max-age=604800`.
- [ ] `curl -I https://chinese.antfarms.xyz/hanzi-data/ARPHICPL.TXT` ⇒ 200 `text/plain`; `/licenses/hanzi-writer.LICENSE.txt` ⇒ 200.
- [ ] Mở `/luyen-viet/爱` trên trình duyệt, DevTools Network suốt phiên luyện: KHÔNG có request tới `cdn.jsdelivr.net` (R-W1); `/hanzi-data/7231.json` 200 và lần tải trang sau lấy từ cache.

## 8. cms-backend (W1) — CHƯA VERIFY bằng Docker (máy viết W1 không có Docker)

> `backend/services/cms-backend` (DDD 4 lớp, chép khuôn `chinese-backend`) — DB `af_cms` riêng,
> schema `access`, audience `af-cms`, route gateway `/cms/**`. `dotnet build`/`dotnet test`
> (`AF_TEST_PG` chưa đặt trên máy này ⇒ 25 ApiTests cần DB tự Skip, 4 test không-DB + 12 UnitTests
> xanh) — xem chi tiết bàn giao W1. Migration `W1_Access` đã sinh được ở chế độ THIẾT KẾ (không cần
> Postgres thật kết nối lúc `dotnet ef migrations add` — chỉ cần `ConnectionStrings:Default` có
> ĐÚNG ĐỊNH DẠNG, không cần đúng mật khẩu).

- [ ] `docker compose build cms-backend` thành công (build context `../backend`, không có
      `additional_contexts` như chinese-backend — cms-backend không có học liệu).
- [ ] `docker run --rm --entrypoint sh <ảnh cms-backend> -c 'ls /app | grep -i appsettings.Development'` ⇒ rỗng (không lọt secret vào ảnh).
- [ ] `docker compose up -d cms-backend` (cần `postgres` + role/DB `af_cms` đã tạo qua
      `deploy/postgres/init/01-create-databases.sh`, hoặc tạo tay nếu volume cũ — xem lệnh SQL ở
      hợp đồng W1 §5.1.1) ⇒ `docker exec <container> wget -qO- http://localhost:8080/health/ready`
      trả `200` kèm check `postgres: Healthy`.
- [ ] Qua gateway (`http://localhost:5280/cms/api/system/info`) ⇒ 200 `{ "service": "cms-backend", ... }`.
- [ ] Đăng nhập ở `apps/chinese` (hoặc gọi thẳng `POST /identity/api/auth/login` qua Scalar) lấy
      access token (audience đã có `af-cms` từ khi identity-service redeploy với `Jwt:Audiences`
      mới — token phát TRƯỚC đó không có `af-cms`, tối đa 15 phút vẫn 401 ở cms-backend, chấp nhận)
      ⇒ `GET http://localhost:5280/cms/api/me` với Bearer trả đúng vai trò (rỗng nếu chưa gán, hoặc
      `admin` nếu email trùng `CMS_BOOTSTRAP_ADMIN_EMAIL`).
- [ ] Máy đã có volume Postgres cũ (từ trước W1) ⇒ script init KHÔNG chạy lại; tạo tay bằng
      `psql -U postgres`: `CREATE ROLE af_cms LOGIN PASSWORD '<mật khẩu>'; CREATE DATABASE af_cms
      OWNER af_cms ENCODING 'UTF8' TEMPLATE template0;` rồi `docker compose up -d cms-backend`.
- [ ] `docker compose ps` ⇒ `cms-backend` KHÔNG có cột PORTS ra host (chỉ `nginx` được publish).

## 9. admin-frontend (W2) — CHƯA VERIFY bằng Docker

- [ ] `docker compose build admin-frontend` thành công; `yarn install --frozen-lockfile` không báo thiếu module.
- [ ] `docker run --rm --entrypoint sh <ảnh admin-frontend> -c 'grep -rl "id.antfarms.xyz" /usr/share/nginx/html | head -3'` có kết quả (biến `VITE_IDENTITY_API_URL` nướng đúng).
- [ ] `curl -I https://admin.antfarms.xyz/` ⇒ 200 và có `X-Robots-Tag: noindex, nofollow`; file `.mjs` trả `application/javascript`; `/abc` ⇒ 200 `index.html` (SPA fallback).
- [ ] Identity có `Auth__AllowedOrigins__1=https://admin.antfarms.xyz` — đăng nhập tại admin không bị 403 `ORIGIN_NOT_ALLOWED`.
- [ ] Chứng chỉ SAN có `admin.antfarms.xyz` (`get-cert.sh` truyền ĐỦ `id. chinese. admin.`), bản ghi DNS Cloudflare `admin` đã tạo.

## 10. identity-service API nội bộ (W10) — CHƯA VERIFY bằng Docker

- [ ] (a) `curl -sk -o /dev/null -w "%{http_code}" https://id.antfarms.xyz/internal/ping` ⇒ `404` (không lộ ra Internet).
- [ ] (b) Từ một container trong `af-net` (gateway; từ W11 là cms-backend): `wget -qO- --header "X-Service-Key: $IDENTITY_INTERNAL_KEY" http://identity-service:8081/internal/ping` ⇒ `{"ok":true}`.
- [ ] (c) `docker compose port identity-service 8081` ⇒ không có ánh xạ cổng.
- [ ] (d) `wget -qO- http://identity-service:8081/.well-known/jwks.json` ⇒ 404 (route công khai không phục vụ trên cổng nội bộ).

## 11. Endpoint đăng nhập mobile (M1, identity-service) — CHƯA VERIFY bằng Docker/HTTPS thật

> M1 thêm `POST /api/auth/mobile/{register,login,refresh,logout,password}` — **không đổi**
> `docker-compose.yml`/nginx/gateway (khối `id.antfarms.xyz` đã chuyển MỌI đường dẫn tới gateway
> `/identity/*`, gateway route `/identity/**` không lọc theo path con, xem `deploy/conf/nginx.conf.example`
> + `backend/services/gateway`). Chỉ verify được cục bộ (`dotnet run`, không Docker) tới nay
> (17/09/2026) — xem lệnh curl dưới, đã chạy thật qua gateway `:5280` local.

- [x] (17/09/2026, MacBook, `dotnet run` không Docker) `curl -i -X POST http://localhost:5280/identity/api/auth/mobile/register -H "Content-Type: application/json" -H "X-AF-Client: chinese-mobile/0.1.0+1 (android)" -d '{...}'` ⇒ `201`, có `refreshToken` trong body, **không** `Set-Cookie`, có `Cache-Control: no-store`.
- [x] (17/09/2026, MacBook) Thiếu `X-AF-Client` ⇒ `400 VALIDATION` JSON có khoá `X-AF-Client`.
- [ ] Trên server thật (sau F12): `curl -s -X POST https://id.antfarms.xyz/api/auth/mobile/refresh -H 'X-AF-Client: chinese-mobile/0.1.0+1 (android)' -H 'Content-Type: application/json' -d '{"refreshToken":"<64 ký tự a>"}'` ⇒ `401 REFRESH_INVALID` JSON.
- [ ] Cùng lệnh thêm `-H 'Origin: https://chinese.antfarms.xyz'` ⇒ `403 ORIGIN_NOT_ALLOWED` (production không có `Auth:MobileDevOrigins`).
- [ ] Thiếu `X-AF-Client` trên server thật ⇒ `400 VALIDATION`.
- [ ] Nếu Cloudflare/WAF có luật chặn request không `Origin`/`User-Agent` lạ (dio native không gửi `Origin`, `User-Agent` là chuỗi dio mặc định) ⇒ phải whitelist `/api/auth/mobile/*`.
- [ ] `deploy/.env.example`/`docker-compose.yml`: xác nhận KHÔNG có biến `Auth__MobileDevOrigins` nào bị đặt ở production (rỗng theo mặc định `appsettings.json`) — đặt nhầm không nguy hiểm (chỉ có tác dụng ở Development) nhưng nên dọn nếu copy nhầm từ dev.
