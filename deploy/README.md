# AntFarm — triển khai production (`deploy/`)

Runbook cho bundle một server: `postgres`, `identity-service`, `chinese-backend`,
`gateway`, `chinese-frontend`, `nginx` (biên), `certbot`. Bám khuôn MedDental
`deploy/app-core` (xem `CLAUDE.md` gốc mục "HTTPS Let's Encrypt cho mọi tên miền").
Kiến trúc đầy đủ + quy ước: `docs/agent-workflow/2026-09-16-antfarm-nen-tang-tieng-trung-mvp-hop-dong-thuc-thi.md` §5.6.

**Máy đích tối thiểu (MVP):** 2 vCPU / 4 GB RAM / 40 GB SSD — 3 service .NET + 1 Postgres +
2 nginx nhẹ.

---

## 0. Trạng thái verify

**CHƯA chạy trên máy Docker + DNS thật** — xem `deploy/VERIFY-DOCKER.md` cho checklist đầy đủ
và những gì đã/chưa kiểm. Runbook này viết đúng theo hợp đồng + đối chiếu MedDental, nhưng
**mọi lệnh dưới đây cần chạy thật trên server rồi đánh dấu lại `VERIFY-DOCKER.md`.**

---

## 1. Yêu cầu máy

- Docker Engine ≥ 23 (Compose v2 ≥ 2.17 — cần cho `additional_contexts` của `chinese-backend`).
  Kiểm: `docker compose version`.
- `openssl` (tạo chứng chỉ tự ký + khoá ký JWT), `git`.
- Domain `antfarms.xyz` (hoặc domain khác — đổi `APP_DOMAIN` trong `.env`) quản lý DNS ở
  Cloudflare theo CLAUDE.md gốc mục "Domain".

## 2. DNS (Cloudflare)

Tạo bản ghi A cho **cả hai** subdomain, trỏ về IP máy này:

| Bản ghi | Trỏ về |
|---|---|
| `id.antfarms.xyz` | IP máy này |
| `chinese.antfarms.xyz` | IP máy này |
| `admin.antfarms.xyz` | IP máy này (W2) |

**Chế độ proxy:**

- **Khuyến nghị lúc mới dựng:** để **DNS-only** (đám mây xám) cho tới khi HTTPS chạy được —
  HTTP-01 xác minh trực tiếp, ít thứ có thể sai.
- **Bật proxy (đám mây cam) sau đó:** SSL/TLS mode **bắt buộc Full (strict)**. **Flexible**
  ⇒ Cloudflare gọi origin bằng HTTP ⇒ nginx `return 301 https` ⇒ **vòng lặp chuyển hướng vô
  hạn**. Proxied ⇒ `$remote_addr` ở nginx là IP Cloudflare — phải bật
  `conf/cloudflare-realip.conf` (bước 6) nếu không thì rate limit/log theo IP sẽ gom mọi người
  dùng thành vài IP của Cloudflare.
- HTTP-01 lấy chứng chỉ lần đầu mà bị chặn (vd rule "Always Use HTTPS"/WAF) → tạm chuyển về
  DNS-only trong lúc chạy `get-cert.sh` rồi bật lại proxy.

## 3. Tường lửa (ufw)

Chỉ `nginx` publish cổng ra ngoài — mọi service khác nằm sau mạng nội bộ `af-net`.

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
sudo ufw status
```

Không mở cổng nào khác ra Internet (Postgres/identity-service/chinese-backend/gateway
KHÔNG có `ports:` trong compose — xem ghi chú đầu `docker-compose.yml`).

## 4. Lấy mã nguồn và chép cấu hình

```bash
git clone <URL_REPO> antfarm && cd antfarm/deploy
cp .env.example .env
cp conf/nginx.conf.example conf/nginx.conf
```

Sửa `.env`:

- `APP_DOMAIN` — domain thật (mặc định `antfarms.xyz`).
- `PUBLIC_BIND=0.0.0.0` — **bắt buộc trên server thật**, mặc định `127.0.0.1` chỉ để máy dev
  chưa cấu hình tường lửa không lộ cổng ra ngoài.
- `POSTGRES_PASSWORD`, `AF_IDENTITY_DB_PASSWORD`, `AF_CHINESE_DB_PASSWORD` — sinh ngẫu nhiên,
  ví dụ `openssl rand -base64 24`. **Không dùng chung một mật khẩu cho 3 biến này.**
- `LETSENCRYPT_EMAIL` — chỉ để tiện nhớ/kiểm ở `preflight.sh`; `get-cert.sh` **không tự đọc
  file `.env`** (xem lý do ở comment trong `.env.example`) — bước 8 dưới sẽ `source .env`
  trước khi gọi lệnh.
- `AUTH_ALLOW_REGISTRATION=false` — **giữ `false`** cho tới bước 9 (tạo tài khoản đầu tiên).
- `CHINESE_ADMIN_EMAIL` — email sẽ được gán vai trò `admin` của chinese-backend ngay lần
  đăng nhập đầu tiên (R-P6, đọc kỹ RK40 trong README gốc: đây là **nguồn sự thật**, mỗi lần
  service khởi động sẽ gán LẠI vai trò `admin` cho email này).
- `REGISTRY` — để **trống** nếu chưa có registry ảnh riêng (mặc định build tại chỗ bằng
  `docker compose build`, xem ghi chú đầu `docker-compose.yml` — KHÔNG để trống rồi lỡ tay
  `docker compose pull`, một số phiên bản có thể cố kéo `localhost/antfarm/...` và báo lỗi rõ
  ràng thay vì âm thầm lấy nhầm ảnh người khác trên Docker Hub).

Sửa `conf/nginx.conf` — thay `<ID_DOMAIN>` → `id.antfarms.xyz`, `<CHINESE_DOMAIN>` →
`chinese.antfarms.xyz` (hoặc domain thật của bạn).

## 5. Preflight

```bash
./scripts/preflight.sh
```

Kiểm file bind-mount đã tồn tại đúng dạng (Docker âm thầm tạo THƯ MỤC RỖNG nếu file chưa có —
lỗi khó hiểu "not a directory"), placeholder còn sót trong `nginx.conf`, biến `.env` còn trống,
và `docker compose config -q`. Chạy lại **mỗi khi sửa `.env`/`nginx.conf`**.

## 6. Chứng chỉ TLS — vòng luẩn quẩn lần đầu

nginx từ chối khởi động nếu thiếu file chứng chỉ; Let's Encrypt cần nginx **đang chạy** để
phục vụ ACME challenge. Phá vòng đó bằng chứng chỉ tự ký tạm:

```bash
./scripts/self-signed.sh id.antfarms.xyz
docker compose build   # chưa có registry — xem ghi chú REGISTRY ở bước 4
docker compose up -d nginx
```

(Cloudflare proxy đã bật) mở `conf/nginx.conf`, bỏ comment dòng
`include /etc/nginx/cloudflare-realip.conf;`, rồi:

```bash
cp conf/cloudflare-realip.conf.example conf/cloudflare-realip.conf
docker compose exec nginx nginx -t && docker compose exec nginx nginx -s reload
```

## 7. Khoá ký RSA của identity-service

`identity-service` đọc file `.pem` (PKCS8, không mã hoá) trong `Jwt:KeysPath` (`/keys` trong
container, volume `identity-keys`) — xem
`backend/services/identity-service/src/AntFarm.Identity.Infrastructure/Security/FileSigningKeyStore.cs`.
Ở **Production**, thư mục trống ⇒ service **dừng hẳn lúc khởi động** (không tự sinh khoá như
Development, R-A13) — phải tạo tay TRƯỚC lần `up -d identity-service` đầu tiên. Tên file bắt
buộc đúng khuôn `yyyyMMdd-<8 hex>.pem` (kid dùng để sắp xếp chọn khoá mới nhất).

```bash
docker volume create antfarm_identity-keys 2>/dev/null || true   # tên = <project>_identity-keys, project mặc định "antfarm"
KID="$(date +%Y%m%d)-$(openssl rand -hex 4)"
mkdir -p /tmp/af-keys && cd /tmp/af-keys
# genpkey (không phải genrsa) ⇒ định dạng PKCS8 "-----BEGIN PRIVATE KEY-----", đúng định dạng
# FileSigningKeyStore.LoadRsaFromPemFile mong đợi (ImportFromPem đọc được cả PKCS1 lẫn PKCS8,
# nhưng PKCS8 khớp CHÍNH XÁC với khoá tự sinh ở Development — nhất quán khi so sánh/di chuyển).
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$KID.pem"
docker run --rm -v antfarm_identity-keys:/keys -v "$PWD":/src alpine:3 sh -c \
  "cp /src/$KID.pem /keys/$KID.pem && chown 1654:1654 /keys/$KID.pem && chmod 600 /keys/$KID.pem"
rm -rf /tmp/af-keys
cd - >/dev/null
```

`1654` là UID/GID cố định của user `app` trong ảnh `mcr.microsoft.com/dotnet/aspnet` (biến
`$APP_UID` — xem Dockerfile identity-service, `USER app`). Sai UID ⇒ container không đọc được
file (Permission denied) dù `ls` từ container khác (root) vẫn thấy file bình thường.

Xác nhận trước khi khởi động thật:

```bash
docker run --rm -v antfarm_identity-keys:/keys alpine:3 ls -la /keys
```

## 8. Chứng chỉ Let's Encrypt thật

```bash
set -a && source .env && set +a
./scripts/get-cert.sh "$LETSENCRYPT_EMAIL" id.antfarms.xyz chinese.antfarms.xyz admin.antfarms.xyz
```

⚠️ **Luôn truyền ĐỦ mọi tên miền của máy này** (cũ + mới) — chỉ truyền tên mới sẽ **đè mất**
chứng chỉ của tên cũ (một máy = một chứng chỉ SAN duy nhất, xem comment trong `get-cert.sh`).
Kiểm:

```bash
openssl x509 -in certs/live/fullchain.pem -noout -text | grep -A1 "Subject Alternative Name"
openssl x509 -in certs/live/fullchain.pem -noout -text | grep "Public Key Algorithm"   # phải là rsaEncryption
```

Cài cron gia hạn (container `certbot` chỉ gia hạn vào `certs/letsencrypt/` mỗi 12 giờ — nginx
đọc bản chép ở `certs/live/`, **thiếu cron dưới đây là hỏng âm thầm** tới khi hết hạn):

```bash
(crontab -l 2>/dev/null; echo "17 3 * * * cd $PWD && ./scripts/renew-cert.sh id.antfarms.xyz >> /var/log/af-renew-cert.log 2>&1") | crontab -
crontab -l   # xác nhận đã có dòng trên
```

## 9. Tạo tài khoản đầu tiên (admin tiếng Trung)

`identity-service` không có màn "tạo admin" riêng — tài khoản đầu tiên đăng ký qua API công
khai rồi được **chinese-backend** tự gán vai trò `admin` theo `ChineseAdmin:BootstrapEmails`
(biến `CHINESE_ADMIN_EMAIL`) ngay lần đầu gọi bất kỳ endpoint nào của nó.

```bash
# 1) Bật đăng ký TẠM THỜI
sed -i 's/^AUTH_ALLOW_REGISTRATION=.*/AUTH_ALLOW_REGISTRATION=true/' .env
docker compose up -d identity-service

# 2) Đăng ký đúng email trong CHINESE_ADMIN_EMAIL (Origin phải khớp Auth:AllowedOrigins)
curl -i -X POST https://id.antfarms.xyz/api/auth/register \
  -H "Content-Type: application/json" -H "Origin: https://chinese.antfarms.xyz" \
  -d '{"email":"admin@antfarms.xyz","password":"<mat-khau-du-manh>","displayName":"Admin","timeZone":"Asia/Ho_Chi_Minh"}'

# 3) Tắt đăng ký công khai ngay sau khi có tài khoản
sed -i 's/^AUTH_ALLOW_REGISTRATION=.*/AUTH_ALLOW_REGISTRATION=false/' .env
docker compose up -d identity-service

# 4) Đăng nhập lên https://chinese.antfarms.xyz — lần gọi /api/me đầu tiên tự provision +
#    AccessSeeder của chinese-backend gán vai trò admin cho email trong CHINESE_ADMIN_EMAIL.
```

⚠️ Nếu bỏ qua bước 3, bất kỳ ai cũng tự đăng ký được tài khoản — luôn tắt lại ngay sau khi có
tài khoản đầu tiên (D4 chưa chốt chính sách mời/duyệt cho MVP).

## 10. Triển khai từng service

```bash
docker compose build identity-service   # rồi lần lượt: chinese-backend, gateway, chinese-frontend
docker compose up -d identity-service
docker compose logs -f identity-service   # chờ tới khi lắng nghe, không lỗi khoá ký
```

Thứ tự khuyến nghị: `identity-service` trước (chinese-backend cần JWKS của nó — RK9: JWKS tải
lười + tự thử lại nên sai thứ tự không crash, chỉ 401 tới khi tải được), rồi `chinese-backend`,
`gateway`, `chinese-frontend`, cuối cùng `nginx`:

```bash
docker compose up -d
docker compose ps   # mọi container "healthy"; CHỈ nginx có cột PORTS ra ngoài
```

Kiểm nhanh:

```bash
curl -sI https://id.antfarms.xyz/.well-known/jwks.json | head -1
curl -sI https://chinese.antfarms.xyz/chinese/api/system/info | head -1
curl -sI https://chinese.antfarms.xyz/ | head -1
```

**Cập nhật sau này (đã có ảnh cũ đang chạy):**

```bash
docker compose pull chinese-backend && docker compose up -d chinese-backend   # có REGISTRY thật
# HOẶC (chưa có registry riêng — build tại chỗ):
docker compose build chinese-backend && docker compose up -d chinese-backend
```

`up -d` **không** tự kéo ảnh mới cùng tag — luôn `pull` (hoặc `build`) trước.

## 11. Sao lưu / phục hồi

```bash
./scripts/backup-db.sh
```

Tạo `deploy/backups/<timestamp>/` gồm `globals.sql` (role + mật khẩu), `af_identity.dump` +
`af_chinese.dump` (định dạng custom `pg_dump -Fc`), `identity-keys.tar.gz` (khoá ký RSA). Giữ 7
bản gần nhất, xoá bản cũ hơn tự động. Đặt cron hằng đêm:

```bash
(crontab -l 2>/dev/null; echo "5 2 * * * cd $PWD && ./scripts/backup-db.sh >> /var/log/af-backup-db.log 2>&1") | crontab -
```

Đây là bản sao lưu **tại chỗ** — không thay thế việc đẩy ra ngoài máy. Đặt `BACKUP_OFFSITE_CMD`
(một dòng lệnh `rsync`/`rclone`, xem chú thích đầu `backup-db.sh`) nếu có nơi khác để đẩy tới;
để trống thì script chỉ nhắc, không lỗi.

**Diễn tập phục hồi định kỳ (bắt buộc — bản sao lưu chưa từng phục hồi thử coi như không tồn
tại):**

```bash
docker compose exec -T postgres psql -U "$POSTGRES_USER" < deploy/backups/<ts>/globals.sql
docker compose exec -T postgres pg_restore -U "$POSTGRES_USER" -d af_identity --clean --if-exists < deploy/backups/<ts>/af_identity.dump
docker compose exec -T postgres pg_restore -U "$POSTGRES_USER" -d af_chinese  --clean --if-exists < deploy/backups/<ts>/af_chinese.dump
docker run --rm -v antfarm_identity-keys:/keys -v "$PWD/deploy/backups/<ts>":/backup alpine:3 \
  sh -c 'rm -rf /keys/* && tar -xzf /backup/identity-keys.tar.gz -C /keys'
docker compose restart identity-service chinese-backend
```

Nên diễn tập trên một máy/VM **khác** máy production (khôi phục xong kiểm đăng nhập + tra từ
điển hoạt động bình thường) — thêm mục này vào `deploy/VERIFY-DOCKER.md` sau khi diễn tập thật.

## 12. Xử lý sự cố

| Triệu chứng | Nguyên nhân thường gặp | Cách kiểm |
|---|---|---|
| `nginx` không lên, log "cannot load certificate" | Thiếu `certs/live/fullchain.pem`/`privkey.pem` | Chạy `self-signed.sh` (bước 6) trước |
| Container "not a directory" lúc khởi động | Docker tạo nhầm thư mục rỗng vì thiếu file trước khi mount | `./scripts/preflight.sh` dọn tự động |
| `identity-service` dừng ngay, log "Không tìm thấy khoá ký RS256..." | Volume `identity-keys` trống ở Production | Bước 7 — tạo khoá tay trước khi `up -d` |
| Đăng nhập xong quay lại trang chủ vẫn chưa đăng nhập | Cookie `af_rt` sai `Domain`/`Secure`/CORS | `Auth:AllowedOrigins` đủ `https://chinese.antfarms.xyz`; domain cookie `.antfarms.xyz` |
| `chinese-backend` trả 401 cho MỌI request | `Auth__JwksUrl` là `https` nhưng `Auth__RequireHttpsMetadata` chưa `"false"` (JWKS nội bộ là `http`) | Xem biến trong `docker-compose.yml` |
| Log ghi IP là IP của gateway/Cloudflare, không phải IP thật người dùng | Thiếu `X-Forwarded-For $remote_addr` (ghi đè) ở nginx biên, hoặc `KnownIPNetworks` sai | So khớp `conf/nginx.conf` với `nginx.conf.example`; xem `ForwardedHeaders__KnownIPNetworks__*` trong compose |
| `docker compose pull` báo lỗi không kéo được ảnh | Bình thường nếu `REGISTRY` để trống (mặc định `localhost/antfarm` — không phải registry thật) | Dùng `docker compose build <svc>` thay vì `pull` cho tới khi có registry riêng |
| Renew Let's Encrypt xong nhưng trình duyệt vẫn báo chứng chỉ hết hạn | Thiếu cron `renew-cert.sh` chép sang `certs/live/` + reload nginx | `crontab -l`; chạy tay `./scripts/renew-cert.sh id.antfarms.xyz` |
| Cloudflare bật proxy ⇒ vòng lặp redirect vô hạn | SSL/TLS mode đang **Flexible** thay vì **Full (strict)** | Đổi mode trong Cloudflare dashboard |
