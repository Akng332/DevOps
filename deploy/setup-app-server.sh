#!/usr/bin/env bash
# =========================================================
# Развёртывание MusicStore на App Server (Debian 12)
# Запускать из корня репозитория: sudo bash deploy/setup-app-server.sh
# =========================================================
set -euo pipefail

APP_USER="musicstore"
APP_DIR="/opt/musicstore"
CONF_DIR="/etc/musicstore"
UNIT_PATH="/etc/systemd/system/musicstore-api.service"

if [[ $EUID -ne 0 ]]; then
  echo "Запустите скрипт от root (sudo)." >&2
  exit 1
fi

echo "==> 1/6. Установка .NET SDK 10 и утилит"
apt-get update
apt-get install -y wget apt-transport-https ca-certificates
wget -q https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O /tmp/ms.deb
dpkg -i /tmp/ms.deb
rm -f /tmp/ms.deb
apt-get update
apt-get install -y dotnet-sdk-10.0

echo "==> 2/6. Создание пользователя ${APP_USER} (если нет)"
if ! id -u "${APP_USER}" >/dev/null 2>&1; then
  useradd -r -s /usr/sbin/nologin -d "${APP_DIR}" "${APP_USER}"
fi

echo "==> 3/6. Публикация приложения"
dotnet restore ./MusicStore.sln
dotnet publish ./src/MusicStore.Api/MusicStore.Api.csproj \
  --configuration Release \
  --output "${APP_DIR}"

chown -R "${APP_USER}:${APP_USER}" "${APP_DIR}"
chmod -R o-rwx "${APP_DIR}"

echo "==> 4/6. Каталог для конфига ${CONF_DIR}"
mkdir -p "${CONF_DIR}"
if [[ ! -f "${CONF_DIR}/musicstore.env" ]]; then
  cp deploy/musicstore.env.example "${CONF_DIR}/musicstore.env"
  echo "!!! Отредактируйте ${CONF_DIR}/musicstore.env и заполните реальные значения."
fi
chown root:root "${CONF_DIR}/musicstore.env"
chmod 600 "${CONF_DIR}/musicstore.env"

echo "==> 5/6. Установка systemd-службы"
cp deploy/musicstore.service "${UNIT_PATH}"
systemctl daemon-reload
systemctl enable musicstore-api

echo "==> 6/6. Готово. Запустите:"
echo "    sudo nano ${CONF_DIR}/musicstore.env   # заполнить секреты"
echo "    sudo systemctl start musicstore-api"
echo "    sudo systemctl status musicstore-api"
