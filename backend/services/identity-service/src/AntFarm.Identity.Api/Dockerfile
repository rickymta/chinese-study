# Build context: ./backend
# KHÔNG khai "# syntax=docker/dockerfile:1": directive đó bắt BuildKit tải frontend từ Docker Hub trước MỖI lần build
# (tag di động) — Docker Hub tắc là build chết ngay bước resolve. Docker Engine 23+ đã hỗ trợ sẵn --mount=type=cache.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# --- Layer restore: chỉ copy thứ restore cần ---
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY shared/ shared/
COPY services/identity-service/src/AntFarm.Identity.Domain/AntFarm.Identity.Domain.csproj                 services/identity-service/src/AntFarm.Identity.Domain/
COPY services/identity-service/src/AntFarm.Identity.Application/AntFarm.Identity.Application.csproj       services/identity-service/src/AntFarm.Identity.Application/
COPY services/identity-service/src/AntFarm.Identity.Infrastructure/AntFarm.Identity.Infrastructure.csproj services/identity-service/src/AntFarm.Identity.Infrastructure/
COPY services/identity-service/src/AntFarm.Identity.Api/AntFarm.Identity.Api.csproj                       services/identity-service/src/AntFarm.Identity.Api/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore services/identity-service/src/AntFarm.Identity.Api/AntFarm.Identity.Api.csproj

# --- Layer publish ---
COPY services/identity-service/src/ services/identity-service/src/
# /m:2: không để MSBuild chiếm hết core khi build trên máy đang chạy production
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish services/identity-service/src/AntFarm.Identity.Api/AntFarm.Identity.Api.csproj \
    -c Release -o /app/publish --no-restore /m:2

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# aspnet image không có wget/curl — cài wget cho HEALTHCHECK. Không cài curl.
RUN apt-get update -qq \
 && apt-get install -y --no-install-recommends wget \
 && rm -rf /var/lib/apt/lists/* \
 && groupadd --gid 1001 app && useradd --uid 1001 --gid app --shell /bin/false --no-create-home app \
 && mkdir -p /keys && chown app:app /keys
COPY --from=build --chown=app:app /app/publish .
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD wget -qO- http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "AntFarm.Identity.Api.dll"]
