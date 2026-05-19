-- Вариант 12. Задание 2 — процедуры и представления для pgAdmin

-- ============================================================
-- 2.1 Отчёт по диссертациям за год (процедура + табличная функция)
-- ============================================================

CREATE OR REPLACE FUNCTION fn_annual_dissertation_report(p_year INTEGER)
RETURNS TABLE (
    report_year INTEGER,
    section_name TEXT,
    spec_name TEXT,
    author_fio TEXT,
    defense_date DATE,
    dissertation_title TEXT,
    diss_type TEXT,
    sort_order INTEGER
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        p_year,
        ss.section_name,
        sp.spec_name,
        format_fio(a.full_name),
        d.defense_date,
        d.title,
        normalize_diss_type(d.type),
        ROW_NUMBER() OVER (
            ORDER BY
                ss.section_name,
                sp.spec_name,
                CASE WHEN normalize_diss_type(d.type) = 'докторская' THEN 1 ELSE 2 END,
                d.defense_date,
                a.full_name
        )::INTEGER
    FROM dissertations d
    JOIN authors a ON a.author_id = d.author_id
    JOIN specializations sp ON sp.spec_id = d.spec_id
    JOIN science_sections ss ON ss.section_id = sp.section_id
    WHERE EXTRACT(YEAR FROM d.defense_date) = p_year
    ORDER BY
        ss.section_name,
        sp.spec_name,
        CASE WHEN normalize_diss_type(d.type) = 'докторская' THEN 1 ELSE 2 END,
        d.defense_date,
        a.full_name;
$$;

COMMENT ON FUNCTION fn_annual_dissertation_report(INTEGER) IS
    'Отчёт по защитам за год — результат в виде таблицы для pgAdmin';


CREATE OR REPLACE PROCEDURE get_annual_dissertation_report(IN target_year INTEGER)
LANGUAGE plpgsql
AS $$
DECLARE
    rec RECORD;
    current_section TEXT := '';
    current_spec TEXT := '';
BEGIN
    RAISE NOTICE 'Год: %', target_year;

    FOR rec IN
        SELECT *
        FROM fn_annual_dissertation_report(target_year)
    LOOP
        IF rec.section_name IS DISTINCT FROM current_section THEN
            RAISE NOTICE '';
            RAISE NOTICE '%', rec.section_name;
            current_section := rec.section_name;
            current_spec := '';
        END IF;

        IF rec.spec_name IS DISTINCT FROM current_spec THEN
            RAISE NOTICE '  %', rec.spec_name;
            current_spec := rec.spec_name;
        END IF;

        RAISE NOTICE '    %  %  %',
            rec.author_fio,
            to_char(rec.defense_date, 'DD.MM.YYYY'),
            rec.dissertation_title;
    END LOOP;
END;
$$;


-- ============================================================
-- 2.2 Поиск и объединение дублей авторов
-- ============================================================

CREATE OR REPLACE FUNCTION fn_duplicate_authors()
RETURNS TABLE (
    author_id_1 INTEGER,
    author_id_2 INTEGER,
    full_name TEXT,
    birth_date DATE,
    passport_1 DATE,
    passport_2 DATE,
    same_direction BOOLEAN
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        a1.author_id,
        a2.author_id,
        a1.full_name,
        a1.birth_date,
        a1.passport_issue_date,
        a2.passport_issue_date,
        EXISTS (
            SELECT 1
            FROM dissertations d1
            JOIN dissertations d2
              ON d1.spec_id = d2.spec_id
             AND d1.author_id = a1.author_id
             AND d2.author_id = a2.author_id
        )
    FROM authors a1
    JOIN authors a2
      ON a1.full_name = a2.full_name
     AND a1.birth_date = a2.birth_date
     AND a1.author_id < a2.author_id;
$$;


CREATE OR REPLACE PROCEDURE merge_duplicate_authors()
LANGUAGE plpgsql
AS $$
DECLARE
    dup RECORD;
    keep_id INTEGER;
    remove_id INTEGER;
BEGIN
    FOR dup IN
        SELECT *
        FROM fn_duplicate_authors()
        WHERE same_direction
    LOOP
        IF dup.passport_2 >= dup.passport_1
           OR (dup.passport_2 IS NULL AND dup.passport_1 IS NOT NULL) THEN
            keep_id := dup.author_id_2;
            remove_id := dup.author_id_1;
        ELSE
            keep_id := dup.author_id_1;
            remove_id := dup.author_id_2;
        END IF;

        UPDATE dissertations
           SET author_id = keep_id
         WHERE author_id = remove_id;

        DELETE FROM authors
         WHERE author_id = remove_id;

        RAISE NOTICE 'Объединены авторы % и % (%). Оставлен author_id = %',
            dup.author_id_1, dup.author_id_2, dup.full_name, keep_id;
    END LOOP;
END;
$$;


-- ============================================================
-- 2.3 Список авторов с учёными степенями
-- ============================================================

CREATE OR REPLACE FUNCTION fn_author_degree(author_id INTEGER)
RETURNS TEXT
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    has_cand BOOLEAN;
    has_doc BOOLEAN;
    cand_section_id INTEGER;
    doc_section_id INTEGER;
    cand_degree TEXT;
    doc_degree TEXT;
BEGIN
    SELECT
        bool_or(normalize_diss_type(d.type) = 'кандидатская'),
        bool_or(normalize_diss_type(d.type) = 'докторская'),
        MAX(ss.section_id) FILTER (WHERE normalize_diss_type(d.type) = 'кандидатская'),
        MAX(ss.section_id) FILTER (WHERE normalize_diss_type(d.type) = 'докторская'),
        MAX(get_degree_full_name(d.type, ss.section_name))
            FILTER (WHERE normalize_diss_type(d.type) = 'кандидатская'),
        MAX(get_degree_full_name(d.type, ss.section_name))
            FILTER (WHERE normalize_diss_type(d.type) = 'докторская')
    INTO has_cand, has_doc, cand_section_id, doc_section_id, cand_degree, doc_degree
    FROM dissertations d
    JOIN specializations sp ON sp.spec_id = d.spec_id
    JOIN science_sections ss ON ss.section_id = sp.section_id
    WHERE d.author_id = fn_author_degree.author_id;

    IF has_cand AND has_doc AND cand_section_id = doc_section_id THEN
        RETURN doc_degree;
    ELSIF has_cand AND has_doc THEN
        RETURN cand_degree || ', ' || doc_degree;
    ELSIF has_doc THEN
        RETURN doc_degree;
    ELSIF has_cand THEN
        RETURN cand_degree;
    END IF;

    RETURN NULL;
END;
$$;


CREATE OR REPLACE FUNCTION fn_authors_degrees()
RETURNS TABLE (
    author_id INTEGER,
    full_name TEXT,
    short_fio TEXT,
    academic_degree TEXT
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        a.author_id,
        a.full_name,
        format_fio(a.full_name),
        fn_author_degree(a.author_id)
    FROM authors a
    WHERE EXISTS (
        SELECT 1 FROM dissertations d WHERE d.author_id = a.author_id
    )
    ORDER BY a.full_name;
$$;


CREATE OR REPLACE PROCEDURE list_authors_degrees()
LANGUAGE plpgsql
AS $$
DECLARE
    rec RECORD;
BEGIN
    FOR rec IN SELECT * FROM fn_authors_degrees() LOOP
        RAISE NOTICE 'Автор: % | Степень: %', rec.full_name, rec.academic_degree;
    END LOOP;
END;
$$;


-- ============================================================
-- Представления для удобного просмотра в pgAdmin
-- ============================================================

DROP VIEW IF EXISTS v_dissertations_full;
DROP VIEW IF EXISTS v_authors_degrees;
DROP VIEW IF EXISTS v_duplicate_authors;

CREATE VIEW v_dissertations_full AS
SELECT
    d.diss_id,
    a.author_id,
    a.full_name,
    format_fio(a.full_name) AS short_fio,
    a.birth_date,
    calculate_age(a.birth_date) AS author_age_now,
    calculate_age(a.birth_date, d.defense_date) AS author_age_at_defense,
    d.title,
    normalize_diss_type(d.type) AS type,
    get_degree_full_name(normalize_diss_type(d.type), ss.section_name) AS degree_full_name,
    sp.spec_code,
    sp.spec_name,
    ss.section_name,
    d.defense_date,
    d.approval_date,
    a.passport_data,
    a.passport_issue_date
FROM dissertations d
JOIN authors a ON a.author_id = d.author_id
JOIN specializations sp ON sp.spec_id = d.spec_id
JOIN science_sections ss ON ss.section_id = sp.section_id;

COMMENT ON VIEW v_dissertations_full IS
    'Диссертации с авторами, степенями и возрастом — для просмотра в pgAdmin';

CREATE VIEW v_authors_degrees AS
SELECT * FROM fn_authors_degrees();

COMMENT ON VIEW v_authors_degrees IS
    'Авторы и их учёные степени — для просмотра в pgAdmin';

CREATE VIEW v_duplicate_authors AS
SELECT * FROM fn_duplicate_authors();

COMMENT ON VIEW v_duplicate_authors IS
    'Пары возможных дублей авторов (ФИО + дата рождения)';
