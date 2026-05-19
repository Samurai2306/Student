from __future__ import annotations

from dataclasses import dataclass, field
from itertools import count
from threading import Lock

from app.schemas import TaskCreate, TaskOut, TaskStatus


@dataclass
class TaskStorage:
    _tasks: dict[int, dict] = field(default_factory=dict)
    _id_seq: count = field(default_factory=lambda: count(start=1))
    _lock: Lock = field(default_factory=Lock)

    def reset(self) -> None:
        with self._lock:
            self._tasks.clear()
            self._id_seq = count(start=1)

    def create_task(self, payload: TaskCreate, owner_id: int) -> TaskOut:
        with self._lock:
            task_id = next(self._id_seq)
            record = {
                "id": task_id,
                "title": payload.title,
                "description": payload.description,
                "status": payload.status,
                "priority": payload.priority,
                "owner_id": owner_id,
            }
            self._tasks[task_id] = record
            return TaskOut(**record)

    def list_tasks(
        self,
        owner_id: int,
        status: TaskStatus | None = None,
        min_priority: int | None = None,
    ) -> list[TaskOut]:
        with self._lock:
            items = [t for t in self._tasks.values() if t["owner_id"] == owner_id]
        if status is not None:
            items = [t for t in items if t["status"] == status]
        if min_priority is not None:
            items = [t for t in items if t["priority"] >= min_priority]
        return [TaskOut(**t) for t in sorted(items, key=lambda x: x["id"])]

    def get_task(self, task_id: int, owner_id: int | None = None) -> TaskOut | None:
        with self._lock:
            record = self._tasks.get(task_id)
        if record is None:
            return None
        if owner_id is not None and record["owner_id"] != owner_id:
            return None
        return TaskOut(**record)

    def update_status(self, task_id: int, owner_id: int, status: TaskStatus) -> TaskOut | None:
        with self._lock:
            record = self._tasks.get(task_id)
            if record is None or record["owner_id"] != owner_id:
                return None
            record["status"] = status
            return TaskOut(**record)

    def delete_task(self, task_id: int, owner_id: int | None = None) -> bool:
        with self._lock:
            record = self._tasks.get(task_id)
            if record is None:
                return False
            if owner_id is not None and record["owner_id"] != owner_id:
                return False
            del self._tasks[task_id]
            return True

    def stats(self) -> dict:
        with self._lock:
            by_status = {"todo": 0, "in_progress": 0, "done": 0}
            for record in self._tasks.values():
                by_status[record["status"]] += 1
            return {"total_tasks": len(self._tasks), "by_status": by_status}


storage = TaskStorage()
