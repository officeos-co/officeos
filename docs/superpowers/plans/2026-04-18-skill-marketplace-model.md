# Skill Marketplace Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a VS Code extension-style metadata model for skills with `skill.json` as the single source of truth, decoupling marketplace metadata from the skill runtime.

**Architecture:** Each skill gets a `skill.json` manifest file containing all marketplace metadata (name, title, logo, categories, keywords, license, author, etc.). A new lightweight CI workflow reads these JSON files directly — no esbuild needed — and seeds them to the backend. The backend stores them in `ManifestJson` and exposes new GraphQL fields. The dashboard renders categories, README, changelog, license, and contributors.

**Tech Stack:** TypeScript (skill-sdk, build-manifests script), C# ASP.NET (backend DTOs, GraphQL resolvers), Next.js React (dashboard), GitHub Actions (CI workflows), Bash (sync script)

---

## Phase 1: SDK + skill.json Schema

### Task 1: Update SkillDefinition to accept skill.json spread

**Files:**
- Modify: `packages/skill-sdk/src/types.ts`

The current `SkillDefinition` requires all fields inline. We need it to also accept marketplace fields that come from `skill.json`, while keeping `doc` and `actions` as code-only fields.

- [ ] **Step 1: Add marketplace fields to SkillDefinition**

In `packages/skill-sdk/src/types.ts`, add optional marketplace fields that will come from `skill.json`. The key insight: `doc` and `actions` stay required (they come from code), but new fields like `categories`, `keywords`, `license`, `author`, `repository`, `contributors`, `version` are optional because they come from `skill.json` and older skills may not have them yet.

```typescript
// Add these types before SkillDefinition:

export interface SkillAuthor {
  name: string;
  url?: string;
}

export interface SkillContributor {
  name: string;
  url?: string;
}

// Add these optional fields to the SkillDefinition interface:
export interface SkillDefinition {
  // ... existing required fields (name, title, logo, description, doc, credentials, actions) stay unchanged ...

  /** Semver version string (e.g. "1.0.0"). */
  version?: string;
  /** SPDX license identifier (e.g. "MIT"). */
  license?: string;
  /** GitHub repository URL. */
  repository?: string;
  /** 1-3 categories from the fixed category list. */
  categories?: string[];
  /** Freeform search keywords, max 30. */
  keywords?: string[];
  /** Skill author. */
  author?: SkillAuthor;
  /** Contributors list. */
  contributors?: SkillContributor[];
}
```

- [ ] **Step 2: Rebuild SDK**

Run: `cd packages/skill-sdk && npm run build`
Expected: Clean build, no errors.

- [ ] **Step 3: Verify existing skills still compile**

Run: `cd packages/skill-runtime && npm run build`
Expected: All 65 skills build successfully (new fields are optional, so nothing breaks).

- [ ] **Step 4: Commit**

```bash
git add packages/skill-sdk/src/types.ts
git commit -m "feat(skill-sdk): add marketplace metadata fields to SkillDefinition"
```

---

## Phase 2: Migration Script — Generate skill.json for 65 Skills

### Task 2: Create the category mapping

**Files:**
- Create: `scripts/skill-categories.json`

This is a manual mapping of all 65 skills to their categories. This file drives the migration script.

- [ ] **Step 1: Create the category mapping file**

```json
{
  "airtable": ["Productivity", "Database"],
  "asana": ["Project Management"],
  "aws": ["Cloud Infrastructure"],
  "azure": ["Cloud Infrastructure"],
  "azure-devops": ["Developer Tools", "Cloud Infrastructure"],
  "browser": ["Developer Tools"],
  "cloudflare": ["Cloud Infrastructure"],
  "crypto": ["Finance"],
  "csv": ["Data & Analytics", "Documents"],
  "datadog": ["Monitoring"],
  "digital-ocean": ["Cloud Infrastructure"],
  "docker": ["Developer Tools", "Cloud Infrastructure"],
  "exa": ["AI & ML"],
  "excel": ["Documents", "Productivity"],
  "firebase": ["Cloud Infrastructure", "Database"],
  "gcp": ["Cloud Infrastructure"],
  "git": ["Developer Tools", "Version Control"],
  "github": ["Developer Tools", "Version Control"],
  "github-actions": ["Developer Tools"],
  "gitlab": ["Developer Tools", "Version Control"],
  "gmail": ["Productivity"],
  "google": ["Productivity"],
  "google-analytics": ["Data & Analytics", "Marketing"],
  "google-calendar": ["Productivity"],
  "google-drive": ["Documents", "Productivity"],
  "google-sheets": ["Documents", "Productivity"],
  "grafana": ["Monitoring"],
  "hubspot": ["CRM", "Marketing"],
  "imap-smtp-email": ["Productivity"],
  "jenkins": ["Developer Tools"],
  "jira": ["Project Management"],
  "klaviyo": ["Marketing", "E-commerce"],
  "kubernetes": ["Cloud Infrastructure"],
  "linear": ["Project Management"],
  "meta-ads": ["Marketing"],
  "mongodb": ["Database"],
  "mysql": ["Database"],
  "newrelic": ["Monitoring"],
  "notion": ["Productivity", "Documents"],
  "obsidian": ["Productivity", "Documents"],
  "pagerduty": ["Monitoring", "Support"],
  "pdf": ["Documents"],
  "perplexity": ["AI & ML"],
  "pipedrive": ["CRM"],
  "postgres": ["Database"],
  "posthog": ["Data & Analytics"],
  "powerpoint": ["Documents", "Productivity"],
  "prometheus": ["Monitoring"],
  "railway": ["Cloud Infrastructure"],
  "redis": ["Database"],
  "s3": ["Cloud Infrastructure"],
  "salesforce": ["CRM"],
  "sentry": ["Monitoring", "Developer Tools"],
  "stock-analysis": ["Finance", "Data & Analytics"],
  "stripe": ["Finance", "E-commerce"],
  "supabase": ["Database", "Cloud Infrastructure"],
  "terraform": ["Cloud Infrastructure"],
  "todoist": ["Productivity", "Project Management"],
  "trello": ["Project Management"],
  "vercel": ["Cloud Infrastructure", "Developer Tools"],
  "video-transcript": ["AI & ML", "Documents"],
  "web-scraper": ["Data & Analytics"],
  "web-search": ["Data & Analytics"],
  "word": ["Documents", "Productivity"],
  "zendesk": ["Support", "CRM"]
}
```

- [ ] **Step 2: Commit**

```bash
git add scripts/skill-categories.json
git commit -m "feat: add skill-to-category mapping for marketplace migration"
```

### Task 3: Create the migration script

**Files:**
- Create: `scripts/migrate-skills-to-json.js`

This script reads each skill's `skill.ts`, extracts metadata from the `defineSkill()` call, and generates `skill.json`, `README.md`, `CHANGELOG.md`, and `LICENSE` for each skill.

- [ ] **Step 1: Write the migration script**

```javascript
#!/usr/bin/env node
/**
 * One-time migration: generate skill.json, README.md, CHANGELOG.md, LICENSE
 * for each skill in packages/skills/.
 *
 * Reads metadata from defineSkill() in skill.ts via regex extraction.
 * Category mapping from scripts/skill-categories.json.
 */
import { readFileSync, writeFileSync, existsSync, readdirSync } from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const skillsDir = resolve(__dirname, "../packages/skills");
const categories = JSON.parse(
  readFileSync(resolve(__dirname, "skill-categories.json"), "utf-8")
);

const MIT_LICENSE = `MIT License

Copyright (c) 2026 OfficeOS

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
`;

const skills = readdirSync(skillsDir).filter((d) =>
  existsSync(resolve(skillsDir, d, "skill.ts"))
);

console.log(`Found ${skills.length} skills to migrate`);

for (const name of skills) {
  const dir = resolve(skillsDir, name);
  const skillTs = readFileSync(resolve(dir, "skill.ts"), "utf-8");

  // Extract metadata from defineSkill() call via regex
  const extractString = (field) => {
    // Match field: "value" or field: 'value' (single line)
    const re = new RegExp(`${field}:\\s*["'\`]([^"'\`]+)["'\`]`);
    const m = skillTs.match(re);
    return m ? m[1] : null;
  };

  // Extract multi-line description (handles template literals and concatenation)
  const extractDescription = () => {
    // Try single-line first
    const single = extractString("description");
    if (single) return single;
    // Try multi-line: description:\n    "...",
    const re = /description:\s*\n?\s*["'`]([^"'`]+)["'`]/;
    const m = skillTs.match(re);
    return m ? m[1].trim() : `OfficeOS ${name} integration`;
  };

  // Extract logo (may be very long SVG, spans one line)
  const extractLogo = () => {
    const re = /logo:\s*["'`](<svg[^"'`]+>[\s\S]*?<\/svg>)["'`]/;
    const m = skillTs.match(re);
    return m ? m[1] : "";
  };

  // Extract credentials block
  const extractCredentials = () => {
    // Find the credentials: { ... } block
    const credsStart = skillTs.indexOf("credentials:");
    if (credsStart === -1) return {};

    // Find the opening brace after "credentials:"
    let braceStart = skillTs.indexOf("{", credsStart);
    if (braceStart === -1) return {};

    // Count braces to find the matching close
    let depth = 0;
    let i = braceStart;
    for (; i < skillTs.length; i++) {
      if (skillTs[i] === "{") depth++;
      if (skillTs[i] === "}") depth--;
      if (depth === 0) break;
    }

    const block = skillTs.slice(braceStart, i + 1);

    // Parse individual credential fields
    const creds = {};
    const fieldRe =
      /(\w+):\s*\{([^}]+)\}/g;
    let fm;
    while ((fm = fieldRe.exec(block)) !== null) {
      const key = fm[1];
      const body = fm[2];
      const cred = {};

      const labelM = body.match(/label:\s*["'`]([^"'`]+)["'`]/);
      if (labelM) cred.label = labelM[1];

      const kindM = body.match(/kind:\s*["'`]([^"'`]+)["'`]/);
      if (kindM) cred.kind = kindM[1];

      const placeholderM = body.match(/placeholder:\s*["'`]([^"'`]+)["'`]/);
      if (placeholderM) cred.placeholder = placeholderM[1];

      const helpM = body.match(/help:\s*["'`]([^"'`]+)["'`]/);
      if (helpM) cred.help = helpM[1];

      if (cred.label) creds[key] = cred;
    }

    return creds;
  };

  const title = extractString("title") || name;
  const description = extractDescription();
  const logo = extractLogo();
  const credentials = extractCredentials();
  const skillCategories = categories[name] || ["Productivity"];

  // Generate skill.json
  const skillJson = {
    name,
    title,
    version: "1.0.0",
    description,
    logo,
    categories: skillCategories,
    keywords: [],
    author: { name: "OfficeOS Team", url: "https://officeos.co" },
    license: "MIT",
    repository: `https://github.com/officeos-co/skill-${name}`,
    contributors: [
      { name: "Harro Krog", url: "https://github.com/HarKro753" },
    ],
    credentials,
  };

  writeFileSync(
    resolve(dir, "skill.json"),
    JSON.stringify(skillJson, null, 2) + "\n"
  );

  // Generate README.md (human-facing marketplace listing)
  if (!existsSync(resolve(dir, "README.md"))) {
    const readme = `# ${title}

${description}

## Installation

Install from the OfficeOS dashboard under **Integrations**, or use the CLI:

\`\`\`
eaos skill install ${name}
\`\`\`

## Credentials

${Object.entries(credentials)
  .map(([key, field]) => `- **${field.label}** (\`${key}\`): ${field.help || "Required"}`)
  .join("\n")}

## License

[MIT](./LICENSE)
`;
    writeFileSync(resolve(dir, "README.md"), readme);
  }

  // Generate CHANGELOG.md
  if (!existsSync(resolve(dir, "CHANGELOG.md"))) {
    const today = new Date().toISOString().split("T")[0];
    const changelog = `# Changelog

## 1.0.0 (${today})

- Initial release
`;
    writeFileSync(resolve(dir, "CHANGELOG.md"), changelog);
  }

  // Generate LICENSE
  if (!existsSync(resolve(dir, "LICENSE"))) {
    writeFileSync(resolve(dir, "LICENSE"), MIT_LICENSE);
  }

  console.log(`  ${name}: skill.json + README.md + CHANGELOG.md + LICENSE`);
}

console.log(`\nMigrated ${skills.length} skills`);
```

- [ ] **Step 2: Run the migration**

Run: `node scripts/migrate-skills-to-json.js`
Expected: `Migrated 65 skills` — each skill directory now has `skill.json`, `README.md`, `CHANGELOG.md`, `LICENSE`.

- [ ] **Step 3: Spot-check 3 skills**

Verify `packages/skills/github/skill.json`, `packages/skills/notion/skill.json`, `packages/skills/stripe/skill.json` have correct name, title, logo, categories, and credentials. Fix any extraction issues and re-run.

- [ ] **Step 4: Commit**

```bash
git add scripts/migrate-skills-to-json.js packages/skills/
git commit -m "feat: migrate 65 skills to skill.json marketplace model"
```

### Task 4: Update skill.ts files to import from skill.json

**Files:**
- Create: `scripts/update-skill-imports.js`
- Modify: `packages/skills/*/skill.ts` (65 files)

Each skill.ts currently inlines name, title, logo, description, credentials in the `defineSkill()` call. We update them to spread from `skill.json`.

- [ ] **Step 1: Write the import updater script**

```javascript
#!/usr/bin/env node
/**
 * Update each skill.ts to import metadata from skill.json.
 *
 * Before:
 *   export default defineSkill({
 *     name: "github",
 *     title: "GitHub",
 *     logo: "...",
 *     description: "...",
 *     doc,
 *     credentials: { ... },
 *     actions: { ... },
 *   });
 *
 * After:
 *   import manifest from "./skill.json" with { type: "json" };
 *
 *   export default defineSkill({
 *     ...manifest,
 *     doc,
 *     actions: { ... },
 *   });
 */
import { readFileSync, writeFileSync, existsSync, readdirSync } from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const skillsDir = resolve(__dirname, "../packages/skills");

const skills = readdirSync(skillsDir).filter((d) =>
  existsSync(resolve(skillsDir, d, "skill.ts"))
);

for (const name of skills) {
  const file = resolve(skillsDir, name, "skill.ts");
  let src = readFileSync(file, "utf-8");

  // Skip if already migrated
  if (src.includes("skill.json")) {
    console.log(`  ${name}: already migrated`);
    continue;
  }

  // Add manifest import after the last import line
  const importLines = src.split("\n").filter((l) => l.startsWith("import "));
  const lastImportIdx = src.lastIndexOf(importLines[importLines.length - 1]);
  const lastImportEnd =
    lastImportIdx + importLines[importLines.length - 1].length;

  src =
    src.slice(0, lastImportEnd) +
    '\nimport manifest from "./skill.json" with { type: "json" };' +
    src.slice(lastImportEnd);

  // Replace the defineSkill body: remove name, title, logo, description, credentials
  // and add ...manifest spread
  // Strategy: find defineSkill({ and replace the metadata fields with ...manifest
  const defineStart = src.indexOf("defineSkill({");
  if (defineStart === -1) {
    console.log(`  ${name}: SKIP — no defineSkill({ found`);
    continue;
  }

  const bodyStart = defineStart + "defineSkill({".length;

  // Find where doc or actions starts (first surviving field)
  const docIdx = src.indexOf("doc", bodyStart);
  const actionsIdx = src.indexOf("actions:", bodyStart);
  const firstSurvivor = Math.min(
    docIdx === -1 ? Infinity : docIdx,
    actionsIdx === -1 ? Infinity : actionsIdx
  );

  if (firstSurvivor === Infinity) {
    console.log(`  ${name}: SKIP — can't find doc or actions`);
    continue;
  }

  // Replace everything between defineSkill({ and the first surviving field with ...manifest,\n
  const indent = "  ";
  src =
    src.slice(0, bodyStart) +
    `\n${indent}...manifest,\n${indent}` +
    src.slice(firstSurvivor);

  writeFileSync(file, src);
  console.log(`  ${name}: updated`);
}

console.log(`\nUpdated ${skills.length} skill.ts files`);
```

- [ ] **Step 2: Run the updater**

Run: `node scripts/update-skill-imports.js`

- [ ] **Step 3: Verify build still works**

Run: `cd packages/skill-runtime && npm run build`
Expected: All 65 skills build. If esbuild doesn't support `import ... with { type: "json" }`, fall back to:
```typescript
import { readFileSync } from "fs";
const manifest = JSON.parse(readFileSync(new URL("./skill.json", import.meta.url), "utf-8"));
```
Or use the esbuild json loader (it already has `.md` → `text`, add `.json` → `json`). The `build.js` file may need a `loader: { ".md": "text", ".json": "json" }` entry.

- [ ] **Step 4: Commit**

```bash
git add scripts/update-skill-imports.js packages/skills/ packages/skill-runtime/build.js
git commit -m "feat: update all skill.ts to import metadata from skill.json"
```

---

## Phase 3: CI Decoupling — Seed Manifests Without Runtime Build

### Task 5: Create the build-manifests script

**Files:**
- Create: `scripts/build-manifests.js`

This script reads `skill.json` + `SKILL.md` + `README.md` + `CHANGELOG.md` from each skill directory, combines them into a single `all.json` manifest array, and writes it to stdout or a file. No esbuild, no bundling. Pure file reads.

**Important:** This script produces the same shape as `extract-manifests.js` (the `RuntimeManifest` type the backend expects), but **without** the `actions` field (which requires Zod schema parsing from bundled code). The backend seed endpoint already handles missing/null fields gracefully since `ManifestJson` stores the raw JSON.

- [ ] **Step 1: Write the script**

```javascript
#!/usr/bin/env node
/**
 * Build skill manifests from skill.json files — no bundling required.
 * Reads skill.json + SKILL.md + README.md + CHANGELOG.md for each skill.
 * Outputs all.json compatible with POST /api/internal/seed-manifests.
 */
import { readFileSync, writeFileSync, existsSync, readdirSync, mkdirSync } from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const skillsDir = resolve(__dirname, "../packages/skills");
const outDir = resolve(__dirname, "../dist/manifests");

mkdirSync(outDir, { recursive: true });

const skills = readdirSync(skillsDir).filter((d) =>
  existsSync(resolve(skillsDir, d, "skill.json"))
);

const manifests = [];

for (const name of skills) {
  const dir = resolve(skillsDir, name);

  const skillJson = JSON.parse(
    readFileSync(resolve(dir, "skill.json"), "utf-8")
  );

  // Read optional markdown files
  const readOpt = (filename) => {
    const p = resolve(dir, filename);
    return existsSync(p) ? readFileSync(p, "utf-8") : null;
  };

  const manifest = {
    name: skillJson.name,
    title: skillJson.title,
    logo: skillJson.logo,
    description: skillJson.description,
    doc: readOpt("SKILL.md"),
    version: skillJson.version || "1.0.0",
    license: skillJson.license || null,
    repository: skillJson.repository || null,
    categories: skillJson.categories || [],
    keywords: skillJson.keywords || [],
    readme: readOpt("README.md"),
    changelog: readOpt("CHANGELOG.md"),
    author: skillJson.author || null,
    contributors: skillJson.contributors || [],
    // credentialFields in the shape the backend expects
    credentialFields: Object.entries(skillJson.credentials || {}).map(
      ([key, field]) => ({
        key,
        label: field.label,
        kind: field.kind,
        required: field.required !== false,
        placeholder: field.placeholder || null,
        help: field.help || null,
      })
    ),
    // actions omitted — they require Zod schema parsing from bundled code.
    // The runtime workflow seeds actions; this workflow seeds metadata only.
    // The backend merges: if actions already exist in ManifestJson, they're preserved.
  };

  manifests.push(manifest);
  console.log(`  ${name}: OK`);
}

writeFileSync(resolve(outDir, "all.json"), JSON.stringify(manifests, null, 2));
console.log(`\nBuilt ${manifests.length} manifests to dist/manifests/all.json`);
```

- [ ] **Step 2: Test locally**

Run: `node scripts/build-manifests.js`
Expected: `Built 65 manifests to dist/manifests/all.json`

- [ ] **Step 3: Verify output shape matches backend expectations**

Run: `node -e "const m = JSON.parse(require('fs').readFileSync('dist/manifests/all.json','utf-8')); console.log(Object.keys(m[0])); console.log(m[0].name, m[0].categories, m[0].credentialFields?.length)"`
Expected: Shows keys including `name`, `title`, `logo`, `categories`, `readme`, `changelog`, `credentialFields`.

- [ ] **Step 4: Commit**

```bash
git add scripts/build-manifests.js
git commit -m "feat: add build-manifests script — reads skill.json without bundling"
```

### Task 6: Update backend RuntimeManifest to accept new fields

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Domain/DTOs/Skills/SkillDto.cs`

The `RuntimeManifest` class needs the new marketplace fields. Since the backend stores the entire manifest as JSON in `ManifestJson`, we just need the DTO to deserialize the new fields.

- [ ] **Step 1: Add new fields to RuntimeManifest**

In `apps/backend/src/EnterpriseAgentOs.Domain/DTOs/Skills/SkillDto.cs`, add after the existing fields in `RuntimeManifest`:

```csharp
// Add new marketplace fields to RuntimeManifest class:
public string? Version { get; set; }
public string? License { get; set; }
public string? Repository { get; set; }
public string[]? Categories { get; set; }
public string[]? Keywords { get; set; }
public string? Readme { get; set; }
public string? Changelog { get; set; }
public ManifestAuthor? Author { get; set; }
public ManifestContributor[]? Contributors { get; set; }
```

Add the supporting types after `RuntimeCredentialField`:

```csharp
public sealed class ManifestAuthor
{
    public required string Name { get; set; }
    public string? Url { get; set; }
}

public sealed class ManifestContributor
{
    public required string Name { get; set; }
    public string? Url { get; set; }
}
```

- [ ] **Step 2: Verify build**

Run: `cd apps/backend && dotnet build`
Expected: Clean build.

- [ ] **Step 3: Commit**

```bash
git add apps/backend/src/EnterpriseAgentOs.Domain/DTOs/Skills/SkillDto.cs
git commit -m "feat(backend): add marketplace fields to RuntimeManifest DTO"
```

### Task 7: Add GraphQL resolvers for new marketplace fields

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Types/SkillTypes.cs`

Add resolvers for the new fields. They all follow the same pattern as `GetLogo()` — deserialize `ManifestJson` and return the field.

- [ ] **Step 1: Add helper method to avoid repetition**

The current `GetLogo` and `GetTools` each independently fetch the record and deserialize. Add a shared helper:

```csharp
// Add inside SkillDashboardResolvers class, before the existing methods:

private async Task<RuntimeManifest?> GetManifest(
    SkillDashboardDto skill,
    ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var record = await catalog.GetByNameAsync(skill.Name, ct);
    if (record is null || string.IsNullOrWhiteSpace(record.ManifestJson)) return null;
    try
    {
        return JsonSerializer.Deserialize<RuntimeManifest>(record.ManifestJson, ManifestJsonOptions);
    }
    catch { return null; }
}
```

- [ ] **Step 2: Refactor GetLogo and GetTools to use the helper**

```csharp
public async Task<string?> GetLogo(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    return manifest?.Logo;
}

public async Task<IReadOnlyList<SkillToolDto>> GetTools(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    if (manifest is null) return Array.Empty<SkillToolDto>();
    return manifest.Actions
        .Select(kv => new SkillToolDto(kv.Key, kv.Value.Description))
        .ToList();
}
```

- [ ] **Step 3: Add new marketplace resolvers**

```csharp
public async Task<string?> GetLicense(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    return manifest?.License;
}

public async Task<string?> GetRepository(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    return manifest?.Repository;
}

public async Task<IReadOnlyList<string>> GetCategories(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    return manifest?.Categories ?? Array.Empty<string>();
}

public async Task<IReadOnlyList<string>> GetKeywords(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    return manifest?.Keywords ?? Array.Empty<string>();
}

public async Task<string?> GetReadme(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    return manifest?.Readme;
}

public async Task<string?> GetChangelog(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    return manifest?.Changelog;
}
```

- [ ] **Step 4: Add author/contributor GraphQL types and resolvers**

Add new record types:

```csharp
[GraphQLName("SkillAuthor")]
public sealed record SkillAuthorDto(string Name, string? Url);

[GraphQLName("SkillContributor")]
public sealed record SkillContributorDto(string Name, string? Url);
```

Add resolvers:

```csharp
public async Task<SkillAuthorDto?> GetAuthor(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    if (manifest?.Author is null) return null;
    return new SkillAuthorDto(manifest.Author.Name, manifest.Author.Url);
}

public async Task<IReadOnlyList<SkillContributorDto>> GetContributors(
    [Parent] SkillDashboardDto skill,
    [Service] ISkillCatalogRepository catalog,
    CancellationToken ct)
{
    var manifest = await GetManifest(skill, catalog, ct);
    if (manifest?.Contributors is null) return Array.Empty<SkillContributorDto>();
    return manifest.Contributors
        .Select(c => new SkillContributorDto(c.Name, c.Url))
        .ToList();
}
```

- [ ] **Step 5: Verify build**

Run: `cd apps/backend && dotnet build`
Expected: Clean build.

- [ ] **Step 6: Commit**

```bash
git add apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Types/SkillTypes.cs
git commit -m "feat(backend): add GraphQL resolvers for marketplace metadata fields"
```

### Task 8: Create seed-skill-manifests CI workflow

**Files:**
- Create: `.github/workflows/seed-skill-manifests.yml`
- Modify: `.github/workflows/build-skill-runtime.yml`

- [ ] **Step 1: Create the new lightweight workflow**

```yaml
name: Seed Skill Manifests

on:
  push:
    branches: [main]
    paths:
      - 'packages/skills/**'

concurrency:
  group: seed-skill-manifests
  cancel-in-progress: true

jobs:
  seed:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-node@v4
        with:
          node-version: '22'

      - name: Build manifests from skill.json files
        run: node scripts/build-manifests.js

      - uses: actions/upload-artifact@v4
        with:
          name: skill-manifests
          path: dist/manifests/all.json

      - name: Connect to Tailscale
        uses: tailscale/github-action@v3
        with:
          oauth-client-id: ${{ secrets.TAILSCALE_OAUTH_CLIENT_ID }}
          oauth-secret: ${{ secrets.TAILSCALE_OAUTH_SECRET }}
          tags: tag:ci

      - name: Seed manifests to backend database
        run: |
          kubectl config set-cluster nova --server=https://tailscale-operator.tail82aae1.ts.net:443 --insecure-skip-tls-verify=true
          kubectl config set-credentials ci --token="${{ secrets.KUBE_TOKEN }}"
          kubectl config set-context nova --cluster=nova --user=ci --namespace=default
          kubectl config use-context nova

          kubectl port-forward svc/eaos-backend-prod 5099:8000 &
          PF_PID=$!
          sleep 5

          RESPONSE=$(curl -sS -w "\n%{http_code}" -X POST "http://localhost:5099/api/internal/seed-manifests" \
            -H "Content-Type: application/json" \
            -d @dist/manifests/all.json)
          HTTP_CODE=$(echo "$RESPONSE" | tail -1)
          BODY=$(echo "$RESPONSE" | sed '$d')
          kill $PF_PID || true
          echo "HTTP $HTTP_CODE — $BODY"
          if [ "$HTTP_CODE" -lt 200 ] || [ "$HTTP_CODE" -ge 300 ]; then
            echo "::error::Seed manifests failed with HTTP $HTTP_CODE"
            exit 1
          fi
          echo "Manifests seeded successfully"
```

- [ ] **Step 2: Remove packages/skills/** from build-skill-runtime.yml trigger**

In `.github/workflows/build-skill-runtime.yml`, change the paths section (lines 6-10):

```yaml
on:
  push:
    branches: [main]
    paths:
      - 'packages/skill-runtime/**'
      - 'packages/skill-sdk/**'
      - '.github/workflows/build-skill-runtime.yml'
```

Remove line 9 (`- 'packages/skills/**'`).

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/seed-skill-manifests.yml .github/workflows/build-skill-runtime.yml
git commit -m "feat(ci): decouple manifest seeding from runtime deployment"
```

---

## Phase 4: Dashboard — Render Marketplace Fields

### Task 9: Update GraphQL query and hook

**Files:**
- Modify: `apps/dashboard/src/lib/graphql/operations/integrations.graphql`
- Modify: `apps/dashboard/src/features/agents/api/useIntegrations.ts`
- Modify: `apps/dashboard/src/features/agents/data/integrations.ts`

- [ ] **Step 1: Add new fields to the GraphQL query**

In `apps/dashboard/src/lib/graphql/operations/integrations.graphql`, add the new fields to the `Skills` query:

```graphql
query Skills {
  skills {
    id
    name
    title
    description
    logo
    sourceCodeUrl
    doc
    status
    installed
    likes
    likedByMe
    commentsCount
    version
    license
    repository
    categories
    keywords
    readme
    changelog
    author {
      name
      url
    }
    contributors {
      name
      url
    }
    tools {
      name
      description
    }
  }
}
```

- [ ] **Step 2: Update the Integration type**

In `apps/dashboard/src/features/agents/data/integrations.ts`, update the `Integration` type to include new fields:

```typescript
export type SkillAuthor = {
  name: string;
  url?: string | null;
};

export type SkillContributor = {
  name: string;
  url?: string | null;
};

// Add to the Integration type:
export type Integration = {
  // ... existing fields ...
  version: string;
  license: string | null;
  repository: string | null;
  categories: string[];
  keywords: string[];
  readme: string | null;
  changelog: string | null;
  author: SkillAuthor | null;
  contributors: SkillContributor[];
};
```

- [ ] **Step 3: Update useIntegrations hook to map new fields**

In `apps/dashboard/src/features/agents/api/useIntegrations.ts`, update the mapping to include new fields from the GraphQL response:

```typescript
// In the map function where Integration objects are created:
version: s.version ?? "1.0.0",
license: s.license ?? null,
repository: s.repository ?? null,
categories: s.categories ?? [],
keywords: s.keywords ?? [],
readme: s.readme ?? null,
changelog: s.changelog ?? null,
author: s.author ?? null,
contributors: s.contributors ?? [],
```

- [ ] **Step 4: Verify dashboard builds**

Run: `cd apps/dashboard && npm run build`
Expected: Clean build (new fields are all optional/nullable so no type errors).

- [ ] **Step 5: Commit**

```bash
git add apps/dashboard/src/lib/graphql/operations/integrations.graphql \
       apps/dashboard/src/features/agents/api/useIntegrations.ts \
       apps/dashboard/src/features/agents/data/integrations.ts
git commit -m "feat(dashboard): add marketplace metadata to GraphQL query and types"
```

### Task 10: Render categories on the integrations list page

**Files:**
- Modify: `apps/dashboard/src/app/(dashboard)/integrations/page.tsx`

- [ ] **Step 1: Add category badges to integration cards**

Read the current file first to understand the layout. Then add category badges next to the integration title/description. Use small Badge components with the category name. Also add a category filter at the top of the page.

This task requires reading the current integrations page to find the exact insertion points. The pattern should be:

```tsx
{/* After the description line in each integration card */}
{integration.categories.length > 0 && (
  <div className="flex gap-1 mt-1">
    {integration.categories.map((cat) => (
      <span key={cat} className="inline-flex items-center rounded-md bg-muted px-2 py-0.5 text-xs text-muted-foreground">
        {cat}
      </span>
    ))}
  </div>
)}
```

- [ ] **Step 2: Add category filter UI at the top**

Add a horizontal scrollable row of category filter buttons above the grid:

```tsx
const [selectedCategory, setSelectedCategory] = useState<string | null>(null);

const allCategories = useMemo(() => {
  const cats = new Set<string>();
  integrations.forEach((i) => i.categories.forEach((c) => cats.add(c)));
  return Array.from(cats).sort();
}, [integrations]);

const filtered = selectedCategory
  ? integrations.filter((i) => i.categories.includes(selectedCategory))
  : integrations;
```

- [ ] **Step 3: Verify it renders**

Run: `cd apps/dashboard && npm run dev`
Visit `http://localhost:3000/integrations` — categories should appear as badges, filter should work.

- [ ] **Step 4: Commit**

```bash
git add apps/dashboard/src/app/\(dashboard\)/integrations/page.tsx
git commit -m "feat(dashboard): show category badges and filter on integrations page"
```

### Task 11: Render marketplace details on the integration detail page

**Files:**
- Modify: `apps/dashboard/src/app/(dashboard)/integrations/[slug]/page.tsx`

- [ ] **Step 1: Read the current detail page**

Read `apps/dashboard/src/app/(dashboard)/integrations/[slug]/page.tsx` to understand the current layout.

- [ ] **Step 2: Add README tab**

The detail page should have tabs: "Overview" (current doc content), "README" (human-facing), "Changelog". Add a tab component if one doesn't exist:

```tsx
const [activeTab, setActiveTab] = useState<"overview" | "readme" | "changelog">("overview");
```

Render the active tab content using the existing markdown renderer (check if the codebase has one, or use `dangerouslySetInnerHTML` with a markdown-to-html library).

- [ ] **Step 3: Add sidebar metadata**

In the detail page sidebar (or create one if it doesn't exist), show:

```tsx
<div className="space-y-4 text-sm">
  {integration.version && (
    <div>
      <dt className="font-medium text-muted-foreground">Version</dt>
      <dd>{integration.version}</dd>
    </div>
  )}
  {integration.license && (
    <div>
      <dt className="font-medium text-muted-foreground">License</dt>
      <dd>{integration.license}</dd>
    </div>
  )}
  {integration.author && (
    <div>
      <dt className="font-medium text-muted-foreground">Author</dt>
      <dd>
        {integration.author.url ? (
          <a href={integration.author.url} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline">
            {integration.author.name}
          </a>
        ) : integration.author.name}
      </dd>
    </div>
  )}
  {integration.repository && (
    <div>
      <dt className="font-medium text-muted-foreground">Repository</dt>
      <dd>
        <a href={integration.repository} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline">
          Source Code
        </a>
      </dd>
    </div>
  )}
  {integration.categories.length > 0 && (
    <div>
      <dt className="font-medium text-muted-foreground">Categories</dt>
      <dd className="flex flex-wrap gap-1 mt-1">
        {integration.categories.map((cat) => (
          <span key={cat} className="inline-flex items-center rounded-md bg-muted px-2 py-0.5 text-xs">
            {cat}
          </span>
        ))}
      </dd>
    </div>
  )}
  {integration.contributors.length > 0 && (
    <div>
      <dt className="font-medium text-muted-foreground">Contributors</dt>
      <dd className="space-y-1 mt-1">
        {integration.contributors.map((c) => (
          <div key={c.name}>
            {c.url ? (
              <a href={c.url} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline text-xs">
                {c.name}
              </a>
            ) : <span className="text-xs">{c.name}</span>}
          </div>
        ))}
      </dd>
    </div>
  )}
</div>
```

- [ ] **Step 4: Verify it renders**

Run: `cd apps/dashboard && npm run dev`
Visit `http://localhost:3000/integrations/github` — should show tabs and sidebar metadata.

- [ ] **Step 5: Commit**

```bash
git add apps/dashboard/src/app/\(dashboard\)/integrations/\[slug\]/page.tsx
git commit -m "feat(dashboard): add marketplace metadata sidebar and tabs to detail page"
```

---

## Phase 5: Sync Script + Cleanup

### Task 12: Update sync-skill-repos.sh

**Files:**
- Modify: `scripts/sync-skill-repos.sh`

- [ ] **Step 1: Update the file copy list**

In `scripts/sync-skill-repos.sh`, update the file copy block (around line 54) to include new marketplace files:

```bash
# Move key files to repo root for discoverability
for f in skill.ts skill.json SKILL.md README.md CHANGELOG.md LICENSE icon.svg icon.png; do
  [ -f "$SRC/$f" ] && cp "$SRC/$f" "$WORK/$REPO/$f"
done
```

- [ ] **Step 2: Remove the auto-generated README block**

Remove the block at lines 69-85 that generates README.md — each skill now has its own:

```bash
# REMOVE this entire block:
# if [ ! -f "$WORK/$REPO/README.md" ]; then
#   ...
# fi
```

- [ ] **Step 3: Update package.json generation to read from skill.json**

Replace the description extraction (line 39) to read from `skill.json`:

```bash
DESC=$(node -e "console.log(JSON.parse(require('fs').readFileSync('$SRC/skill.json','utf-8')).description)" 2>/dev/null || echo "OfficeOS skill: $skill")
```

- [ ] **Step 4: Commit**

```bash
git add scripts/sync-skill-repos.sh
git commit -m "feat: update sync-skill-repos to carry marketplace files"
```

### Task 13: Update extract-manifests.js to include new fields

**Files:**
- Modify: `packages/skill-runtime/src/manifest.ts`

The runtime's `extractManifest` function should also extract the new fields when they exist on the `SkillDefinition`, so the runtime workflow still produces complete manifests.

- [ ] **Step 1: Update SkillManifest interface and extractManifest**

In `packages/skill-runtime/src/manifest.ts`, add the new fields:

```typescript
export interface SkillManifest {
  name: string;
  title: string;
  logo: string;
  description: string;
  doc: string;
  version?: string;
  license?: string;
  repository?: string;
  categories?: string[];
  keywords?: string[];
  author?: { name: string; url?: string };
  contributors?: { name: string; url?: string }[];
  actions: Record<string, { description: string; params: Record<string, unknown>; returns?: Record<string, unknown> }>;
  credentialFields: CredentialFieldManifest[];
}
```

In `extractManifest`, add after `doc: def.doc`:

```typescript
version: def.version,
license: def.license,
repository: def.repository,
categories: def.categories,
keywords: def.keywords,
author: def.author,
contributors: def.contributors,
```

- [ ] **Step 2: Verify runtime build**

Run: `cd packages/skill-runtime && npm run build`
Expected: Clean build.

- [ ] **Step 3: Commit**

```bash
git add packages/skill-runtime/src/manifest.ts
git commit -m "feat(skill-runtime): extract marketplace fields from definitions"
```

### Task 14: Update CLAUDE.md documentation

**Files:**
- Modify: `packages/skills/CLAUDE.md` (or create if not exists)
- Modify: `packages/skill-sdk/CLAUDE.md`

- [ ] **Step 1: Document the new skill file structure**

Update the skills CLAUDE.md to document the standard skill layout:

```markdown
## Skill File Structure

Every skill follows this standard layout:

| File | Purpose |
|------|---------|
| `skill.json` | Marketplace manifest — single source of truth for metadata |
| `skill.ts` | defineSkill() — imports from skill.json, defines actions |
| `SKILL.md` | Agent-facing documentation (runtime injects into agent context) |
| `README.md` | Human-facing marketplace listing |
| `CHANGELOG.md` | Version history |
| `LICENSE` | License file (MIT for first-party) |
| `package.json` | npm dependencies only — no marketplace metadata |

## skill.json

Source of truth for all marketplace metadata. Fields: name, title, version, description, logo (inline SVG), categories (1-3 from fixed list), keywords, author, license, repository, contributors, credentials.

### Fixed Categories

Developer Tools, Version Control, Project Management, CRM, Communication, Productivity, Data & Analytics, Cloud Infrastructure, Marketing, Finance, Support, Security, Monitoring, Database, Documents, AI & ML, E-commerce, HR
```

- [ ] **Step 2: Update skill-sdk CLAUDE.md with new types**

Document `SkillAuthor`, `SkillContributor`, and the new optional fields on `SkillDefinition`.

- [ ] **Step 3: Commit**

```bash
git add packages/skills/CLAUDE.md packages/skill-sdk/CLAUDE.md
git commit -m "docs: update CLAUDE.md for marketplace skill structure"
```

---

## Verification Checklist

After all tasks are complete:

1. **Migration check:** Every skill in `packages/skills/*/` has `skill.json`, `README.md`, `CHANGELOG.md`, `LICENSE`
2. **Build check:** `cd packages/skill-runtime && npm run build` succeeds (defineSkill accepts skill.json spread)
3. **Manifest check:** `node scripts/build-manifests.js` produces `dist/manifests/all.json` with 65 entries including categories, readme, changelog
4. **Backend check:** `cd apps/backend && dotnet build` succeeds
5. **Dashboard check:** `cd apps/dashboard && npm run build` succeeds
6. **CI check:** Push a skill-only change → only `seed-skill-manifests.yml` triggers, not `build-skill-runtime.yml`
7. **End-to-end:** After CI seeds manifests, `/integrations` page shows categories, detail page shows README tab, changelog tab, sidebar metadata
