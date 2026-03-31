# Practice 11

Доработка практики 10: RBAC (роли) + управление пользователями.

## Роли

- `user`: просмотр товаров
- `seller`: права `user` + создание/редактирование товаров
- `admin`: права `seller` + удаление товаров + управление пользователями

## Маршруты (минимум по заданию)

- Auth (гость): `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`
- Пользователь: `GET /api/auth/me`, `GET /api/products`, `GET /api/products/:id`
- Продавец: `POST /api/products`, `PUT /api/products/:id`
- Администратор: `DELETE /api/products/:id`, `GET/PUT/DELETE /api/users*`

## Запуск

### Сервер


```bash
cd KR_2/Practice_11/server
npm i
npm run start
```

### Клиент

```bash
cd KR_2/Practice_11/client
npm i
npm run dev
```

## Данные для входа (администратор)

После запуска сервера создается тестовый администратор:

- `email`: `admin@practice11.local`
- `password`: `admin123`

При необходимости можно переопределить через переменные окружения сервера:

- `ADMIN_EMAIL`
- `ADMIN_PASSWORD`

