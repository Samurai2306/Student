# Базовый синтаксис PostgreSQL (SQL + psql) — подробное обучение с нуля

Этот файл — подробный учебник по базовому SQL в PostgreSQL и самым полезным командам клиента `psql`. Он рассчитан на учебные работы: таблицы, запросы, отчёты, функции, а также типовые ошибки и отладка.

## 1) Термины и структура: PostgreSQL, SQL, psql, база, схема

- **PostgreSQL** — сервер СУБД: хранит данные и выполняет SQL.
- **SQL** — язык запросов и описания объектов БД.
- **`psql`** — консольный клиент PostgreSQL (удобно запускать `.sql` файлы и смотреть структуру объектов).
- **База данных** — контейнер для схем и объектов.
- **Схема** — «папка» внутри базы (`public` по умолчанию). Полное имя: `schema.table`.

Практическое правило: если не указана схема, PostgreSQL ищет объект по `search_path` (обычно `public`).

## 2) `psql`: что нужно уметь (команды начинаются с `\`)

Команды `psql` — не SQL. Они начинаются с `\`.

### 2.1) Подключение и окружение

- `\l` — список баз
- `\c имя_бд` — подключиться к базе
- `\conninfo` — показать текущее подключение
- `\set ON_ERROR_STOP on` — останавливать выполнение скрипта при первой ошибке (очень полезно)

### 2.2) Схемы и объекты

- `\dn` — схемы
- `\dt` — таблицы в текущей схеме
- `\dt *.*` — таблицы во всех схемах
- `\dv` — представления
- `\dm` — materialized views
- `\df` — функции
- `\d имя` — краткая структура объекта (таблица/вью/индекс/тип)
- `\d+ имя` — расширенная структура

### 2.3) Удобство вывода

- `\x` — вертикальный вывод (широкие строки)
- `\timing` — показывать время выполнения запросов
- `\pset pager off` — выключить пейджер, чтобы вывод не «залипал»

### 2.4) Запуск SQL-файлов

- `\i путь\к\файлу.sql` — выполнить файл

SQL-запрос должен заканчиваться `;`.

## 3) Комментарии

```sql
-- однострочный

/*
многострочный
*/
```

## 4) Имена объектов и кавычки

- Без кавычек имена приводятся к нижнему регистру (`MyTable` → `mytable`).
- В двойных кавычках регистр сохраняется (`"MyTable"`).

Практическое правило: **не используйте** `"..."` в именах таблиц/полей, если можно обойтись.

## 5) Типы данных: что чаще всего используют

### 5.1) Числа

- `SMALLINT`, `INTEGER`, `BIGINT` — целые.
- `NUMERIC(p,s)` / `DECIMAL(p,s)` — точные десятичные (деньги).
- `REAL`, `DOUBLE PRECISION` — «плавающая точка» (может давать погрешности).

Для денег обычно выбирают `NUMERIC(12,2)` или `NUMERIC(10,2)`.

### 5.2) Строки

- `TEXT` — универсальный тип строки (часто лучший выбор).
- `VARCHAR(n)` — строка с ограничением длины.

### 5.3) Дата и время

- `DATE` — дата.
- `TIME` — время.
- `TIMESTAMP` — дата+время (без часового пояса).
- `TIMESTAMPTZ` — дата+время (с часовой зоной, обычно безопаснее для реальных систем).

### 5.4) Прочее

- `BOOLEAN` — `TRUE/FALSE`.
- `UUID` — уникальный идентификатор.

## 6) NULL: самая частая причина «почему запрос не работает»

`NULL` = «нет значения/неизвестно». Он **не равен** ничему, даже самому себе.

### 6.1) Неправильно

```sql
WHERE col = NULL
```

### 6.2) Правильно

```sql
WHERE col IS NULL
WHERE col IS NOT NULL
```

### 6.3) Полезные функции

- `COALESCE(a,b,c)` — первое не-NULL
- `NULLIF(a,b)` — NULL если `a=b` (часто для деления)

Защита от деления на ноль:

```sql
price / NULLIF(area_m2, 0)
```

## 7) DDL: создание таблиц, ключей, ограничений

### 7.1) Базовый пример таблицы

```sql
CREATE TABLE employees (
  id SERIAL PRIMARY KEY,
  last_name TEXT NOT NULL,
  salary NUMERIC(12,2) NOT NULL DEFAULT 0,
  created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

Что тут важно:
- `PRIMARY KEY` — уникальность + индекс.
- `NOT NULL` — запрет пустого значения.
- `DEFAULT` — значение по умолчанию.

### 7.2) CHECK, UNIQUE

```sql
CREATE TABLE products (
  id SERIAL PRIMARY KEY,
  name TEXT NOT NULL UNIQUE,
  price NUMERIC(12,2) NOT NULL CHECK (price >= 0)
);
```

### 7.3) Внешние ключи (FOREIGN KEY)

```sql
CREATE TABLE sales (
  id SERIAL PRIMARY KEY,
  employee_id INTEGER NOT NULL REFERENCES employees(id),
  amount NUMERIC(12,2) NOT NULL CHECK (amount >= 0),
  sale_date DATE NOT NULL
);
```

### 7.4) Удаление таблиц

```sql
DROP TABLE IF EXISTS sales;
DROP TABLE IF EXISTS employees;
```

`CASCADE` удаляет зависимые объекты — используйте осторожно.

## 8) DML: INSERT / UPDATE / DELETE + RETURNING

### 8.1) INSERT

```sql
INSERT INTO employees(last_name, salary)
VALUES ('Иванов', 50000),
       ('Петров', 60000);
```

### 8.2) RETURNING — полезнейшая вещь

```sql
INSERT INTO employees(last_name, salary)
VALUES ('Сидоров', 70000)
RETURNING id, created_at;
```

### 8.3) UPDATE + RETURNING

```sql
UPDATE employees
SET salary = salary * 1.10
WHERE last_name = 'Иванов'
RETURNING id, salary;
```

### 8.4) DELETE + RETURNING

```sql
DELETE FROM employees
WHERE salary < 1000
RETURNING id, last_name;
```

## 9) SELECT: порядок частей и «как читать запрос»

Шаблон:

```sql
SELECT ...
FROM ...
JOIN ...
WHERE ...
GROUP BY ...
HAVING ...
ORDER BY ...
LIMIT ... OFFSET ...;
```

Но исполняется логически так:
1) `FROM/JOIN`
2) `WHERE`
3) `GROUP BY`
4) `HAVING`
5) `SELECT`
6) `ORDER BY`
7) `LIMIT/OFFSET`

## 10) WHERE: фильтрация, логика, примеры

Операторы:
- сравнения: `=`, `<>`, `<`, `<=`, `>`, `>=`
- логика: `AND`, `OR`, `NOT`
- диапазон: `BETWEEN a AND b`
- множество: `IN (...)`

Примеры:

```sql
SELECT *
FROM sales
WHERE amount BETWEEN 10000 AND 50000
  AND sale_date >= DATE '2025-01-01';
```

```sql
SELECT *
FROM employees
WHERE last_name IN ('Иванов', 'Петров');
```

## 11) Строки: LIKE/ILIKE, конкатенация, полезные функции

### 11.1) LIKE/ILIKE

- `%` — любая длина
- `_` — один символ

```sql
SELECT *
FROM employees
WHERE last_name ILIKE 'иван%';
```

### 11.2) Конкатенация

```sql
SELECT 'Сотрудник: ' || last_name AS label
FROM employees;
```

### 11.3) Частые строковые функции

```sql
SELECT
  LENGTH(last_name) AS len,
  UPPER(last_name) AS up,
  LOWER(last_name) AS low,
  TRIM(last_name)  AS trimmed
FROM employees;
```

## 12) JOIN: виды, типовые ошибки и как их избегать

### 12.1) INNER JOIN

```sql
SELECT e.last_name, s.amount, s.sale_date
FROM sales s
JOIN employees e ON e.id = s.employee_id;
```

### 12.2) LEFT JOIN

```sql
SELECT e.last_name, s.amount
FROM employees e
LEFT JOIN sales s ON s.employee_id = e.id;
```

### 12.3) Ошибка: LEFT JOIN превращается в INNER JOIN из-за WHERE

Плохо (убирает сотрудников без продаж):

```sql
SELECT e.last_name, s.amount
FROM employees e
LEFT JOIN sales s ON s.employee_id = e.id
WHERE s.amount > 0;
```

Хорошо (условие на правую таблицу — в `ON`):

```sql
SELECT e.last_name, s.amount
FROM employees e
LEFT JOIN sales s
  ON s.employee_id = e.id AND s.amount > 0;
```

### 12.4) «Размножение строк» в JOIN

Если связь 1:N (один сотрудник — много продаж), то в результате будет много строк на сотрудника. Это не баг, а логика.
Чтобы получить «по одному сотруднику», добавляй агрегацию (`GROUP BY`) или выбирай одну строку (например, последнюю продажу).

## 13) Агрегации: COUNT/SUM/AVG, GROUP BY, HAVING

### 13.1) Отчёт по сумме продаж

```sql
SELECT e.id, e.last_name, COUNT(*) AS cnt, SUM(s.amount) AS total
FROM sales s
JOIN employees e ON e.id = s.employee_id
GROUP BY e.id, e.last_name
ORDER BY total DESC;
```

### 13.2) Сотрудники без продаж (LEFT JOIN + COALESCE)

```sql
SELECT e.id, e.last_name,
       COALESCE(COUNT(s.id), 0) AS cnt,
       COALESCE(SUM(s.amount), 0) AS total
FROM employees e
LEFT JOIN sales s ON s.employee_id = e.id
GROUP BY e.id, e.last_name
ORDER BY total DESC;
```

### 13.3) HAVING (фильтр по агрегатам)

```sql
SELECT employee_id, SUM(amount) AS total
FROM sales
GROUP BY employee_id
HAVING SUM(amount) > 100000;
```

## 14) Подзапросы: IN, EXISTS, скалярный подзапрос

### 14.1) IN

```sql
SELECT *
FROM employees
WHERE id IN (
  SELECT employee_id
  FROM sales
  WHERE amount > 10000
);
```

### 14.2) EXISTS (часто предпочтительнее)

```sql
SELECT *
FROM employees e
WHERE EXISTS (
  SELECT 1
  FROM sales s
  WHERE s.employee_id = e.id
    AND s.amount > 10000
);
```

### 14.3) Скалярный подзапрос

```sql
SELECT
  e.last_name,
  (SELECT COUNT(*) FROM sales s WHERE s.employee_id = e.id) AS sales_cnt
FROM employees e;
```

## 15) CTE (WITH): многошаговые запросы

```sql
WITH big_sales AS (
  SELECT employee_id, amount
  FROM sales
  WHERE amount > 10000
),
agg AS (
  SELECT employee_id, COUNT(*) AS cnt, SUM(amount) AS total
  FROM big_sales
  GROUP BY employee_id
)
SELECT e.last_name, a.cnt, a.total
FROM agg a
JOIN employees e ON e.id = a.employee_id
ORDER BY a.total DESC;
```

## 16) Оконные функции (Window Functions): RANK, LEAD, running total

Оконные функции добавляют вычисления «поверх строк», не сворачивая их.

### 16.1) Нумерация строк внутри группы

```sql
SELECT
  s.*,
  ROW_NUMBER() OVER (PARTITION BY employee_id ORDER BY sale_date) AS rn
FROM sales s;
```

### 16.2) Предыдущая/следующая строка

```sql
SELECT
  s.*,
  LAG(amount)  OVER (PARTITION BY employee_id ORDER BY sale_date) AS prev_amount,
  LEAD(amount) OVER (PARTITION BY employee_id ORDER BY sale_date) AS next_amount
FROM sales s;
```

### 16.3) Накопительная сумма

```sql
SELECT
  s.*,
  SUM(amount) OVER (
    PARTITION BY employee_id
    ORDER BY sale_date
    ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
  ) AS running_total
FROM sales s;
```

## 17) Дата/время: NOW, CURRENT_DATE, EXTRACT, DATE_TRUNC, INTERVAL, TO_CHAR

### 17.1) Текущее время

```sql
SELECT NOW(), CURRENT_DATE, CURRENT_TIMESTAMP;
```

### 17.2) EXTRACT

```sql
SELECT
  EXTRACT(YEAR FROM sale_date)  AS y,
  EXTRACT(MONTH FROM sale_date) AS m
FROM sales;
```

### 17.3) Группировка по месяцу

```sql
SELECT
  DATE_TRUNC('month', sale_date::timestamp) AS month_start,
  SUM(amount) AS total
FROM sales
GROUP BY month_start
ORDER BY month_start;
```

### 17.4) Интервалы

```sql
SELECT NOW() - INTERVAL '6 months';
SELECT CURRENT_DATE + INTERVAL '10 days';
```

### 17.5) Форматирование

```sql
SELECT TO_CHAR(NOW(), 'YYYY-MM-DD HH24:MI:SS');
```

## 18) Транзакции и блокировки: базовое понимание

### 18.1) Транзакции

```sql
BEGIN;
  UPDATE accounts SET balance = balance - 100 WHERE id = 1;
  UPDATE accounts SET balance = balance + 100 WHERE id = 2;
COMMIT;
```

Если ошибка:

```sql
ROLLBACK;
```

### 18.2) Блокировка строк: SELECT ... FOR UPDATE

```sql
SELECT balance
FROM accounts
WHERE id = 1
FOR UPDATE;
```

## 19) Индексы и производительность: основы + EXPLAIN

Создание индекса:

```sql
CREATE INDEX idx_sales_employee_date
ON sales(employee_id, sale_date);
```

Проверка плана:

```sql
EXPLAIN ANALYZE
SELECT *
FROM sales
WHERE employee_id = 10
  AND sale_date >= DATE '2025-01-01';
```

## 20) VIEW и MATERIALIZED VIEW

### 20.1) Представление (VIEW)

```sql
CREATE OR REPLACE VIEW v_employee_sales AS
SELECT e.id, e.last_name, COALESCE(SUM(s.amount), 0) AS total
FROM employees e
LEFT JOIN sales s ON s.employee_id = e.id
GROUP BY e.id, e.last_name;
```

### 20.2) Материализованное представление (кэш отчёта)

```sql
CREATE MATERIALIZED VIEW mv_employee_sales AS
SELECT e.id, e.last_name, COALESCE(SUM(s.amount), 0) AS total
FROM employees e
LEFT JOIN sales s ON s.employee_id = e.id
GROUP BY e.id, e.last_name;
```

```sql
REFRESH MATERIALIZED VIEW mv_employee_sales;
```

## 21) Функции: SQL и PL/pgSQL (минимум)

### 21.1) SQL-функция (один запрос)

```sql
CREATE OR REPLACE FUNCTION sales_sum_by_employee(p_employee_id INTEGER)
RETURNS NUMERIC
LANGUAGE sql
AS $$
  SELECT COALESCE(SUM(amount), 0)
  FROM sales
  WHERE employee_id = p_employee_id
$$;
```

### 21.2) PL/pgSQL-функция (условия/проверки)

```sql
CREATE OR REPLACE FUNCTION safe_div(a NUMERIC, b NUMERIC)
RETURNS NUMERIC
LANGUAGE plpgsql
AS $$
BEGIN
  IF b IS NULL OR b = 0 THEN
    RETURN NULL;
  END IF;
  RETURN a / b;
END;
$$;
```

## 22) Роли и права (минимально)

Дать право читать таблицу:

```sql
GRANT SELECT ON employees TO some_user;
```

Дать права на все таблицы в схеме:

```sql
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO some_user;
```

## 23) Отладка запросов: мини-чеклист

- **0 строк**: проверь `WHERE`, даты, и особенно `NULL`.
- **дубликаты**: проверь `JOIN` (1:N размножает строки).
- **LEFT JOIN “не работает”**: условия по правой таблице перенеси из `WHERE` в `ON`.
- **медленно**: `EXPLAIN ANALYZE`, индексы, лишние CTE/подзапросы.
- **ошибка типов**: приводи типы явно (`::DATE`, `::NUMERIC`, `::INT`).

## 24) Частые ошибки

- `= NULL` вместо `IS NULL`.
- Деление на 0 без `NULLIF`.
- Фильтр по правой таблице в `WHERE` после `LEFT JOIN`.
- В `GROUP BY` забыли добавить поля, которые стоят в `SELECT` без агрегатных функций.

