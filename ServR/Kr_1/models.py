"""
Pydantic-модели для приложения FastAPI.
Задание 1.4: User (name, id).
Задание 1.5: UserWithAge (name, age).
Задание 2.1–2.2: Feedback с ограничениями и кастомной валидацией.
"""

from pydantic import BaseModel, Field, field_validator


class User(BaseModel):
    """Пользователь (name, id) для GET /users."""
    name: str
    id: int


class UserWithAge(BaseModel):
    """Пользователь (name, age) для POST /user и проверки is_adult."""
    name: str
    age: int

# Нежелательные слова (проверяем вхождение подстроки в любом регистре и падеже)
FORBIDDEN_WORDS = ("кринж", "рофл", "вайб")


class Feedback(BaseModel):
    """Обратная связь: name 2–50 символов, message 10–500, без недопустимых слов."""

    name: str = Field(..., min_length=2, max_length=50)
    message: str = Field(..., min_length=10, max_length=500)

    @field_validator("message")
    @classmethod
    def message_no_forbidden_words(cls, v: str) -> str:
        lower = v.lower()
        for word in FORBIDDEN_WORDS:
            if word in lower:
                raise ValueError("Использование недопустимых слов")
        return v
