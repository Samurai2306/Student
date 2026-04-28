from __future__ import annotations

import os

from dotenv import load_dotenv


load_dotenv()


def get_database_url() -> str:
    return os.getenv("DATABASE_URL", "sqlite:///./app.db")

