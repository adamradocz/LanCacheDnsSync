FROM alpine:latest

# git     - clone/update the cache-domains repository
# jq      - parse config.json / cache_domains.json
# su-exec - drop privileges to user-specified PUID/PGID
# shadow  - user/group modification utilities (usermod, groupmod)
RUN apk add --no-cache git jq su-exec shadow \
    && git config --system --add safe.directory "*" \
    && addgroup -g 1000 appgroup \
    && adduser -D -u 1000 -G appgroup -h /app -s /bin/sh appuser

WORKDIR /app

COPY scripts/create-adguardhome-ash.sh scripts/check-for-updates.sh scripts/entrypoint.sh ./
RUN chmod +x ./create-adguardhome-ash.sh ./check-for-updates.sh ./entrypoint.sh

# CACHE_DOMAINS_REPO - git URL to clone; always uses the repo's default branch.
ENV CACHE_DOMAINS_REPO="https://github.com/uklans/cache-domains.git"

# CACHE_DOMAINS_DIR - directory the cache-domains repo is cloned into.
ENV CACHE_DOMAINS_DIR="/data/cache-domains"

ENV CONFIG_FILE_PATH="/data/config.json"

# PUID / PGID - user and group ID under which the update script runs.
ENV PUID=1000
ENV PGID=1000

VOLUME ["/data", "/userfilters"]

CMD ["./entrypoint.sh"]
