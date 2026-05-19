-- Тесты для отчёта «Чернов ГА кр4»
\set ON_ERROR_STOP on
\pset border 2
\pset format aligned

\echo ===== ЗАДАНИЕ 1.1 format_fio =====
SELECT full_name, format_fio(full_name) AS short_fio
FROM authors
ORDER BY author_id
LIMIT 6;

\echo ===== ЗАДАНИЕ 1.2 get_degree_full_name =====
SELECT get_degree_full_name('докторская', 'Технические науки') AS degree1,
       get_degree_full_name('кандидатская', 'Экономические науки') AS degree2;

\echo ===== ЗАДАНИЕ 1.3 calculate_age =====
SELECT a.full_name, a.birth_date, d.defense_date,
       calculate_age(a.birth_date) AS age_now,
       calculate_age(a.birth_date, d.defense_date) AS age_at_defense
FROM authors a
JOIN dissertations d ON d.author_id = a.author_id
WHERE a.author_id IN (1, 2, 5)
ORDER BY a.full_name, d.defense_date;

\echo ===== ЗАДАНИЕ 2.1 fn_annual_dissertation_report(2023) =====
SELECT * FROM fn_annual_dissertation_report(2023);

\echo ===== ЗАДАНИЕ 2.2 v_duplicate_authors =====
SELECT * FROM v_duplicate_authors;

\echo ===== ЗАДАНИЕ 2.3 v_authors_degrees =====
SELECT * FROM v_authors_degrees
WHERE full_name IN ('Алексеев Алексей Алексеевич', 'Лебедев Станислав Юрьевич', 'Иванов Иван Сергеевич');

\echo ===== ЗАДАНИЕ 3.2 триггер normalize_type =====
BEGIN;
INSERT INTO dissertations (author_id, title, type, spec_id, defense_date, approval_date)
VALUES (1, 'Тест нормализации типа', 'канд', 1, '2024-06-01', '2024-07-01');
SELECT diss_id, type FROM dissertations WHERE title = 'Тест нормализации типа';
ROLLBACK;

\echo ===== ЗАДАНИЕ 3.1 триггер validate (ошибка: дата в будущем) =====
INSERT INTO dissertations (author_id, title, type, spec_id, defense_date)
VALUES (1, 'Тест будущей даты', 'кандидатская', 1, '2099-01-01');
