#!/usr/bin/env bash
set -euo pipefail

: "${TEST_DATABASE_URL:?Set TEST_DATABASE_URL to a disposable, empty PostgreSQL database}"

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

table_count="$(psql "$TEST_DATABASE_URL" -X -v ON_ERROR_STOP=1 -Atqc \
    "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'")"
[[ "$table_count" == 0 ]] || { echo 'Test database must be empty' >&2; exit 2; }

export MIGRATION_DATABASE_URL="$TEST_DATABASE_URL"
MIGRATION_TARGET=001 bash scripts/migrate.sh

# Existing data must survive every later migration.
psql "$TEST_DATABASE_URL" -X -v ON_ERROR_STOP=1 -q <<'SQL'
SET ROLE musicstore_migrator;
INSERT INTO "Musicians" ("Name") VALUES ('__lab3_check__');
INSERT INTO "Compositions" ("Title", "DurationSeconds", "PerformerType", "PerformerId")
VALUES ('__lab3_check__', 0, '', 0);
INSERT INTO "Discs" ("Title", "ImageUrl", "Price", "Quantity", "MusicianId", "CompositionId")
VALUES ('__lab3_check__', '', 100, 5,
    (SELECT "Id" FROM "Musicians" WHERE "Name" = '__lab3_check__'),
    (SELECT "Id" FROM "Compositions" WHERE "Title" = '__lab3_check__'));
SQL

bash scripts/migrate.sh
bash scripts/migrate.sh

check_value="$(psql "$TEST_DATABASE_URL" -X -v ON_ERROR_STOP=1 -Atqc \
    "SELECT \"Quantity\" || ':' || \"ReorderThreshold\" FROM \"Discs\" WHERE \"Title\" = '__lab3_check__'")"
[[ "$check_value" == '5:3' ]] || { echo 'Migration changed existing data' >&2; exit 1; }

backup_file="$(mktemp "${TMPDIR:-/tmp}/musicstore-lab3.XXXXXX.dump")"
trap 'rm -f "$backup_file"' EXIT
bash scripts/backup.sh "$backup_file"
psql "$TEST_DATABASE_URL" -X -v ON_ERROR_STOP=1 -q -c \
    'UPDATE "Discs" SET "Quantity" = 0 WHERE "Title" = '\''__lab3_check__'\'''

export RESTORE_DATABASE_URL="$TEST_DATABASE_URL"
export RESTORE_CONFIRM=restore-musicstore
bash scripts/restore.sh "$backup_file"

restored="$(psql "$TEST_DATABASE_URL" -X -v ON_ERROR_STOP=1 -Atqc \
    "SELECT \"Quantity\" FROM \"Discs\" WHERE \"Title\" = '__lab3_check__'")"
[[ "$restored" == 5 ]] || { echo 'Restore did not recover the quantity' >&2; exit 1; }

echo 'Clean migration, populated migration, repeat run and restore passed'
