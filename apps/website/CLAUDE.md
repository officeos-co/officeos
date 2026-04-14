# website — Next.js 16 + Bun

Public landing page at harrokrog.com.

## Commands

```bash
bun install
bun run dev          # Dev server on :3000
npx tsc --noEmit     # Type check
```

## Project structure

```
src/
  app/
    layout.tsx              Root layout (fonts, theme, metadata)
    page.tsx                Home page — composes all sections
    metadata.ts             SEO metadata (imports from lib/site.ts)
    about/page.tsx          About page
    privacy/page.tsx        Privacy policy
    terms/page.tsx          Terms of service
  components/
    sections/               Page sections — each is self-contained with inline content
      navbar.tsx            Top navigation bar
      hero-section.tsx      Hero with headline + CTAs
      company-showcase.tsx  Logo grid of backers/partners
      bento-section.tsx     Feature grid with animations
      feature-section.tsx   Skill execution features (slideshow)
      growth-section.tsx    Scale/performance section with globe + stats
      trust-section.tsx     Enterprise trust points (4-card grid)
      faq-section.tsx       Accordion FAQ
      cta-section.tsx       Final call-to-action
      footer-section.tsx    Footer with links + flickering grid
    ui/                     shadcn/ui + custom animation components
    section-header.tsx      Reusable section header wrapper
    cal-modal.tsx           Cal.com booking modal
    icons.tsx               Icon components
  hooks/
    use-cal-modal.ts        Cal.com modal state
    use-media-query.ts      Responsive breakpoint hook
    use-mobile.ts           Mobile detection hook
  lib/
    site.ts                 Minimal site metadata (name, url, description) — used by metadata.ts
    utils.ts                cn() helper (clsx + tailwind-merge)
```

## Key rules

- **Content lives inline in each section component.** No centralized config objects — all copy, data arrays, and content are defined directly in the component that renders them.
- **`lib/site.ts` is for metadata only.** Site name, URL, and description for SEO/OpenGraph. Not for page content.
- **Section components are self-contained.** Each section in `components/sections/` owns its own content, layout, and styling.
- **`page.tsx` only composes sections.** The home page imports and renders section components in order — no content or logic.
- **`"use client"` only when needed.** Sections using hooks, state, or browser APIs (motion, scroll, media queries) need it. Static sections can be server components.
- **shadcn/ui for primitives.** Accordion, dialog, tooltip, etc. from `components/ui/`.
- **Tailwind CSS 4 + CSS variables.** Same design token system as dashboard.
- **Animations via motion/react.** Framer Motion for scroll-triggered animations and transitions.

## Anti-patterns

- Do not create centralized config/content files that separate copy from components.
- Do not use `lib/site.ts` for page content — it's only for metadata.
- Do not add logic or content to `page.tsx` — it only composes sections.
