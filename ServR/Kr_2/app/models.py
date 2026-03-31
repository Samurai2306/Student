import re
from pydantic import BaseModel, EmailStr, Field, field_validator

class UserCreate(BaseModel):
    name: str = Field(min_length=2, max_length=50)
    email: EmailStr
    age: int | None = Field(gt=0, le=100)
    is_subscribed: bool | None = None

class Product(BaseModel):
    product_id: int = Field(ge=1, le=1000)
    name: str = Field(min_length=2, max_length=50)
    category: str = Field(min_length=2, max_length=50)
    price: float = Field(ge=0, le=1000000)

class LoginRequest(BaseModel):
    username: str = Field(min_length=2, max_length=50)
    password: str = Field(min_length=5, max_length=100)


class CommonHeaders(BaseModel):
    user_agent: str = Field(min_length=1)
    accept_language: str = Field(min_length=1)

    @field_validator("accept_language")
    @classmethod
    def validate_accept_language(cls, value: str) -> str:
        pattern = r"^[a-z]{2}(?:-[A-Z]{2})?(?:,\s*[a-z]{2}(?:-[A-Z]{2})?(?:;q=(?:0(?:\.\d{1,3})?|1(?:\.0{1,3})?))?)*$"
        if not re.fullmatch(pattern, value):
            raise ValueError("Invalid Accept-Language format")
        return value