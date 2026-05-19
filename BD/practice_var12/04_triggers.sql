-- Вариант 12. Задание 3 — триггеры

-- ============================================================
-- 3.1 Проверка доменов и бизнес-ограничений для dissertations
-- ============================================================

CREATE OR REPLACE FUNCTION trg_validate_dissertation()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    -- Название
    IF NEW.title IS NULL OR btrim(NEW.title) = '' THEN
        RAISE EXCEPTION 'Название диссертации не может быть пустым';
    END IF;

    -- Тип (после нормализации триггером normalize_type)
    IF NEW.type IS NOT NULL THEN
        PERFORM NEW.type::diss_type;
    END IF;

    -- Даты не в будущем
    IF NEW.defense_date IS NOT NULL THEN
        PERFORM NEW.defense_date::past_or_today_date;
    END IF;

    IF NEW.approval_date IS NOT NULL THEN
        PERFORM NEW.approval_date::past_or_today_date;
    END IF;

    -- Дата защиты не позже даты утверждения
    IF NEW.defense_date IS NOT NULL
       AND NEW.approval_date IS NOT NULL
       AND NEW.defense_date > NEW.approval_date THEN
        RAISE EXCEPTION
            'Дата защиты (%) не может быть позже даты утверждения (%)',
            NEW.defense_date, NEW.approval_date;
    END IF;

    -- Ссылочная целостность (явная проверка для понятных сообщений)
    IF NEW.author_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM authors WHERE author_id = NEW.author_id) THEN
        RAISE EXCEPTION 'Автор с id = % не найден', NEW.author_id;
    END IF;

    IF NEW.spec_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM specializations WHERE spec_id = NEW.spec_id) THEN
        RAISE EXCEPTION 'Специальность с id = % не найдена', NEW.spec_id;
    END IF;

    RETURN NEW;
END;
$$;


DROP TRIGGER IF EXISTS check_dates_trigger ON dissertations;
DROP TRIGGER IF EXISTS validate_dissertation_trigger ON dissertations;

CREATE TRIGGER validate_dissertation_trigger
    BEFORE INSERT OR UPDATE ON dissertations
    FOR EACH ROW
    EXECUTE FUNCTION trg_validate_dissertation();


-- ============================================================
-- 3.2 Замена сокращённых значений поля type
-- ============================================================

CREATE OR REPLACE FUNCTION trg_normalize_type()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.type IS NULL THEN
        RETURN NEW;
    END IF;

    NEW.type := CASE lower(btrim(NEW.type))
        WHEN 'канд' THEN 'кандидатская'
        WHEN 'канд.' THEN 'кандидатская'
        WHEN 'к.н.' THEN 'кандидатская'
        WHEN 'кандидат' THEN 'кандидатская'
        WHEN 'док' THEN 'докторская'
        WHEN 'док.' THEN 'докторская'
        WHEN 'д.н.' THEN 'докторская'
        WHEN 'доктор' THEN 'докторская'
        ELSE NEW.type
    END;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS normalize_type_trigger ON dissertations;

CREATE TRIGGER normalize_type_trigger
    BEFORE INSERT OR UPDATE ON dissertations
    FOR EACH ROW
    EXECUTE FUNCTION trg_normalize_type();


-- ============================================================
-- 3.3 Архив изменений сведений об авторах
-- ============================================================

CREATE OR REPLACE FUNCTION trg_archive_author_changes()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO authors_archive (
        author_id,
        old_full_name,
        old_passport_data,
        change_timestamp
    )
    VALUES (
        OLD.author_id,
        OLD.full_name,
        OLD.passport_data,
        CURRENT_TIMESTAMP
    );

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS archive_author_trigger ON authors;

CREATE TRIGGER archive_author_trigger
    AFTER UPDATE ON authors
    FOR EACH ROW
    WHEN (
        OLD.full_name IS DISTINCT FROM NEW.full_name
        OR OLD.passport_data IS DISTINCT FROM NEW.passport_data
        OR OLD.passport_issue_date IS DISTINCT FROM NEW.passport_issue_date
    )
    EXECUTE FUNCTION trg_archive_author_changes();
