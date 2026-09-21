# ⚠️ CHƯA VERIFY bằng Docker (máy viết W1 không có Docker) — xem deploy/VERIFY-DOCKER.md.
# Build context: ./backend
# KHÔNG khai "# syntax=docker/dockerfile:1": directive đó bắt BuildKit tải frontend từ Docker Hub trước MỖI lần build
# (tag di động) — Docker Hub tắc là build chết ngay bước resolve. Docker Engine 23+ đã hỗ trợ sẵn --mount=type=cache.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# --- Layer restore: chỉ copy thứ restore cần ---
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY shared/ shared/
COPY services/cms-backend/src/AntFarm.Cms.Domain/AntFarm.Cms.Domain.csproj                 services/cms-backend/src/AntFarm.Cms.Domain/
COPY services/cms-backend/src/AntFarm.Cms.Application/AntFarm.Cms.Application.csproj       services/cms-backend/src/AntFarm.Cms.Application/
COPY services/cms-backend/src/AntFarm.Cms.Infrastructure/AntFarm.Cms.Infrastructure.csproj services/cms-backend/src/AntFarm.Cms.Infrastructure/
COPY services/cms-backend/src/AntFarm.Cms.Api/AntFarm.Cms.Api.csproj                       services/cms-backend/src/AntFarm.Cms.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore services/cms-backend/src/AntFarm.Cms.Api/AntFarm.Cms.Api.csproj

# --- Layer publish ---
COPY services/cms-backend/src/ services/cms-backend/src/
# /m:2: không để MSBuild chiếm hết core khi build trên máy đang chạy production
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish services/cms-backend/src/AntFarm.Cms.Api/AntFarm.Cms.Api.csproj \
    -c Release -o /app/publish --no-restore /m:2

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# aspnet image không có wget/curl — cài wget cho HEALTHCHECK. Không cài curl.
# Ảnh .NET 8+ ĐÃ CÓ sẵn user/group `app` (UID 1654, biến $APP_UID) — KHÔNG groupadd/useradd lại
# (groupadd báo "group 'app' already exists", exit 9 ⇒ build hỏng — bài học chinese-backend F5).
RUN apt-get update -qq \
 && apt-get install -y --no-install-recommends wget \
 && rm -rf /var/lib/apt/lists/*
COPY --from=build --chown=app:app /app/publish .
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "AntFarm.Cms.Api.dll"]
