# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/LanCacheDnsRewriteGen/LanCacheDnsRewriteGen.csproj", "src/LanCacheDnsRewriteGen/"]
RUN dotnet restore "./src/LanCacheDnsRewriteGen/LanCacheDnsRewriteGen.csproj"
COPY . .
WORKDIR "/src/src/LanCacheDnsRewriteGen"
RUN dotnet build "./LanCacheDnsRewriteGen.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LanCacheDnsRewriteGen.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY entrypoint.sh check-for-updates.sh update-dns-rewrite-rules.sh ./

ENV LANCACHE_IPV4="" \
    CACHE_DOMAINS_REPO="https://github.com/uklans/cache-domains.git"

RUN apt-get update && apt-get install -y git cron \
    # Cleaning up
    && rm -rf /var/lib/apt/lists/* \
    # Setup cron job to run the script daily
    && chmod +x ./entrypoint.sh ./check-for-updates.sh ./update-dns-rewrite-rules.sh \
    && mkdir -p /data/userfilters \
    # Direct cron output to stdout/stderr, instead of a log file.
    && echo "0 0 * * * /app/check-for-updates.sh >> /proc/1/fd/1 2>&1" | crontab -

VOLUME ["/data/cache-domains", "/data/userfilters"]

CMD ["./entrypoint.sh"]