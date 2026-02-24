# Практика 2 
REST API с CRUD для списка товаров. Объект товара: **id**, **название**, **стоимость**.
```bash
npm install
npm start
```
| GET | /products | Список всех товаров |
| GET | /products/:id | Товар по id |
| POST | /products | Добавить товар (body: `{"name":"...","cost":123}`) |
| PATCH | /products/:id | Редактировать товар |
| DELETE | /products/:id | Удалить товар |

В POST и PATCH поле `cost` должно быть неотрицательным числом. Запросы логируются в консоль сервера.
