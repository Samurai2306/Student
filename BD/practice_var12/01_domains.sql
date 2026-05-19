-- Вариант 12. Домены для проверки значений полей таблицы dissertations
-- Запускать в базе bd_student от пользователя admin

DO $$
BEGIN
    CREATE DOMAIN diss_type AS VARCHAR(50)
        CHECK (VALUE IN ('кандидатская', 'докторская'));
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$
BEGIN
    CREATE DOMAIN past_or_today_date AS DATE
        CHECK (VALUE <= CURRENT_DATE);
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

COMMENT ON DOMAIN diss_type IS 'Тип диссертации: кандидатская или докторская';
COMMENT ON DOMAIN past_or_today_date IS 'Дата не позже текущего дня';
