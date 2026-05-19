from __future__ import annotations

from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException

from app.dependencies import get_current_user
from app.schemas import UserOut

router = APIRouter(prefix="/users", tags=["users"])


@router.get("/me", response_model=UserOut)
def get_me(user: Annotated[UserOut, Depends(get_current_user)]) -> UserOut:
    return user


@router.get("/{user_id}", response_model=UserOut)
def get_user(
    user_id: int,
    current: Annotated[UserOut, Depends(get_current_user)],
) -> UserOut:
    if user_id != current.id and current.role != "admin":
        raise HTTPException(status_code=404, detail="User not found")
    if user_id == current.id:
        return current
    return UserOut(id=user_id, role="user")
