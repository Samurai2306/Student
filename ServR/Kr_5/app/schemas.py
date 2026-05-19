from __future__ import annotations

from typing import Literal

from pydantic import BaseModel, Field

TaskStatus = Literal["todo", "in_progress", "done"]


class UserOut(BaseModel):
    id: int
    role: str


class TaskCreate(BaseModel):
    title: str = Field(min_length=3, max_length=80)
    description: str | None = None
    status: TaskStatus = "todo"
    priority: int = Field(ge=1, le=5)


class TaskStatusUpdate(BaseModel):
    status: TaskStatus


class TaskOut(BaseModel):
    id: int
    title: str
    description: str | None
    status: TaskStatus
    priority: int
    owner_id: int


class AdminStatsOut(BaseModel):
    total_tasks: int
    by_status: dict[str, int]


class RoomUsersOut(BaseModel):
    room_id: str
    users: list[str]
