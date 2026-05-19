-- Вариант 12. Задание 1 — функции

-- 1.1 ФИО → фамилия с инициалами
CREATE OR REPLACE FUNCTION format_fio(full_name TEXT)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
AS $$
DECLARE
    parts TEXT[];
BEGIN
    IF full_name IS NULL OR btrim(full_name) = '' THEN
        RETURN '#############';
    END IF;

    parts := regexp_split_to_array(btrim(full_name), '\s+');

    IF array_length(parts, 1) = 3 THEN
        RETURN parts[1] || ' ' || left(parts[2], 1) || '.' || left(parts[3], 1) || '.';
    ELSIF array_length(parts, 1) = 2 THEN
        RETURN parts[1] || ' ' || left(parts[2], 1) || '.';
    ELSE
        RETURN '#############';
    END IF;
END;
$$;

COMMENT ON FUNCTION format_fio(TEXT) IS
    'Преобразует ФИО в формат "Фамилия И.О."; при ошибке возвращает #############';


-- Вспомогательная нормализация типа (для SELECT по уже сохранённым данным)
CREATE OR REPLACE FUNCTION normalize_diss_type(raw_type TEXT)
RETURNS TEXT
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT CASE
        WHEN raw_type IS NULL THEN NULL
        WHEN lower(btrim(raw_type)) IN ('канд', 'канд.', 'к.н.', 'кандидат', 'кандидатская')
            THEN 'кандидатская'
        WHEN lower(btrim(raw_type)) IN ('док', 'док.', 'д.н.', 'доктор', 'докторская')
            THEN 'докторская'
        ELSE raw_type
    END;
$$;


-- 1.2 Полное название учёной степени
CREATE OR REPLACE FUNCTION get_degree_full_name(d_type TEXT, section_name TEXT)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
AS $$
DECLARE
    prefix TEXT;
    suffix TEXT;
BEGIN
    IF d_type IS NULL OR section_name IS NULL THEN
        RETURN NULL;
    END IF;

    d_type := normalize_diss_type(d_type);

    IF d_type = 'докторская' THEN
        prefix := 'доктор';
    ELSIF d_type = 'кандидатская' THEN
        prefix := 'кандидат';
    ELSE
        RETURN d_type;
    END IF;

    suffix := CASE section_name
        WHEN 'Технические науки' THEN 'технических наук'
        WHEN 'Экономические науки' THEN 'экономических наук'
        WHEN 'Физико-математические науки' THEN 'физико-математических наук'
        WHEN 'Филологические науки' THEN 'филологических наук'
        WHEN 'Юридические науки' THEN 'юридических наук'
        WHEN 'Биологические науки' THEN 'биологических наук'
        ELSE lower(section_name)
    END;

    RETURN prefix || ' ' || suffix;
END;
$$;

COMMENT ON FUNCTION get_degree_full_name(TEXT, TEXT) IS
    'Возвращает полное название степени, напр. "доктор технических наук"';


-- 1.3 Возраст по двум датам (вторая дата по умолчанию — сегодня)
CREATE OR REPLACE FUNCTION calculate_age(
    birth_date DATE,
    target_date DATE DEFAULT CURRENT_DATE
)
RETURNS INTEGER
LANGUAGE plpgsql
STABLE
AS $$
BEGIN
    IF birth_date IS NULL OR target_date IS NULL THEN
        RETURN NULL;
    END IF;

    IF birth_date > target_date THEN
        RETURN NULL;
    END IF;

    RETURN EXTRACT(YEAR FROM age(target_date, birth_date))::INTEGER;
END;
$$;

COMMENT ON FUNCTION calculate_age(DATE, DATE) IS
    'Возраст на указанную дату; без второго параметра — на текущую дату';
