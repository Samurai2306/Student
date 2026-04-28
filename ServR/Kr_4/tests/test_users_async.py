from __future__ import annotations

import pytest
from faker import Faker


@pytest.mark.asyncio
async def test_create_user_returns_201(async_client) -> None:
    faker = Faker()
    payload = {"username": faker.user_name(), "age": faker.random_int(min=19, max=90)}
    r = await async_client.post("/users", json=payload)
    assert r.status_code == 201
    data = r.json()
    assert "id" in data
    assert data["username"] == payload["username"]
    assert data["age"] == payload["age"]


@pytest.mark.asyncio
async def test_get_existing_user_returns_200(async_client) -> None:
    faker = Faker()
    payload = {"username": faker.user_name(), "age": faker.random_int(min=19, max=90)}
    created = await async_client.post("/users", json=payload)
    user_id = created.json()["id"]

    r = await async_client.get(f"/users/{user_id}")
    assert r.status_code == 200
    assert r.json()["id"] == user_id


@pytest.mark.asyncio
async def test_get_missing_user_returns_404(async_client) -> None:
    r = await async_client.get("/users/99999")
    assert r.status_code == 404


@pytest.mark.asyncio
async def test_delete_existing_user_returns_204(async_client) -> None:
    created = await async_client.post("/users", json={"username": "bob", "age": 25})
    user_id = created.json()["id"]

    r = await async_client.delete(f"/users/{user_id}")
    assert r.status_code == 204


@pytest.mark.asyncio
async def test_delete_same_user_twice_returns_404(async_client) -> None:
    created = await async_client.post("/users", json={"username": "bob", "age": 25})
    user_id = created.json()["id"]

    r1 = await async_client.delete(f"/users/{user_id}")
    assert r1.status_code == 204

    r2 = await async_client.delete(f"/users/{user_id}")
    assert r2.status_code == 404

