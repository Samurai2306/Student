from __future__ import annotations

import pytest
from httpx import ASGITransport, AsyncClient

from app.main import app, reset_users_state


@pytest.fixture(autouse=True)
def _clean_users_state() -> None:
    reset_users_state()


@pytest.fixture
async def async_client() -> AsyncClient:
    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as client:
        yield client

