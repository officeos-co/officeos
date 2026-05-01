from __future__ import annotations

import unittest

from app.browser_manager import BrowserManager


class ActionVerificationTests(unittest.TestCase):
    def test_click_verification_uses_page_change_signals(self) -> None:
        before = {
            "url": "https://example.com",
            "title": "Before",
            "active_element": {"tag": "body"},
            "text_excerpt": "before text",
            "dom_outline": {"counts": {"links": 1, "buttons": 1, "inputs": 0, "forms": 0}},
        }
        after = {
            "url": "https://example.com/next",
            "title": "After",
            "active_element": {"tag": "button", "element_id": "op-1"},
            "text_excerpt": "after text",
            "dom_outline": {"counts": {"links": 2, "buttons": 1, "inputs": 0, "forms": 0}},
            "interactables": [],
        }

        verification = BrowserManager._action_verification(
            "click",
            {"element_id": "op-1", "selector": '[data-operator-id="op-1"]'},
            before,
            after,
        )

        self.assertTrue(verification["verified"])
        self.assertIn("url_changed", verification["signals"])
        self.assertIn("target_no_longer_visible", verification["signals"])

    def test_navigate_requires_real_navigation_signal(self) -> None:
        before = {
            "url": "https://example.com",
            "title": "Example",
            "active_element": None,
            "text_excerpt": "same",
            "dom_outline": {"counts": {"links": 1}},
        }
        after = {
            "url": "https://example.com",
            "title": "Example",
            "active_element": None,
            "text_excerpt": "same",
            "dom_outline": {"counts": {"links": 1}},
            "interactables": [],
        }

        verification = BrowserManager._action_verification("navigate", {"url": "https://example.com"}, before, after)

        self.assertFalse(verification["verified"])
        self.assertEqual(verification["signals"], [])
