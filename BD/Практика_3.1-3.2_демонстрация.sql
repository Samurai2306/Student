-- =============================================================================
-- Демонстрация триггеров из отчёта «Практика_3.1-3.2_отчёт.md»
-- =============================================================================
-- План (что смотреть в выводе / в SELECT внизу):
--   3.2.4  — SELECT: телефон 79996667788 → +7 (999) 666 77 88
--   3.1.1+3.1.4 — INSERT продажи: два RAISE WARNING (цена и дата)
--   3.1.5+3.2.1 — INSERT продажи: строки в sales_journal и realtor_bonuses
--   3.1.2       — SAVEPOINT + вторая продажа объекта: RAISE EXCEPTION
--   3.1.3       — две комнаты: RAISE WARNING на превышение суммарной площади
--   3.2.2       — INSERT бонуса 600000: RAISE NOTICE (порог 500000 в функции)
--   3.2.3       — SAVEPOINT + паспорт без маски: RAISE EXCEPTION; затем ОК-вставка
--   3.2.1       — DELETE продажи: бонус риэлтора уменьшается
-- =============================================================================
-- Перед запуском: выполните из отчёта блок «Схема и подготовка» и все задания
-- (создание функций и триггеров).
--
-- ВНИМАНИЕ: блок «Очистка» ниже удаляет ВСЕ строки из перечисленных таблиц.
-- Используйте только учебную базу или закомментируйте очистку и подставьте свои id.
--
-- Сообщения RAISE WARNING / RAISE NOTICE смотрите в выводе клиента:
--   psql — в консоли; в pgAdmin — вкладка Messages / Notice;
--   при необходимости выполните: SET client_min_messages TO NOTICE;
-- =============================================================================

SET client_min_messages TO NOTICE;

-- -----------------------------------------------------------------------------
-- Очистка (опционально: закомментируйте, если в таблицах нужны ваши данные)
-- -----------------------------------------------------------------------------
TRUNCATE TABLE
  sales,
  property_rooms,
  sales_journal,
  realtor_bonuses,
  properties,
  realtors
RESTART IDENTITY;

-- -----------------------------------------------------------------------------
-- Тестовые данные
-- -----------------------------------------------------------------------------
INSERT INTO realtors (last_name, phone, passport_data)
VALUES
  ('ДЕМО Иванов', '79996667788', '4510 123456'),
  ('ДЕМО Петров', NULL, NULL);

-- Проверка 3.2.4: телефон первого риэлтора должен стать +7 (999) 666 77 88
SELECT id, last_name, phone, passport_data
FROM realtors
ORDER BY id;

INSERT INTO properties (address, district, rooms, price_rub, total_area_m2, created_at)
VALUES
  ('ДЕМО: объект А (проверка цены и даты)', 'ДЕМО', 2, 1000000.00, 100.00, TIMESTAMP '2025-06-15 12:00:00'),
  ('ДЕМО: объект Б (повторная продажа)', 'ДЕМО', 2, 5000000.00, 80.00, TIMESTAMP '2025-06-01 10:00:00'),
  ('ДЕМО: объект В (комнаты)', 'ДЕМО', 2, 3000000.00, 30.00, TIMESTAMP '2025-05-01 09:00:00');

SELECT id, address, price_rub, total_area_m2, created_at FROM properties ORDER BY id;

-- =============================================================================
-- Практика 3.1, задания 1 и 4 — одна вставка, два предупреждения (если клиент показывает оба)
--   Зад. 1: заявленная 1 000 000, продажа 700 000 → отклонение 30% (> 20%)
--   Зад. 4: дата продажи 2025-06-10 < дата размещения объявления 2025-06-15
-- Важно: вторая продажа того же property_id запрещена триггером задания 2 — поэтому оба
-- сценария объединены в одну строку продажи.
-- =============================================================================
INSERT INTO sales (property_id, realtor_id, sale_date, sale_price_rub, commission_rub)
VALUES (1, 1, TIMESTAMP '2025-06-10 12:00:00', 700000.00, NULL);

-- =============================================================================
-- Практика 3.1, задание 5 — журнал + 3.2.1 бонусы (INSERT в sales пишет журнал и бонус)
-- Проверка журнала и бонусов после вставок выше
-- =============================================================================
SELECT * FROM sales_journal ORDER BY id;

SELECT * FROM realtor_bonuses ORDER BY realtor_id;

-- =============================================================================
-- Практика 3.1, задание 2 — запрет повторной продажи объекта
-- Ожидание: RAISE EXCEPTION; транзакция не должна сломаться целиком благодаря SAVEPOINT
-- =============================================================================
BEGIN;

INSERT INTO sales (property_id, realtor_id, sale_date, sale_price_rub, commission_rub)
VALUES (2, 1, TIMESTAMP '2025-08-01 11:00:00', 4800000.00, NULL);

SAVEPOINT demo_no_duplicate_second_sale;

INSERT INTO sales (property_id, realtor_id, sale_date, sale_price_rub, commission_rub)
VALUES (2, 2, TIMESTAMP '2025-08-02 11:00:00', 4700000.00, NULL);
-- Ожидается ошибка: повторная продажа объекта 2

ROLLBACK TO SAVEPOINT demo_no_duplicate_second_sale;

COMMIT;

SELECT id, property_id, sale_price_rub FROM sales WHERE property_id = 2;

-- =============================================================================
-- Практика 3.1, задание 3 — сумма площадей комнат больше общей площади
-- Ожидание: RAISE WARNING на второй вставке (20 + 15 = 35 > 30)
-- =============================================================================
INSERT INTO property_rooms (property_id, room_name, room_area_m2)
VALUES (3, 'Комната 1', 20.00);

INSERT INTO property_rooms (property_id, room_name, room_area_m2)
VALUES (3, 'Комната 2', 15.00);

-- =============================================================================
-- Практика 3.2, задание 2 — уведомление о превышении максимума бонуса (500 000 в функции)
-- Ожидание: RAISE NOTICE при вставке строки с bonus_amount > 500000
-- =============================================================================
INSERT INTO realtors (last_name, phone, passport_data)
VALUES ('ДЕМО Богатых', NULL, '4601 987654');

INSERT INTO realtor_bonuses (realtor_id, bonus_amount)
SELECT id, 600000.00
FROM realtors
WHERE last_name = 'ДЕМО Богатых';

-- =============================================================================
-- Практика 3.2, задание 3 — неверный формат паспорта
-- Ожидание: RAISE EXCEPTION; откат только демо-вставки через SAVEPOINT
-- =============================================================================
BEGIN;

SAVEPOINT demo_bad_passport;

INSERT INTO realtors (last_name, passport_data)
VALUES ('ДЕМО Плохой паспорт', '1234567890');
-- Ожидается ошибка: не совпало с маской «XXXX YYYYYY»

ROLLBACK TO SAVEPOINT demo_bad_passport;

COMMIT;

-- Корректный паспорт (для сравнения) — отдельная транзакция
INSERT INTO realtors (last_name, passport_data)
VALUES ('ДЕМО Хороший паспорт', '4601 112233');

-- =============================================================================
-- Практика 3.2, задание 1 — уменьшение бонуса при удалении продажи
-- Запомним бонус риэлтора 1 до удаления, удалим одну его продажу, сравним
-- =============================================================================
SELECT realtor_id, bonus_amount AS bonus_before_delete
FROM realtor_bonuses
WHERE realtor_id = 1;

DELETE FROM sales
WHERE id = (
  SELECT id FROM sales WHERE realtor_id = 1 ORDER BY id LIMIT 1
);

SELECT realtor_id, bonus_amount AS bonus_after_delete
FROM realtor_bonuses
WHERE realtor_id = 1;

-- =============================================================================
-- Итог: журнал, продажи, комнаты, риэлторы
-- =============================================================================
SELECT * FROM sales_journal ORDER BY id;

SELECT * FROM sales ORDER BY id;

SELECT * FROM property_rooms WHERE property_id = 3 ORDER BY id;

SELECT id, last_name, phone, passport_data FROM realtors ORDER BY id;

SELECT * FROM realtor_bonuses ORDER BY realtor_id;
