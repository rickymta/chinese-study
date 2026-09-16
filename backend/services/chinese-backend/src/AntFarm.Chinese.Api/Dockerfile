# Build context: ./backend
# KHÔNG khai "# syntax=docker/dockerfile:1": directive đó bắt BuildKit tải frontend từ Docker Hub trước MỖI lần build
# (tag di động) — Docker Hub tắc là build chết ngay bước resolve. Docker Engine 23+ đã hỗ trợ sẵn --mount=type=cache.
#
# ⚠️ Từ F5: chinese-backend cần đọc học liệu content/chinese/data lúc khởi động ⇒ build context
# phải đổi thành GỐC REPO (hoặc additional_contexts của compose). F0 giữ context ./backend vì
# chưa có học liệu nào cần đóng gói.
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
RUN apt-get update -qq \
 && apt-get install -y --no-install-recommends wget \
 && rm -rf /var/lib/apt/lists/* \
 && groupadd --gid 1001 app && useradd --uid 1001 --gid app --shell /bin/false --no-create-home app
COPY --from=build --chown=app:app /app/publish .
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "AntFarm.Chinese.Api.dll"]
