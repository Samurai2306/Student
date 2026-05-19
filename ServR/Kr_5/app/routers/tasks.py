from __future__ import annotations

from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, Query, Response

from app.dependencies import get_current_user, get_storage
from app.schemas import TaskCreate, TaskOut, TaskStatus, TaskStatusUpdate, UserOut
from app.storage import TaskStorage

router = APIRouter(prefix="/tasks", tags=["tasks"])


@router.post("", response_model=TaskOut, status_code=201)
def create_task(
    payload: TaskCreate,
    user: Annotated[UserOut, Depends(get_current_user)],
    store: Annotated[TaskStorage, Depends(get_storage)],
) -> TaskOut:
    return store.create_task(payload, user.id)


@router.get("", response_model=list[TaskOut])
def list_tasks(
    user: Annotated[UserOut, Depends(get_current_user)],
    store: Annotated[TaskStorage, Depends(get_storage)],
    status: TaskStatus | None = None,
    min_priority: int | None = None,
) -> list[TaskOut]:
    if min_priority is not None and min_priority < 1:
        raise HTTPException(status_code=400, detail="min_priority must be between 1 and 5")
    return store.list_tasks(user.id, status=status, min_priority=min_priority)


@router.get("/{task_id}", response_model=TaskOut)
def get_task(
    task_id: int,
    user: Annotated[UserOut, Depends(get_current_user)],
    store: Annotated[TaskStorage, Depends(get_storage)],
) -> TaskOut:
    task = store.get_task(task_id, owner_id=user.id)
    if task is None:
        raise HTTPException(status_code=404, detail="Task not found")
    return task


@router.patch("/{task_id}/status", response_model=TaskOut)
def update_task_status(
    task_id: int,
    payload: TaskStatusUpdate,
    user: Annotated[UserOut, Depends(get_current_user)],
    store: Annotated[TaskStorage, Depends(get_storage)],
) -> TaskOut:
    task = store.update_status(task_id, user.id, payload.status)
    if task is None:
        raise HTTPException(status_code=404, detail="Task not found")
    return task


@router.delete("/{task_id}", status_code=204)
def delete_task(
    task_id: int,
    user: Annotated[UserOut, Depends(get_current_user)],
    store: Annotated[TaskStorage, Depends(get_storage)],
) -> Response:
    if not store.delete_task(task_id, owner_id=user.id):
        raise HTTPException(status_code=404, detail="Task not found")
    return Response(status_code=204)
