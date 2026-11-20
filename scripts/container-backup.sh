#!/usr/bin/env bash
set -euo pipefail

export PGPASSWORD="$(cat /run/secrets/postgres_password)"
export MIGRATION_DATABASE_URL=postgresql://postgres@db:5432/musicstore
exec bash /scripts/backup.sh "${1:-/backups/musicstore-lab4.dump}"
