from __future__ import annotations

from typing import Annotated

from fastapi import Depends, Header, HTTPException

from app.schemas import UserOut
from app.storage import TaskStorage, storage


def get_current_user(
    x_user_id: Annotated[str | None, Header(alias="X-User-Id")] = None,
    x_user_role: Annotated[str | None, Header(alias="X-User-Role")] = None,
) -> UserOut:
    if x_user_id is None:
        raise HTTPException(status_code=401, detail="Missing X-User-Id header")
    try:
        user_id = int(x_user_id)
    except ValueError as exc:
        raise HTTPException(status_code=401, detail="Invalid X-User-Id header") from exc
    role = (x_user_role or "user").strip() or "user"
    return UserOut(id=user_id, role=role)


def require_admin(user: Annotated[UserOut, Depends(get_current_user)]) -> UserOut:
    if user.role != "admin":
        raise HTTPException(status_code=403, detail="Admin access required")
    return user


def get_storage() -> TaskStorage:
    return storage
