-- Вариант 12. БД диссертаций — полная установка
-- База: bd_student, пользователь: admin
-- Запуск в pgAdmin: Query Tool → Open File → Execute (F5)

\echo '=== 01. Домены ==='
\ir 01_domains.sql

\echo '=== 02. Функции ==='
\ir 02_functions.sql

\echo '=== 03. Процедуры и представления ==='
\ir 03_procedures_and_views.sql

\echo '=== 04. Триггеры ==='
\ir 04_triggers.sql

\echo '=== Готово. Смотрите 05_demo_pgadmin.sql для проверки ==='
