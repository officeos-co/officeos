"""
integrations.veo3 — Vertex AI Veo 3 video generation client.
research.viral  — YouTube + Reddit niche research → virality scoring → Veo3 prompt synthesis.
"""
from __future__ import annotations

import asyncio
import json
import logging
import os
from pathlib import Path
from typing import Any

import httpx

logger = logging.getLogger(__name__)


# ===========================================================================
# Veo3 Client
# ===========================================================================

_VERTEX_API = "https://{region}-aiplatform.googleapis.com/v1"
_VEO3_MODEL = "veo-003"


class Veo3Client:
    """
    Vertex AI Veo 3 video generation.

    Flow: submit_generation() → poll_operation() → download_video()
    """

    def __init__(
        self,
        project_id: str,
        location: str = "us-central1",
        credentials_path: str = "",
    ) -> None:
        self._project_id = project_id
        self._location = location
        self._credentials_path = credentials_path or os.environ.get("GOOGLE_APPLICATION_CREDENTIALS", "")
        self._base = _VERTEX_API.format(region=location)

    @classmethod
    def from_env(cls) -> "Veo3Client":
        return cls(
            project_id=os.environ["GOOGLE_CLOUD_PROJECT"],
            location=os.environ.get("VERTEX_LOCATION", "us-central1"),
        )

    async def _access_token(self) -> str:
        """Get a GCP access token via gcloud CLI (simplest for VPS setups)."""
        proc = await asyncio.create_subprocess_exec(
            "gcloud", "auth", "print-access-token",
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE,
        )
        stdout, _ = await proc.communicate()
        return stdout.decode().strip()

    async def _headers(self) -> dict[str, str]:
        token = await self._access_token()
        return {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}

    async def submit_generation(
        self,
        prompt: str,
        duration_seconds: int = 8,
        aspect_ratio: str = "16:9",
        negative_prompt: str = "",
    ) -> str:
        """
        Submit a Veo 3 generation job.
        Returns the long-running operation name.
        """
        url = (
            f"{self._base}/projects/{self._project_id}/locations/{self._location}"
            f"/publishers/google/models/{_VEO3_MODEL}:predict"
        )
        body = {
            "instances": [{"prompt": prompt}],
            "parameters": {
                "durationSeconds": duration_seconds,
                "aspectRatio": aspect_ratio,
                "sampleCount": 1,
            },
        }
        if negative_prompt:
            body["parameters"]["negativePrompt"] = negative_prompt

        headers = await self._headers()
        async with httpx.AsyncClient(timeout=60) as client:
            resp = await client.post(url, headers=headers, json=body)
            resp.raise_for_status()
            data = resp.json()

        # For long-running ops, Vertex returns an operation name
        op_name = data.get("name") or data.get("metadata", {}).get("operationName", "")
        if not op_name:
            # Some Veo endpoints return synchronous predictions
            # Try to extract video bytes directly
            return self._save_sync_prediction(data)

        logger.info("veo3.submit: operation=%s", op_name)
        return op_name

    def _save_sync_prediction(self, data: dict) -> str:
        """Handle synchronous prediction responses (fallback)."""
        predictions = data.get("predictions", [])
        if not predictions:
            raise RuntimeError(f"Veo3 returned no predictions: {data}")
        return f"sync:{json.dumps(predictions[0])}"

    async def poll_operation(
        self, operation_name: str, max_wait: int = 300, interval: int = 10
    ) -> dict[str, Any]:
        """
        Poll a long-running operation until done.
        Returns the response dict when complete.
        """
        if operation_name.startswith("sync:"):
            return json.loads(operation_name[5:])

        op_url = f"{self._base}/{operation_name}"
        headers = await self._headers()
        elapsed = 0

        async with httpx.AsyncClient(timeout=30) as client:
            while elapsed < max_wait:
                resp = await client.get(op_url, headers=headers)
                resp.raise_for_status()
                data = resp.json()
                if data.get("done"):
                    if "error" in data:
                        raise RuntimeError(f"Veo3 operation failed: {data['error']}")
                    return data.get("response", data)
                progress = data.get("metadata", {}).get("progressPercent", 0)
                logger.debug("veo3.poll: operation=%s progress=%s%%", operation_name, progress)
                await asyncio.sleep(interval)
                elapsed += interval

        raise TimeoutError(f"Veo3 operation {operation_name} not done after {max_wait}s")

    async def download_video(
        self, operation_result: dict[str, Any], output_path: str
    ) -> str:
        """
        Extract the video from the operation result and save to disk.
        Returns the local file path.
        """
        # Video is typically base64-encoded in the prediction
        predictions = operation_result.get("predictions", [])
        if not predictions:
            # Try alternative response shapes
            video_b64 = (
                operation_result.get("videoBytesBase64Encoded")
                or operation_result.get("videoBytes")
                or ""
            )
        else:
            pred = predictions[0]
            video_b64 = (
                pred.get("videoBytesBase64Encoded")
                or pred.get("videoBytes")
                or pred.get("video", {}).get("bytesBase64Encoded", "")
            )

        if not video_b64:
            # Try GCS URI
            gcs_uri = (
                operation_result.get("gcsUri")
                or (predictions[0].get("gcsUri") if predictions else "")
            )
            if gcs_uri:
                return await self._download_from_gcs(gcs_uri, output_path)
            raise RuntimeError(f"Veo3 result contains no video data: {list(operation_result.keys())}")

        import base64
        video_bytes = base64.b64decode(video_b64)
        out = Path(output_path)
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_bytes(video_bytes)
        logger.info("veo3.download: saved %d bytes to %s", len(video_bytes), output_path)
        return str(out)

    async def _download_from_gcs(self, gcs_uri: str, output_path: str) -> str:
        """Download from a GCS URI using gsutil."""
        out = Path(output_path)
        out.parent.mkdir(parents=True, exist_ok=True)
        proc = await asyncio.create_subprocess_exec(
            "gsutil", "cp", gcs_uri, str(out),
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE,
        )
        _, stderr = await proc.communicate()
        if proc.returncode != 0:
            raise RuntimeError(f"gsutil cp failed: {stderr.decode()}")
        return str(out)

    async def generate(
        self,
        prompt: str,
        output_path: str,
        duration_seconds: int = 8,
        aspect_ratio: str = "16:9",
        negative_prompt: str = "",
    ) -> str:
        """
        End-to-end: submit → poll → download. Returns local file path.
        """
        op = await self.submit_generation(
            prompt=prompt,
            duration_seconds=duration_seconds,
            aspect_ratio=aspect_ratio,
            negative_prompt=negative_prompt,
        )
        result = await self.poll_operation(op)
        return await self.download_video(result, output_path)


# ===========================================================================
# Viral Research Engine
# ===========================================================================

class ViralResearchEngine:
    """
    Research a niche on YouTube + Reddit, score by virality,
    extract trending topics, and synthesize a Veo3-ready prompt.
    """

    def __init__(
        self,
        youtube_client: Any,   # YouTubeClient
        reddit_client: Any,    # RedditClient
        llm_fn: Any = None,    # async (prompt: str) -> str — Claude or similar
    ) -> None:
        self._yt = youtube_client
        self._reddit = reddit_client
        self._llm = llm_fn

    async def research(
        self,
        niche: str,
        subreddits: list[str] = None,
        yt_results: int = 20,
        reddit_results: int = 20,
    ) -> dict[str, Any]:
        """
        Full research pipeline for a niche.

        Returns:
            {
                "niche": ...,
                "trending_topics": [...],
                "top_videos": [...],
                "top_reddit_posts": [...],
                "virality_report": "...",
                "veo3_prompt": "...",
            }
        """
        logger.info("viral.research: niche=%r", niche)

        # Parallel fetch
        yt_task = asyncio.create_task(self._fetch_youtube(niche, yt_results))
        reddit_task = asyncio.create_task(self._fetch_reddit(niche, subreddits or [], reddit_results))
        yt_videos, reddit_posts = await asyncio.gather(yt_task, reddit_task)

        # Score and rank
        scored_videos = self._score_videos(yt_videos)
        trending_topics = self._extract_topics(scored_videos, reddit_posts)

        # Synthesize prompt via LLM
        prompt = await self._synthesize_prompt(niche, trending_topics, scored_videos[:5], reddit_posts[:5])

        return {
            "niche": niche,
            "trending_topics": trending_topics,
            "top_videos": scored_videos[:10],
            "top_reddit_posts": reddit_posts[:10],
            "veo3_prompt": prompt,
        }

    async def _fetch_youtube(self, niche: str, max_results: int) -> list[dict]:
        try:
            return await self._yt.search_videos(niche, max_results=max_results, order="viewCount")
        except Exception as exc:
            logger.warning("viral.youtube_fetch failed: %s", exc)
            return []

    async def _fetch_reddit(self, niche: str, subreddits: list[str], limit: int) -> list[dict]:
        posts = []
        targets = subreddits if subreddits else [niche.replace(" ", ""), "videos", "interestingvideos"]
        for sr in targets[:3]:
            try:
                headers = await self._reddit._headers()
                async with httpx.AsyncClient(timeout=20) as client:
                    resp = await client.get(
                        f"https://oauth.reddit.com/r/{sr}/hot.json",
                        headers=headers,
                        params={"limit": limit // len(targets)},
                    )
                    if resp.status_code == 200:
                        for item in resp.json().get("data", {}).get("children", []):
                            d = item["data"]
                            posts.append({
                                "title": d.get("title", ""),
                                "score": d.get("score", 0),
                                "url": d.get("url", ""),
                                "subreddit": sr,
                            })
            except Exception as exc:
                logger.warning("viral.reddit_fetch %s failed: %s", sr, exc)
        return sorted(posts, key=lambda x: x["score"], reverse=True)

    def _score_videos(self, videos: list[dict]) -> list[dict]:
        """Add a virality_score field and sort descending."""
        for v in videos:
            views = v.get("view_count", 0) or 0
            likes = v.get("like_count", 0) or 0
            # Simple virality formula: log(views) * (like_ratio + 0.1)
            import math
            like_ratio = (likes / max(views, 1))
            v["virality_score"] = math.log10(max(views, 1)) * (like_ratio + 0.05)
        return sorted(videos, key=lambda x: x.get("virality_score", 0), reverse=True)

    def _extract_topics(self, videos: list[dict], reddit_posts: list[dict]) -> list[str]:
        """Extract top trending topic phrases from titles."""
        import re
        from collections import Counter
        all_titles = [v.get("title", "") for v in videos[:15]] + \
                     [p.get("title", "") for p in reddit_posts[:15]]
        words = []
        for title in all_titles:
            tokens = re.findall(r"[A-Za-z]{4,}", title)
            words.extend([t.lower() for t in tokens])
        _STOP = {"this", "that", "with", "from", "they", "have", "will", "what", "when", "were"}
        counts = Counter(w for w in words if w not in _STOP)
        return [word for word, _ in counts.most_common(10)]

    async def _synthesize_prompt(
        self,
        niche: str,
        topics: list[str],
        top_videos: list[dict],
        top_posts: list[dict],
    ) -> str:
        """Use the LLM to synthesize a Veo3-ready video prompt."""
        if self._llm is None:
            # Fallback: template-based prompt
            topic_str = ", ".join(topics[:5])
            return (
                f"A captivating, high-energy video about {niche}. "
                f"Trending topics: {topic_str}. "
                f"Cinematic quality, dynamic camera movement, vibrant colors. "
                f"Optimized for social media engagement and virality."
            )

        video_context = "\n".join(
            f"- {v.get('title','')} (virality: {v.get('virality_score',0):.2f})"
            for v in top_videos
        )
        reddit_context = "\n".join(
            f"- {p.get('title','')} (score: {p.get('score',0)})"
            for p in top_posts
        )

        synthesis_prompt = f"""You are a viral video strategist. Generate a Veo3 video generation prompt.

Niche: {niche}
Trending topics: {', '.join(topics)}
Top performing YouTube videos:
{video_context}
Top Reddit posts:
{reddit_context}

Write a single Veo3 video generation prompt (2-3 sentences) that:
1. Captures the viral angle from the trending topics
2. Specifies visual style, camera movement, and mood
3. Is optimized for {niche} audience engagement

Return ONLY the prompt text, nothing else."""

        try:
            return await self._llm(synthesis_prompt)
        except Exception as exc:
            logger.warning("viral.llm_synthesis failed: %s", exc)
            return f"Engaging {niche} video featuring {', '.join(topics[:3])}. Cinematic, high-energy, social-media optimized."
