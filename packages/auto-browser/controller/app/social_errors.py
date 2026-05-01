from __future__ import annotations

from typing import Any


class SocialActionError(Exception):
    def __init__(
        self,
        message: str,
        *,
        code: str = "social_action_failed",
        action: str | None = None,
        retryable: bool | None = None,
        url: str | None = None,
        details: dict[str, Any] | None = None,
    ) -> None:
        super().__init__(message)
        self.message = message
        self.code = code
        self.action = action
        self.retryable = retryable
        self.url = url
        self.details = details or {}

    @property
    def payload(self) -> dict[str, Any]:
        return {
            "ok": False,
            "error": self.message,
            "code": self.code,
            "action": self.action,
            "retryable": self.retryable,
            "url": self.url,
            **self.details,
        }
