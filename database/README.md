# Изменения схемы

`postgresql-schema.sql` создаёт начальную схему для PostgreSQL. Исторические EF Core миграции SQLite удалены при переводе проекта на PostgreSQL и не подходят для применения к PostgreSQL.

Для учебного стенда ведите последующие изменения схемы отдельными упорядоченными SQL-скриптами и применяйте их владельцем схемы `musicstore_migrator`. Приложение запускается от `musicstore_app`, которому выданы только `SELECT`, `INSERT`, `UPDATE`, `DELETE` и доступ к identity-sequences.
