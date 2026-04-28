# Practice 23 — Docker Compose (2 backends + Nginx)

Стек из **двух одинаковых backend‑сервисов** (Express) и **Nginx** как балансировщика.

## Запуск (WSL / Docker)

```bash
cd KR_4/practice_23_docker_compose
docker compose up --build
```

Проверка балансировки:

```bash
curl http://localhost:8088/
curl http://localhost:8088/
curl http://localhost:8088/
```

Ответы должны по очереди приходить от `backend-1` и `backend-2`.

## Отказоустойчивость (проверка)

Остановите один backend:

```bash
docker compose stop backend1
curl http://localhost:8088/
```

Nginx должен продолжить отвечать через `backend-2`.

