#!/usr/bin/env bash
set -euo pipefail

app_password="$(cat "$APP_DB_PASSWORD_FILE")"
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
    -v app_password="$app_password" <<'SQL'
CREATE ROLE musicstore_migrator NOLOGIN;
CREATE ROLE musicstore_app LOGIN PASSWORD :'app_password';
REVOKE ALL ON DATABASE musicstore FROM PUBLIC;
GRANT CONNECT ON DATABASE musicstore TO musicstore_app;
GRANT USAGE ON SCHEMA public TO musicstore_app;
GRANT CREATE ON SCHEMA public TO musicstore_migrator;
ALTER DEFAULT PRIVILEGES FOR ROLE musicstore_migrator IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO musicstore_app;
ALTER DEFAULT PRIVILEGES FOR ROLE musicstore_migrator IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO musicstore_app;
SQL
