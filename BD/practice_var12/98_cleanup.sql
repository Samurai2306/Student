-- Очистка bd_student: удаление данных, не относящихся к варианту 12
-- Запуск: psql -U admin -d bd_student -f 98_cleanup.sql

\set ON_ERROR_STOP on

BEGIN;

-- Тестовый автор без диссертаций
DELETE FROM authors
 WHERE author_id = 19
    OR full_name = 'John Doe';

-- Диссертация с датой защиты в будущем (тестовая запись для триггера)
DELETE FROM dissertations
 WHERE diss_id = 14
    OR defense_date > CURRENT_DATE
    OR approval_date > CURRENT_DATE;

-- Привести сокращённые типы к полным (если остались после старых вставок)
UPDATE dissertations
   SET type = normalize_diss_type(type)
 WHERE type IS NOT NULL
   AND type IS DISTINCT FROM normalize_diss_type(type);

-- Удалить авторов без диссертаций (если появятся после очистки)
DELETE FROM authors a
 WHERE NOT EXISTS (
     SELECT 1 FROM dissertations d WHERE d.author_id = a.author_id
 );

COMMIT;

-- Сброс последовательностей под фактический максимум id
SELECT setval('authors_author_id_seq',        COALESCE((SELECT MAX(author_id)   FROM authors), 1));
SELECT setval('dissertations_diss_id_seq',    COALESCE((SELECT MAX(diss_id)   FROM dissertations), 1));
SELECT setval('authors_archive_archive_id_seq', COALESCE((SELECT MAX(archive_id) FROM authors_archive), 1));

\echo '=== После очистки ==='
SELECT 'authors' AS tbl, COUNT(*) FROM authors
UNION ALL SELECT 'dissertations', COUNT(*) FROM dissertations
UNION ALL SELECT 'authors_archive', COUNT(*) FROM authors_archive;
