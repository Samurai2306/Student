# Practice 19 — PostgreSQL API

CRUD API для сущности **User** с хранением данных в PostgreSQL.

## Запуск (через Docker)

```bash
cd KR_4/practice_19_postgres_api
docker compose up -d
npm i
npm start
```

По умолчанию API слушает `http://localhost:3005`.

## Эндпоинты

- `POST /api/users`
- `GET /api/users`
- `GET /api/users/:id`
- `PATCH /api/users/:id`
- `DELETE /api/users/:id`

## Переменные окружения

- `DATABASE_URL` (пример: `postgres://postgres:postgres@localhost:5432/kr4_practice19`)
- `PORT`

