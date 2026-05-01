"""
startup.extensions — Initialize all 1.0 subsystems at app startup.

Call register_extensions(app) from main.py after app creation.
All clients are initialized from environment variables.
Missing credentials = subsystem disabled with a warning (never crash on startup).
"""
from __future__ import annotations

import logging
import os
from pathlib import Path

logger = logging.getLogger(__name__)


def register_extensions(app) -> None:
    """
    Wire all auto-browser 1.0 subsystems into app.state.

    Subsystems initialized:
        mesh_identity       — NodeIdentity
        peer_registry       — PeerRegistryFile
        delegation_manager  — DelegationManager
        network_inspectors  — dict[session_id, NetworkInspector]
        cdp_sessions        — dict[session_id, CDPPassthrough]
        workflow_engine     — WorkflowEngine (with all action handlers)
        youtube_client      — YouTubeClient (if env set)
        instagram_client    — InstagramClient (if env set)
        reddit_client       — RedditClient (if env set)
        x_client            — XClient (if env set)
        veo3_client         — Veo3Client (if env set)
        viral_engine        — ViralResearchEngine (if YT+Reddit available)
    """
    _init_mesh(app)
    _init_network_stores(app)
    _init_workflow_engine(app)
    _init_social_clients(app)
    _init_curator(app)
    _register_workflow_actions(app)
    _register_session_hooks(app)
    logger.info("startup.extensions: all 1.0 subsystems registered")


# ---------------------------------------------------------------------------
# Skills Curator adapter
# ---------------------------------------------------------------------------

def _init_curator(app) -> None:
    """Initialize the Skills Curator LLM adapter. None when no API key is set."""
    try:
        from app.curator_llm import build_curator_adapter
        adapter = build_curator_adapter()
        app.state.curator_adapter = adapter
        if adapter is not None and adapter.ready:
            logger.info("startup.curator: %s adapter ready (model=%s)", adapter.provider, adapter.model)
        else:
            logger.info("startup.curator: no API key — degraded mode (raw-skill passthrough only)")
    except Exception as exc:
        logger.warning("startup.curator: adapter init failed — %s", exc)
        app.state.curator_adapter = None


# ---------------------------------------------------------------------------
# Mesh
# ---------------------------------------------------------------------------

def _init_mesh(app) -> None:
    mesh_enabled = os.environ.get("MESH_ENABLED", "false").lower() == "true"
    if not mesh_enabled:
        app.state.mesh_identity = None
        app.state.peer_registry = None
        app.state.delegation_manager = None
        logger.info("startup.mesh: disabled (set MESH_ENABLED=true to enable)")
        return

    try:
        from app.mesh.delegation import DelegationManager
        from app.mesh.identity import NodeIdentity
        from app.mesh.peers import PeerRegistryFile

        identity_dir = Path(os.environ.get("MESH_IDENTITY_DIR", "/data/mesh/identity"))
        peers_path = Path(os.environ.get("MESH_PEERS_PATH", "/data/mesh/peers.json"))
        timestamp_window = float(os.environ.get("MESH_TIMESTAMP_WINDOW", "30"))

        identity = NodeIdentity(identity_dir)
        peers = PeerRegistryFile(peers_path)
        mgr = DelegationManager(
            identity=identity,
            peers=peers,
            timestamp_window=timestamp_window,
            tool_gateway=_build_mesh_tool_gateway(app),
        )

        app.state.mesh_identity = identity
        app.state.peer_registry = peers
        app.state.delegation_manager = mgr
        logger.info("startup.mesh: initialized node_id=%s", identity.node_id[:16])
    except Exception as exc:
        logger.error("startup.mesh: initialization failed — %s", exc)
        app.state.mesh_identity = None
        app.state.peer_registry = None
        app.state.delegation_manager = None


# ---------------------------------------------------------------------------
# Network / CDP stores (populated per-session by session manager)
# ---------------------------------------------------------------------------

def _init_network_stores(app) -> None:
    app.state.network_inspectors = {}   # session_id → NetworkInspector
    app.state.cdp_sessions = {}         # session_id → CDPPassthrough
    logger.debug("startup.extensions: network/CDP stores initialized")


def _build_mesh_tool_gateway(app):
    gateway = getattr(app.state, "tool_gateway", None)
    if gateway is None:
        return None

    async def _call(tool_name: str, arguments: dict, session_id: str) -> dict:
        from app.models import McpToolCallRequest

        payload_args = dict(arguments or {})
        if session_id and "session_id" not in payload_args:
            payload_args["session_id"] = session_id
        response = await gateway.call_tool(McpToolCallRequest(name=tool_name, arguments=payload_args))

        structured = response.structuredContent
        if isinstance(structured, dict):
            result = dict(structured)
        elif structured is not None:
            result = {"result": structured}
        else:
            text = "".join(item.text or "" for item in response.content)
            result = {"text": text} if text else {}

        if response.isError and result.get("status") != "approval_required":
            result["_mesh_error"] = True
        return result

    return _call


# ---------------------------------------------------------------------------
# Workflow engine
# ---------------------------------------------------------------------------

def _init_workflow_engine(app) -> None:
    from app.workflow.engine import WorkflowEngine
    wf_root = Path(os.environ.get("WORKFLOWS_ROOT", "/data/workflows"))
    engine = WorkflowEngine(workflows_root=wf_root)
    app.state.workflow_engine = engine
    logger.info("startup.workflow: engine initialized root=%s", wf_root)


# ---------------------------------------------------------------------------
# Social clients
# ---------------------------------------------------------------------------

def _init_social_clients(app) -> None:
    app.state.youtube_client = None
    app.state.instagram_client = None
    app.state.reddit_client = None
    app.state.x_client = None
    app.state.veo3_client = None
    app.state.viral_engine = None

    # YouTube
    if os.environ.get("YOUTUBE_CLIENT_ID"):
        try:
            from app.social.youtube import YouTubeClient
            app.state.youtube_client = YouTubeClient.from_env()
            logger.info("startup.social: YouTube client initialized")
        except Exception as exc:
            logger.warning("startup.social: YouTube init failed — %s", exc)

    # Instagram
    if os.environ.get("INSTAGRAM_ACCESS_TOKEN"):
        try:
            from app.social.clients import InstagramClient
            app.state.instagram_client = InstagramClient.from_env()
            logger.info("startup.social: Instagram client initialized")
        except Exception as exc:
            logger.warning("startup.social: Instagram init failed — %s", exc)

    # Reddit
    if os.environ.get("REDDIT_CLIENT_ID"):
        try:
            from app.social.clients import RedditClient
            app.state.reddit_client = RedditClient.from_env()
            logger.info("startup.social: Reddit client initialized")
        except Exception as exc:
            logger.warning("startup.social: Reddit init failed — %s", exc)

    # X / Twitter
    if os.environ.get("X_API_KEY"):
        try:
            from app.social.clients import XClient
            app.state.x_client = XClient.from_env()
            logger.info("startup.social: X client initialized")
        except Exception as exc:
            logger.warning("startup.social: X init failed — %s", exc)

    # Veo3
    if os.environ.get("GOOGLE_CLOUD_PROJECT"):
        try:
            from app.integrations.veo3_and_research import Veo3Client
            app.state.veo3_client = Veo3Client.from_env()
            logger.info("startup.social: Veo3 client initialized")
        except Exception as exc:
            logger.warning("startup.social: Veo3 init failed — %s", exc)

    # Viral research engine (needs YT + Reddit)
    if app.state.youtube_client and app.state.reddit_client:
        try:
            from app.integrations.veo3_and_research import ViralResearchEngine
            app.state.viral_engine = ViralResearchEngine(
                youtube_client=app.state.youtube_client,
                reddit_client=app.state.reddit_client,
            )
            logger.info("startup.social: Viral research engine initialized")
        except Exception as exc:
            logger.warning("startup.social: ViralResearchEngine init failed — %s", exc)


# ---------------------------------------------------------------------------
# Workflow action handlers
# ---------------------------------------------------------------------------

def _register_workflow_actions(app) -> None:
    engine = app.state.workflow_engine
    if engine is None:
        return

    # social.research.viral
    async def _research(action, params, ctx):
        engine_ref = app.state.viral_engine
        if engine_ref is None:
            return {"error": "viral_engine not initialized"}
        return await engine_ref.research(
            niche=params.get("niche", ""),
            subreddits=params.get("subreddits", []),
            yt_results=params.get("yt_results", 20),
        )
    engine.register_action("social.research.viral", _research)

    # social.veo3.generate
    async def _veo3_gen(action, params, ctx):
        import uuid
        veo3 = app.state.veo3_client
        if veo3 is None:
            return {"error": "veo3_client not initialized"}
        output = params.get("output_filename") or f"/data/social/generated/{uuid.uuid4().hex}.mp4"
        path = await veo3.generate(
            prompt=params["prompt"],
            output_path=output,
            duration_seconds=params.get("duration_seconds", 8),
            aspect_ratio=params.get("aspect_ratio", "16:9"),
        )
        return {"path": path, "prompt": params["prompt"]}
    engine.register_action("social.veo3.generate", _veo3_gen)

    # social.youtube.upload
    async def _yt_upload(action, params, ctx):
        yt = app.state.youtube_client
        if yt is None:
            return {"error": "youtube_client not initialized"}
        if params.get("make_short"):
            result = await yt.create_short(
                file_path=params["file_path"],
                title=params.get("title", ""),
                description=params.get("description", ""),
                tags=params.get("tags", []),
            )
        else:
            result = await yt.upload_video(
                file_path=params["file_path"],
                title=params.get("title", ""),
                description=params.get("description", ""),
                tags=params.get("tags", []),
                privacy=params.get("privacy", "public"),
            )
        return {"video_id": result.get("id"), "result": result}
    engine.register_action("social.youtube.upload", _yt_upload)

    # social.crosspost
    async def _crosspost(action, params, ctx):
        results = {}
        video_url = params.get("video_url", "")
        title = params.get("title", "")
        description = params.get("description", "")
        platforms = params.get("platforms", [])

        if "reddit" in platforms and app.state.reddit_client:
            for sr in params.get("subreddits", ["videos"]):
                try:
                    r = await app.state.reddit_client.submit_link(sr, title, video_url)
                    results[f"reddit/{sr}"] = r
                except Exception:
                    results[f"reddit/{sr}"] = {"error": "crosspost_failed"}

        if "x" in platforms and app.state.x_client:
            try:
                r = await app.state.x_client.post_tweet(f"{title}\n\n{video_url}")
                results["x"] = r
            except Exception:
                results["x"] = {"error": "crosspost_failed"}

        if "instagram" in platforms and app.state.instagram_client:
            try:
                r = await app.state.instagram_client.post_reel(video_url, description)
                results["instagram"] = r
            except Exception:
                results["instagram"] = {"error": "crosspost_failed"}

        return {"results": results}
    engine.register_action("social.crosspost", _crosspost)

    # social.auth.verify (warm-up checker stub — real impl uses browser session)
    async def _auth_verify(action, params, ctx):
        platform = params.get("platform", "")
        profile = params.get("auth_profile", f"{platform}-default")
        # Real impl would open a session with the auth profile and check login state
        return {"platform": platform, "profile": profile, "status": "check_requires_browser_session"}
    engine.register_action("social.auth.verify", _auth_verify)

    logger.info("startup.extensions: %d workflow actions registered", 5)


def _register_session_hooks(app) -> None:
    manager = getattr(app.state, "browser_manager", None)
    if manager is None or not hasattr(manager, "register_extension_hooks"):
        logger.debug("startup.extensions: browser manager hook registration unavailable")
        return

    async def _created(session_id: str, page) -> None:
        await on_session_created(app, session_id, page)

    async def _closed(session_id: str) -> None:
        await on_session_closed(app, session_id)

    manager.register_extension_hooks(session_created=_created, session_closed=_closed)


# ---------------------------------------------------------------------------
# Session lifecycle hooks (called by browser manager)
# ---------------------------------------------------------------------------

async def on_session_created(app, session_id: str, page) -> None:
    """
    Called when a new browser session is created.
    Attaches NetworkInspector and CDPPassthrough to the session.
    """
    manager = getattr(app.state, "browser_manager", None)
    session = manager.sessions.get(session_id) if manager is not None else None
    if session is not None and getattr(session, "network_inspector", None) is not None:
        app.state.network_inspectors[session_id] = session.network_inspector

    try:
        from app.cdp.passthrough import CDPPassthrough

        app.state.cdp_sessions[session_id] = await CDPPassthrough.from_page(page)
    except Exception as exc:
        logger.warning("on_session_created: cdp session failed — %s", exc)

    try:
        stealth_profile = os.environ.get("STEALTH_PROFILE", "off")
        if stealth_profile != "off":
            from app.stealth.fingerprint import FingerprintConfig, apply_fingerprint
            config = FingerprintConfig(session_id, stealth_profile)
            await apply_fingerprint(page.context, config)
            logger.debug("on_session_created: stealth profile=%s applied", stealth_profile)
    except Exception as exc:
        logger.warning("on_session_created: stealth fingerprint failed — %s", exc)


async def on_session_closed(app, session_id: str) -> None:
    """Called when a browser session is closed. Cleans up per-session resources."""
    app.state.network_inspectors.pop(session_id, None)
    app.state.cdp_sessions.pop(session_id, None)

    # Post-session Curator review.
    # Fire-and-forget — never blocks session close, never raises.
    try:
        curator = getattr(app.state, "curator_adapter", None)
        if curator is not None and getattr(curator, "ready", False):
            import asyncio as _asyncio
            _asyncio.create_task(_curator_review_session(app, session_id, curator))
    except Exception as exc:
        logger.debug("on_session_closed: curator review skipped — %s", exc)


async def _curator_review_session(app, session_id: str, curator) -> None:
    """
    Curator reviews the session's audit trail and optionally drafts a skill
    into /data/skills-staging/<session_id>/. Errors are swallowed; this hook
    must never disturb the foreground close path.
    """
    try:
        from pathlib import Path
        staging = Path(os.environ.get("SKILLS_STAGING_ROOT", "/data/skills-staging")) / session_id
        staging.mkdir(parents=True, exist_ok=True)
        # Build a lightweight transcript stub — callers that wire a richer
        # audit hook can override this path.
        transcript = f"Session {session_id}: (audit-trail transcript would go here)"
        synthesis_prompt = (
            "You are a Skills Curator. Given a browser-agent session transcript,"
            " write a short reusable `interaction-skill` markdown snippet (title,"
            " when-to-use, steps) if-and-only-if the session contains a repeatable"
            " pattern worth saving. Otherwise, reply exactly: NO_SKILL."
        )
        reply = await curator.complete(prompt=transcript, system=synthesis_prompt)
        if reply and reply.strip() != "NO_SKILL":
            (staging / "draft.md").write_text(reply)
            logger.info("curator: drafted staging skill for session %s", session_id)
    except Exception as exc:  # pragma: no cover — defensive, fire-and-forget
        logger.debug("curator review failed for session %s: %s", session_id, exc)
