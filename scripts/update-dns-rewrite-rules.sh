#!/bin/sh
last_commit_time=$(git --git-dir="/data/cache-domains/.git" log -1 --date=iso8601-strict --format=%cd)

dotnet LanCacheDnsRewriteGen.dll --repository-path "/data/cache-domains" --lancache-ipv4 $LANCACHE_IPV4 --last-modified $last_commit_time
exit_code=$?
if [ $exit_code -eq 0 ]; then
    cp lancache.txt /userfilters/lancache.txt
    echo "[INFO] lancache.txt updated successfully."
else
    echo "[ERROR] LanCacheDnsRewriteGen failed with exit code $exit_code."
    exit $exit_code
fi