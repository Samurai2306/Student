from __future__ import annotations

import os

from fastapi import FastAPI, WebSocket, WebSocketDisconnect

from app.room_manager import room_manager
from app.routers import admin, tasks, users
from app.schemas import RoomUsersOut
from app.storage import storage

APP_ENV = os.getenv("APP_ENV", "local")

app = FastAPI(title="KR-5 FastAPI")
app.include_router(tasks.router)
app.include_router(users.router)
app.include_router(admin.router)


def reset_app_state() -> None:
    storage.reset()
    room_manager.reset()


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "env": APP_ENV}


@app.get("/rooms/{room_id}/users", response_model=RoomUsersOut)
def list_room_users(room_id: str) -> RoomUsersOut:
    return RoomUsersOut(room_id=room_id, users=room_manager.get_users(room_id))


@app.websocket("/ws/rooms/{room_id}")
async def websocket_room(room_id: str, websocket: WebSocket) -> None:
    username = (websocket.query_params.get("username") or "").strip()
    if not username:
        await websocket.close(code=1008)
        return

    await websocket.accept()
    room_manager.connect(room_id, username, websocket)
    await room_manager.broadcast(
        room_id,
        {"type": "join", "room_id": room_id, "username": username},
    )

    try:
        while True:
            data = await websocket.receive_json()
            if data.get("type") != "message":
                continue

            text = data.get("text", "")
            if len(text) > 300:
                await websocket.send_json(
                    {"type": "error", "detail": "Message is too long"},
                )
                continue

            await room_manager.broadcast(
                room_id,
                {
                    "type": "message",
                    "room_id": room_id,
                    "username": username,
                    "text": text,
                },
            )
    except WebSocketDisconnect:
        pass
    finally:
        room_manager.disconnect(room_id, username, websocket)
        await room_manager.broadcast(
            room_id,
            {"type": "leave", "room_id": room_id, "username": username},
        )
