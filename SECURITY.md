\# Требования безопасности



Документ фиксирует обязательные требования к развёртыванию MusicStore

и описывает, как проверить их соблюдение.



\## 1. Приложение не запускается от root



Служба `musicstore-api` работает под непривилегированным системным

пользователем (`musicstore`, создаётся с `--system --shell /usr/sbin/nologin`).



Проверка на App Server:



&#x20;   grep -E '^User=' /etc/systemd/system/musicstore-api.service

&#x20;   ps -o user= -p "$(systemctl show -p MainPID --value musicstore-api)"



Ожидается: `User=musicstore` и пользователь процесса — не `root`.



\## 2. Лишние порты не открыты



\### App Server



Разрешено входящее:



\- 22/tcp (SSH) — только из доверенной подсети.

\- 8080/tcp (HTTP API) — для клиентов.

\- Всё остальное закрыто (ufw `default deny incoming`).



\### DB Server



Разрешено входящее:



\- 22/tcp (SSH) — только из доверенной подсети.

\- 5432/tcp (PostgreSQL) — только с IP App Server.

\- Всё остальное закрыто.



Проверка:



&#x20;   sudo ufw status verbose

&#x20;   sudo ss -tlnp



\## 3. Пароли и секреты не хранятся в репозитории



\- В репозитории разрешены только шаблоны: `.env.example`, `deploy/musicstore.env.example`.

\- Реальные значения — в `/etc/musicstore/musicstore.env` на App Server:

&#x20; владелец `root:root`, права `600`.

\- JWT-ключ генерируется на сервере: `openssl rand -base64 32`.

\- Пароль роли PostgreSQL задаётся интерактивно (`\\password`).



Проверка (ищем только зашитые значения, не переменные в JS):



&#x20;   git grep -nE '(Host=\[^;]\*Password=\[^"<;]+|Jwt\_\_Key=\[A-Za-z0-9+/=]{20,})' -- '\*.cs' '\*.json' '\*.sh' '\*.yml' '\*.yaml' '\*.env' ':!\*.example' ':!deploy/\*.sh' ':!docs/\*'



Ожидается: пусто.



\### Известные ложные срабатывания



Файл `src/MusicStore.Api/wwwroot/script.js` содержит слово `password`

в клиентском JS — это не секрет, а ссылка на поле формы.



\## 4. Права на файлы и каталоги



\- `/opt/musicstore` — владелец `musicstore:musicstore`, `chmod -R o-rwx`.

\- `/etc/musicstore/musicstore.env` — `root:root`, `600`.

\- Unit-файл — `root:root`, `644`.

\- SSH: вход по ключам, `PermitRootLogin no`, `PasswordAuthentication no`.



Проверка:



&#x20;   ls -la /opt/musicstore /etc/musicstore

&#x20;   sudo sshd -T | grep -Ei 'permitrootlogin|passwordauthentication'

