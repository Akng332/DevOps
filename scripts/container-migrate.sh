#!/usr/bin/env bash
set -euo pipefail

export PGPASSWORD="$(cat /run/secrets/postgres_password)"
exec bash /scripts/migrate.sh
