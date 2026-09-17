#!/bin/bash
# ════════════════════════════════════════════════════════════════════════════
# Tạo chứng chỉ TỰ KÝ tạm để nginx khởi động được.
#
#     ./scripts/self-signed.sh <ten_mien>
#
# Vì sao cần: nginx từ chối nạp cấu hình nếu `ssl_certificate` trỏ tới file
# không tồn tại. Mà Let's Encrypt lại cần nginx ĐANG CHẠY để phục vụ ACME
# challenge trên cổng 80. Đây là vòng luẩn quẩn — chứng chỉ tự ký phá vòng đó.
#
# Sau khi có chứng chỉ thật, ./scripts/get-cert.sh sẽ GHI ĐÈ hai file này.
#
# ⚠️ Chứng chỉ tự ký chỉ đủ để nginx lên và trình duyệt vào được (kèm cảnh báo).
# Service .NET ở máy khác gọi sang sẽ TỪ CHỐI KẾT NỐI vì không tin CA nội bộ —
# nên đừng dừng ở bước này khi máy có service khác gọi vào.
# ════════════════════════════════════════════════════════════════════════════
set -euo pipefail
cd "$(dirname "$0")/.."

DOM="${1:-localhost}"
mkdir -p certs/live

openssl req -new -x509 -days 90 -nodes \
    -subj "/CN=$DOM" \
    -addext "subjectAltName=DNS:$DOM" \
    -out certs/live/fullchain.pem \
    -keyout certs/live/privkey.pem

chmod 644 certs/live/fullchain.pem
chmod 600 certs/live/privkey.pem

echo "Đã tạo chứng chỉ tự ký cho $DOM (90 ngày)."
echo "Khởi động nginx rồi lấy chứng chỉ thật:"
echo "    docker compose up -d nginx"
echo "    ./scripts/get-cert.sh <email> $DOM"
