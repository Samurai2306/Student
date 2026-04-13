import sqlite3
from contextlib import contextmanager
from pathlib import Path

from config import get_settings


def _connect() -> sqlite3.Connection:
    path = Path(get_settings().database_path)
    conn = sqlite3.connect(path, check_same_thread=False)
    conn.row_factory = sqlite3.Row
    return conn


def init_db() -> None:
    conn = _connect()
    try:
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                password TEXT NOT NULL,
                role TEXT NOT NULL DEFAULT 'guest'
            )
            """
        )
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS todos (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                description TEXT,
                completed INTEGER NOT NULL DEFAULT 0
            )
            """
        )
        conn.commit()
    finally:
        conn.close()


@contextmanager
def get_db_connection():
    conn = _connect()
    try:
        yield conn
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()


def fetch_all_users(conn: sqlite3.Connection):
    return conn.execute("SELECT id, username, password, role FROM users").fetchall()


def find_user_row_by_username(conn: sqlite3.Connection, username: str):
    import secrets

    for row in fetch_all_users(conn):
        if secrets.compare_digest(row["username"], username):
            return row
    return None
