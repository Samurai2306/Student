from __future__ import annotations

from collections import defaultdict
from threading import Lock

from fastapi import WebSocket


class RoomManager:
    def __init__(self) -> None:
        self._rooms: dict[str, dict[str, set[WebSocket]]] = defaultdict(lambda: defaultdict(set))
        self._lock = Lock()

    def reset(self) -> None:
        with self._lock:
            self._rooms.clear()

    def connect(self, room_id: str, username: str, websocket: WebSocket) -> None:
        with self._lock:
            self._rooms[room_id][username].add(websocket)

    def disconnect(self, room_id: str, username: str, websocket: WebSocket) -> None:
        with self._lock:
            room = self._rooms.get(room_id)
            if not room:
                return
            connections = room.get(username)
            if not connections:
                return
            connections.discard(websocket)
            if not connections:
                del room[username]
            if not room:
                del self._rooms[room_id]

    async def broadcast(self, room_id: str, payload: dict, exclude: WebSocket | None = None) -> None:
        with self._lock:
            room = self._rooms.get(room_id, {})
            sockets = {ws for conns in room.values() for ws in conns}

        for websocket in sockets:
            if websocket is exclude:
                continue
            await websocket.send_json(payload)

    def get_users(self, room_id: str) -> list[str]:
        with self._lock:
            room = self._rooms.get(room_id, {})
            return sorted(room.keys())


room_manager = RoomManager()
