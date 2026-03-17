# Practice 9

Доработка сервера из практики 8:

- генерация `refreshToken` при входе
- хранение refresh-токенов в памяти
- `POST /api/auth/refresh`:
  - **берёт refresh-токен из заголовка** `x-refresh-token` (также поддерживается `refresh-token`)
  - возвращает новую пару токенов:
    ```json
    { "accessToken": "", "refreshToken": "" }
    ```

## Запуск

```bash
cd KR_2/Practice_9/server
npm i
npm run start
```

