"""
Контрольная работа №1. FastAPI: маршруты, модели, валидация.
Запуск (имя переменной приложения — web_application):
    uvicorn app:web_application --reload
"""

from pathlib import Path

from fastapi import FastAPI
from fastapi.responses import FileResponse

from models import User, UserWithAge, Feedback


web_application = FastAPI(title="Контрольная №1")

feedbacks: list[Feedback] = []

current_user = User(name="Ваше Имя и Фамилия", id=1)

@web_application.get("/welcome")
def welcome_json():
    """JSON: «Добро пожаловать в моё приложение FastAPI!» (задание 1.1)."""
    return {"message": "Добро пожаловать в моё приложение FastAPI!"}

@web_application.get("/")
def root_html():
    """Возвращает index.html по корневому маршруту."""
    index_path = Path(__file__).parent / "index.html"
    return FileResponse(index_path, media_type="text/html; charset=utf-8")


@web_application.post("/calculate")
def calculate(num1: float, num2: float):
    """Принимает num1 и num2, возвращает их сумму."""
    return {"result": num1 + num2}


@web_application.get("/users")
def get_users():
    """Возвращает JSON с данными о пользователе."""
    return current_user.model_dump()

@web_application.post("/user")
def post_user(user: UserWithAge):
    """Принимает JSON с name и age, возвращает данные с полем is_adult."""
    is_adult = user.age >= 18
    return {
        "name": user.name,
        "age": user.age,
        "is_adult": is_adult,
    }

@web_application.post("/feedback")
def post_feedback(feedback: Feedback):
    """Принимает JSON по модели Feedback, сохраняет в список, возвращает сообщение."""
    feedbacks.append(feedback)
    return {"message": f"Спасибо, {feedback.name}! Ваш отзыв сохранён."}
