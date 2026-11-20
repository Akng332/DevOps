#!/usr/bin/env bash
set -euo pipefail

: "${RESTORE_DATABASE_URL:?Set RESTORE_DATABASE_URL for the destination database}"
: "${RESTORE_CONFIRM:?Set RESTORE_CONFIRM=restore-musicstore after checking the destination}"
[[ "$RESTORE_CONFIRM" == restore-musicstore ]] || { echo 'Invalid RESTORE_CONFIRM' >&2; exit 2; }

backup_file="${1:?Usage: scripts/restore.sh path/to/backup.dump}"
[[ -f "$backup_file" ]] || { echo "Backup not found: $backup_file" >&2; exit 2; }

pg_restore --clean --if-exists --no-owner --no-acl --role=musicstore_migrator \
    --exit-on-error -d "$RESTORE_DATABASE_URL" "$backup_file"
echo 'Restore completed'
