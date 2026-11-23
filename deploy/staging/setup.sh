#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
umask 077
mkdir -p secrets
chmod 700 secrets
for name in postgres_password app_db_password jwt_key; do
    if [[ ! -s "secrets/$name" ]]; then
        openssl rand -hex 32 > "secrets/$name"
    fi
    chmod 644 "secrets/$name"
done
echo 'Staging secrets are ready'
