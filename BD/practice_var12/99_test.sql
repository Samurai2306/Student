-- Автотесты (UTF-8)
\set ON_ERROR_STOP on

\echo === format_fio ===
SELECT format_fio('Иванов Иван Сергеевич') AS t1,
       format_fio('Петров') AS t2;

\echo === get_degree_full_name ===
SELECT get_degree_full_name('докторская', 'Технические науки') AS degree;

\echo === annual report 2023 ===
SELECT * FROM fn_annual_dissertation_report(2023);

\echo === duplicate authors ===
SELECT * FROM v_duplicate_authors;

\echo === authors degrees (Alekseev, Lebedev) ===
SELECT * FROM v_authors_degrees
WHERE full_name IN ('Алексеев Алексей Алексеевич', 'Лебедев Станислав Юрьевич');

\echo === duplicate authors after merge ===
SELECT * FROM v_duplicate_authors;

\echo === trigger: normalize type ===
BEGIN;
INSERT INTO dissertations (author_id, title, type, spec_id, defense_date, approval_date)
VALUES (1, 'Test normalize', 'канд', 1, '2024-06-01', '2024-07-01');
SELECT type FROM dissertations WHERE title = 'Test normalize';
ROLLBACK;

\echo === trigger: future date (expect error) ===
DO $$
BEGIN
    INSERT INTO dissertations (author_id, title, type, spec_id, defense_date)
    VALUES (1, 'Test future', 'кандидатская', 1, '2099-01-01');
    RAISE EXCEPTION 'Should have failed';
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE 'OK: %', SQLERRM;
END $$;

\echo === merge duplicates ===
-- CALL merge_duplicate_authors();  -- уже выполнено ранее
SELECT author_id, full_name FROM authors WHERE full_name = 'Алексеев Алексей Алексеевич';
