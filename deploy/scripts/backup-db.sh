#!/bin/bash
# ════════════════════════════════════════════════════════════════════════════
# Sao lưu Postgres (mọi database + role) và khoá ký RSA của identity-service.
# Idempotent, an toàn chạy mỗi đêm bằng cron.
#
#     ./scripts/backup-db.sh
#
# Cron gợi ý (chạy trước giờ renew-cert.sh để tránh tranh CPU):
#     5 2 * * * cd <thư_mục_bundle> && ./scripts/backup-db.sh >> /var/log/af-backup-db.log 2>&1
#
# Sao lưu gì:
#   - pg_dumpall --globals-only: role + mật khẩu (không nằm trong pg_dump từng DB).
#   - pg_dump -Fc cho từng database ứng dụng (af_identity, af_chinese) — định dạng
#     custom, phục hồi bằng `pg_restore`, nén sẵn, phục hồi được TỪNG bảng nếu cần.
#   - tar volume identity-keys — MẤT khoá này ⇒ mọi access token cũ hết hiệu lực NGAY
#     (không mất tài khoản/refresh token trong DB, nhưng mọi người phải đăng nhập lại).
#
# Giữ 7 bản gần nhất (7 đêm) trong BACKUP_DIR — xoá bản cũ hơn tự động.
#
# ⚠️ Đây là bản sao lưu TẠI CHỖ (cùng ổ đĩa với server) — không thay thế được việc
# đẩy ra NGOÀI máy. Ổ đĩa hỏng/máy bị xoá nhầm thì bản sao lưu tại chỗ cũng mất theo.
# Gợi ý (không bắt buộc, để biến trống thì bỏ qua bước này): rsync/rclone sang nơi
# khác ngay sau khi tạo xong, vd:
#     rsync -a "$BACKUP_DIR/$STAMP/" user@may-khac:/backup/antfarm/$STAMP/
#     rclone copy "$BACKUP_DIR/$STAMP" remote:antfarm-backup/$STAMP
# Đặt qua biến môi trường BACKUP_OFFSITE_CMD (một dòng lệnh, "$STAMP"/"$BACKUP_DIR"
# đã export sẵn cho lệnh đó dùng) nếu muốn tự động hoá — để trống thì script chỉ in
# nhắc nhở, không lỗi.
# ════════════════════════════════════════════════════════════════════════════
set -euo pipefail
cd "$(dirname "$0")/.."

: "${BACKUP_DIR:=./backups}"
: "${BACKUP_KEEP_DAYS:=7}"
STAMP="$(date +%Y%m%d-%H%M%S)"
DEST="$BACKUP_DIR/$STAMP"
export STAMP BACKUP_DIR

if [ ! -f .env ]; then
    echo "!! Thiếu .env — chạy từ thư mục deploy/ đã cp .env.example .env chưa?" >&2
    exit 1
fi

# Đọc mật khẩu postgres từ .env để không phải gõ tay (không export ra ngoài script này).
POSTGRES_USER="$(grep -E '^POSTGRES_USER=' .env | cut -d= -f2-)"
: "${POSTGRES_USER:?Thiếu POSTGRES_USER trong .env}"

if ! docker compose ps postgres --status running --quiet | grep -q .; then
    echo "!! postgres chưa chạy — không có gì để sao lưu." >&2
    exit 1
fi

mkdir -p "$DEST"

echo "==> [1/3] pg_dumpall --globals-only (role + mật khẩu)"
docker compose exec -T postgres pg_dumpall -U "$POSTGRES_USER" --globals-only \
    > "$DEST/globals.sql"

echo "==> [2/3] pg_dump từng database ứng dụng (định dạng custom, nén sẵn)"
for db in af_identity af_chinese; do
    docker compose exec -T postgres pg_dump -U "$POSTGRES_USER" -Fc "$db" \
        > "$DEST/$db.dump"
done

echo "==> [3/3] Khoá ký RSA identity-service (volume identity-keys)"
# Tên volume thật = "<project>_identity-keys" (project mặc định lấy từ `name: antfarm` trong
# compose, hoặc thư mục nếu không khai `name:`) — dò bằng nhãn compose thay vì đoán tên cứng,
# để đúng cả khi ai đó đổi tên project. Đi qua container tạm — không cần identity-service đang chạy.
KEYS_VOLUME="$(docker volume ls --filter label=com.docker.compose.volume=identity-keys --format '{{.Name}}' | head -1)"
: "${KEYS_VOLUME:?Không tìm thấy volume identity-keys — postgres/identity-service đã compose up lần nào trên máy này chưa?}"
docker run --rm \
    -v "$KEYS_VOLUME":/keys:ro \
    -v "$PWD/$DEST":/backup \
    alpine:3 tar -czf /backup/identity-keys.tar.gz -C /keys .

# Dọn bản cũ hơn BACKUP_KEEP_DAYS ngày — theo tên thư mục ngày, không phải mtime, để
# ổn định kể cả khi rsync/cron đổi mtime lúc đẩy đi nơi khác.
echo "==> Dọn bản backup cũ hơn ${BACKUP_KEEP_DAYS} ngày trong $BACKUP_DIR"
find "$BACKUP_DIR" -mindepth 1 -maxdepth 1 -type d -mtime "+${BACKUP_KEEP_DAYS}" -exec rm -rf {} + 2>/dev/null || true

echo
echo "Xong: $DEST"
du -sh "$DEST"/* 2>/dev/null || true

if [ -n "${BACKUP_OFFSITE_CMD:-}" ]; then
    echo "==> Đẩy ra ngoài máy (BACKUP_OFFSITE_CMD)"
    eval "$BACKUP_OFFSITE_CMD"
else
    echo
    echo "(gợi ý) chưa đặt BACKUP_OFFSITE_CMD — bản sao lưu này CHỈ nằm trên máy này."
    echo "Cân nhắc rsync/rclone sang nơi khác — xem chú thích đầu file."
fi

echo
echo "Phục hồi (diễn tập định kỳ — xem deploy/README.md mục Sao lưu/Phục hồi):"
echo "    docker compose exec -T postgres psql -U $POSTGRES_USER < $DEST/globals.sql"
echo "    docker compose exec -T postgres pg_restore -U $POSTGRES_USER -d af_identity --clean --if-exists < $DEST/af_identity.dump"
echo "    docker compose exec -T postgres pg_restore -U $POSTGRES_USER -d af_chinese  --clean --if-exists < $DEST/af_chinese.dump"
echo "    docker run --rm -v $KEYS_VOLUME:/keys -v \$PWD/$DEST:/backup alpine:3 sh -c 'rm -rf /keys/* && tar -xzf /backup/identity-keys.tar.gz -C /keys'"
