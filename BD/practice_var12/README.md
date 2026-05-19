# Вариант 12 — БД диссертаций

Рабочая папка для практики в базе **bd_student** (пользователь **admin**).

## Установка в pgAdmin

1. Подключитесь к серверу PostgreSQL → база **bd_student**.
2. **Tools → Query Tool**.
3. **File → Open** → выберите `00_install_all.sql` (или по очереди `01`…`04`).
4. Нажмите **Execute/Refresh (F5)**.

Через psql из командной строки:

```bash
cd practice_var12
psql -U admin -d bd_student -h localhost -f 00_install_all.sql
```

## Что смотреть в pgAdmin

| Объект | Назначение |
|--------|------------|
| `format_fio()` | ФИО → «Иванов И.С.» |
| `get_degree_full_name()` | «доктор технических наук» |
| `calculate_age()` | возраст на дату |
| `fn_annual_dissertation_report(год)` | отчёт за год (таблица) |
| `fn_duplicate_authors()` / `v_duplicate_authors` | поиск дублей |
| `merge_duplicate_authors()` | объединение дублей |
| `v_authors_degrees` | авторы и степени |
| `v_dissertations_full` | все диссертации с JOIN |

Демо-запросы: **`05_demo_pgadmin.sql`**.

Подробное обучение (что, зачем, как работает): **[`LEARN/КР4_вариант12_обучение.md`](../LEARN/КР4_вариант12_обучение.md)**.

## Структура файлов

- `01_domains.sql` — домены `diss_type`, `past_or_today_date`
- `02_functions.sql` — 3 функции (задание 1)
- `03_procedures_and_views.sql` — 3 процедуры + представления
- `04_triggers.sql` — 3 триггера (задание 3)
- `05_demo_pgadmin.sql` — готовые SELECT для проверки
