# KR-4 FastAPI (Alembic + ошибки + async-тесты)

## Установка зависимостей и запуск

1) Создайте и активируйте виртуальное окружение (пример для Windows PowerShell):

```powershell
py -m venv .venv
.\.venv\Scripts\Activate.ps1
```

2) Установите зависимости:

```powershell
pip install -r requirements.txt
```

3) Настройте переменные окружения:

- Скопируйте `.env.example` → `.env` и при необходимости измените `DATABASE_URL`.

4) Примените миграции и запустите приложение:

```powershell
python -m alembic upgrade head
python -m uvicorn app.main:app --reload
```

После запуска откройте Swagger UI: `http://127.0.0.1:8000/docs`.

## Проверка основной функциональности

### 1) CRUD пользователей (in-memory) — ключевые сценарии из задания 11.2

- Создать пользователя:

```powershell
curl -X POST "http://127.0.0.1:8000/users" `
  -H "Content-Type: application/json" `
  -d "{\"username\":\"alice\",\"age\":22}"
```

- Получить пользователя:

```powershell
curl "http://127.0.0.1:8000/users/1"
```

- Удалить пользователя:

```powershell
curl -X DELETE "http://127.0.0.1:8000/users/1" -i
```

### 2) Пользовательские ошибки (задание 10.2)

Две конечные точки, которые специально выбрасывают разные исключения:

```powershell
curl -i "http://127.0.0.1:8000/errors/a"
curl -i "http://127.0.0.1:8000/errors/b"
```

### 3) Валидация входных данных (задание 10.2 / 11.1)

Эндпоинт принимает JSON и валидирует его. Пример ошибки валидации:

```powershell
curl -X POST "http://127.0.0.1:8000/validate-user" `
  -H "Content-Type: application/json" `
  -d "{\"username\":\"u\",\"age\":10,\"email\":\"not-an-email\",\"password\":\"123\"}"
```

## Тестирование ключевых сценариев

Запуск всех тестов:

```powershell
python -m pytest -q
```

Тесты асинхронные и ходят в приложение через `httpx.AsyncClient + ASGITransport` (без поднятия Uvicorn).

## Миграции БД (Alembic)

Применить миграции:

```powershell
python -m alembic upgrade head
```

Откатить на одну миграцию:

```powershell
python -m alembic downgrade -1
```

Примечание для PowerShell: вместо `&&` используйте `;` (или запускайте команды отдельно).

Сгенерировать новую миграцию (если вы меняете модели):

```powershell
python -m alembic revision --autogenerate -m "your message"
```
