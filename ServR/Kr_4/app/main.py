from __future__ import annotations

from itertools import count
from threading import Lock

from fastapi import Depends, FastAPI, HTTPException, Request, Response
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from sqlalchemy import select
from sqlalchemy.orm import Session

from app.db import get_db
from app.exceptions import CustomExceptionA, CustomExceptionB
from app.models import Product
from app.schemas import (
    ErrorResponse,
    ProductIn,
    ProductOut,
    SimpleUserIn,
    UserIn,
    UserOut,
)


app = FastAPI(title="KR-4 FastAPI")


@app.exception_handler(CustomExceptionA)
async def custom_exception_a_handler(_request: Request, exc: CustomExceptionA) -> JSONResponse:
    return JSONResponse(
        status_code=418,
        content=ErrorResponse(code="CUSTOM_A", message=exc.message).model_dump(),
    )


@app.exception_handler(CustomExceptionB)
async def custom_exception_b_handler(_request: Request, exc: CustomExceptionB) -> JSONResponse:
    return JSONResponse(
        status_code=409,
        content=ErrorResponse(code="CUSTOM_B", message=exc.message).model_dump(),
    )


@app.exception_handler(RequestValidationError)
async def validation_exception_handler(_request: Request, exc: RequestValidationError) -> JSONResponse:
    return JSONResponse(
        status_code=422,
        content=ErrorResponse(
            code="VALIDATION_ERROR",
            message="Request validation failed",
            details=exc.errors(),
        ).model_dump(),
    )


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/errors/a")
def raise_a() -> None:
    raise CustomExceptionA("Condition for A failed")


@app.get("/errors/b")
def raise_b() -> None:
    raise CustomExceptionB("Resource not found / conflict for B")


@app.post("/validate-user", response_model=dict[str, str])
def validate_user(_payload: UserIn) -> dict[str, str]:
    return {"result": "valid"}


# In-memory storage for assignment 11.2
db_users: dict[int, dict] = {}
_id_seq = count(start=1)
_id_lock = Lock()


def next_user_id() -> int:
    with _id_lock:
        return next(_id_seq)

def reset_users_state() -> None:
    global _id_seq
    db_users.clear()
    _id_seq = count(start=1)


@app.post("/users", response_model=UserOut, status_code=201)
def create_user(user: SimpleUserIn) -> UserOut:
    user_id = next_user_id()
    db_users[user_id] = user.model_dump()
    return UserOut(id=user_id, **db_users[user_id])


@app.get("/users/{user_id}", response_model=UserOut)
def get_user(user_id: int) -> UserOut:
    if user_id not in db_users:
        raise HTTPException(status_code=404, detail="User not found")
    return UserOut(id=user_id, **db_users[user_id])


@app.delete("/users/{user_id}", status_code=204)
def delete_user(user_id: int) -> Response:
    if db_users.pop(user_id, None) is None:
        raise HTTPException(status_code=404, detail="User not found")
    return Response(status_code=204)


# Small DB-backed example for Product (assignment 9.1)
@app.post("/products", response_model=ProductOut, status_code=201)
def create_product(payload: ProductIn, db: Session = Depends(get_db)) -> ProductOut:
    product = Product(**payload.model_dump())
    db.add(product)
    db.commit()
    db.refresh(product)
    return ProductOut(
        id=product.id,
        title=product.title,
        price=product.price,
        count=product.count,
        description=product.description,
    )


@app.get("/products", response_model=list[ProductOut])
def list_products(db: Session = Depends(get_db)) -> list[ProductOut]:
    products = db.execute(select(Product).order_by(Product.id)).scalars().all()
    return [
        ProductOut(
            id=p.id,
            title=p.title,
            price=p.price,
            count=p.count,
            description=p.description,
        )
        for p in products
    ]
