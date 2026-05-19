from __future__ import annotations

import pytest
from fastapi.testclient import TestClient

from app.main import app, reset_app_state


@pytest.fixture(autouse=True)
def _clean_state() -> None:
    reset_app_state()


@pytest.fixture
def client() -> TestClient:
    return TestClient(app)
