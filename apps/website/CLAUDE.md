# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

OfficeOS marketing/product website — a Next.js 16 app using Bun, Tailwind CSS v4, and shadcn/ui (new-york style). Deployed as a standalone Docker container. Site URL: https://www.officeos.co

## Commands

```bash
bun install          # install dependencies
bun run dev          # dev server with Turbopack
bun run build        # production build
bun run lint         # ESLint
```

## Architecture

- **Runtime**: Bun + Next.js 16 (App Router, RSC enabled)
- **Styling**: Tailwind CSS v4 with `@tailwindcss/postcss`, CSS variables for theming
- **UI components**: shadcn/ui (new-york style) in `src/components/ui/`
- **Font**: GeistMono (monospace across the site)
- **Path aliases**: `@/` maps to `src/`

### Key directories

- `src/app/` — App Router pages: landing, pricing, about, changelog, product/*, solutions/*, legal pages
- `src/components/sections/` — Page-level sections (hero, navbar, footer, FAQ, CTA, etc.)
- `src/components/ui/` — shadcn/ui primitives
- `src/lib/site.ts` — Site-wide config (name, URL, description)
- `src/lib/changelog.ts` — Changelog data/parsing
- `public/` — Static assets

### Docker build

Dockerfile uses multi-stage build (deps → build → runtime) with `output: "standalone"` in next.config.ts. The build copies `changelog/` from the monorepo root. Runtime serves via `bun server.js` on port 3000.

## Conventions

- Light-mode only (hardcoded `className="light"` on `<html>`)
- Security headers configured in next.config.ts (HSTS, X-Frame-Options, etc.)
- Use `cn()` from `@/lib/utils` for conditional class merging (clsx + tailwind-merge)
