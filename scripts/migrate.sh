#!/usr/bin/env bash
set -euo pipefail

: "${MIGRATION_DATABASE_URL:?Set MIGRATION_DATABASE_URL for the schema owner or an administrator}"

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# The application account has no schema-changing permissions.
psql "$MIGRATION_DATABASE_URL" -X -v ON_ERROR_STOP=1 -q -c '
SET ROLE musicstore_migrator;
CREATE TABLE IF NOT EXISTS public.schema_migrations (
    version text PRIMARY KEY,
    applied_at timestamptz NOT NULL DEFAULT now()
);'

for file in "$root"/database/migrations/[0-9][0-9][0-9]_*.sql; do
    version="$(basename "$file" | cut -d_ -f1)"
    if [[ -n "${MIGRATION_TARGET:-}" && "$version" > "$MIGRATION_TARGET" ]]; then
        break
    fi
    applied="$(psql "$MIGRATION_DATABASE_URL" -X -v ON_ERROR_STOP=1 -Atqc \
        "SELECT count(*) FROM public.schema_migrations WHERE version = '$version'")"
    if [[ "$applied" == 1 ]]; then
        echo "Skipping $version (already applied)"
        continue
    fi

    echo "Applying $version"
    {
        printf 'SET ROLE musicstore_migrator;\nBEGIN;\n'
        cat "$file"
        printf "\nINSERT INTO public.schema_migrations(version) VALUES ('%s');\nCOMMIT;\n" "$version"
    } | psql "$MIGRATION_DATABASE_URL" -X -v ON_ERROR_STOP=1 -q
done
