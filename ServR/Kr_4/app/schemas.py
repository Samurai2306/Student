from __future__ import annotations

from pydantic import BaseModel, EmailStr, Field, conint, constr


class ErrorResponse(BaseModel):
    code: str
    message: str
    details: list[dict] | None = None


class UserIn(BaseModel):
    username: str = Field(min_length=1, max_length=50)
    age: conint(gt=18)
    email: EmailStr
    password: constr(min_length=8, max_length=16)
    phone: str | None = "Unknown"


class SimpleUserIn(BaseModel):
    username: str
    age: int


class UserOut(BaseModel):
    id: int
    username: str
    age: int


class ProductIn(BaseModel):
    title: str = Field(min_length=1, max_length=200)
    price: int = Field(ge=0)
    count: int = Field(ge=0)
    description: str = Field(min_length=1, max_length=500)


class ProductOut(ProductIn):
    id: int
