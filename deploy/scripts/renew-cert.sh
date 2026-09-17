#!/bin/bash
# ════════════════════════════════════════════════════════════════════════════
# Chép chứng chỉ Let's Encrypt đã được gia hạn sang đường dẫn nginx đang đọc,
# rồi reload nginx. Idempotent — chạy mỗi đêm bằng cron là an toàn.
#
#     ./scripts/renew-cert.sh <ten_mien_chinh>
#
# Vì sao cần: service `certbot` trong compose gia hạn vào certs/letsencrypt/
# mỗi 12 giờ, nhưng nginx đọc certs/live/fullchain.pem (bản chép). Không có
# script này thì certbot gia hạn xong mà nginx vẫn phục vụ bản cũ cho tới khi
# hết hạn — hỏng im lặng, không log nào báo.
#
# Cron gợi ý (chạy sau giờ certbot renew, trước giờ làm việc):
#     17 3 * * * cd <thư_mục_bundle> && ./scripts/renew-cert.sh <ten_mien_chinh> >> /var/log/af-renew-cert.log 2>&1
# ════════════════════════════════════════════════════════════════════════════
set -euo pipefail
cd "$(dirname "$0")/.."

CHINH="${1:?Dùng: $0 <ten_mien_chinh>}"
SRC="certs/letsencrypt/live/$CHINH/fullchain.pem"
DST="certs/live/fullchain.pem"

if [ ! -f "$SRC" ]; then
    echo "$(date -Is) KHONG THAY $SRC — chưa xin chứng chỉ? Chạy get-cert.sh trước." >&2
    exit 1
fi

# Chỉ làm việc khi nội dung thật sự khác — tránh reload nginx vô ích mỗi đêm.
if [ -f "$DST" ] && cmp -s "$SRC" "$DST"; then
    echo "$(date -Is) không đổi — hết hạn: $(openssl x509 -enddate -noout -in "$DST" | cut -d= -f2)"
    exit 0
fi

mkdir -p certs/live
cp -L "$SRC" "$DST"
cp -L "certs/letsencrypt/live/$CHINH/privkey.pem" certs/live/privkey.pem
chmod 644 certs/live/fullchain.pem
chmod 600 certs/live/privkey.pem

docker compose exec -T nginx nginx -t
docker compose exec -T nginx nginx -s reload
echo "$(date -Is) ĐÃ NẠP bản mới — hết hạn: $(openssl x509 -enddate -noout -in "$DST" | cut -d= -f2)"
