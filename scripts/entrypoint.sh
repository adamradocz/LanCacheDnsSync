#!/bin/sh

# Update GID if needed
if [ "${PGID}" != "$(id -g appuser 2>/dev/null)" ]; then
    groupmod -o -g "${PGID}" appgroup 2>/dev/null || true
fi

# Update UID if needed
if [ "${PUID}" != "$(id -u appuser 2>/dev/null)" ]; then
    usermod -o -u "${PUID}" appuser 2>/dev/null || true
fi

# Ensure directories and existing files are accessible and writable by the target user.
# The 2>/dev/null || true ensures that if /data/config.json is mounted read-only (:ro), the command will safely continue without errors.
chown -R "${PUID}:${PGID}" /app /data /userfilters 2>/dev/null || true

# Direct cron output to stdout/stderr instead of a log file, run once a day at 02:00.
echo "0 2 * * * su-exec ${PUID}:${PGID} /app/check-for-updates.sh >> /proc/1/fd/1 2>&1" | crontab -

echo "[INFO] Running as UID: ${PUID}, GID: ${PGID}"

# Run the update check immediately on container start as the configured user.
su-exec "${PUID}:${PGID}" /app/check-for-updates.sh

# Run cron in the foreground (-f) so the container stays alive; -l 8 logs to
# stdout (BusyBox crond flag), letting `docker logs` show cron activity.
exec crond -f -l 8