from typing import Literal

from pydantic import BaseModel


class UserBase(BaseModel):
    username: str


class User(UserBase):
    password: str


class UserRegister(User):
    """Регистрация с опциональной ролью (задание 7.1)."""

    role: Literal["admin", "user", "guest"] = "guest"


class UserInDB(UserBase):
    hashed_password: str
    role: Literal["admin", "user", "guest"] = "guest"


class TokenResponse(BaseModel):
    access_token: str
    token_type: str = "bearer"


class TodoCreate(BaseModel):
    title: str
    description: str = ""


class TodoUpdate(BaseModel):
    title: str
    description: str
    completed: bool


class TodoOut(BaseModel):
    id: int
    title: str
    description: str
    completed: bool

    model_config = {"from_attributes": True}
