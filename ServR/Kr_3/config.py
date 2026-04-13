from functools import lru_cache
from typing import Literal

from pydantic import Field, ValidationError
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    mode: Literal["DEV", "PROD"] = Field(default="DEV", validation_alias="MODE")
    docs_user: str = Field(default="docsadmin", validation_alias="DOCS_USER")
    docs_password: str = Field(default="docssecret", validation_alias="DOCS_PASSWORD")
    jwt_secret: str = Field(default="change-me", validation_alias="JWT_SECRET")
    jwt_algorithm: str = Field(default="HS256", validation_alias="JWT_ALGORITHM")
    jwt_expire_minutes: int = Field(default=60, validation_alias="JWT_EXPIRE_MINUTES")
    database_path: str = Field(default="app.db", validation_alias="DATABASE_PATH")


@lru_cache
def get_settings() -> Settings:
    try:
        return Settings()
    except ValidationError as exc:
        raise RuntimeError(
            "Неверная конфигурация окружения: MODE допускает только значения DEV или PROD; "
            "остальные переменные см. в .env.example.\n"
            f"Подробности валидации: {exc}"
        ) from exc
