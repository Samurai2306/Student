from contextlib import asynccontextmanager

from fastapi import Depends, FastAPI, HTTPException, Request, status
from fastapi.openapi.docs import get_swagger_ui_html
from fastapi.responses import JSONResponse, PlainTextResponse
from fastapi.security import HTTPBasic, HTTPBasicCredentials
from slowapi import Limiter
from slowapi.errors import RateLimitExceeded
from slowapi.middleware import SlowAPIMiddleware
from slowapi.util import get_remote_address

from config import get_settings
from database import find_user_row_by_username, get_db_connection, init_db
from dependencies import get_current_user_payload, require_roles
from models import (
    TodoCreate,
    TodoOut,
    TodoUpdate,
    TokenResponse,
    User,
    UserInDB,
    UserRegister,
)
from security import (
    auth_user,
    create_access_token,
    hash_password,
    verify_docs_credentials,
    verify_password,
)

limiter = Limiter(key_func=get_remote_address)
docs_basic = HTTPBasic(auto_error=False)


@asynccontextmanager
async def lifespan(_: FastAPI):
    init_db()
    yield


settings = get_settings()
app = FastAPI(
    title="КР3 — серверные приложения",
    lifespan=lifespan,
    docs_url=None,
    redoc_url=None,
    openapi_url=None,
)
app.state.limiter = limiter
app.add_middleware(SlowAPIMiddleware)


@app.exception_handler(RateLimitExceeded)
async def rate_limit_handler(request: Request, _exc: RateLimitExceeded):
    return JSONResponse(
        status_code=status.HTTP_429_TOO_MANY_REQUESTS,
        content={"detail": "Too many requests"},
    )


async def require_docs_user(
    credentials: HTTPBasicCredentials | None = Depends(docs_basic),
):
    verify_docs_credentials(credentials)


def _attach_openapi_factory(application: FastAPI) -> None:
    from fastapi.openapi.utils import get_openapi

    def custom_openapi():
        if application.openapi_schema:
            return application.openapi_schema
        application.openapi_schema = get_openapi(
            title=application.title,
            version="0.1.0",
            routes=application.routes,
        )
        return application.openapi_schema

    application.openapi = custom_openapi


if settings.mode == "DEV":
    _attach_openapi_factory(app)

    @app.get("/docs", include_in_schema=False)
    async def dev_docs(_: None = Depends(require_docs_user)):
        return get_swagger_ui_html(openapi_url="/openapi.json", title=app.title)

    @app.get("/openapi.json", include_in_schema=False)
    async def dev_openapi(_: None = Depends(require_docs_user)):
        return app.openapi()

else:

    @app.get("/docs", include_in_schema=False)
    async def prod_docs():
        raise HTTPException(status_code=404, detail="Not Found")

    @app.get("/openapi.json", include_in_schema=False)
    async def prod_openapi():
        raise HTTPException(status_code=404, detail="Not Found")

    @app.get("/redoc", include_in_schema=False)
    async def prod_redoc():
        raise HTTPException(status_code=404, detail="Not Found")


@app.get("/secret", include_in_schema=True)
async def lesson_6_1_secret(user: dict = Depends(auth_user)):
    del user
    return PlainTextResponse("You got my secret, welcome")


@app.get("/login", include_in_schema=True)
async def lesson_6_2_login_basic(user: dict = Depends(auth_user)):
    return {"message": f"Welcome, {user['username']}!"}


@app.post("/register", status_code=status.HTTP_201_CREATED)
@limiter.limit("1/minute")
async def register(request: Request, payload: UserRegister):
    with get_db_connection() as conn:
        if find_user_row_by_username(conn, payload.username) is not None:
            raise HTTPException(
                status_code=status.HTTP_409_CONFLICT,
                detail="User already exists",
            )
        user_in_db = UserInDB(
            username=payload.username,
            hashed_password=hash_password(payload.password),
            role=payload.role,
        )
        conn.execute(
            "INSERT INTO users (username, password, role) VALUES (?, ?, ?)",
            (user_in_db.username, user_in_db.hashed_password, user_in_db.role),
        )
    return {"message": "New user created"}


@app.post("/login", response_model=TokenResponse)
@limiter.limit("5/minute")
async def login_jwt(request: Request, payload: User):
    with get_db_connection() as conn:
        row = find_user_row_by_username(conn, payload.username)
        if row is None:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail="User not found",
            )
        if not verify_password(payload.password, row["password"]):
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="Authorization failed",
            )
        token = create_access_token(subject=row["username"], role=row["role"])
    return TokenResponse(access_token=token)


@app.get("/protected_resource")
async def protected_resource(
    user: dict = Depends(require_roles("admin", "user")),
):
    del user
    return {"message": "Access granted"}


def _todo_row_out(row) -> TodoOut:
    return TodoOut(
        id=row["id"],
        title=row["title"],
        description=row["description"] or "",
        completed=bool(row["completed"]),
    )


@app.get("/todos", response_model=list[TodoOut])
async def list_todos(_: dict = Depends(get_current_user_payload)):
    with get_db_connection() as conn:
        rows = conn.execute("SELECT * FROM todos ORDER BY id ASC").fetchall()
    return [_todo_row_out(row) for row in rows]


@app.post("/todos", response_model=TodoOut, status_code=status.HTTP_201_CREATED)
async def create_todo(
    payload: TodoCreate,
    _: dict = Depends(require_roles("admin")),
):
    with get_db_connection() as conn:
        cur = conn.execute(
            "INSERT INTO todos (title, description, completed) VALUES (?, ?, 0)",
            (payload.title, payload.description),
        )
        tid = cur.lastrowid
        row = conn.execute("SELECT * FROM todos WHERE id = ?", (tid,)).fetchone()
    return _todo_row_out(row)


@app.get("/todos/{todo_id}", response_model=TodoOut)
async def read_todo(
    todo_id: int,
    _: dict = Depends(get_current_user_payload),
):
    with get_db_connection() as conn:
        row = conn.execute("SELECT * FROM todos WHERE id = ?", (todo_id,)).fetchone()
    if row is None:
        raise HTTPException(status_code=404, detail="Todo not found")
    return _todo_row_out(row)


@app.put("/todos/{todo_id}", response_model=TodoOut)
async def update_todo(
    todo_id: int,
    payload: TodoUpdate,
    _: dict = Depends(require_roles("admin", "user")),
):
    with get_db_connection() as conn:
        row = conn.execute("SELECT * FROM todos WHERE id = ?", (todo_id,)).fetchone()
        if row is None:
            raise HTTPException(status_code=404, detail="Todo not found")
        conn.execute(
            "UPDATE todos SET title = ?, description = ?, completed = ? WHERE id = ?",
            (payload.title, payload.description, int(payload.completed), todo_id),
        )
        row = conn.execute("SELECT * FROM todos WHERE id = ?", (todo_id,)).fetchone()
    return _todo_row_out(row)


@app.delete("/todos/{todo_id}", status_code=status.HTTP_200_OK)
async def delete_todo(
    todo_id: int,
    _: dict = Depends(require_roles("admin")),
):
    with get_db_connection() as conn:
        cur = conn.execute("DELETE FROM todos WHERE id = ?", (todo_id,))
        if cur.rowcount == 0:
            raise HTTPException(status_code=404, detail="Todo not found")
    return {"message": "Todo deleted successfully"}


@app.get("/rbac/roles")
async def rbac_matrix(_: dict = Depends(get_current_user_payload)):
    return {
        "admin": {"todos": "create, read, update, delete"},
        "user": {"todos": "read, update"},
        "guest": {"todos": "read"},
    }
