---
title: "Alpha Launch — OfficeOS is live"
date: "2026-03-01"
tags: ["Launch", "Alpha"]
version: "0.1"
---

The first working version of OfficeOS is deployed and running on Kubernetes.

## What's working

- **Dashboard** — Next.js operator UI for managing agents, providers, and skills
- **Backend** — C# ASP.NET Core orchestrator handling all state, credentials, and K8s control
- **Agent runtime** — Rust-based zeroclaw binary running inside Kubernetes pods, one pod per agent
- **Skill runtime** — Node.js service executing TypeScript skills with Zod validation
- **Provider system** — connect OpenAI, Anthropic, and other LLM providers with encrypted API keys
- **CI/CD** — push to main, everything builds and deploys automatically via GitHub Actions + Tailscale

## Architecture highlights

- Agents boot with a single env var (`ZEROCLAW_AGENT_ID`) and fetch everything else from the backend
- Credentials never leave the backend — agents call the LLM proxy, not the providers directly
- Skills run in an isolated Node.js runtime, not inside the agent pod
- All deployments use Docker Hub `harkro123/*:latest` images with Kubernetes rollout restarts

## Known limitations

- No session history UI yet
- Skill error reporting is minimal
- Dashboard has no auth (single-tenant assumption)
- No audit logging
