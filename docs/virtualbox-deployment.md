# Развёртывание MusicStore на двух ВМ VirtualBox

Инструкция описывает учебный стенд с двумя Debian 13 без графической оболочки:

| ВМ | Host-only IPv4 | Назначение |
| --- | --- | --- |
| `MusicStore-App` | `192.168.56.10/24` | ASP.NET Core API |
| `MusicStore-DB` | `192.168.56.11/24` | PostgreSQL |
| Хост Windows | `192.168.56.1/24` | SSH и браузер/API-клиент |

В VirtualBox у каждой ВМ два адаптера: Adapter 1 — NAT для исходящего доступа в интернет, Adapter 2 — Host-only Adapter с сетью `192.168.56.0/24`. Используется уже существующий Host-only адаптер `192.168.56.1`; на гостях постоянные адреса `.10` и `.11`. Шлюз по умолчанию и DNS предоставляет NAT-адаптер.

Рекомендуемый размер каждой ВМ: 2 vCPU, 2 ГБ ОЗУ и динамический VDI 20 ГБ. При установке Debian выберите только **SSH server** и **standard system utilities**, не выбирайте desktop environment. Обновите ОС сразу после установки.

## Сеть Debian

Уточните имена интерфейсов командой `ip -br link`. В типичной конфигурации VirtualBox NAT — `enp0s3`, Host-only — `enp0s8`. На каждой машине настройте `/etc/network/interfaces` (подставьте её адрес):

```ini
auto lo
iface lo inet loopback

allow-hotplug enp0s3
iface enp0s3 inet dhcp

auto enp0s8
iface enp0s8 inet static
    address 192.168.56.10/24
```

На сервере БД укажите `192.168.56.11/24`. Не задавайте шлюз на Host-only интерфейсе. Перезапустите сеть или ВМ, затем проверьте `ip -br addr`, `ip route`, `ping -c 2 192.168.56.1` и связь между `.10` и `.11`.

## Администратор и SSH

Создайте отдельного администратора `admin`. Задайте ему пароль для локального `sudo`; SSH-парольная аутентификация будет отключена, а SSH-вход разрешён только по ключу. Права каталога `.ssh` — `700`, файла `authorized_keys` — `600`:

```bash
sudo useradd --create-home --shell /bin/bash --groups sudo admin
sudo passwd admin
sudo passwd -l root
sudo install -d -m 0700 -o admin -g admin /home/admin/.ssh
printf '%s\n' 'ВСТАВЬТЕ_СЮДА_ПУБЛИЧНЫЙ_SSH_КЛЮЧ' | sudo tee /home/admin/.ssh/authorized_keys >/dev/null
sudo chown admin:admin /home/admin/.ssh/authorized_keys
sudo chmod 0600 /home/admin/.ssh/authorized_keys
```

Для каждой ВМ примените `/etc/ssh/sshd_config.d/10-hardening.conf`:

```text
PermitRootLogin no
PasswordAuthentication no
KbdInteractiveAuthentication no
PubkeyAuthentication yes
AllowUsers admin
```

Проверьте конфигурацию `sudo sshd -t`, затем `sudo systemctl reload ssh`. С хоста проверьте `ssh admin@192.168.56.10` и `ssh admin@192.168.56.11`. Убедитесь, что `ssh root@...` запрещён. Не отключайте текущий сеанс, пока вход `admin` по ключу не подтверждён.

Создайте системную учётную запись приложения только на сервере приложения: `sudo useradd --system --home /var/lib/musicstore --create-home --shell /usr/sbin/nologin musicstore`. Разместите опубликованные файлы в `/opt/musicstore-api`; владелец `root:musicstore`, каталоги `0750`, файлы `0640`. Учётная запись `musicstore` получает чтение, но не запись в исполняемые файлы.

## PostgreSQL

На сервере БД установите `postgresql`. Оставьте административную роль PostgreSQL локальной. Приложение должно подключаться только к Host-only адресу БД.

Создайте роль-владельца схемы и runtime-роль. Задайте пароль runtime-роли интерактивно через `\password musicstore_app`, не включайте пароль в командную строку:

Откройте `psql` локально от системного пользователя PostgreSQL и создайте роли и БД:

```bash
sudo -u postgres psql
```

В интерактивном `psql`:

```sql
CREATE ROLE musicstore_migrator NOLOGIN;
CREATE ROLE musicstore_app LOGIN;
CREATE DATABASE musicstore OWNER musicstore_migrator;
\password musicstore_app
\q
```

Команда `\password` запросит пароль без отображения ввода. Затем скопируйте файл схемы на сервер БД: `scp database/postgresql-schema.sql admin@192.168.56.11:/tmp/postgresql-schema.sql`. Примените его от имени локального администратора PostgreSQL: `sudo -u postgres psql -d musicstore -f /tmp/postgresql-schema.sql`. Откройте подключение `sudo -u postgres psql -d musicstore` и выполните следующие права:

```sql
REVOKE ALL ON DATABASE musicstore FROM PUBLIC;
GRANT CONNECT ON DATABASE musicstore TO musicstore_app;
GRANT USAGE ON SCHEMA public TO musicstore_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO musicstore_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO musicstore_app;
ALTER DEFAULT PRIVILEGES FOR ROLE musicstore_migrator IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO musicstore_app;
ALTER DEFAULT PRIVILEGES FOR ROLE musicstore_migrator IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO musicstore_app;
```

Разрешите PostgreSQL слушать `localhost` и `192.168.56.11`. В `pg_hba.conf` добавьте правило для `musicstore_app` с источником `192.168.56.10/32` и методом `scram-sha-256`; не открывайте порт PostgreSQL для всей сети. Ограничьте firewall: SSH только от `192.168.56.1`, TCP/5432 только от `192.168.56.10`.

Например, через UFW на сервере БД:

```bash
sudo apt install ufw
sudo ufw default deny incoming
sudo ufw allow from 192.168.56.1 to any port 22 proto tcp
sudo ufw allow from 192.168.56.10 to any port 5432 proto tcp
sudo ufw enable
```

Изменение схемы выполняется отдельно администратором/владельцем `musicstore_migrator`. Runtime-роль `musicstore_app` не владеет таблицами и не может создавать/изменять схему. Приложение больше не запускает миграции БД при каждом старте.

## ASP.NET Core и systemd

На сервере приложения добавьте официальный репозиторий Microsoft для Debian 13 и установите SDK .NET 10 (он включает ASP.NET Core Runtime):

```bash
wget https://packages.microsoft.com/config/debian/13/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
sudo dpkg -i /tmp/packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
```

Сборку выполните из каталога проекта:

```bash
dotnet publish src/MusicStore.Api/MusicStore.Api.csproj -c Release -o ./publish
```

Скопируйте содержимое `publish` в `/opt/musicstore-api`, а `images` и `wwwroot` включите в публикацию. Создайте root-owned файл `/etc/musicstore-api.env` с правами `0640` и группой `musicstore`:

```ini
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
ConnectionStrings__Default=Host=192.168.56.11;Port=5432;Database=musicstore;Username=musicstore_app;Password=ЗАДАТЬ_ИНТЕРАКТИВНО
Jwt__Key=СЛУЧАЙНАЯ_СТРОКА_НЕ_КОРОЧЕ_32_БАЙТ
Jwt__Issuer=MusicStoreApi
Jwt__Audience=MusicStoreClient
Jwt__ExpiresInHours=24
```

Если нужно создать первого администратора, добавьте `BootstrapAdmin__Username` и уникальный `BootstrapAdmin__Password` длиной не менее 16 символов только на первый запуск. После успешного создания удалите эти две строки и перезапустите сервис. Не используйте `ad1/ad1`.

Пример `/etc/systemd/system/musicstore-api.service`:

```ini
[Unit]
Description=MusicStore ASP.NET Core API
After=network-online.target
Wants=network-online.target

[Service]
User=musicstore
Group=musicstore
WorkingDirectory=/opt/musicstore-api
EnvironmentFile=/etc/musicstore-api.env
ExecStart=/usr/bin/dotnet /opt/musicstore-api/MusicStore.Api.dll
Restart=on-failure
RestartSec=5
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true

[Install]
WantedBy=multi-user.target
```

Включите сервис: `sudo systemctl daemon-reload && sudo systemctl enable --now musicstore-api`. Ограничьте firewall на сервере приложения:

```bash
sudo apt install ufw
sudo ufw default deny incoming
sudo ufw allow from 192.168.56.1 to any port 22 proto tcp
sudo ufw allow from 192.168.56.1 to any port 8080 proto tcp
sudo ufw enable
```

Для учебного стенда интерфейс доступен с Windows по `http://192.168.56.10:8080`.

## Проверка

Проверьте `systemctl status musicstore-api`, `journalctl -u musicstore-api`, доступность API с хоста и соединение с БД. В PostgreSQL проверьте, что `musicstore_app` может читать/изменять строки, но не создавать таблицы. Перезагрузите обе ВМ и убедитесь, что статические адреса, SSH и сервис сохраняются.
