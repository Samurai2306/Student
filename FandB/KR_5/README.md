# KR_5 — Social Feed (Контрольная работа №5)

Итоговый веб-проект: **мини-социальная сеть** (вариант 2 из практики 28) с лентой постов, лайками, комментариями, real-time уведомлениями и кэшированием ленты в Redis.

## Описание

Пользователи регистрируются и входят по JWT. В ленте можно публиковать посты, ставить лайки и оставлять комментарии. Автор поста и **admin** могут редактировать и удалять записи. При лайке и комментарии автор поста получает уведомление через **WebSocket (Socket.IO)**. Лента постов кэшируется в **Redis** (заголовок `X-Cache: HIT|MISS`), на фронтенде реализована **бесконечная прокрутка**.

## Стек технологий

- **Frontend:** React, Vite, React Router, Socket.IO Client
- **Backend:** Node.js, Express, Socket.IO
- **База данных:** PostgreSQL
- **Кэш:** Redis
- **Авторизация:** JWT + RBAC (`user`, `admin`)
- **Контейнеризация:** Docker, Docker Compose

## Запуск проекта

### Требования

- Docker и Docker Compose

### Шаги

1. Клонировать репозиторий: `git clone https://github.com/Samurai2306/Student.git`
2. Перейти в проект:
   ```bash
   cd FandB/KR_5/social-feed
   ```
3. Скопировать переменные окружения:
   ```bash
   cp .env.example .env
   ```
4. Запустить все сервисы:
   ```bash
   docker compose up --build
   ```
5. Открыть в браузере: **http://localhost:3000**

### Учётные данные admin (по умолчанию)

- Email: `admin@social.local`
- Пароль: `admin123`

## Переменные окружения

| Переменная | Описание |
|---|---|
| `ACCESS_SECRET` | Секрет для access JWT |
| `REFRESH_SECRET` | Секрет для refresh JWT |
| `ADMIN_EMAIL` | Email администратора (создаётся при старте) |
| `ADMIN_PASSWORD` | Пароль администратора |
| `DATABASE_URL` | Строка подключения PostgreSQL |
| `REDIS_URL` | URL Redis |
| `PORT` | Порт API (по умолчанию 4000) |

## API (кратко)

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/auth/register` | Регистрация |
| POST | `/api/auth/login` | Вход |
| POST | `/api/auth/refresh` | Обновление access token |
| GET | `/api/posts?cursor=&limit=` | Лента (infinite scroll) |
| POST | `/api/posts` | Создать пост |
| PATCH | `/api/posts/:id` | Изменить пост |
| DELETE | `/api/posts/:id` | Удалить пост |
| POST/DELETE | `/api/posts/:id/like` | Лайк / убрать лайк |
| GET/POST | `/api/posts/:id/comments` | Комментарии |

## Запуск тестов

```bash
cd KR_5/social-feed/backend
npm install
npm test
```

или:

```bash
npm run test:coverage
```

Покрытие ≥ 50% для модулей `src/lib` и `src/middleware`.

## Локальная разработка (без Docker для Node)

```bash
# Терминал 1 — БД и Redis
cd KR_5/social-feed
docker compose up db redis

# Терминал 2 — API
cd backend && npm i && npm run dev

# Терминал 3 — Frontend
cd frontend && npm i && npm run dev
```

Frontend dev-сервер: http://localhost:5173 (проксирует `/api` и WebSocket на `:4000`).

## Проверка требований KR5

- [x] Express API + PostgreSQL
- [x] React SPA
- [x] JWT + RBAC (`user`, `admin`)
- [x] Docker Compose — один запуск всего стека
- [x] README с инструкцией
- [x] Тесты с покрытием ≥ 50%
- [x] WebSocket уведомления
- [x] Redis-кэш ленты
- [x] Infinite scroll на фронтенде

## Practice 28–30

Практики 28–30 — подготовка, доработка и сдача этого проекта. Ссылку на публичный репозиторий прикрепите в СДО: **Задания текущего контроля → Контрольная работа №5**.
