# Build context: ./backend
# KHÔNG khai "# syntax=docker/dockerfile:1": directive đó bắt BuildKit tải frontend từ Docker Hub trước MỖI lần build
# (tag di động) — Docker Hub tắc là build chết ngay bước resolve. Docker Engine 23+ đã hỗ trợ sẵn --mount=type=cache.
#
# F5 (RK24, D32): chinese-backend cần đọc học liệu content/chinese/data lúc khởi động. GIỮ context
# ./backend (không viết lại mọi COPY/.dockerignore) — dùng BUILD CONTEXT PHỤ "content" (Docker
# Compose additional_contexts, cần Compose ≥ 2.17 — RK38; build tay: --build-context content=content).
# deploy/docker-compose.yml khai `additional_contexts: { content: ../content }` cho service này.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# --- Layer restore: chỉ copy thứ restore cần ---
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY shared/ shared/
COPY services/chinese-backend/src/AntFarm.Chinese.Domain/AntFarm.Chinese.Domain.csproj                 services/chinese-backend/src/AntFarm.Chinese.Domain/
COPY services/chinese-backend/src/AntFarm.Chinese.Application/AntFarm.Chinese.Application.csproj       services/chinese-backend/src/AntFarm.Chinese.Application/
COPY services/chinese-backend/src/AntFarm.Chinese.Infrastructure/AntFarm.Chinese.Infrastructure.csproj services/chinese-backend/src/AntFarm.Chinese.Infrastructure/
COPY services/chinese-backend/src/AntFarm.Chinese.Api/AntFarm.Chinese.Api.csproj                       services/chinese-backend/src/AntFarm.Chinese.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore services/chinese-backend/src/AntFarm.Chinese.Api/AntFarm.Chinese.Api.csproj

# --- Layer publish ---
COPY services/chinese-backend/src/ services/chinese-backend/src/
# /m:2: không để MSBuild chiếm hết core khi build trên máy đang chạy production
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish services/chinese-backend/src/AntFarm.Chinese.Api/AntFarm.Chinese.Api.csproj \
    -c Release -o /app/publish --no-restore /m:2

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# aspnet image không có wget/curl — cài wget cho HEALTHCHECK. Không cài curl.
# Ảnh .NET 8+ ĐÃ CÓ sẵn user/group `app` (UID 1654, biến $APP_UID) — KHÔNG groupadd/useradd lại
# (groupadd báo "group 'app' already exists", exit 9 ⇒ build hỏng; phát hiện khi verify Docker ở F5).
RUN apt-get update -qq \
 && apt-get install -y --no-install-recommends wget \
 && rm -rf /var/lib/apt/lists/*
COPY --from=build --chown=app:app /app/publish .
# F5: học liệu từ build context phụ "content" (compose additional_contexts; build tay:
# --build-context content=../content) — COPY vào STAGE RUNTIME (không phải build), vì chỉ
# "COPY --from=build /app/publish" đi sang runtime, mọi COPY khác ở stage build bị bỏ lại.
# Content__RootPath (env, compose) phải trỏ TUYỆT ĐỐI "/content/chinese" khớp đường dẫn này.
COPY --from=content --chown=app:app chinese/data/ /content/chinese/data/
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "AntFarm.Chinese.Api.dll"]
