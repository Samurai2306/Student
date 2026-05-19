from __future__ import annotations

import pytest
from fastapi.testclient import TestClient


def test_connect_without_username_closes_with_1008(client: TestClient) -> None:
    with pytest.raises(Exception):
        with client.websocket_connect("/ws/rooms/python?username="):
            pass


def test_connect_with_valid_username(client: TestClient) -> None:
    with client.websocket_connect("/ws/rooms/python?username=alice") as ws:
        event = ws.receive_json()
        assert event["type"] == "join"
        assert event["username"] == "alice"


def test_send_and_receive_message(client: TestClient) -> None:
    with client.websocket_connect("/ws/rooms/python?username=alice") as ws:
        ws.receive_json()
        ws.send_json({"type": "message", "text": "Всем привет"})
        message = ws.receive_json()
        assert message == {
            "type": "message",
            "room_id": "python",
            "username": "alice",
            "text": "Всем привет",
        }


def test_two_clients_receive_same_message(client: TestClient) -> None:
    with client.websocket_connect("/ws/rooms/python?username=alice") as alice:
        with client.websocket_connect("/ws/rooms/python?username=bob") as bob:
            alice.receive_json()
            bob.receive_json()
            alice.receive_json()

            alice.send_json({"type": "message", "text": "Общий чат"})
            alice_msg = alice.receive_json()
            bob_msg = bob.receive_json()

            assert alice_msg["text"] == "Общий чат"
            assert bob_msg["text"] == "Общий чат"


def test_different_rooms_are_isolated(client: TestClient) -> None:
    with client.websocket_connect("/ws/rooms/python?username=alice") as py_ws:
        with client.websocket_connect("/ws/rooms/java?username=bob") as java_ws:
            py_ws.receive_json()
            java_ws.receive_json()

            py_ws.send_json({"type": "message", "text": "Только python"})
            py_msg = py_ws.receive_json()
            assert py_msg["room_id"] == "python"

            java_ws.send_json({"type": "message", "text": "ping"})
            java_msg = java_ws.receive_json()
            assert java_msg["room_id"] == "java"


def test_message_too_long_returns_error(client: TestClient) -> None:
    with client.websocket_connect("/ws/rooms/python?username=alice") as ws:
        ws.receive_json()
        ws.send_json({"type": "message", "text": "x" * 301})
        error = ws.receive_json()
        assert error == {"type": "error", "detail": "Message is too long"}


def test_room_users_after_disconnect(client: TestClient) -> None:
    with client.websocket_connect("/ws/rooms/python?username=alice") as ws:
        ws.receive_json()

    response = client.get("/rooms/python/users")
    assert response.status_code == 200
    assert response.json() == {"room_id": "python", "users": []}
