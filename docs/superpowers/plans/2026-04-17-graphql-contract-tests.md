# GraphQL Contract Tests — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a CI job that validates every dashboard GraphQL query/mutation compiles against the live backend schema, catching type mismatches before deploy.

**Architecture:** A single test file extracts all `gql` operations from dashboard hooks, fetches the backend's introspection schema, and validates each operation using `@graphql-tools/utils` `validate()`. Runs as `bun test` in CI before the Docker build. No session cookie needed — introspection is unauthenticated schema metadata.

**Tech Stack:** Bun test runner (built into Bun), `graphql` (peer dep of Apollo — already installed), `@graphql-tools/utils` + `@graphql-tools/wrap` for schema introspection + validation.

---

### Task 1: Install test dependencies

**Files:**
- Modify: `apps/dashboard-2/package.json`

- [ ] **Step 1: Install deps**

```bash
cd apps/dashboard-2
bun add -d @graphql-tools/utils @graphql-tools/wrap
```

`graphql` is already a dependency via Apollo Client.

- [ ] **Step 2: Verify install**

```bash
cd apps/dashboard-2 && bun run -e "require('@graphql-tools/utils')" && echo ok
```

Expected: `ok`

- [ ] **Step 3: Commit**

```bash
git add apps/dashboard-2/package.json apps/dashboard-2/bun.lock
git commit -m "chore: add graphql-tools for contract tests"
```

---

### Task 2: Create the contract test

**Files:**
- Create: `apps/dashboard-2/src/__tests__/graphql-contracts.test.ts`

- [ ] **Step 1: Create the test file**

This test:
1. Fetches the introspection schema from the live backend (`GRAPHQL_SCHEMA_URL` env or default `https://api.officeos.co/api/dashboard/graphql`)
2. Extracts every `gql` tagged template from all hook files
3. Validates each parsed document against the introspection schema
4. Fails with a clear message showing which operation has which error

```typescript
import { describe, test, expect } from "bun:test"
import { buildClientSchema, getIntrospectionQuery, parse, validate } from "graphql"
import { readFileSync, readdirSync } from "fs"
import { join } from "path"

const SCHEMA_URL =
  process.env.GRAPHQL_SCHEMA_URL ?? "https://api.officeos.co/api/dashboard/graphql"

const HOOKS_DIR = join(import.meta.dir, "..", "hooks")

/** Extract all gql`` template bodies from a TypeScript source file. */
function extractGqlBodies(source: string): string[] {
  const bodies: string[] = []
  // Match gql`...` — backtick-delimited, non-greedy
  const re = /gql\s*`([\s\S]*?)`/g
  let m: RegExpExecArray | null
  while ((m = re.exec(source)) !== null) {
    bodies.push(m[1])
  }
  return bodies
}

/** Fetch the introspection schema from a live GraphQL endpoint. */
async function fetchSchema() {
  const res = await fetch(SCHEMA_URL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ query: getIntrospectionQuery() }),
  })
  if (!res.ok) throw new Error(`Schema fetch failed: ${res.status} ${res.statusText}`)
  const json = (await res.json()) as { data: Parameters<typeof buildClientSchema>[0] }
  return buildClientSchema(json.data)
}

describe("GraphQL contract tests", () => {
  let schema: ReturnType<typeof buildClientSchema>
  let operations: Array<{ file: string; name: string; body: string }>

  // ── Collect operations from hook files ──
  const hookFiles = readdirSync(HOOKS_DIR).filter((f) => f.endsWith(".ts"))
  operations = []
  for (const file of hookFiles) {
    const source = readFileSync(join(HOOKS_DIR, file), "utf-8")
    const bodies = extractGqlBodies(source)
    for (const body of bodies) {
      // Pull the operation name from the document for readable test names
      const nameMatch = body.match(/(?:query|mutation|subscription)\s+(\w+)/)
      const name = nameMatch?.[1] ?? "anonymous"
      operations.push({ file, name, body })
    }
  }

  test("schema introspection succeeds", async () => {
    schema = await fetchSchema()
    expect(schema).toBeDefined()
  })

  // ── One test per operation ──
  for (const op of operations) {
    test(`${op.name} (${op.file})`, () => {
      if (!schema) throw new Error("Schema not loaded — introspection test must pass first")
      const doc = parse(op.body)
      const errors = validate(schema, doc)
      if (errors.length > 0) {
        const msgs = errors.map((e) => e.message).join("\n  ")
        throw new Error(`${op.name} failed validation:\n  ${msgs}`)
      }
    })
  }
})
```

- [ ] **Step 2: Run the test locally**

```bash
cd apps/dashboard-2
bun test src/__tests__/graphql-contracts.test.ts
```

Expected: All operations pass (including the two we just fixed). If any fail, that's a real bug to fix before merging.

- [ ] **Step 3: Commit**

```bash
git add apps/dashboard-2/src/__tests__/graphql-contracts.test.ts
git commit -m "test: add GraphQL contract tests validating frontend queries against backend schema"
```

---

### Task 3: Add test step to CI workflow

**Files:**
- Modify: `.github/workflows/deploy-dashboard-prod.yml`

- [ ] **Step 1: Add a `test` job that runs before `build`**

The test job installs Bun, runs the contract tests against `api.officeos.co`, and gates the build. Add this job before the existing `build` job, and make `build` depend on it:

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: oven-sh/setup-bun@v2

      - name: Install dependencies
        working-directory: apps/dashboard-2
        run: bun install --frozen-lockfile

      - name: Run contract tests
        working-directory: apps/dashboard-2
        run: bun test src/__tests__/graphql-contracts.test.ts
        env:
          GRAPHQL_SCHEMA_URL: https://api.officeos.co/api/dashboard/graphql

  build:
    needs: test
    # ... rest unchanged
```

- [ ] **Step 2: Verify the YAML is valid**

```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/deploy-dashboard-prod.yml'))" && echo ok
```

Expected: `ok`

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/deploy-dashboard-prod.yml
git commit -m "ci: run GraphQL contract tests before dashboard build"
```

---

### Task 4: Add a bun test script to package.json

**Files:**
- Modify: `apps/dashboard-2/package.json`

- [ ] **Step 1: Add test script**

Add to the `"scripts"` section:

```json
"test": "bun test"
```

- [ ] **Step 2: Verify it works**

```bash
cd apps/dashboard-2 && bun run test
```

Expected: contract tests run and pass.

- [ ] **Step 3: Commit**

```bash
git add apps/dashboard-2/package.json
git commit -m "chore: add test script to dashboard package.json"
```
