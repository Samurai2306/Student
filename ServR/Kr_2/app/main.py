import time
import uuid
from datetime import datetime
from typing import Annotated

from fastapi import Cookie, Depends, FastAPI, Form, Header, HTTPException, Request, Response
from fastapi.responses import JSONResponse
from itsdangerous import BadSignature, Signer
from pydantic import ValidationError

from app.models import CommonHeaders, LoginRequest, Product, UserCreate

app = FastAPI()

products = [
    {"product_id": 123, "name": "Smartphone", "category": "Electronics", "price": 599.99},
    {"product_id": 456, "name": "Phone Case", "category": "Accessories", "price": 19.99},
    {"product_id": 789, "name": "Iphone", "category": "Electronics", "price": 1299.99},
    {"product_id": 101, "name": "Headphones", "category": "Accessories", "price": 99.99},
    {"product_id": 202, "name": "Smartwatch", "category": "Electronics", "price": 299.99},
]

users_db = {
    "admin": "12345",
    "user": "12345678",
    "user2": "12345678",
}

sessions_db: dict[str, str] = {}

SECRET_KEY = "my-super-secret-key-123"
signer = Signer(SECRET_KEY)
SESSION_LIFETIME_SECONDS = 300
SESSION_REFRESH_FROM_SECONDS = 180


@app.post("/create_user", response_model=UserCreate, summary="Создание пользователя")
def create_user(user: UserCreate):
    return user


@app.get("/product/{product_id}", response_model=Product, summary="Получение продукта")
def get_product(product_id: int):
    for product in products:
        if product["product_id"] == product_id:
            return product
    raise HTTPException(status_code=404, detail="Продукт не найден")


@app.get("/products/search", response_model=list[Product], summary="Поиск продуктов")
def search_products(keyword: str, category: str | None = None, limit: int = 10):
    matches = []
    for product in products:
        has_keyword = keyword.lower() in product["name"].lower()
        category_ok = category is None or product["category"] == category
        if has_keyword and category_ok:
            matches.append(product)
    return matches[:limit]


def _build_session_token(user_id: str, last_activity: int) -> str:
    payload = f"{user_id}.{last_activity}"
    signature = signer.get_signature(payload).decode("utf-8")
    return f"{payload}.{signature}"


def _validate_session_token(token: str) -> tuple[str, int]:
    parts = token.split(".", 2)
    if len(parts) != 3:
        raise ValueError("Invalid session")

    user_id, timestamp_raw, signature = parts
    payload = f"{user_id}.{timestamp_raw}"
    signed_value = f"{payload}.{signature}".encode("utf-8")

    try:
        signer.unsign(signed_value)
    except BadSignature as exc:
        raise ValueError("Invalid session") from exc

    try:
        last_activity = int(timestamp_raw)
    except ValueError as exc:
        raise ValueError("Invalid session") from exc

    return user_id, last_activity


@app.post("/login", summary="Вход с подписанной Cookie")
async def login(
    response: Response,
    request: Request,
    username: str | None = Form(default=None),
    password: str | None = Form(default=None),
):
    # Сначала поддерживаем form-data (удобно для Swagger UI)
    if username and password:
        login_data = LoginRequest.model_validate({"username": username, "password": password})
    else:
        # Если form-поля не пришли, пытаемся читать JSON
        try:
            payload = await request.json()
        except Exception:
            payload = None

        if not isinstance(payload, dict):
            raise HTTPException(status_code=400, detail="Передайте username/password в JSON или форме")

        try:
            login_data = LoginRequest.model_validate(payload)
        except ValidationError:
            raise HTTPException(status_code=400, detail="Передайте username/password в JSON или форме")

    username, password = login_data.username, login_data.password

    if username not in users_db or users_db[username] != password:
        raise HTTPException(status_code=401, detail="Invalid credentials")

    user_id = str(uuid.uuid4())
    sessions_db[user_id] = username
    now_ts = int(time.time())
    session_token = _build_session_token(user_id, now_ts)

    response.set_cookie(
        key="session_token",
        value=session_token,
        httponly=True,
        secure=False,
        max_age=SESSION_LIFETIME_SECONDS,
    )
    return {"message": "Успешный вход", "user_id": user_id}


@app.get("/user", summary="Проверка авторизации через Cookie")
def get_user(session_token: str | None = Cookie(default=None)):
    if not session_token:
        return JSONResponse(status_code=401, content={"message": "Unauthorized"})
    try:
        user_id, _ = _validate_session_token(session_token)
    except ValueError:
        return JSONResponse(status_code=401, content={"message": "Unauthorized"})
    username = sessions_db.get(user_id)
    if not username:
        return JSONResponse(status_code=401, content={"message": "Unauthorized"})

    return {"message": f"Успешный вход пользователя {username}!"}


@app.get("/profile", summary="Профиль с безопасной сессией")
def get_profile(response: Response, session_token: str | None = Cookie(default=None)):
    if not session_token:
        return JSONResponse(status_code=401, content={"message": "Session expired"})

    try:
        user_id, last_activity = _validate_session_token(session_token)
    except ValueError:
        return JSONResponse(status_code=401, content={"message": "Invalid session"})
    username = sessions_db.get(user_id)
    if not username:
        return JSONResponse(status_code=401, content={"message": "Session expired"})

    now_ts = int(time.time())
    diff = now_ts - last_activity

    if diff < 0:
        return JSONResponse(status_code=401, content={"message": "Invalid session"})
    if diff >= SESSION_LIFETIME_SECONDS:
        return JSONResponse(status_code=401, content={"message": "Session expired"})

    if SESSION_REFRESH_FROM_SECONDS <= diff < SESSION_LIFETIME_SECONDS:
        refreshed = _build_session_token(user_id, now_ts)
        response.set_cookie(
            key="session_token",
            value=refreshed,
            httponly=True,
            secure=False,
            max_age=SESSION_LIFETIME_SECONDS,
        )

    return {"message": "Welcome to your profile", "user_id": user_id, "username": username}


def get_common_headers(
    user_agent: Annotated[str | None, Header(alias="User-Agent")] = None,
    accept_language: Annotated[str | None, Header(alias="Accept-Language")] = None,
) -> CommonHeaders:
    if not user_agent or not accept_language:
        raise HTTPException(status_code=400, detail="Missing required headers")

    try:
        return CommonHeaders(user_agent=user_agent, accept_language=accept_language)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.get("/headers", summary="Чтение заголовков запроса")
def read_headers(headers: CommonHeaders = Depends(get_common_headers)):
    return {"User-Agent": headers.user_agent, "Accept-Language": headers.accept_language}


@app.get("/info", summary="Заголовки + дополнительная информация")
def read_info(response: Response, headers: CommonHeaders = Depends(get_common_headers)):
    response.headers["X-Server-Time"] = datetime.now().isoformat(timespec="seconds")
    return {
        "message": "Добро пожаловать! Ваши заголовки успешно обработаны.",
        "headers": {
            "User-Agent": headers.user_agent,
            "Accept-Language": headers.accept_language,
        },
    }

