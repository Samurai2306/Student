"""Однократное создание таблиц SQLite (задание 8.1)."""

from database import init_db

if __name__ == "__main__":
    init_db()
    print("Схема БД создана или уже существует.")
