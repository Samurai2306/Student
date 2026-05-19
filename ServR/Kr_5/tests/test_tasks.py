from __future__ import annotations

from fastapi.testclient import TestClient

TASK_PAYLOAD = {
    "title": "Подготовить тесты",
    "description": "Написать интеграционные тесты для основных сценариев",
    "status": "todo",
    "priority": 4,
}


def test_create_task_success(client: TestClient) -> None:
    response = client.post(
        "/tasks",
        json=TASK_PAYLOAD,
        headers={"X-User-Id": "10"},
    )
    assert response.status_code == 201
    body = response.json()
    assert body["id"] == 1
    assert body["owner_id"] == 10
    assert body["title"] == TASK_PAYLOAD["title"]


def test_create_task_validation_error(client: TestClient) -> None:
    response = client.post(
        "/tasks",
        json={**TASK_PAYLOAD, "title": "ab"},
        headers={"X-User-Id": "10"},
    )
    assert response.status_code == 422


def test_create_task_unauthorized(client: TestClient) -> None:
    response = client.post("/tasks", json=TASK_PAYLOAD)
    assert response.status_code == 401


def test_invalid_user_id_header_returns_401(client: TestClient) -> None:
    response = client.get("/tasks", headers={"X-User-Id": "not-a-number"})
    assert response.status_code == 401


def test_invalid_min_priority_returns_400(client: TestClient) -> None:
    response = client.get("/tasks?min_priority=0", headers={"X-User-Id": "10"})
    assert response.status_code == 400


def test_user_sees_only_own_tasks(client: TestClient) -> None:
    client.post("/tasks", json=TASK_PAYLOAD, headers={"X-User-Id": "10"})
    client.post(
        "/tasks",
        json={**TASK_PAYLOAD, "title": "Чужая задача"},
        headers={"X-User-Id": "20"},
    )

    response = client.get("/tasks", headers={"X-User-Id": "10"})
    assert response.status_code == 200
    assert len(response.json()) == 1
    assert response.json()[0]["owner_id"] == 10


def test_filter_tasks(client: TestClient) -> None:
    client.post(
        "/tasks",
        json={**TASK_PAYLOAD, "status": "todo", "priority": 2},
        headers={"X-User-Id": "10"},
    )
    client.post(
        "/tasks",
        json={**TASK_PAYLOAD, "title": "Вторая задача", "status": "done", "priority": 5},
        headers={"X-User-Id": "10"},
    )

    by_status = client.get("/tasks?status=done", headers={"X-User-Id": "10"})
    assert len(by_status.json()) == 1
    assert by_status.json()[0]["status"] == "done"

    by_priority = client.get("/tasks?min_priority=4", headers={"X-User-Id": "10"})
    assert len(by_priority.json()) == 1
    assert by_priority.json()[0]["priority"] == 5


def test_update_task_status(client: TestClient) -> None:
    created = client.post("/tasks", json=TASK_PAYLOAD, headers={"X-User-Id": "10"}).json()
    response = client.patch(
        f"/tasks/{created['id']}/status",
        json={"status": "done"},
        headers={"X-User-Id": "10"},
    )
    assert response.status_code == 200
    assert response.json()["status"] == "done"


def test_foreign_or_missing_task_returns_404(client: TestClient) -> None:
    created = client.post("/tasks", json=TASK_PAYLOAD, headers={"X-User-Id": "10"}).json()

    foreign = client.get(f"/tasks/{created['id']}", headers={"X-User-Id": "20"})
    assert foreign.status_code == 404

    missing = client.get("/tasks/999", headers={"X-User-Id": "10"})
    assert missing.status_code == 404


def test_delete_task(client: TestClient) -> None:
    created = client.post("/tasks", json=TASK_PAYLOAD, headers={"X-User-Id": "10"}).json()
    response = client.delete(f"/tasks/{created['id']}", headers={"X-User-Id": "10"})
    assert response.status_code == 204

    follow_up = client.get(f"/tasks/{created['id']}", headers={"X-User-Id": "10"})
    assert follow_up.status_code == 404
