# Контрольная работа №5 (FastAPI)

Приложение объединяет задания КР-5:

- REST API задач с заголовком `X-User-Id`
- интеграционные тесты (`pytest` + `TestClient`)
- Docker / Docker Compose
- WebSocket-комнаты для чата
- модульная маршрутизация (`APIRouter`), зависимости и RBAC для `/admin`

## Структура проекта

```text
app/
  main.py
  dependencies.py
  schemas.py
  storage.py
  room_manager.py
  routers/
    tasks.py
    users.py
    admin.py
tests/
  test_tasks.py
  test_websocket.py
  test_dependencies_and_routing.py
Dockerfile
docker-compose.yml
requirements.txt
```

## Локальный запуск

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn app.main:app --reload
```

Swagger UI: `http://127.0.0.1:8000/docs`

## Тесты

```powershell
pytest
```

## Docker

```powershell
docker compose up --build
```

Проверка API в контейнере:

```powershell
curl http://localhost:8000/health
curl http://localhost:8000/tasks -H "X-User-Id: 10"
```

Ожидаемый ответ для пустого списка задач: `[]`.

## Примеры запросов

### Создать задачу

```powershell
curl -X POST "http://127.0.0.1:8000/tasks" `
  -H "Content-Type: application/json" `
  -H "X-User-Id: 10" `
  -d "{\"title\":\"Подготовить тесты\",\"description\":\"Интеграционные тесты\",\"status\":\"todo\",\"priority\":4}"
```

### Список задач с фильтрами

```powershell
curl "http://127.0.0.1:8000/tasks?status=todo&min_priority=3" -H "X-User-Id: 10"
```

### Админ-статистика

```powershell
curl "http://127.0.0.1:8000/admin/stats" -H "X-User-Id: 1" -H "X-User-Role: admin"
```

### WebSocket

Подключение: `ws://127.0.0.1:8000/ws/rooms/python?username=alice`

Сообщение:

```json
{"type": "message", "text": "Всем привет"}
```

### Активные пользователи комнаты

```powershell
curl "http://127.0.0.1:8000/rooms/python/users"
```

## Переменные окружения

| Переменная | Описание |
|------------|----------|
| `APP_ENV` | Окружение (`local`, `docker` и т.д.), отображается в `/health` |

В `docker-compose.yml` задано `APP_ENV=docker`.
