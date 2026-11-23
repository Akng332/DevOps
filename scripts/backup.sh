#!/usr/bin/env bash
set -euo pipefail

: "${MIGRATION_DATABASE_URL:?Set MIGRATION_DATABASE_URL}"

backup_file="${1:?Usage: scripts/backup.sh path/to/backup.dump}"
mkdir -p "$(dirname "$backup_file")"
pg_dump -Fc --no-owner --no-acl -f "$backup_file" "$MIGRATION_DATABASE_URL"
echo "Backup saved to $backup_file"
