# Развёртывание MusicStore с нуля на новом устройстве

Это последовательная инструкция для восстановления учебного проекта **MusicStore** из GitHub на новом компьютере: исходники, основное приложение с PostgreSQL, отдельный staging, автоматическая доставка из GitHub Actions и проверки. Команды соответствуют ветке [`lab6/cd`](https://github.com/Akng332/DevOps/tree/lab6/cd). Для обычного использования магазина после установки см. [руководство пользователя](USER_GUIDE.md).

> **Основной маршрут:** Windows-хост с VirtualBox и ВМ `MusicStore-App` на Debian 13. На этой ВМ Docker Compose запускает приложение и БД на порту `8080`, а второй Compose-проект запускает staging на порту `18080`. Это воспроизводит актуальный контейнерный проект. ВМ `MusicStore-DB` и systemd-вариант из ранней ЛР описаны отдельно в конце: одновременно с основным Compose-приложением на порту `8080` их запускать не нужно.

## 0. Что потребуется

- Компьютер с аппаратной виртуализацией, доступом в интернет и запасом свободного места для двух ВМ, образов Docker и резервных копий. Не начинайте загрузку образов, если системный диск почти заполнен: VirtualBox может поставить ВМ на паузу.
- Git на Windows, VirtualBox и установочный ISO Debian 13. Для основного маршрута нужна одна ВМ; для воспроизведения отдельной ЛР по двум серверам — вторая.
- Доступ к публичному репозиторию `https://github.com/Akng332/DevOps`. Для автоматической доставки нужна доступность GitHub API и GHCR **с ВМ**. Если пакет GHCR станет закрытым, учётной записи доставки дополнительно понадобится авторизация с правом чтения пакета.
- Доступ к учётной записи GitHub, если планируете изменять репозиторий и запускать свой CI. Для простого клонирования и запуска опубликованного кода вход в GitHub не нужен.

Здесь и далее используются адреса существующего учебного стенда. Они **зашиты** в `deploy/staging/compose.yaml` и `deploy/staging/deploy.sh`; для пошагового воспроизведения назначьте их точно так же:

| Устройство | Интерфейс Host-only | Назначение |
| --- | --- | --- |
| Windows-хост | `192.168.56.1/24` | Браузер, SSH, проверка API |
| `MusicStore-App` | `192.168.56.10/24` | Основное приложение и staging |
| `MusicStore-DB` — необязательно | `192.168.56.11/24` | Отдельная БД для systemd-варианта ранней ЛР |

Если сеть уже использует другие адреса, перед установкой поменяйте и проверьте все упоминания `192.168.56.10` в staging-конфигурации и скрипте доставки. Простого изменения адреса ВМ недостаточно.

## 1. Получить исходники на Windows

Откройте PowerShell в удобном каталоге:

```powershell
git clone https://github.com/Akng332/DevOps.git
cd DevOps
git switch lab6/cd
git rev-parse --short HEAD
```

Ветка `main` может содержать более раннюю версию проекта. Для полного стенда с CI и staging используйте именно `lab6/cd`. После клонирования прочитайте [README](../README.md), [описание CI](lab5.md) и [описание CD](lab6.md), если нужна внутренняя логика.

## 2. Создать ВМ `MusicStore-App`

1. В VirtualBox создайте ВМ Debian 13 без графического окружения. Для учебного стенда выделите хотя бы 2 vCPU, 2 ГБ ОЗУ и динамический диск не менее 20 ГБ; размер образов и резервных копий потребует свободное место на хосте.
2. Установите Debian с компонентами **SSH server** и **standard system utilities**. Создайте обычную учётную запись `admin` с правом `sudo` либо создайте её после установки через консоль ВМ.
3. В настройках ВМ задайте **Adapter 1: NAT** для выхода в интернет и **Adapter 2: Host-only Adapter** для связи с Windows. Создайте/выберите Host-only-сеть `192.168.56.0/24`, адрес хоста — `192.168.56.1`.
4. В Debian определите имена интерфейсов: `ip -br addr`. В типовой VirtualBox-конфигурации NAT — `enp0s3`, Host-only — `enp0s8`. Если имена другие, подставьте свои.
5. Для системы с `ifupdown` настройте Host-only-интерфейс в `/etc/network/interfaces`:

```ini
auto lo
iface lo inet loopback

allow-hotplug enp0s3
iface enp0s3 inet dhcp

auto enp0s8
iface enp0s8 inet static
    address 192.168.56.10/24
```

Не задавайте на Host-only-интерфейсе второй шлюз по умолчанию: его предоставляет NAT. Если установщик настроил сеть через NetworkManager или systemd-networkd, внесите статический адрес в **используемый** менеджер сети вместо создания конфликтующей конфигурации. После перезапуска проверьте `ip -br addr`, `ip route`, доступность `192.168.56.1` и исходящий доступ к `github.com`.

С Windows проверьте `ping 192.168.56.10`. Затем подключитесь: `ssh admin@192.168.56.10`. Для постоянной работы настройте вход по ключу, проверьте его в **новой** сессии и только после этого отключайте парольный SSH. Порядок настройки ключей и SSH описан в [инструкции по двум ВМ](virtualbox-deployment.md#администратор-и-ssh).

## 3. Установить Docker Engine и Compose на ВМ

Следующие команды выполняются в Debian на `MusicStore-App`. Они основаны на [официальной инструкции Docker для Debian](https://docs.docker.com/engine/install/debian/). Если Docker уже установлен, сначала проверьте `docker --version` и `docker compose version` и не переустанавливайте его без необходимости.

```bash
sudo apt update
sudo apt install -y ca-certificates curl git openssl python3 cron make
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc
sudo tee /etc/apt/sources.list.d/docker.sources >/dev/null <<EOF
Types: deb
URIs: https://download.docker.com/linux/debian
Suites: $(. /etc/os-release && echo "$VERSION_CODENAME")
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo systemctl enable --now docker cron
sudo docker run --rm hello-world
```

Для основного пользователя можно запускать Docker через `sudo`. Указанные ниже команды для `admin` рассчитаны на членство в группе `docker`:

```bash
sudo usermod -aG docker admin
```

После команды **завершите SSH-сеанс и войдите снова**. Проверьте `id` и `docker info`. Участник группы `docker` фактически получает широкие права на хосте, поэтому добавляйте только доверенные учётные записи. Docker отдельно предупреждает, что опубликованные порты контейнеров могут обходить правила UFW; ограничивайте доступ средствами сети VirtualBox и правилами Docker/хоста, а не полагайтесь только на UFW.

## 4. Запустить основное окружение MusicStore

На ВМ клонируйте ту же ветку. Не копируйте каталог `secrets` с другого компьютера, если хотите получить новый независимый стенд:

```bash
cd /home/admin
git clone --branch lab6/cd --single-branch https://github.com/Akng332/DevOps.git musicstore
cd /home/admin/musicstore
bash scripts/container-setup.sh
docker compose config --quiet
docker compose up -d --build db migrator app
docker compose ps
```

Скрипт создаёт локальные файлы `secrets/postgres_password`, `secrets/app_db_password` и `secrets/jwt_key`; они исключены из Git. Compose последовательно запускает PostgreSQL 17, мигратор схемы и ASP.NET Core API. Основная БД хранится в томе `musicstore-lab4_db_data`, поэтому пересоздание контейнеров само по себе не удаляет записи.

Проверка на ВМ:

```bash
curl -fsS http://127.0.0.1:8080/api/health
curl -fsS http://127.0.0.1:8080/version
curl -fsS http://127.0.0.1:8080/api/discs
```

На Windows откройте `http://192.168.56.10:8080`. В пустой базе появятся шесть демонстрационных альбомов. `/version` у локально собранного Compose-образа может вернуть `dev`: SHA передаётся именно при сборке публикуемого CI-образа.

### Создать первого администратора

Обычная регистрация в интерфейсе выдаёт роль `user`. Для первого администратора приложение принимает `BootstrapAdmin__Username` и `BootstrapAdmin__Password` **при старте**. На учебной ВМ можно один раз запустить дополнительный экземпляр приложения без публикации порта; действующая БД к этому моменту уже должна работать:

```bash
cd /home/admin/musicstore
read -rsp 'Пароль первого администратора, минимум 16 символов: ' BootstrapAdmin__Password
echo
export BootstrapAdmin__Username=admin BootstrapAdmin__Password
docker compose run --rm --no-deps \
  -e BootstrapAdmin__Username -e BootstrapAdmin__Password app
```

Дождитесь сообщения о старте приложения и остановите **этот дополнительный экземпляр** сочетанием `Ctrl+C`. Учётная запись уже создана в БД; основной контейнер продолжает работать. Затем удалите переменные из текущей сессии: `unset BootstrapAdmin__Username BootstrapAdmin__Password`. Войдите в веб-интерфейс под новым администратором и проверьте роль в профиле. Не записывайте пароль в Git или в постоянный Compose-файл.

## 5. Проверить и обслуживать основной Compose-проект

```bash
cd /home/admin/musicstore
docker compose ps
docker compose logs --tail=100 db migrator app
make container-check
```

`make container-check` выполняет расширенный учебный сценарий: пересоздание контейнеров, контроль сохранности данных, резервную копию и восстановление в отдельный том. Для обычной проверки после перезагрузки достаточно `docker compose ps` и запроса `/api/health`.

Создать резервную копию основного окружения:

```bash
cd /home/admin/musicstore
mkdir -p backups
docker compose --profile tools run --rm db-tools \
  /scripts/container-backup.sh /backups/musicstore-manual.dump
ls -lh backups/musicstore-manual.dump
```

Храните копию также **вне диска ВМ**. Команда `docker compose down` удаляет контейнеры, но сохраняет именованные тома; удаление томов или VDI без проверенной копии приведёт к потере БД. Восстановление сначала проверяйте на отдельном томе по [сценарию ЛР 4](lab4.md), не на единственной действующей базе.

## 6. Настроить отдельный staging и автоматическую доставку

Staging запускается **на той же ВМ**, но в другом Compose-проекте с собственной БД и портом `18080`. Его источник — опубликованный образ GHCR, а не локальная сборка. Настройка ниже выполняется после успешного CI в ветке `lab6/cd`.

### 6.1. Отдельная учётная запись и файлы

```bash
sudo useradd --create-home --shell /bin/bash --groups docker musicstore-deploy
sudo install -d -m 0750 -o musicstore-deploy -g musicstore-deploy /opt/musicstore-staging
sudo -u musicstore-deploy git clone --branch lab6/cd --single-branch \
  https://github.com/Akng332/DevOps.git /opt/musicstore-staging/source
sudo -u musicstore-deploy cp /opt/musicstore-staging/source/deploy/staging/compose.yaml /opt/musicstore-staging/compose.yaml
sudo -u musicstore-deploy cp /opt/musicstore-staging/source/deploy/staging/deploy.sh /opt/musicstore-staging/deploy.sh
sudo -u musicstore-deploy cp /opt/musicstore-staging/source/deploy/staging/poll.sh /opt/musicstore-staging/poll.sh
sudo -u musicstore-deploy cp /opt/musicstore-staging/source/deploy/staging/setup.sh /opt/musicstore-staging/setup.sh
sudo -u musicstore-deploy mkdir -p /opt/musicstore-staging/database /opt/musicstore-staging/scripts
sudo -u musicstore-deploy cp -a /opt/musicstore-staging/source/database/. /opt/musicstore-staging/database/
sudo -u musicstore-deploy cp -a /opt/musicstore-staging/source/scripts/. /opt/musicstore-staging/scripts/
sudo -u musicstore-deploy bash /opt/musicstore-staging/setup.sh
sudo -u musicstore-deploy docker info >/dev/null
```

`setup.sh` создаёт **новые** пароли staging и JWT-ключ. Не переносите туда файлы `secrets` основного Compose-проекта. Каталог `/opt/musicstore-staging/source` нужен `poll.sh` для сверки SHA исходников с прошедшим CI коммитом.

### 6.2. Установить расписание доставки

На чистой учётной записи создайте cron-задание:

```bash
printf '%s\n' '*/3 * * * * /bin/bash /opt/musicstore-staging/poll.sh >> /opt/musicstore-staging/deploy.log 2>&1' \
  | sudo crontab -u musicstore-deploy -
sudo crontab -u musicstore-deploy -l
```

Раз в три минуты `poll.sh` читает последний push-run ветки `lab6/cd`. Он действует только после `completed/success`, сверяет SHA ветки с SHA run, пропускает уже принятый тег и вызывает `deploy.sh`. При ошибке тег записывается в `.failed-image` и не повторяется бесконечно. Сама доставка блокируется `flock`, чтобы два запуска не конфликтовали. Скрипт `deploy.sh` скачивает образ, запускает Compose с `--no-build`, проверяет контейнер, `/api/health`, `/version` и `/api/discs`, затем обновляет `.last-good-image`. При сбое он пытается вернуть предыдущий образ.

Для **первого** развёртывания не обязательно ждать три минуты: после успешного CI выполните `sudo -u musicstore-deploy bash /opt/musicstore-staging/poll.sh`. Если последний run ещё выполняется, скрипт корректно завершится без выпуска; повторите после его завершения. Если GHCR требует вход, настройте `docker login ghcr.io` именно для `musicstore-deploy` с токеном, имеющим право чтения пакета, через `--password-stdin`; не сохраняйте токен в репозитории.

### 6.3. Проверить staging

```bash
sudo -u musicstore-deploy cat /opt/musicstore-staging/.last-good-image
sudo -u musicstore-deploy tail -n 100 /opt/musicstore-staging/deploy.log
sudo -u musicstore-deploy docker ps --filter name=musicstore-staging
curl -fsS http://192.168.56.10:18080/api/health
curl -fsS http://192.168.56.10:18080/version
curl -fsS http://192.168.56.10:18080/api/discs
```

На Windows откройте `http://192.168.56.10:18080`. SHA из `/version` должен совпасть с концом тега в `.last-good-image` и коммитом успешного запуска в [GitHub Actions](https://github.com/Akng332/DevOps/actions). Не считайте только статус `healthy` достаточным: проверка каталога подтверждает основной сценарий приложения.

Ручное развёртывание конкретного **уже опубликованного** тега: `sudo -u musicstore-deploy bash /opt/musicstore-staging/deploy.sh ghcr.io/akng332/devops:sha-<полный-SHA>`. Автоматический откат меняет образ приложения, **не** возвращает прежнюю схему или данные БД; перед несовместимой миграцией подготовьте отдельную копию и план восстановления.

## 7. GitHub Actions на новом устройстве

CI работает на GitHub, а не на Windows или ВМ. При push в `lab6/cd` workflow `.github/workflows/ci.yml` запускает тесты и покрытие, проверку миграций в отдельной БД, Semgrep/Trivy, контейнерный сценарий и после их успеха публикует образ `ghcr.io/akng332/devops:sha-<SHA>`. Для pull request публикация отключена. Если вы клонировали существующий репозиторий и не меняли код, переустанавливать CI не нужно.

Для собственного форка проверьте: Actions разрешены; токен workflow имеет право публикации пакета в GHCR; в `poll.sh` и имени образа указан **ваш** GitHub-репозиторий; целевой branch совпадает с workflow. Сам файл `.github/CODEOWNERS` не включает обязательное ревью — для него отдельно настраивают защиту ветки в GitHub.

При изменении файлов в `lab6/cd` новый успешный push-run вызовет выпуск в staging. Поэтому правка только документации в этой ветке тоже создаёт образ с новым SHA и может обновить staging. Учитывайте это при планировании демонстрации.

## 8. Отдельная ВМ БД и systemd-вариант ранней работы

Для воспроизведения ЛР 2 создайте вторую Debian-ВМ `MusicStore-DB` с NAT и Host-only-адресом `192.168.56.11`. Установите PostgreSQL 17, создайте роли `musicstore_migrator` и `musicstore_app`, БД `musicstore`, примените миграции `database/migrations`, выдайте runtime-роли права на таблицы и последовательности. Разрешите подключение к 5432 **только** с `192.168.56.10`, затем опубликуйте .NET-приложение на `MusicStore-App` и запустите службу systemd. Полные команды сети, SSH, PostgreSQL, .NET и firewall приведены в [инструкции по двум ВМ](virtualbox-deployment.md); на Debian 13 установку .NET 10 сверяйте с [официальной инструкцией Microsoft](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian).

Это **альтернативный** режим запуска приложения. Он использует тот же адрес `192.168.56.10:8080`, что и основной Compose-проект. Перед запуском systemd-версии остановите контейнерное приложение или назначьте другой порт. Старые скрипты `deploy/setup-db-server.sh` и `deploy/setup-app-server.sh` содержат пути пакетов для Debian 12 и применяют исходную схему; не запускайте их без адаптации на Debian 13 поверх уже созданной БД с миграциями.

## 9. Итоговая проверка нового устройства

| Проверка | Ожидаемый результат |
| --- | --- |
| `git branch --show-current` на ВМ | `lab6/cd` для исходного клона основного проекта. |
| `docker compose ps` в `/home/admin/musicstore` | `db` и `app` работают, мигратор завершён без ошибки. |
| `http://192.168.56.10:8080/api/health` | JSON со `status: Healthy`. |
| `http://192.168.56.10:8080/api/discs` | JSON-каталог с демонстрационными альбомами. |
| `sudo crontab -u musicstore-deploy -l` | Строка с запуском `poll.sh` каждые три минуты. |
| `.last-good-image` и staging `/version` | Один и тот же полный SHA. |
| `http://192.168.56.10:18080/api/health` и `/api/discs` | Здоровый staging и доступный каталог. |
| GitHub Actions последнего push в `lab6/cd` | Все обязательные jobs завершились успешно; образ опубликован. |
| После перезагрузки ВМ | Docker и cron поднялись, данные в томах сохранились, оба адреса снова доступны. |

## 10. Типовые проблемы

- **`docker compose` пишет, что нет доступа к сокету:** выполните команду через `sudo` или заново войдите по SSH после добавления пользователя в группу `docker`.
- **Порт 8080 занят:** проверьте `docker ps`, `ss -tlnp` и systemd-службу ранней ЛР. Не держите systemd и основной Compose на одном порту.
- **Контейнер `migrator` завершился с ошибкой:** прочитайте `docker compose logs migrator db`; проверьте, что `secrets` созданы, а том БД не содержит частично восстановленную схему.
- **Staging-контейнер здоров, но порт 18080 недоступен:** проверьте Host-only-адрес `192.168.56.10`, подключение приложения к сетям `backend` и `frontend`, правила сети и `docker ps`.
- **`.last-good-image` не появился:** проверьте, что последний push-run `lab6/cd` полностью успешен и образ доступен из ВМ; прочитайте `/opt/musicstore-staging/deploy.log`.
- **ВМ неожиданно перешла в `paused`:** проверьте свободное место на диске Windows. При нуле свободных байт VirtualBox может приостановить ВМ во время скачивания образа.
- **После переноса на другой ПК не видно старых пользователей/покупок:** Git хранит исходники, но не содержимое томов PostgreSQL. Для переноса данных нужна отдельная резервная копия БД и проверенное восстановление.

## Источники и связанные документы

- [README проекта](../README.md) и [руководство пользователя](USER_GUIDE.md).
- [Контейнерная проверка ЛР 4](lab4.md), [CI ЛР 5](lab5.md), [CD ЛР 6](lab6.md).
- [Две Debian-ВМ, сеть, SSH и systemd](virtualbox-deployment.md).
- [Официальная установка Docker Engine на Debian](https://docs.docker.com/engine/install/debian/), [официальная установка .NET на Debian](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian).
