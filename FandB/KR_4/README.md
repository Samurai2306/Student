# KR_4 (Практики 19–24)

## Содержание

- **Practice 19**: `practice_19_postgres_api` — CRUD API пользователей на PostgreSQL.
- **Practice 20**: `practice_20_mongo_api` — CRUD API пользователей на MongoDB.
- **Practice 21**: доработка `KR_2/Practice_11/server` — кэширование `GET /api/users*` и `GET /api/products*` через Redis с инвалидацией.
- **Practice 22**: `practice_22_load_balancer` — ручная балансировка Nginx и пример HAProxy.
- **Practice 23**: `practice_23_docker_compose` — Docker Compose (2 backend + Nginx).
- **Practice 24**: тестирование и корректный README.

## Practice 21 (Redis cache для Practice_11)

Перейдите в сервер практики 11 и установите зависимости:

```bash
cd KR_2/Practice_11/server
npm i
```

Запуск Redis (пример через Docker):

```bash
docker run -d --name redis-cache -p 6379:6379 redis
```

Запуск сервера:

```bash
npm start
```

При включенном Redis ответы на маршруты:

- `GET /api/users`
- `GET /api/users/:id`
- `GET /api/products`
- `GET /api/products/:id`

возвращают обычные данные (как в практике 11), а источник можно увидеть по заголовку **`X-Cache: HIT|MISS`**.

## Practice 24

Репозиторий содержит все файлы и инструкции запуска. Для сдачи: проверить работоспособность (по README выше), убедиться что репозиторий публичный, и прикрепить ссылку в СДО.

