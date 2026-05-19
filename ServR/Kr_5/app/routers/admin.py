from __future__ import annotations

from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException, Response

from app.dependencies import get_storage, require_admin
from app.schemas import AdminStatsOut, UserOut
from app.storage import TaskStorage

router = APIRouter(prefix="/admin", tags=["admin"])


@router.get("/stats", response_model=AdminStatsOut)
def admin_stats(
    _admin: Annotated[UserOut, Depends(require_admin)],
    store: Annotated[TaskStorage, Depends(get_storage)],
) -> AdminStatsOut:
    return AdminStatsOut(**store.stats())


@router.delete("/tasks/{task_id}", status_code=204)
def admin_delete_task(
    task_id: int,
    _admin: Annotated[UserOut, Depends(require_admin)],
    store: Annotated[TaskStorage, Depends(get_storage)],
) -> Response:
    if not store.delete_task(task_id):
        raise HTTPException(status_code=404, detail="Task not found")
    return Response(status_code=204)
