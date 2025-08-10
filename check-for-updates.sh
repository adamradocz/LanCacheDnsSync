#!/bin/sh

# Ensures the repo is only cloned if it hasn’t been cloned yet.
if [ ! -d /data/cache-domains/.git ]; then
    echo "[INFO] Cloning $CACHE_DOMAINS_REPO"
    git clone --depth=1 --single-branch $CACHE_DOMAINS_REPO /data/cache-domains
    /app/update-dns-rewrite-rules.sh
    return
fi

# Step into the directory where the git repository is located
cd /data/cache-domains

git fetch origin

# Get the latest commit from the local repository
LOCAL=$(git rev-parse HEAD)

# Get the latest commit from the remote tracking branch
REMOTE=$(git rev-parse @{u})

# Update if the local repository is behind the remote repository
if [ "$LOCAL" != "$REMOTE" ]; then
    echo "[INFO] Local repository is behind remote. Updating..."
    git pull
    cd /app
    ./update-dns-rewrite-rules.sh
else
    echo "[INFO] Local repository is up to date. No update needed."
fi