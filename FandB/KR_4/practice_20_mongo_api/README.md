# Practice 20 — MongoDB API

CRUD API для сущности **User** с хранением данных в MongoDB (документная NoSQL БД).

## Запуск (через Docker)

```bash
cd KR_4/practice_20_mongo_api
docker compose up -d
npm i
npm start
```

По умолчанию API слушает `http://localhost:3006`.

## Эндпоинты

- `POST /api/users`
- `GET /api/users`
- `GET /api/users/:id`
- `PATCH /api/users/:id`
- `DELETE /api/users/:id`

## Переменные окружения

- `MONGO_URL` (пример: `mongodb://localhost:27017/kr4_practice20`)
- `PORT`

