#!/bin/bash
# ════════════════════════════════════════════════════════════════════════════
# Lấy chứng chỉ Let's Encrypt và nạp vào nginx.
#
#     ./scripts/get-cert.sh <email> <ten_mien> [ten_mien_2 ...]
#
# Ví dụ:
#     ./scripts/get-cert.sh admin@antfarms.xyz id.antfarms.xyz chinese.antfarms.xyz
#
# ── Cách hoạt động ──────────────────────────────────────────────────────────
# nginx luôn đọc CÙNG MỘT đường dẫn cố định: certs/live/fullchain.pem và
# certs/live/privkey.pem. Script này chỉ thay nội dung hai file đó. Nhờ vậy
# không phải sửa nginx.conf khi đổi từ chứng chỉ tự ký sang Let's Encrypt, và
# lệnh gia hạn về sau cũng dùng đúng cơ chế.
#
# ⚠️ MỘT MÁY = MỘT CHỨNG CHỈ DUY NHẤT. Vì nginx đọc cùng một file cho mọi khối
# server, mỗi lần chạy phải truyền ĐỦ tên miền của máy này (cũ + mới). Chạy chỉ
# với tên miền mới là đè mất chứng chỉ của các tên miền cũ — trình duyệt báo
# lỗi ngay. Xem danh sách đang có: docker run --rm -v "$PWD/certs/letsencrypt:/etc/letsencrypt" certbot/certbot certificates
#
# ⚠️ ĐIỀU KIỆN: bản ghi DNS của mọi tên miền truyền vào PHẢI đã trỏ về máy này,
# và cổng 80 phải mở ra Internet. Let's Encrypt xác minh bằng cách gọi ngược lại
# http://<ten_mien>/.well-known/acme-challenge/ — không có hai thứ đó thì thất
# bại với "Timeout during connect", và Let's Encrypt giới hạn 5 lần hỏng mỗi giờ
# cho cùng một tên miền nên đừng thử lại liên tục.
# ════════════════════════════════════════════════════════════════════════════
# ── VÌ SAO ÉP --key-type rsa ──────────────────────────────────────────────────
# Certbot từ bản 2.0 mặc định cấp khoá ECDSA, chuỗi đi qua ISRG Root YE → ISRG
# Root X2. Một số nơi dùng tường lửa bật SSL deep inspection (vd FortiGate) dùng
# kho CA cũ không có gốc ECDSA đời mới nên coi như chứng chỉ không hợp lệ.
#
# RSA đi qua R10/R11 → ISRG Root X1 — gốc có trong gần như mọi kho tin cậy cũ.
# Đánh đổi: bắt tay TLS chậm hơn ECDSA một chút, chấp nhận được để đổi lấy việc
# người dùng sau tường lửa doanh nghiệp vào được hệ thống.
#
# ⚠️ Cờ này CHỈ áp cho lần cấp MỚI. Chứng chỉ ECDSA đã cấp trước đó sẽ giữ nguyên
#    khi gia hạn — muốn đổi phải cấp lại một lần có --force-renewal, rồi chạy
#    ./scripts/renew-cert.sh <ten_mien_chinh> để chép sang certs/live/ và reload
#    nginx. Thiếu bước sau thì nginx vẫn phục vụ chứng chỉ ECDSA cũ.
set -euo pipefail
cd "$(dirname "$0")/.."

if [ $# -lt 2 ]; then
    echo "Dùng: $0 <email> <ten_mien> [ten_mien_2 ...]" >&2
    exit 1
fi

EMAIL="$1"; shift
CHINH="$1"
DOMS=()
for d in "$@"; do DOMS+=(-d "$d"); done

echo "==> Kiểm tra nginx đang chạy (cần để phục vụ ACME challenge)"
if ! docker compose ps nginx --status running --quiet | grep -q .; then
    echo "!! nginx chưa chạy. Khởi động nó trước:" >&2
    echo "     docker compose up -d nginx" >&2
    echo "   Nếu nginx không lên được vì thiếu chứng chỉ, tạo bản tự ký tạm:" >&2
    echo "     ./scripts/self-signed.sh $CHINH" >&2
    exit 1
fi

# Đổi LOẠI KHOÁ (vd ECDSA -> RSA) không tự kích hoạt cấp lại: --keep-until-expiring
# ở dưới sẽ bỏ qua vì chứng chỉ cũ còn hạn. Chạy với FORCE=1 để ép cấp lại ngay.
#     FORCE=1 ./scripts/get-cert.sh admin@antfarms.xyz <ten_mien...>
# CẢNH BÁO: Let's Encrypt giới hạn 5 chứng chỉ TRÙNG bộ tên miền mỗi tuần — đừng ép lặp lại.
FORCE_ARGS=()
if [ "${FORCE:-0}" = "1" ]; then
    FORCE_ARGS+=(--force-renewal)
    echo "==> FORCE=1 — cấp lại BẤT KỂ hạn còn dài"
fi
echo "==> Xin chứng chỉ cho: $*"
docker run --rm \
    -v "$PWD/certs/letsencrypt:/etc/letsencrypt" \
    -v "$PWD/certs/acme:/var/www/certbot" \
    certbot/certbot:latest certonly \
        --webroot -w /var/www/certbot \
        --email "$EMAIL" --agree-tos --no-eff-email \
        --non-interactive --keep-until-expiring \
        --key-type rsa \
        ${FORCE_ARGS[@]+"${FORCE_ARGS[@]}"} \
        --cert-name "$CHINH" --expand \
        "${DOMS[@]}"

echo "==> Nạp vào đường dẫn cố định mà nginx đang đọc"
mkdir -p certs/live
cp -L "certs/letsencrypt/live/$CHINH/fullchain.pem" certs/live/fullchain.pem
cp -L "certs/letsencrypt/live/$CHINH/privkey.pem"   certs/live/privkey.pem
chmod 644 certs/live/fullchain.pem
chmod 600 certs/live/privkey.pem

echo "==> Nạp lại nginx"
docker compose exec nginx nginx -t
docker compose exec nginx nginx -s reload

echo
echo "Xong. Kiểm tra:"
echo "    curl -sI https://$CHINH | head -1"
echo
echo 'Gia hạn: service certbot trong compose chạy `certbot renew` mỗi 12 giờ, nhưng'
echo 'nginx đọc bản chép ở certs/live/ nên PHẢI có cron chép sang + reload nginx:'
echo "    (crontab -l 2>/dev/null; echo '17 3 * * * cd $PWD && ./scripts/renew-cert.sh $CHINH >> /var/log/af-renew-cert.log 2>&1') | crontab -"
