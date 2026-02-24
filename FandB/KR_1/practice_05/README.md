# Практика 5 — Расширенный REST API (Swagger)

## Что добавлено по сравнению с ПЗ 4

- Генерация спецификации OpenAPI из JSDoc-комментариев в коде
- Интерактивный Swagger UI для просмотра и вызова API
- Описание схемы Product (все поля, пример)
- Описание всех эндпоинтов: POST, GET, GET /:id, PATCH /:id, DELETE /:id
- Примеры тела запроса для POST (создание) и PATCH (обновление)
- Описание ответов 200/201, 400 (ошибки валидации), 404
- Логирование запросов и валидация на сервере (как в ПЗ 4)

## Структура проекта

```
practice_05/
├── server/
│   ├── app.js       # Express + Swagger (swagger-jsdoc, swagger-ui-express), CRUD товаров
│   └── package.json
└── README.md
```

## Запуск сервера

```bash
cd server
npm install
npm start
```

- **API:** http://localhost:3000  
- **Документация Swagger (интерактивная):** http://localhost:3000/api-docs  

В Swagger UI можно нажать «Try it out» у любого метода, подставить параметры и отправить запрос.

## Проверка работы

1. Откройте http://localhost:3000/api-docs
2. Разверните **GET /api/products** → Try it out → Execute — должен вернуться список товаров
3. Разверните **POST /api/products** → введите тело из примера (name, category, price, quantityInStock) → Execute — ответ 201 и созданный объект
4. Для **PATCH** и **DELETE** укажите id существующего товара из списка

## Клиент (интерфейс магазина)

Используйте клиент из папки **practice_04/client**: он обращается к `http://localhost:3000/api`. Запустите сервер ПЗ 5 вместо сервера ПЗ 4 — клиент будет работать с тем же API, при этом документация доступна на `/api-docs`.
