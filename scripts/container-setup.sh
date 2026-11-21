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
    # Compose bind-mounts each secret into containers with different UIDs.
    # The host directory is private; only services explicitly mounting a
    # given file receive it.
    chmod 644 "secrets/$name"
done
echo 'Container secrets and backup directory are ready'
