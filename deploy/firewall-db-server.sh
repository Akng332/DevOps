#!/usr/bin/env bash
# =========================================================
# Firewall на DB Server. Запускать от root: sudo bash deploy/firewall-db-server.sh
#
# Порт 5432 (PostgreSQL) доступен ТОЛЬКО с IP App Server.
# =========================================================
set -euo pipefail

APP_SERVER_IP="192.168.56.10"     # <-- ЗАМЕНИТЕ на реальный IP App Server
TRUSTED_SSH="192.168.56.0/24"     # подсеть для SSH

apt-get update
apt-get install -y ufw

ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow from "${TRUSTED_SSH}" to any port 22 proto tcp comment 'SSH'
ufw allow from "${APP_SERVER_IP}" to any port 5432 proto tcp comment 'PostgreSQL for App Server'
ufw --force enable

ufw status verbose
echo "==> Firewall на DB Server настроен. 5432 доступен только ${APP_SERVER_IP}."
