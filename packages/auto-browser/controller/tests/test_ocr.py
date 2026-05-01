from __future__ import annotations

import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from PIL import Image

from app.ocr import OCRExtractor


class OCRExtractorTests(unittest.IsolatedAsyncioTestCase):
    async def test_extract_from_image_returns_bounded_blocks(self) -> None:
        with tempfile.TemporaryDirectory() as tempdir:
            image_path = Path(tempdir) / "sample.png"
            Image.new("RGB", (200, 80), "white").save(image_path)
            extractor = OCRExtractor(enabled=True, language="eng", max_blocks=2, text_limit=20)

            with patch("app.ocr.pytesseract.image_to_data") as mock_image_to_data:
                mock_image_to_data.return_value = {
                    "text": ["Hello", "", "World", "Ignored"],
                    "conf": ["92", "-1", "88", "12"],
                    "left": [10, 0, 60, 0],
                    "top": [10, 0, 10, 0],
                    "width": [40, 0, 45, 0],
                    "height": [12, 0, 12, 0],
                }
                payload = await extractor.extract_from_image(image_path)

            self.assertTrue(payload["available"])
            self.assertEqual(payload["dimensions"], {"width": 200, "height": 80})
            self.assertEqual(payload["text_excerpt"], "Hello World")
            self.assertEqual(len(payload["blocks"]), 2)
            self.assertEqual(payload["blocks"][1]["text"], "World")

    async def test_disabled_ocr_returns_empty_payload(self) -> None:
        payload = await OCRExtractor(enabled=False, language="eng", max_blocks=5, text_limit=100).extract_from_image(
            "/tmp/ignored.png"
        )

        self.assertFalse(payload["available"])
        self.assertFalse(payload["enabled"])
        self.assertEqual(payload["blocks"], [])
