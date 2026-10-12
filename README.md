# Музыкальный магазин

MusicStore — ASP.NET Core API и веб-интерфейс для учёта музыкантов, произведений, дисков, продаж и остатков. Приложение использует PostgreSQL и токены JWT.

## Требования

- .NET SDK 10.x.
- PostgreSQL 17.x (локально или на отдельном сервере).
- `psql` для создания базы и применения схемы.
- Git для получения исходного кода.

Проверить установленные версии можно командами:

```powershell
dotnet --list-sdks
psql --version
```

## Подготовить PostgreSQL

Команды ниже создают отдельного владельца схемы и пользователя приложения. Приложение работает от `musicstore_app`; эта роль не должна владеть таблицами или создавать их.

Откройте локальную сессию администратора PostgreSQL:

```powershell
psql -U postgres
```

В `psql` выполните:

```sql
CREATE ROLE musicstore_migrator NOLOGIN;
CREATE ROLE musicstore_app LOGIN;
CREATE DATABASE musicstore OWNER musicstore_migrator;
\password musicstore_app
\q
```

Команда `\password` интерактивно задаст пароль роли. Не вставляйте пароль в SQL-команду или историю терминала. Из корня репозитория примените начальную схему:

```powershell
psql -U postgres -d musicstore -f .\database\postgresql-schema.sql
```

Затем откройте базу от имени администратора `postgres` и выдайте приложению права на подключение и работу с данными:

```powershell
psql -U postgres -d musicstore
```

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
\q
```

Скрипт схемы находится в [database/postgresql-schema.sql](./database/postgresql-schema.sql). Описание дальнейшего сопровождения схемы — в [database/README.md](./database/README.md). Приложение не создаёт и не мигрирует таблицы автоматически при запуске.

## Настроить приложение

Из корня репозитория откройте PowerShell и задайте строку подключения. Пароль БД будет запрошен скрытым вводом и останется только в переменной текущей сессии:

```powershell
$dbPasswordSecure = Read-Host "Пароль роли musicstore_app" -AsSecureString
$dbPassword = [System.Net.NetworkCredential]::new("", $dbPasswordSecure).Password
$env:ConnectionStrings__Default = "Host=localhost;Port=5432;Database=musicstore;Username=musicstore_app;Password=$dbPassword"
$dbPassword = $null
```

Создайте случайный JWT-ключ длиной 32 байта и задайте параметры токена:

```powershell
$randomBytes = New-Object byte[] 32
$randomGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$randomGenerator.GetBytes($randomBytes)
$env:Jwt__Key = [Convert]::ToBase64String($randomBytes)
$randomGenerator.Dispose()
$env:Jwt__Issuer = "MusicStoreApi"
$env:Jwt__Audience = "MusicStoreClient"
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

При первом запуске можно создать администратора приложения. Введите уникальный пароль длиной не менее 16 символов:

```powershell
$adminPasswordSecure = Read-Host "Пароль первого администратора (не менее 16 символов)" -AsSecureString
$env:BootstrapAdmin__Username = "admin"
$env:BootstrapAdmin__Password = [System.Net.NetworkCredential]::new("", $adminPasswordSecure).Password
```

Эти переменные нужны только при первом создании пользователя. После того как приложение запустится и создаст администратора, остановите его сочетанием `Ctrl+C`, удалите bootstrap-переменные и запустите снова:

```powershell
Remove-Item Env:BootstrapAdmin__Username, Env:BootstrapAdmin__Password -ErrorAction SilentlyContinue
dotnet run --project .\src\MusicStore.Api\MusicStore.Api.csproj --launch-profile http
```

Пример конфигурации для systemd находится в [.env.example](./.env.example). Это шаблон переменных окружения; приложение само не загружает файл `.env`. Не коммитьте реальные пароли и JWT-ключи.

## Восстановить, собрать и запустить

Из корня репозитория выполните:

```powershell
dotnet restore .\MusicStore.sln
dotnet build .\MusicStore.sln --configuration Release --no-restore
dotnet test .\MusicStore.sln --configuration Release --no-build
dotnet run --project .\src\MusicStore.Api\MusicStore.Api.csproj --launch-profile http
```

При первом старте приложение заполнит пустую базу демонстрационными данными. Откройте веб-интерфейс по адресу `http://localhost:5058`, Swagger — `http://localhost:5058/swagger`.

Проверить API можно в другом окне PowerShell:

```powershell
Invoke-RestMethod http://localhost:5058/api/health
Invoke-RestMethod http://localhost:5058/api/discs
```

В текущем solution отдельный тестовый проект пока не добавлен, поэтому `dotnet test` не запускает тестовые кейсы.

## Опубликовать приложение

Создайте Release-публикацию:

```powershell
dotnet publish .\src\MusicStore.Api\MusicStore.Api.csproj --configuration Release --output .\publish
```

Для локального запуска опубликованной сборки сохраните переменные подключения и JWT из раздела настройки и выполните:

```powershell
dotnet .\publish\MusicStore.Api.dll
```

Полная инструкция по развёртыванию на двух Debian-ВМ без графической оболочки, включая статические адреса, SSH, firewall и systemd, находится в [docs/virtualbox-deployment.md](./docs/virtualbox-deployment.md).

## Внести изменения

Правила веток, проверок и pull request описаны в [CONTRIBUTING.md](./CONTRIBUTING.md).
