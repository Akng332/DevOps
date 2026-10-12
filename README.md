# Музыкальный магазин

Учёт музыкантов, ансамблей, произведений, дисков, цен, продаж и остатков.

## Быстрый старт

Инструкция по развёртыванию ASP.NET Core API и PostgreSQL на двух headless Debian ВМ в VirtualBox находится в [docs/virtualbox-deployment.md](./docs/virtualbox-deployment.md).

Приложение использует PostgreSQL. Начальная схема БД: [database/postgresql-schema.sql](./database/postgresql-schema.sql); правила её обновления описаны в [database/README.md](./database/README.md). Строка подключения и JWT-ключ задаются через переменные окружения .NET; пример находится в `.env.example`. Не используйте демонстрационные пароли для развёртывания.

## Правила внесения изменений

См. [CONTRIBUTING.md](./CONTRIBUTING.md)
