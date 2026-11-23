#!/usr/bin/env bash
set -euo pipefail

export PGPASSWORD="$(cat /run/secrets/postgres_password)"
export RESTORE_DATABASE_URL="${RESTORE_DATABASE_URL:-postgresql://postgres@restore-db:5432/musicstore}"
export RESTORE_CONFIRM=restore-musicstore
exec bash /scripts/restore.sh "${1:-/backups/musicstore-lab4.dump}"
