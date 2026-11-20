#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
umask 077
mkdir -p secrets backups
chmod 700 secrets
for name in postgres_password app_db_password jwt_key; do
    if [[ ! -s "secrets/$name" ]]; then
        openssl rand -hex 32 > "secrets/$name"
    fi
    if [[ "$name" == app_db_password ]]; then
        # The PostgreSQL init script runs as a different UID. The enclosing
        # secrets directory remains private to the host user.
        chmod 644 "secrets/$name"
    else
        chmod 600 "secrets/$name"
    fi
done
echo 'Container secrets and backup directory are ready'
