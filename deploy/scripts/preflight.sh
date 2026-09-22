#!/bin/bash
# ════════════════════════════════════════════════════════════════════════════
# Kiểm tra trước khi `docker compose up -d` (chép khuôn MedDental deploy/app-core,
# bỏ phần CA nội bộ/Redis/DB-01 — AntFarm dùng Postgres NGAY TRONG bundle này, không
# có máy DB riêng, không dùng Redis).
#
# Vì sao cần: compose bind-mount một số FILE (không phải thư mục). Nếu file đó
# chưa tồn tại, Docker KHÔNG báo lỗi rõ ràng mà lặng lẽ TẠO MỘT THƯ MỤC RỖNG
# đúng chỗ ấy, rồi container chết với thông báo khó hiểu:
#
#     not a directory: Are you trying to mount a directory onto a file?
#
# Tệ hơn: thư mục rác đó nằm lại, nên lần chạy sau vẫn hỏng cho tới khi xoá tay.
#
#     ./scripts/preflight.sh
# ════════════════════════════════════════════════════════════════════════════
set -uo pipefail
cd "$(dirname "$0")/.."

RED=$'\e[31m'; GREEN=$'\e[32m'; YELLOW=$'\e[33m'; RESET=$'\e[0m'
loi=0

# ── 1. Dọn thư mục rác do Docker tạo nhầm ở lần chạy hỏng trước ─────────────
for f in conf/nginx.conf conf/cloudflare-realip.conf certs/live/fullchain.pem certs/live/privkey.pem; do
    if [ -d "$f" ]; then
        echo "${YELLOW}dọn${RESET}   $f là THƯ MỤC rỗng do Docker tạo nhầm — đang xoá"
        rmdir "$f" 2>/dev/null || rm -rf "$f"
    fi
done

kiem_tra() {
    if [ -f "$1" ]; then echo "${GREEN}ok${RESET}    $1"
    else echo "${RED}THIẾU${RESET} $1"; echo "        → $2"; loi=1; fi
}

echo "── Cấu hình ───────────────────────────────────────────────"
kiem_tra .env            "cp .env.example .env  rồi điền giá trị"
kiem_tra conf/nginx.conf "cp conf/nginx.conf.example conf/nginx.conf  rồi thay tên miền"

echo
echo "── Chứng chỉ TLS cho nginx ────────────────────────────────"
kiem_tra certs/live/fullchain.pem "./scripts/self-signed.sh <ten_mien>  (tạm), rồi ./scripts/get-cert.sh <email> <ten_mien...>"
kiem_tra certs/live/privkey.pem   "sinh cùng lúc với fullchain.pem ở trên"

# cloudflare-realip.conf chỉ bắt buộc khi nginx.conf đã bật dòng include tương ứng.
if [ -f conf/nginx.conf ] && grep -q "^[[:space:]]*include /etc/nginx/cloudflare-realip.conf;" conf/nginx.conf 2>/dev/null; then
    echo
    echo "── cloudflare-realip.conf (vì nginx.conf đã bật include) ──"
    kiem_tra conf/cloudflare-realip.conf "cp conf/cloudflare-realip.conf.example conf/cloudflare-realip.conf"
fi

# ── 2. Khoá ký JWT của identity-service (volume identity-keys) ─────────────
# Không đọc được TRỰC TIẾP từ host (nằm trong named volume) — chỉ nhắc, không chặn
# (identity-service tự sinh khoá nếu Development, nhưng Production sẽ DỪNG HẲN nếu
# thư mục trống — xem FileSigningKeyStore.cs). Xác nhận bằng:
#     docker compose run --rm --entrypoint sh identity-service -c 'ls /keys/*.pem'
echo
echo "── Khoá ký RSA identity-service ────────────────────────────"
echo "${YELLOW}lưu ý${RESET} không kiểm được từ host (nằm trong volume identity-keys)."
echo "        Xác nhận bằng: docker compose run --rm --entrypoint sh identity-service -c 'ls /keys/*.pem'"
echo "        Trống ở Production ⇒ identity-service DỪNG HẲN lúc khởi động (xem deploy/README.md)."

# ── 3. Placeholder còn sót trong nginx.conf ────────────────────────────────
if [ -f conf/nginx.conf ]; then
    echo
    echo "── Placeholder trong nginx.conf ───────────────────────────"
    # Bỏ qua dòng đã comment (vd khối mẫu "<LANG>" dành cho ngôn ngữ thêm sau) — placeholder
    # thật nằm ở dòng server_name đang hoạt động, không phải trong ghi chú.
    con=$(grep -v '^[[:space:]]*#' conf/nginx.conf | grep -oE '<[A-Z_0-9]+>' | sort -u | tr '\n' ' ')
    if [ -n "$con" ]; then
        echo "${RED}SAI${RESET}   còn placeholder chưa thay: $con"
        echo "        Để nguyên thì nginx vẫn chạy nhưng định tuyến sai tên miền."
        loi=1
    else
        echo "${GREEN}ok${RESET}    đã thay hết"
    fi
fi

# ── 4. Biến bắt buộc trong .env ─────────────────────────────────────────────
if [ -f .env ]; then
    echo
    echo "── Biến trong .env ────────────────────────────────────────"
    van_de=0
    while IFS='=' read -r k v; do
        case "$k" in \#*|'') continue ;; esac
        [ -z "$v" ] && { echo "${YELLOW}trống${RESET} $k"; van_de=1; continue; }
        case "$v" in CHANGE_ME*|*'<'*'>'*)
            echo "${RED}SAI${RESET}   $k vẫn là giá trị mẫu: $v"; loi=1; van_de=1 ;;
        esac
    done < .env
    [ "$van_de" -eq 0 ] && echo "${GREEN}ok${RESET}    tất cả biến đã có giá trị thật"
fi

# ── Cấu trúc docker-compose.yml ────────────────────────────────────────────
# `docker compose config -q` phân giải cả file: biến, anchor, và quan trọng nhất
# là các tham chiếu chéo. Nó bắt được lớp lỗi mắt thường đọc lướt sẽ bỏ qua — ví
# dụ khối `volumes:` bị thụt vào dưới `networks:`, làm volume biến thành network
# rỗng và service chết với "refers to undefined volume".
echo
echo "── Cấu trúc compose ───────────────────────────────────────"
if loi_compose=$(docker compose config -q 2>&1); then
    echo "${GREEN}ok${RESET}    docker-compose.yml hợp lệ"
else
    echo "${RED}SAI${RESET}   docker-compose.yml không hợp lệ:"
    echo "$loi_compose" | sed "s/^/        /"
    loi=1
fi

echo
if [ "$loi" -eq 0 ]; then
    echo "${GREEN}Sẵn sàng.${RESET} Chưa có registry riêng thì build tại chỗ trước:"
    echo "    docker compose build && docker compose up -d"
    stale=$(docker compose ps -aq --status=created --status=exited 2>/dev/null | wc -l)
    if [ "${stale:-0}" -gt 0 ]; then
        echo
        echo "${YELLOW}lưu ý${RESET} có container chưa chạy được từ lần trước. Nếu vẫn gặp"
        echo "        lỗi 'not a directory', xoá hẳn rồi tạo lại:"
        echo "            docker compose rm -sf <ten_service> && docker compose up -d"
    fi
else
    echo "${RED}Còn thiếu ở trên — sửa xong rồi chạy lại script này.${RESET}"
    exit 1
fi
