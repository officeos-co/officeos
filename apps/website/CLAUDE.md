# website — Next.js 15 + Bun

Public landing page at officeos.co. No backend connection. Pure marketing — no auth, no API calls, no dashboard functionality.

## Commands

```bash
bun install
bun run dev          # Dev server on :3000
bun run build        # Production build
npx tsc --noEmit     # Type check only
```

## Project structure

```
src/
  app/
    layout.tsx              Root layout — fonts, theme, OpenGraph metadata
    page.tsx                Home — composes sections in order, no logic
    metadata.ts             SEO metadata (title, description, OpenGraph)
    about/page.tsx          About page
    privacy/page.tsx        Privacy policy
    terms/page.tsx          Terms of service
  components/
    sections/               One component per page section, self-contained with inline content
      navbar.tsx
      hero-section.tsx
      company-showcase.tsx
      bento-section.tsx
      feature-section.tsx
      growth-section.tsx
      trust-section.tsx
      faq-section.tsx
      cta-section.tsx
      footer-section.tsx
    ui/                     shadcn/ui + custom animation components
    section-header.tsx      Shared section header wrapper
    cal-modal.tsx           Cal.com booking modal
  hooks/
    use-cal-modal.ts        Cal.com modal open/close state
    use-media-query.ts      Responsive breakpoint detection
  lib/
    site.ts                 Site metadata only: name, url, description — used by metadata.ts
    utils.ts                cn() helper (clsx + tailwind-merge)
```

## Key rules

- **`page.tsx` only composes sections.** The home page imports and renders section components in order. No content, no logic, no state in `page.tsx`.
- **Content lives inline in each section.** Copy, data arrays, and config are defined directly in the component that renders them. No shared content files.
- **`lib/site.ts` is metadata only.** Name, URL, and description for SEO/OpenGraph — nothing else.
- **`"use client"` only when needed.** Sections using hooks, scroll listeners, or browser APIs need it. Static sections are server components.
- **Animations via motion/react.** Use Framer Motion for scroll-triggered animations. Do not add other animation libraries.

## Anti-patterns

- Do not add backend API calls. This site has no backend connection.
- Do not add logic or content to `page.tsx` — it only composes sections.
- Do not create centralized content config files that separate copy from the section rendering it.
- Do not use `lib/site.ts` for page content — it is only for SEO metadata consumed by `metadata.ts`.
- Do not add auth, sessions, or any dashboard functionality — this is a public marketing site.
