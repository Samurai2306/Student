# Practice 8

Доработка сервера из практики 7:

- выдача JWT `accessToken` при входе (`POST /api/auth/login`)
- защищённый маршрут `GET /api/auth/me`
- защита маршрутов:
  - `GET /api/products/:id`
  - `PUT /api/products/:id`
  - `DELETE /api/products/:id`

## Запуск

```bash
cd KR_2/Practice_8/server
npm i
npm run start
```

