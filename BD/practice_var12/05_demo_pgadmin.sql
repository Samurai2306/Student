-- Вариант 12. Демонстрационные запросы для pgAdmin (Query Tool → F5)
-- Каждый блок можно выполнять отдельно; результат появится на вкладке Data Output

-- ============================================================
-- ЗАДАНИЕ 1. Функции
-- ============================================================

-- 1.1 ФИО с инициалами
SELECT
    full_name,
    format_fio(full_name) AS short_fio
FROM authors
ORDER BY full_name;

-- 1.2 Полное название степени
SELECT DISTINCT
    d.type,
    ss.section_name,
    get_degree_full_name(d.type, ss.section_name) AS degree_full_name
FROM dissertations d
JOIN specializations sp ON sp.spec_id = d.spec_id
JOIN science_sections ss ON ss.section_id = sp.section_id
ORDER BY ss.section_name, d.type;

-- 1.3 Возраст автора на текущую дату и на дату защиты
SELECT
    a.full_name,
    a.birth_date,
    d.defense_date,
    calculate_age(a.birth_date) AS age_now,
    calculate_age(a.birth_date, d.defense_date) AS age_at_defense
FROM authors a
JOIN dissertations d ON d.author_id = a.author_id
ORDER BY a.full_name, d.defense_date;


-- ============================================================
-- ЗАДАНИЕ 2. Процедуры (табличный вывод для pgAdmin)
-- ============================================================

-- 2.1 Отчёт по диссертациям за 2023 год
SELECT * FROM fn_annual_dissertation_report(2023);

-- 2.1 Отчёт за 2024 год
SELECT * FROM fn_annual_dissertation_report(2024);

-- 2.2 Поиск дублей авторов (до объединения)
SELECT * FROM v_duplicate_authors;

-- 2.2 Объединение дублей (CALL — сообщения на вкладке Messages)
-- CALL merge_duplicate_authors();

-- 2.3 Список авторов с учёными степенями
SELECT * FROM v_authors_degrees;


-- ============================================================
-- ЗАДАНИЕ 3. Триггеры — проверка
-- ============================================================

-- 3.1 Нормализация типа при вставке (раскомментируйте для проверки)
-- BEGIN;
-- INSERT INTO dissertations (author_id, title, type, spec_id, defense_date, approval_date)
-- VALUES (1, 'Тест нормализации типа', 'канд', 1, '2024-01-01', '2024-02-01');
-- SELECT diss_id, type FROM dissertations WHERE title = 'Тест нормализации типа';
-- ROLLBACK;

-- 3.1 Ошибка: дата в будущем (раскомментируйте — должна быть ошибка)
-- INSERT INTO dissertations (author_id, title, type, spec_id, defense_date)
-- VALUES (1, 'Тест будущей даты', 'кандидатская', 1, '2099-01-01');

-- 3.3 Архив изменений авторов (раскомментируйте для проверки)
-- UPDATE authors SET passport_data = '4501 111111 NEW' WHERE author_id = 1;
-- SELECT * FROM authors_archive ORDER BY archive_id DESC LIMIT 5;


-- ============================================================
-- Удобные представления
-- ============================================================

-- Полная информация по всем диссертациям
SELECT * FROM v_dissertations_full ORDER BY defense_date DESC;
