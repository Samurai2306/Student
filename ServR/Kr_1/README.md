# Контрольная работа №1 — FastAPI

Установка зависимостей:
```bash
pip install -r requirements.txt
```
Переменная приложения — `web_application` (задание 1.1).
```bash
python -m uvicorn app:web_application --reload
```


- **http://localhost:8000** — HTML-страница (задание 1.2)
- **http://localhost:8000/welcome** — JSON приветствие (задание 1.1)
- **http://localhost:8000/docs** — интерактивная документация API

## Маршруты

| Метод | Маршрут      | Описание |
|-------|--------------|----------|
| GET   | /            | HTML (index.html) |
| GET   | /welcome     | JSON: приветствие |
| POST  | /calculate   | Сумма num1 + num2 (query: ?num1=5&num2=10) |
| GET   | /users       | Данные пользователя (User) |
| POST  | /user        | JSON {name, age} → + is_adult |
| POST  | /feedback    | JSON {name, message} с валидацией |


- `app.py` — приложение FastAPI
- `models.py` — модели Pydantic (User, UserWithAge, Feedback)
- `index.html` — страница для задания 1.2
