#!/bin/sh

# Run the update script immediately.
/app/check-for-updates.sh

# Run cron in the foreground (-f), so container doesn't exit.
cron -f