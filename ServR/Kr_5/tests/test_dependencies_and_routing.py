from __future__ import annotations

from fastapi.testclient import TestClient

TASK_PAYLOAD = {
    "title": "Админская проверка",
    "description": "Описание",
    "status": "todo",
    "priority": 3,
}


def test_users_me(client: TestClient) -> None:
    response = client.get("/users/me", headers={"X-User-Id": "10", "X-User-Role": "user"})
    assert response.status_code == 200
    assert response.json() == {"id": 10, "role": "user"}


def test_users_by_id(client: TestClient) -> None:
    response = client.get("/users/10", headers={"X-User-Id": "10", "X-User-Role": "user"})
    assert response.status_code == 200
    assert response.json() == {"id": 10, "role": "user"}


def test_missing_user_header_returns_401(client: TestClient) -> None:
    response = client.get("/users/me")
    assert response.status_code == 401


def test_regular_user_forbidden_on_admin_stats(client: TestClient) -> None:
    response = client.get("/admin/stats", headers={"X-User-Id": "10", "X-User-Role": "user"})
    assert response.status_code == 403


def test_admin_stats(client: TestClient) -> None:
    client.post("/tasks", json=TASK_PAYLOAD, headers={"X-User-Id": "10", "X-User-Role": "user"})
    client.post(
        "/tasks",
        json={**TASK_PAYLOAD, "title": "Вторая", "status": "done"},
        headers={"X-User-Id": "20", "X-User-Role": "user"},
    )

    response = client.get("/admin/stats", headers={"X-User-Id": "1", "X-User-Role": "admin"})
    assert response.status_code == 200
    body = response.json()
    assert body["total_tasks"] == 2
    assert body["by_status"]["todo"] == 1
    assert body["by_status"]["done"] == 1


def test_user_cannot_delete_foreign_task(client: TestClient) -> None:
    created = client.post(
        "/tasks",
        json=TASK_PAYLOAD,
        headers={"X-User-Id": "10", "X-User-Role": "user"},
    ).json()

    response = client.delete(
        f"/tasks/{created['id']}",
        headers={"X-User-Id": "20", "X-User-Role": "user"},
    )
    assert response.status_code == 404


def test_admin_can_delete_foreign_task(client: TestClient) -> None:
    created = client.post(
        "/tasks",
        json=TASK_PAYLOAD,
        headers={"X-User-Id": "10", "X-User-Role": "user"},
    ).json()

    response = client.delete(
        f"/admin/tasks/{created['id']}",
        headers={"X-User-Id": "1", "X-User-Role": "admin"},
    )
    assert response.status_code == 204


def test_openapi_tags_grouped(client: TestClient) -> None:
    schema = client.get("/openapi.json").json()
    operation_tags: set[str] = set()
    for path_item in schema.get("paths", {}).values():
        for operation in path_item.values():
            if isinstance(operation, dict):
                operation_tags.update(operation.get("tags", []))
    assert {"tasks", "users", "admin"}.issubset(operation_tags)


def test_health(client: TestClient) -> None:
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json()["status"] == "ok"
    assert "env" in response.json()
