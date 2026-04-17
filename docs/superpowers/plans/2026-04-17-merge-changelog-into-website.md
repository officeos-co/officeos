# Merge Changelog Into Website Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the standalone `apps/changelog/` app and serve changelog content at `/changelog` within the website (`apps/website/`), matching the website's existing design system.

**Architecture:** Add `gray-matter` + `react-markdown` + `remark-gfm` to the website to parse the existing `changelog/*.md` files at build time. Create a `/changelog` route that uses the website's Navbar/Footer and design tokens. Remove the standalone app, its CI/CD workflow, K8s manifest, and CLAUDE.md references.

**Tech Stack:** Next.js 16, gray-matter, react-markdown, remark-gfm, Tailwind CSS v4, existing website Navbar/Footer components.

---

### Task 1: Add markdown dependencies to the website

**Files:**
- Modify: `apps/website/package.json`

- [ ] **Step 1: Install dependencies**

```bash
cd apps/website && bun add gray-matter react-markdown remark-gfm @tailwindcss/typography
```

- [ ] **Step 2: Verify installation**

```bash
cd apps/website && bun run build 2>&1 | head -5
```

Expected: Build starts without dependency errors (may fail for other reasons, that's fine — we just need the packages resolved).

- [ ] **Step 3: Commit**

```bash
git add apps/website/package.json apps/website/bun.lock
git commit -m "feat(website): add markdown rendering dependencies for changelog"
```

---

### Task 2: Create the changelog data loader

**Files:**
- Create: `apps/website/src/lib/changelog.ts`

This module reads all `.md` files from the repo-root `changelog/` directory, parses frontmatter with `gray-matter`, and returns sorted entries.

- [ ] **Step 1: Create the loader**

```ts
// apps/website/src/lib/changelog.ts
import fs from "fs";
import path from "path";
import matter from "gray-matter";

export interface ChangelogEntry {
  title: string;
  date: string;
  version?: string;
  tags?: string[];
  content: string; // raw markdown body
}

export function getChangelogEntries(): ChangelogEntry[] {
  const changelogDir = path.join(process.cwd(), "../../changelog");
  
  if (!fs.existsSync(changelogDir)) {
    return [];
  }

  const files = fs
    .readdirSync(changelogDir)
    .filter((f) => f.endsWith(".md"))
    .sort()
    .reverse(); // newest filename first

  return files.map((filename) => {
    const raw = fs.readFileSync(path.join(changelogDir, filename), "utf-8");
    const { data, content } = matter(raw);
    return {
      title: data.title,
      date: data.date,
      version: data.version,
      tags: data.tags,
      content,
    };
  });
}
```

- [ ] **Step 2: Verify it compiles**

```bash
cd apps/website && npx tsc --noEmit 2>&1 | head -20
```

Expected: No errors related to `changelog.ts` (there may be pre-existing errors elsewhere).

- [ ] **Step 3: Commit**

```bash
git add apps/website/src/lib/changelog.ts
git commit -m "feat(website): add changelog markdown loader"
```

---

### Task 3: Create the `/changelog` page

**Files:**
- Create: `apps/website/src/app/changelog/page.tsx`

This is a **server component** (no "use client") that reads changelog entries at build time and renders them with the website's Navbar + Footer. The design follows the existing page pattern from `about/page.tsx` — `max-w-7xl` outer container with vertical border lines, Navbar, main content, Footer.

- [ ] **Step 1: Create the page**

```tsx
// apps/website/src/app/changelog/page.tsx
import { Navbar } from "@/components/sections/navbar";
import { FooterSection } from "@/components/sections/footer-section";
import { getChangelogEntries } from "@/lib/changelog";
import { ChangelogTimeline } from "@/components/changelog-timeline";

export const metadata = {
  title: "Changelog — OfficeOS",
  description: "Latest updates and releases from OfficeOS.",
};

export default function ChangelogPage() {
  const entries = getChangelogEntries();

  return (
    <div className="relative mx-auto max-w-7xl border-x">
      <div className="absolute top-0 left-6 z-10 block h-full w-px border-border border-l" />
      <div className="absolute top-0 right-6 z-10 block h-full w-px border-border border-r" />
      <Navbar />

      <main className="flex min-h-screen w-full flex-col items-center">
        <div className="mx-auto max-w-4xl w-full px-6 pt-20 pb-28 md:pt-28">
          <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl lg:text-6xl">
            Changelog
          </h1>
          <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
            New updates and improvements to OfficeOS.
          </p>

          <div className="mt-20">
            <ChangelogTimeline entries={entries} />
          </div>
        </div>
      </main>

      <FooterSection />
    </div>
  );
}
```

- [ ] **Step 2: Commit (page won't build yet — ChangelogTimeline created in next task)**

```bash
git add apps/website/src/app/changelog/page.tsx
git commit -m "feat(website): add /changelog page shell"
```

---

### Task 4: Create the ChangelogTimeline component

**Files:**
- Create: `apps/website/src/components/changelog-timeline.tsx`

Client component that renders the timeline and markdown content. Reuses the timeline layout from the old changelog app but styled with the website's design tokens — no dark mode, matching typography.

- [ ] **Step 1: Create the component**

```tsx
// apps/website/src/components/changelog-timeline.tsx
"use client";

import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { ChangelogEntry } from "@/lib/changelog";

function formatDate(date: Date): string {
  return date.toLocaleDateString("en-US", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });
}

export function ChangelogTimeline({ entries }: { entries: ChangelogEntry[] }) {
  return (
    <div className="relative">
      {entries.map((entry, i) => (
        <div key={i} className="relative">
          <div className="flex flex-col md:flex-row gap-y-6">
            {/* Left — date + version */}
            <div className="md:w-48 flex-shrink-0">
              <div className="md:sticky md:top-8 pb-10">
                <time className="text-sm font-medium text-muted-foreground block mb-3">
                  {formatDate(new Date(entry.date))}
                </time>
                {entry.version && (
                  <div className="inline-flex items-center justify-center w-10 h-10 text-primary border border-border rounded-lg text-sm font-bold">
                    {entry.version}
                  </div>
                )}
              </div>
            </div>

            {/* Right — content */}
            <div className="flex-1 md:pl-8 relative pb-10">
              {/* Timeline line + dot */}
              <div className="hidden md:block absolute top-2 left-0 w-px h-full bg-border">
                <div className="absolute -translate-x-1/2 size-3 bg-primary rounded-full" />
              </div>

              <div className="space-y-6">
                <div className="flex flex-col gap-2">
                  <h2 className="text-2xl font-semibold tracking-tight text-balance">
                    {entry.title}
                  </h2>
                  {entry.tags && entry.tags.length > 0 && (
                    <div className="flex flex-wrap gap-2">
                      {entry.tags.map((tag) => (
                        <span
                          key={tag}
                          className="h-6 w-fit px-2 text-xs font-medium bg-muted text-muted-foreground rounded-full border flex items-center justify-center"
                        >
                          {tag}
                        </span>
                      ))}
                    </div>
                  )}
                </div>

                <div className="prose max-w-none prose-headings:font-semibold prose-headings:tracking-tight prose-headings:text-balance prose-p:tracking-tight prose-p:text-muted-foreground prose-p:leading-relaxed prose-a:no-underline prose-li:text-muted-foreground">
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>
                    {entry.content}
                  </ReactMarkdown>
                </div>
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
```

- [ ] **Step 2: Verify the build**

```bash
cd apps/website && bun run build 2>&1 | tail -20
```

Expected: Build succeeds. The `/changelog` page renders.

- [ ] **Step 3: Verify locally**

```bash
cd apps/website && bun run dev &
sleep 3
curl -s http://localhost:3000/changelog | head -50
kill %1
```

Expected: HTML output containing "Changelog" heading and the entry content.

- [ ] **Step 4: Commit**

```bash
git add apps/website/src/components/changelog-timeline.tsx
git commit -m "feat(website): add ChangelogTimeline component with markdown rendering"
```

---

### Task 5: Update the Dockerfile to include changelog content

**Files:**
- Modify: `apps/website/Dockerfile`

The website Dockerfile needs to copy the `changelog/` directory from the repo root so `getChangelogEntries()` can read the files at build time. The standalone output also needs the files available if using ISR/SSR, but since this is a static read at build time via `output: "standalone"`, the data is baked into the page at build.

- [ ] **Step 1: Check current Dockerfile**

Read `apps/website/Dockerfile` to find the build stage where files are copied.

- [ ] **Step 2: Add COPY for changelog directory**

In the build stage (after the workspace files are copied, before `bun run build`), add:

```dockerfile
COPY changelog/ ../../changelog/
```

The exact line placement depends on the current Dockerfile structure — it must appear before the `RUN bun run build` step, and the path must be `../../changelog/` relative to the app's working directory (or adjusted to match the WORKDIR).

**Important:** The `getChangelogEntries()` function uses `path.join(process.cwd(), "../../changelog")`. Verify the WORKDIR in the Dockerfile and adjust the COPY destination so the relative path resolves correctly. If WORKDIR is `/app`, copy to `/changelog/` and update `changelog.ts` to use `path.join(process.cwd(), "../changelog")` — or just use an absolute path `/changelog`.

- [ ] **Step 3: Update changelog.ts path if needed**

If the Docker WORKDIR doesn't match the local dev relative path, make the path configurable:

```ts
const changelogDir = process.env.CHANGELOG_DIR 
  ?? path.join(process.cwd(), "../../changelog");
```

- [ ] **Step 4: Test Docker build**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs && docker build -f apps/website/Dockerfile -t test-website . 2>&1 | tail -20
```

Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add apps/website/Dockerfile apps/website/src/lib/changelog.ts
git commit -m "feat(website): include changelog content in Docker build"
```

---

### Task 6: Update footer link from external to internal

**Files:**
- Modify: `apps/website/src/components/sections/footer-section.tsx` (line ~44)

- [ ] **Step 1: Change the Changelog URL**

In `footer-section.tsx`, find the footerLinks array entry:

```ts
{ id: 13, title: "Changelog", url: "https://changelog.officeos.co" },
```

Change to:

```ts
{ id: 13, title: "Changelog", url: "/changelog" },
```

- [ ] **Step 2: Check if navbar also links to changelog**

Search `navbar.tsx` for "changelog". If found, update that link too.

- [ ] **Step 3: Commit**

```bash
git add apps/website/src/components/sections/footer-section.tsx
git commit -m "fix(website): update changelog link from external to internal /changelog"
```

---

### Task 7: Update CI/CD — add changelog trigger to website workflow

**Files:**
- Modify: `.github/workflows/deploy-website-prod.yml`

The website workflow needs to rebuild when `changelog/*.md` files change.

- [ ] **Step 1: Read current workflow**

Read `.github/workflows/deploy-website-prod.yml` to see the trigger paths.

- [ ] **Step 2: Add changelog path trigger**

In the `on.push.paths` array, add:

```yaml
- 'changelog/**'
```

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/deploy-website-prod.yml
git commit -m "ci: trigger website deploy on changelog content changes"
```

---

### Task 8: Remove the standalone changelog app

**Files:**
- Delete: `apps/changelog/` (entire directory)
- Delete: `.github/workflows/deploy-changelog-prod.yml`
- Delete: `k8s/changelog.yaml`

- [ ] **Step 1: Remove the changelog app directory**

```bash
rm -rf apps/changelog/
```

- [ ] **Step 2: Remove the CI workflow**

```bash
rm .github/workflows/deploy-changelog-prod.yml
```

- [ ] **Step 3: Remove the K8s manifest**

```bash
rm k8s/changelog.yaml
```

- [ ] **Step 4: Verify no broken references**

```bash
grep -r "changelog.officeos.co" apps/website/src/ --include="*.tsx" --include="*.ts"
grep -r "apps/changelog" .github/ CLAUDE.md
```

Expected: No references to `changelog.officeos.co` in website src. CLAUDE.md references will be cleaned in next task.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: remove standalone changelog app, CI workflow, and K8s manifest"
```

---

### Task 9: Update CLAUDE.md documentation

**Files:**
- Modify: `CLAUDE.md` (root)

- [ ] **Step 1: Update the package table**

In the root `CLAUDE.md`, remove the `apps/changelog/` row from the package table:

```
| `apps/changelog/` | Public changelog — reads `.md` files, no backend, pure static | no CLAUDE.md needed |
```

And add a note about changelog in the website row or add a line about `changelog/` content:

```
| `changelog/` | Changelog `.md` content files — rendered by `apps/website/` at `/changelog` | no CLAUDE.md needed |
```

- [ ] **Step 2: Update the system mental model**

Remove:

```
apps/changelog/  Next.js — public changelog. Reads .md files. No backend. No auth.
```

- [ ] **Step 3: Update the CI/CD table**

Remove the `deploy-changelog-prod.yml` row. Update the `deploy-website-prod.yml` row to note it also triggers on `changelog/**`.

- [ ] **Step 4: Update the conventions section**

Remove `changelog.officeos.co` from the prod hostnames line.

- [ ] **Step 5: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: update CLAUDE.md to reflect changelog merged into website"
```

---

### Task 10: Final verification

- [ ] **Step 1: Build the website**

```bash
cd apps/website && bun run build
```

Expected: Build succeeds with `/changelog` in the output routes.

- [ ] **Step 2: Type check**

```bash
cd apps/website && npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 3: Visual check**

```bash
cd apps/website && bun run dev
```

Open `http://localhost:3000/changelog` in a browser. Verify:
- Navbar renders at top (same as other website pages)
- "Changelog" heading centered with subtitle
- Timeline entries with date, version badge, tags, markdown content
- Footer renders at bottom
- Typography matches the rest of the website (muted foreground body text, semibold headings)
- No dark mode toggle (website is light-only)

- [ ] **Step 4: Check other pages still work**

Navigate to `/`, `/about`, `/pricing` — confirm nothing broke.
