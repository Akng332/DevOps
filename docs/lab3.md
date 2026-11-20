# Лабораторная работа №3: проверки, миграции и резервное копирование

## Что добавлено

- Команда `make setup` восстанавливает пакеты .NET и устанавливает закреплённую
  версию Semgrep в локальное окружение. Версия .NET SDK задана в `global.json`,
  версии библиотек — в файлах проектов.
- `make run`, `make format`, `make test`, `make sast`, `make migrate`,
  `make backup`, `make restore` и `make verify` выполняют отдельные шаги.
- Тесты проверяют регистрацию и вход, каталог, ограничения входных данных,
  оформление покупки, остатки, начальные данные и новую функцию списка товаров
  с низким остатком. Тестовая база SQLite создаётся в памяти отдельно для
  каждого теста. Порог покрытия строк в `make test` — 40%.
- Четыре версии схемы включают начальную схему, ограничения данных, порог
  остатка и ограничение ролей. `GET /api/discs/low-stock` доступен администратору;
  порог можно указать при создании и обновлении диска.
- `make db-check` на пустой отдельной PostgreSQL-базе проверяет создание схемы,
  обновление заполненной базы, повторный запуск миграций и восстановление после
  намеренного изменения количества. Эта проверка входит в `make verify`.

## Как проверить на Linux-стенде

Нужны .NET SDK 10.0.404, Python 3 с `venv`, GNU Make, PostgreSQL 17 с утилитами
`psql`, `pg_dump` и `pg_restore`. Для статического анализа Semgrep требуется
доступ к его реестру правил при запуске.

Создайте отдельную пустую базу `musicstore_lab3_test`. Администратор PostgreSQL
должен иметь право переключаться на роль `musicstore_migrator`; сама роль уже
используется в настройке ЛР 2. Укажите адрес только тестовой базы:

```bash
export TEST_DATABASE_URL='postgresql://postgres@localhost:5432/musicstore_lab3_test'
make setup
make verify
```

Сценарий `check-db.sh` откажется работать, если в тестовой базе уже есть
таблицы. Он не затрагивает действующую базу приложения.

Для приложения задайте `MIGRATION_DATABASE_URL`, выполните `make backup`, затем
`make migrate`. Для восстановления задайте адрес назначения в
`RESTORE_DATABASE_URL` и подтверждение `RESTORE_CONFIRM=restore-musicstore`:

```bash
export MIGRATION_DATABASE_URL='postgresql://postgres@localhost:5432/musicstore'
make backup BACKUP_FILE=backups/before-migration.dump
make migrate

export RESTORE_DATABASE_URL='postgresql://postgres@localhost:5432/musicstore_lab3_test'
export RESTORE_CONFIRM=restore-musicstore
make restore BACKUP_FILE=backups/before-migration.dump
```

Не подставляйте пароль в командную строку. Используйте `.pgpass` с правами
`0600` или интерактивный запрос PostgreSQL. Файлы `backups/` исключены из Git.

## Проверка на защите

`make verify` последовательно проверяет формат, запускает тесты с порогом
покрытия, анализирует код Semgrep и проверяет изменения базы. Для демонстрации
дефекта измените условие `Quantity <= ReorderThreshold` в `GetLowStock` на
`Quantity < ReorderThreshold`: тест `LowStock_UsesPersistedThreshold` должен
упасть. Верните исходное условие и повторите проверку.

После прохождения проверки можно показать версии в `schema_migrations`,
количество товара до повреждения и после восстановления, а также отчёт покрытия
`tests/MusicStore.Tests/coverage.cobertura.xml`.

Проверки не следует пропускать, а порог покрытия и правила анализатора менять
без объяснения причины в запросе на слияние.
