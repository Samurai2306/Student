from __future__ import annotations


class CustomExceptionA(Exception):
    def __init__(self, message: str = "Custom exception A occurred") -> None:
        super().__init__(message)
        self.message = message


class CustomExceptionB(Exception):
    def __init__(self, message: str = "Custom exception B occurred") -> None:
        super().__init__(message)
        self.message = message

