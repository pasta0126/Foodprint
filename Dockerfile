# syntax=docker/dockerfile:1

# Multi-stage build for Foodprint.Web, targeting linux-arm64 (void-server is a Raspberry Pi).
# Build for the Pi with:  docker buildx build --platform linux/arm64 -t foodprint:latest --load .

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Foodprint.Core/Foodprint.Core.csproj src/Foodprint.Core/
COPY src/Foodprint.Web/Foodprint.Web.csproj src/Foodprint.Web/
COPY src/Foodprint.Cli/Foodprint.Cli.csproj src/Foodprint.Cli/
RUN dotnet restore src/Foodprint.Web/Foodprint.Web.csproj

COPY src/ src/
RUN dotnet publish src/Foodprint.Web/Foodprint.Web.csproj -c Release -o /app --no-restore

# aspnet runtime image ships tzdata, which TimeZoneInfo needs for profile time zones.
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Foodprint__ConnectionStrings__Default="Data Source=/data/foodprint.db" \
    ConnectionStrings__Default="Data Source=/data/foodprint.db" \
    Foodprint__DataProtectionKeyPath="/keys" \
    ForwardedHeaders__Enabled="true"

VOLUME ["/data", "/keys"]
EXPOSE 8080

# Applies migrations (and seeds the meal-group catalog) before the app takes traffic.
ENTRYPOINT ["dotnet", "Foodprint.Web.dll"]
