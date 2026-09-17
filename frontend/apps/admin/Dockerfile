# Build context: ./frontend
# ⚠️ CHƯA VERIFY bằng Docker (máy dev không có Docker) — checklist deploy/VERIFY-DOCKER.md.
# KHÔNG khai "# syntax=docker/dockerfile:1" (bắt BuildKit gọi Docker Hub trước mỗi lần build).
FROM node:22-alpine AS builder
WORKDIR /app

# Layer manifest trước: chỉ cài lại dependency khi package.json/yarn.lock đổi.
# Chỉ copy package.json của ĐÚNG app + các package dùng chung ⇒ peer dependency thiếu ở app sẽ chỉ lộ Ở ĐÂY
# (máy dev hoisted nên không báo). Thêm package @af/* mới thì thêm dòng COPY tương ứng (W13: chinese-kit).
COPY package.json yarn.lock turbo.json ./
COPY packages/tsconfig/package.json ./packages/tsconfig/
COPY packages/ui/package.json       ./packages/ui/
COPY packages/api/package.json      ./packages/api/
COPY packages/utils/package.json    ./packages/utils/
COPY packages/auth/package.json     ./packages/auth/
# chinese-kit CHƯA được admin dùng ở W2, nhưng nằm trong workspace `packages/*` của yarn.lock —
# thiếu package.json của nó thì `yarn install --frozen-lockfile` báo lockfile cần cập nhật và dừng.
COPY packages/chinese-kit/package.json ./packages/chinese-kit/
COPY apps/admin/package.json        ./apps/admin/
RUN yarn install --frozen-lockfile --network-timeout 300000

COPY packages ./packages
COPY apps/admin ./apps/admin

# Vite nướng biến vào bundle LÚC BUILD — sai giá trị thì ảnh vẫn build/chạy, chỉ hỏng khi bấm đăng nhập.
# ⚠️ Mặc định dưới đây là URL PRODUCTION THẬT (id.antfarms.xyz) — build "chay" không qua compose mà QUÊN truyền
# --build-arg sẽ ra một ảnh TRÔNG như build được nhưng đăng nhập của môi trường khác lại gọi sang production thật.
# Luôn build qua `deploy/docker-compose.yml` (mục admin-frontend truyền `args: { VITE_IDENTITY_API_URL:
# "https://id.${APP_DOMAIN}/api" }`), hoặc truyền tay `--build-arg VITE_IDENTITY_API_URL=<url đúng môi trường>`.
ARG VITE_IDENTITY_API_URL=https://id.antfarms.xyz/api
ENV VITE_IDENTITY_API_URL=$VITE_IDENTITY_API_URL
RUN yarn workspace @af/admin build

FROM nginx:1.27-alpine AS runtime
COPY --from=builder /app/apps/admin/dist /usr/share/nginx/html
COPY apps/admin/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=3s CMD wget -qO- http://127.0.0.1/ || exit 1
