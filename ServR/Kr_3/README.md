# Контрольная работа №3 (FastAPI)

Единое приложение закрывает задания **6.1–8.2**:

- HTTP Basic (`/secret`, `GET /login`)
- регистрация пользователей с хешированием пароля (`passlib` + `bcrypt`)
- JWT-аутентификация (`POST /login`, `Bearer`)
- RBAC (`admin`, `user`, `guest`)
- rate limiting (`slowapi`)
- режимы документации `DEV` / `PROD`
- SQLite (`users`, `todos`) и CRUD по `Todo`

Подробная архитектура и сопоставление с пунктами методички: `ОПИСАНИЕ_РАБОТЫ.md`.

## 1) Что нужно установить

- Python 3.11+ (проверено на более новых версиях тоже)
- `pip`
- (опционально) `curl` для ручного тестирования

Проверка:

```bash
python --version
pip --version
```

## 2) Подготовка проекта

Откройте терминал в корне проекта и перейдите в каталог КР:

```bash
cd ServR/Kr_3
```

### Windows (PowerShell)

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
```

### Linux/macOS

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env
```

## 3) Настройка `.env`

Откройте `.env` и задайте значения (минимум):

```env
MODE=DEV
DOCS_USER=docsadmin
DOCS_PASSWORD=docssecret
JWT_SECRET=replace-with-very-long-random-secret
JWT_ALGORITHM=HS256
JWT_EXPIRE_MINUTES=60
DATABASE_PATH=app.db
```

Важно:

- `MODE` должен быть только `DEV` или `PROD`.
- `JWT_SECRET` в реальном использовании должен быть длинным и случайным.
- Файл `.env` не коммитится (уже в `.gitignore`).

## 4) Инициализация базы

Обычно не нужна отдельно (таблицы создаются на старте приложения), но для явной проверки можно выполнить:

```bash
python init_schema.py
```

После этого появится файл базы (`app.db` или путь из `DATABASE_PATH`).

## 5) Запуск приложения

```bash
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Приложение доступно по адресу: `http://127.0.0.1:8000`.

## 6) Документация API (DEV/PROD)

### DEV

- `GET /docs` и `GET /openapi.json` защищены Basic-аутентификацией (`DOCS_USER`/`DOCS_PASSWORD`).
- `GET /redoc` не используется.

Проверка:

```bash
curl -u docsadmin:docssecret http://127.0.0.1:8000/docs
```

### PROD

Если в `.env` поставить `MODE=PROD`, то:

- `/docs`, `/openapi.json`, `/redoc` возвращают `404 Not Found`.

Проверка:

```bash
curl http://127.0.0.1:8000/docs
```

## 7) Полный сценарий проверки API

Ниже последовательность, которую можно показать при сдаче.

### Шаг 1. Регистрация пользователя

```bash
curl -X POST http://127.0.0.1:8000/register \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"alice\",\"password\":\"qwerty123\",\"role\":\"admin\"}"
```

Ожидаемо: `201` и `{"message":"New user created"}`.

Повторная регистрация того же `username`: `409 User already exists`.

### Шаг 2. Basic-аутентификация (задания 6.1 и 6.2)

```bash
# 6.2
curl -u alice:qwerty123 http://127.0.0.1:8000/login

# 6.1
curl -u alice:qwerty123 http://127.0.0.1:8000/secret
```

Ожидаемо:

- `/login` -> `{"message":"Welcome, alice!"}`
- `/secret` -> `You got my secret, welcome`

### Шаг 3. JWT-логин

```bash
curl -X POST http://127.0.0.1:8000/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"alice\",\"password\":\"qwerty123\"}"
```

Ожидаемо: JSON с `access_token` и `token_type`.

### Шаг 4. Защищённый ресурс с Bearer

Подставьте токен:

```bash
curl http://127.0.0.1:8000/protected_resource \
  -H "Authorization: Bearer <access_token>"
```

Ожидаемо: `{"message":"Access granted"}` (для ролей `admin`/`user`).

### Шаг 5. CRUD Todo

```bash
# Список
curl http://127.0.0.1:8000/todos -H "Authorization: Bearer <token>"

# Создание (admin)
curl -X POST http://127.0.0.1:8000/todos \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Buy groceries\",\"description\":\"Milk, eggs, bread\"}"

# Чтение по id
curl http://127.0.0.1:8000/todos/1 -H "Authorization: Bearer <token>"

# Обновление (admin/user)
curl -X PUT http://127.0.0.1:8000/todos/1 \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d "{\"title\":\"Buy groceries\",\"description\":\"Milk, eggs, bread\",\"completed\":true}"

# Удаление (admin)
curl -X DELETE http://127.0.0.1:8000/todos/1 -H "Authorization: Bearer <token>"
```

## 8) Ограничение запросов (Rate limit)

- `POST /register`: 1 запрос в минуту
- `POST /login`: 5 запросов в минуту

При превышении:

- статус `429`
- тело `{"detail":"Too many requests"}`

## 9) Частые проблемы и решения

- **Ошибка по `MODE`**: проверьте, что в `.env` стоит только `DEV` или `PROD`.
- **401 на `/docs` в DEV**: проверьте `DOCS_USER`/`DOCS_PASSWORD` и Basic-авторизацию.
- **401/403 на защищённых маршрутах**:
  - 401 — токен отсутствует/битый/просрочен
  - 403 — роль пользователя не имеет прав на операцию
- **База «чистая» для новой проверки**: остановите приложение и удалите `app.db`, затем запустите снова.

## 10) Что именно сдавать

1. Код в репозитории (`ServR/Kr_3`).
2. Этот `README.md`.
3. Файл `ОПИСАНИЕ_РАБОТЫ.md` с подробным описанием реализации.
4. Ссылку на репозиторий в СДО.
