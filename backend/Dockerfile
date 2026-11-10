# Backend image. Build from the repo root: docker build -t school-admission-backend ./backend

# --- Build: publish only the Api project (tests are never built into the image) ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from project files only, so the layer is cached until a dependency changes.
COPY Directory.Build.props ./
COPY src/SchoolAdmission.Domain/SchoolAdmission.Domain.csproj src/SchoolAdmission.Domain/
COPY src/SchoolAdmission.Application/SchoolAdmission.Application.csproj src/SchoolAdmission.Application/
COPY src/SchoolAdmission.Infrastructure/SchoolAdmission.Infrastructure.csproj src/SchoolAdmission.Infrastructure/
COPY src/SchoolAdmission.Api/SchoolAdmission.Api.csproj src/SchoolAdmission.Api/
RUN dotnet restore src/SchoolAdmission.Api/SchoolAdmission.Api.csproj

COPY src/ src/
RUN dotnet publish src/SchoolAdmission.Api/SchoolAdmission.Api.csproj \
    -c Release --no-restore -o /app/publish -p:UseAppHost=false

# --- Runtime: small Alpine image, framework-dependent, non-root ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# HTTP only on 8080. Alpine has no ICU, so globalization is invariant.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
EXPOSE 8080

COPY --from=build /app/publish .

# APP_UID is the built-in non-root "app" user of the .NET images.
USER $APP_UID

# Exec form so `docker stop` sends SIGTERM straight to the app.
ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
