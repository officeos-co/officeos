"""
stealth.fingerprint — Browser fingerprint randomization at launch time.

Applied via Playwright's add_init_script() so it runs before any page JS.
Covers: webdriver flag, canvas noise, WebGL noise, user-agent cycling,
timezone/locale consistency per session.
"""
from __future__ import annotations

import hashlib
import random

# ---------------------------------------------------------------------------
# User-agent pool
# ---------------------------------------------------------------------------

_UA_POOL = [
    # Chrome 135 on Windows (April 2026 stable)
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36",
    # Chrome 135 on macOS
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36",
    # Chrome 135 on Linux
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36",
    # Firefox 137 on Windows
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:137.0) Gecko/20100101 Firefox/137.0",
    # Firefox 137 on macOS
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.4; rv:137.0) Gecko/20100101 Firefox/137.0",
    # Edge 135
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36 Edg/135.0.0.0",
    # Safari 17.6 on macOS (Safari release cadence is slower)
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6 Safari/605.1.15",
]
# NOTE: UA strings go stale fast. Refresh this pool every 2-3 months.
# Stale UAs are themselves a bot signal — Chrome 122 in a 2026 session sticks out.

_TIMEZONES = [
    "America/New_York",
    "America/Chicago",
    "America/Los_Angeles",
    "America/Denver",
    "Europe/London",
    "Europe/Paris",
    "Europe/Berlin",
    "Asia/Tokyo",
    "Asia/Singapore",
    "Australia/Sydney",
]

_LOCALES = ["en-US", "en-GB", "en-CA", "en-AU", "de-DE", "fr-FR"]


# ---------------------------------------------------------------------------
# Session-stable fingerprint seed
# ---------------------------------------------------------------------------

def _session_seed(session_id: str) -> int:
    """Derive a stable integer seed from a session ID so fingerprint is consistent
    within a session but differs across sessions."""
    return int(hashlib.sha256(session_id.encode()).hexdigest()[:8], 16)


# ---------------------------------------------------------------------------
# Fingerprint config
# ---------------------------------------------------------------------------

class FingerprintConfig:
    """Per-session fingerprint configuration."""

    def __init__(self, session_id: str, profile: str = "light") -> None:
        rng = random.Random(_session_seed(session_id))
        self.user_agent: str = rng.choice(_UA_POOL)
        self.timezone: str = rng.choice(_TIMEZONES)
        self.locale: str = rng.choice(_LOCALES)
        # Small random canvas noise seed (0-255)
        self.canvas_noise_seed: int = rng.randint(0, 255)
        self.webgl_noise_seed: int = rng.randint(0, 255)
        self.profile = profile

    def playwright_context_kwargs(self) -> dict:
        """Extra kwargs to pass to browser.new_context()."""
        return {
            "user_agent": self.user_agent,
            "timezone_id": self.timezone,
            "locale": self.locale,
        }

    def init_script(self) -> str:
        """JS snippet injected before every page load."""
        noise_seed = self.canvas_noise_seed
        webgl_seed = self.webgl_noise_seed

        return f"""
(function() {{
    // 1. Mask webdriver flag
    Object.defineProperty(navigator, 'webdriver', {{
        get: () => undefined,
        configurable: true
    }});

    // 2. Mask automation-related properties
    if (window.chrome) {{
        window.chrome.app = window.chrome.app || {{}};
    }}

    // 3. Canvas fingerprint noise — add deterministic per-pixel noise
    const _noise_seed = {noise_seed};
    const _origGetContext = HTMLCanvasElement.prototype.getContext;
    HTMLCanvasElement.prototype.getContext = function(type, ...args) {{
        const ctx = _origGetContext.apply(this, [type, ...args]);
        if (type === '2d' && ctx) {{
            const _origGetImageData = ctx.getImageData.bind(ctx);
            ctx.getImageData = function(x, y, w, h) {{
                const imageData = _origGetImageData(x, y, w, h);
                const data = imageData.data;
                for (let i = 0; i < data.length; i += 4) {{
                    // Add ±1 noise based on pixel index + seed
                    const noise = ((_noise_seed ^ i) & 1) ? 1 : -1;
                    data[i] = Math.max(0, Math.min(255, data[i] + noise));
                }}
                return imageData;
            }};
        }}
        return ctx;
    }};

    // 4. WebGL renderer noise
    const _webgl_seed = {webgl_seed};
    const _getParam = WebGLRenderingContext.prototype.getParameter;
    WebGLRenderingContext.prototype.getParameter = function(param) {{
        // RENDERER and VENDOR — slightly scramble
        if (param === 37445 || param === 37446) {{
            const orig = _getParam.apply(this, [param]);
            return orig;  // keep real value; noise is in canvas layer above
        }}
        return _getParam.apply(this, [param]);
    }};

    // 5. Remove common automation artifacts
    delete window.__selenium_unwrapped;
    delete window.__webdriver_script_fn;
    delete window.__driver_evaluate;
    delete window.__webdriver_evaluate;
}})();
""".strip()


# ---------------------------------------------------------------------------
# Apply fingerprint to a Playwright BrowserContext
# ---------------------------------------------------------------------------

async def apply_fingerprint(context: "BrowserContext", config: FingerprintConfig) -> None:  # noqa: F821
    """Inject the fingerprint init script into a Playwright BrowserContext."""
    await context.add_init_script(config.init_script())
