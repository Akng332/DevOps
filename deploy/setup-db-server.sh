#!/usr/bin/env bash
# =========================================================
# Развёртывание PostgreSQL на DB Server (Debian 12)
# Запускать из корня репозитория: sudo bash deploy/setup-db-server.sh
# =========================================================
set -euo pipefail

DB_NAME="musicstore"
DB_MIGRATOR="musicstore_migrator"   # владелец схемы, NOLOGIN
DB_APP="musicstore_app"             # роль приложения, LOGIN

if [[ $EUID -ne 0 ]]; then
  echo "Запустите скрипт от root (sudo)." >&2
  exit 1
fi

echo "==> 1/4. Установка PostgreSQL 17"
apt-get update
apt-get install -y postgresql postgresql-contrib

echo "==> 2/4. Создание роли-владельца схемы (${DB_MIGRATOR}) и роли приложения (${DB_APP})"
sudo -u postgres psql <<SQL
DO \$\$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${DB_MIGRATOR}') THEN
    CREATE ROLE ${DB_MIGRATOR} NOLOGIN;
  END IF;
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${DB_APP}') THEN
    CREATE ROLE ${DB_APP} LOGIN;
  END IF;
END
\$\$;
SQL

echo "Задайте пароль для роли ${DB_APP}:"
sudo -u postgres psql -c "\\password ${DB_APP}"

echo "==> 3/4. Создание базы данных ${DB_NAME}"
sudo -u postgres psql -tc "SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'" | grep -q 1 || \
  sudo -u postgres createdb -O "${DB_MIGRATOR}" "${DB_NAME}"

echo "==> 4/4. Применение схемы и выдача прав"
sudo -u postgres psql -d "${DB_NAME}" -f database/postgresql-schema.sql
sudo -u postgres psql -d "${DB_NAME}" <<SQL
REVOKE ALL ON DATABASE ${DB_NAME} FROM PUBLIC;
GRANT CONNECT ON DATABASE ${DB_NAME} TO ${DB_APP};
GRANT USAGE ON SCHEMA public TO ${DB_APP};
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO ${DB_APP};
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO ${DB_APP};
ALTER DEFAULT PRIVILEGES FOR ROLE ${DB_MIGRATOR} IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ${DB_APP};
ALTER DEFAULT PRIVILEGES FOR ROLE ${DB_MIGRATOR} IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO ${DB_APP};
SQL

echo "==> Готово. Не забудьте настроить pg_hba.conf для доступа только с App Server."
