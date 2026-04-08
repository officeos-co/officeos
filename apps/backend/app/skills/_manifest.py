"""Manifest dataclasses for first-party skills.

Lives in its own module so individual skill packages can import it
without triggering the `app.skills.__init__` registry build (which in
turn imports every skill).
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(frozen=True)
class LlmTool:
    name: str
    description: str
    parameters: dict[str, Any]  # JSON schema


@dataclass(frozen=True)
class CredentialField:
    key: str
    label: str
    kind: str  # "password" | "text" | "textarea"
    required: bool = True
    placeholder: str | None = None
    help: str | None = None


@dataclass(frozen=True)
class SkillManifest:
    name: str
    title: str
    description: str
    emoji: str
    credential_fields: list[CredentialField] = field(default_factory=list)
    llm_tools: list[LlmTool] = field(default_factory=list)
