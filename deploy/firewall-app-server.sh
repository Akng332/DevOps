#!/usr/bin/env bash
# =========================================================
# Firewall на App Server. Запускать от root: sudo bash deploy/firewall-app-server.sh
#
# Открывает:
#   - SSH (22)          только с доверенного адреса / подсети
#   - HTTP API (8080)   только для клиентов
# Закрывает всё остальное (default deny incoming).
# =========================================================
set -euo pipefail

TRUSTED_SSH="192.168.56.0/24"     # подсеть, откуда разрешён SSH
API_PORT="8080"                   # порт приложения

apt-get update
apt-get install -y ufw

ufw --force reset
ufw default deny incoming
ufw default allow outgoing
ufw allow from "${TRUSTED_SSH}" to any port 22 proto tcp comment 'SSH'
ufw allow "${API_PORT}/tcp" comment 'MusicStore API'
ufw --force enable

ufw status verbose
echo "==> Firewall на App Server настроен."
