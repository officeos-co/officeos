from __future__ import annotations

import asyncio
from pathlib import Path
from typing import Any

import pytesseract
from PIL import Image
from pytesseract import Output, TesseractNotFoundError


class OCRExtractor:
    def __init__(self, *, enabled: bool, language: str, max_blocks: int, text_limit: int):
        self.enabled = enabled
        self.language = language
        self.max_blocks = max(1, max_blocks)
        self.text_limit = max(200, text_limit)

    async def extract_from_image(self, image_path: str | Path) -> dict[str, Any]:
        if not self.enabled:
            return {
                "available": False,
                "enabled": False,
                "engine": "tesseract",
                "language": self.language,
                "text_excerpt": "",
                "blocks": [],
            }
        return await asyncio.to_thread(self._extract_sync, Path(image_path))

    def _extract_sync(self, image_path: Path) -> dict[str, Any]:
        try:
            with Image.open(image_path) as image:
                width, height = image.size
                data = pytesseract.image_to_data(
                    image,
                    lang=self.language,
                    output_type=Output.DICT,
                )
        except TesseractNotFoundError:
            return self._error_payload(image_path, "tesseract_not_found")
        except Exception:  # pragma: no cover - defensive
            return self._error_payload(image_path, "ocr_extraction_failed")

        blocks: list[dict[str, Any]] = []
        parts: list[str] = []
        char_count = 0
        for idx, raw_text in enumerate(data.get("text", [])):
            text = str(raw_text or "").strip()
            if not text:
                continue
            try:
                confidence = float(data.get("conf", [])[idx])
            except Exception:
                confidence = -1.0
            if confidence < 30:
                continue
            block = {
                "text": text,
                "confidence": round(confidence, 2),
                "bbox": {
                    "x": int(data.get("left", [0])[idx]),
                    "y": int(data.get("top", [0])[idx]),
                    "width": int(data.get("width", [0])[idx]),
                    "height": int(data.get("height", [0])[idx]),
                },
            }
            blocks.append(block)
            if char_count < self.text_limit:
                remaining = self.text_limit - char_count
                chunk = text[:remaining]
                if chunk:
                    parts.append(chunk)
                    char_count += len(chunk) + 1
            if len(blocks) >= self.max_blocks:
                break

        return {
            "available": True,
            "enabled": True,
            "engine": "tesseract",
            "language": self.language,
            "image_path": str(image_path),
            "dimensions": {"width": width, "height": height},
            "text_excerpt": " ".join(parts).strip(),
            "blocks": blocks,
        }

    def _error_payload(self, image_path: Path, error: str) -> dict[str, Any]:
        return {
            "available": False,
            "enabled": self.enabled,
            "engine": "tesseract",
            "language": self.language,
            "image_path": str(image_path),
            "text_excerpt": "",
            "blocks": [],
            "error": error,
        }
