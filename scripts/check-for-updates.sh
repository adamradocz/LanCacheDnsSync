#!/bin/sh

if [ -z "${CACHE_DOMAINS_REPO}" ]; then
    echo "[ERROR] CACHE_DOMAINS_REPO is not set."
    exit 1
fi

if [ ! -f "${CONFIG_FILE_PATH}" ]; then
    echo "[ERROR] ${CONFIG_FILE_PATH:-config.json} not found."
    echo "[ERROR] Mount your config.json into /data (see config.example.json)."
    exit 1
fi

generate_rules() {
    cd /app
    ./create-adguardhome-ash.sh
    cp -fv output/adguardhome/*.txt /userfilters
}

# Clone once; afterwards only fetch/pull. No --branch is passed, so the repository's default branch is used automatically.
if [ ! -d "${CACHE_DOMAINS_DIR}/.git" ]; then
    echo "[INFO] Cloning ${CACHE_DOMAINS_REPO}"
    git clone --depth=1 --single-branch "${CACHE_DOMAINS_REPO}" "${CACHE_DOMAINS_DIR}"
    generate_rules
    exit $?
fi

# Step into the directory where the git repository is located
cd "${CACHE_DOMAINS_DIR}"

git fetch origin

# Get the latest commit from the local repository
local_rev=$(git rev-parse HEAD)

# Get the latest commit from the remote tracking branch
remote_rev=$(git rev-parse '@{u}')

# Update if the local repository is behind the remote repository, or if /userfilters has no rules
if [ "${local_rev}" != "${remote_rev}" ]; then
    echo "[INFO] Local repository is behind remote. Updating..."
    git pull
    generate_rules
elif ! ls /userfilters/*.txt >/dev/null 2>&1; then
    echo "[INFO] /userfilters is empty. Generating rules..."
    generate_rules
else
    echo "[INFO] Local repository is up to date. No update needed."
fi
