# Practice 10

Фронтенд (React + Vite) для сервера из практики 9.

## Что сделано

- страницы регистрации и входа
- страница списка товаров
- просмотр товара по id (требует access-токен)
- создание / редактирование / удаление товара
- хранение токенов в `localStorage`
- авто‑подстановка `Authorization: Bearer <accessToken>` в запросы
- авто‑обновление токенов при `401` через `POST /api/auth/refresh`

## Запуск

### Сервер

```bash
cd KR_2/Practice_10/server
npm i
npm run start
```

### Клиент

```bash
cd KR_2/Practice_10/client
npm i
npm run dev
```

Vite проксирует запросы `/api/*` на `http://localhost:3000`.

