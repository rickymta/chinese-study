#!/bin/bash
# ════════════════════════════════════════════════════════════════════════════
# Tạo role + database riêng cho từng service AntFarm.
#
# Ảnh postgres chính thức chỉ chạy các file trong /docker-entrypoint-initdb.d
# ĐÚNG MỘT LẦN — khi thư mục data (volume pg-data) còn TRỐNG. Nếu volume đã có
# dữ liệu (vd container restart, hoặc đã init từ trước), script này KHÔNG chạy
# lại — đúng ý "chỉ chạy khi volume trống" của hợp đồng §5.6.5.
#
# Dùng .sh chứ không phải .sql (như tên gợi ý ban đầu trong hợp đồng) vì .sql
# không đọc được biến môi trường — mỗi service cần MẬT KHẨU RIÊNG
# (AF_IDENTITY_DB_PASSWORD, AF_CHINESE_DB_PASSWORD) để service khác không đọc/
# ghi chéo DB (R-N3), và .sh mới nội suy được ${VAR} vào câu lệnh psql.
#
# Dùng CHUNG cho cả production (deploy/docker-compose.yml) lẫn dev
# (deploy/dev/docker-compose.dev.yml) — hai .env khác nhau, script giống hệt.
# ════════════════════════════════════════════════════════════════════════════
set -euo pipefail

: "${AF_IDENTITY_DB_PASSWORD:?Thiếu biến môi trường AF_IDENTITY_DB_PASSWORD}"
: "${AF_CHINESE_DB_PASSWORD:?Thiếu biến môi trường AF_CHINESE_DB_PASSWORD}"
: "${AF_CMS_DB_PASSWORD:?Thiếu biến môi trường AF_CMS_DB_PASSWORD}"

# Mật khẩu truyền qua biến psql (:'pw') để psql tự thoát dấu nháy — nội suy thẳng
# '$VAR' vào SQL sẽ hỏng (hoặc bị chèn lệnh) khi mật khẩu chứa dấu '.
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" \
     -v id_pw="$AF_IDENTITY_DB_PASSWORD" -v zh_pw="$AF_CHINESE_DB_PASSWORD" -v cms_pw="$AF_CMS_DB_PASSWORD" <<-'EOSQL'
    CREATE ROLE af_identity LOGIN PASSWORD :'id_pw';
    CREATE DATABASE af_identity OWNER af_identity ENCODING 'UTF8' TEMPLATE template0;

    CREATE ROLE af_chinese LOGIN PASSWORD :'zh_pw';
    CREATE DATABASE af_chinese OWNER af_chinese ENCODING 'UTF8' TEMPLATE template0;

    -- W1: cms-backend (schema access/site trong DB riêng af_cms).
    CREATE ROLE af_cms LOGIN PASSWORD :'cms_pw';
    CREATE DATABASE af_cms OWNER af_cms ENCODING 'UTF8' TEMPLATE template0;
EOSQL
