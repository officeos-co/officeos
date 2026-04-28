#!/usr/bin/env node

/**
 * generate-api-doc.js
 *
 * Introspects the live GraphQL schema and reads REST controller source to
 * generate a single API.md reference document for AI agents.
 *
 * Usage:
 *   node scripts/generate-api-doc.js                     # uses production
 *   API_URL=http://localhost:8000 node scripts/generate-api-doc.js  # local
 *
 * Output: apps/backend/API.md
 */

const fs = require("fs");
const path = require("path");

const BASE_URL = process.env.API_URL || "https://api.officeos.co";
const DASHBOARD_GQL = `${BASE_URL}/api/dashboard/graphql`;
const AGENT_GQL = `${BASE_URL}/api/graphql`;
const BACKEND = path.resolve(__dirname, "../apps/backend");
const API_SRC = path.join(BACKEND, "src/EnterpriseAgentOs.Api/Features");
const OUT = path.join(BACKEND, "API.md");

// ── helpers ──────────────────────────────────────────────────────────────────

function readIfExists(filePath) {
  try {
    return fs.readFileSync(filePath, "utf-8");
  } catch {
    return null;
  }
}

function findFiles(dir, pattern) {
  const results = [];
  if (!fs.existsSync(dir)) return results;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...findFiles(full, pattern));
    } else if (pattern.test(entry.name)) {
      results.push(full);
    }
  }
  return results.sort();
}

// ── GraphQL introspection ────────────────────────────────────────────────────

const INTROSPECTION_QUERY = `{
  __schema {
    queryType {
      fields {
        name
        description
        args {
          name
          description
          defaultValue
          type { ...TypeRef }
        }
        type { ...TypeRef }
      }
    }
    mutationType {
      fields {
        name
        description
        args {
          name
          description
          defaultValue
          type { ...TypeRef }
        }
        type { ...TypeRef }
      }
    }
    subscriptionType {
      fields {
        name
        description
        args {
          name
          description
          defaultValue
          type { ...TypeRef }
        }
        type { ...TypeRef }
      }
    }
    types {
      name
      kind
      description
      fields {
        name
        description
        type { ...TypeRef }
      }
      inputFields {
        name
        description
        defaultValue
        type { ...TypeRef }
      }
      enumValues {
        name
        description
      }
    }
  }
}

fragment TypeRef on __Type {
  name
  kind
  ofType {
    name
    kind
    ofType {
      name
      kind
      ofType {
        name
        kind
        ofType { name kind }
      }
    }
  }
}`;

async function introspect(url) {
  try {
    const res = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ query: INTROSPECTION_QUERY }),
    });
    if (!res.ok) return null;
    const json = await res.json();
    return json.data?.__schema || null;
  } catch {
    return null;
  }
}

// ── type formatting ──────────────────────────────────────────────────────────

function formatType(typeRef) {
  if (!typeRef) return "unknown";
  switch (typeRef.kind) {
    case "NON_NULL":
      return `${formatType(typeRef.ofType)}!`;
    case "LIST":
      return `[${formatType(typeRef.ofType)}]`;
    case "SCALAR":
    case "ENUM":
    case "OBJECT":
    case "INPUT_OBJECT":
      return typeRef.name;
    default:
      return typeRef.name || "unknown";
  }
}

// Strip NON_NULL wrappers for cleaner display (GraphQL clients handle nullability)
function formatTypeClean(typeRef) {
  const raw = formatType(typeRef);
  return raw.replace(/!/g, "");
}

// ── format operations ────────────────────────────────────────────────────────

function formatField(field) {
  const args = field.args
    .map((a) => {
      let s = `${a.name}: ${formatTypeClean(a.type)}`;
      if (a.defaultValue) s += ` = ${a.defaultValue}`;
      return s;
    })
    .join(", ");

  const sig = args ? `\`${field.name}(${args})\`` : `\`${field.name}\``;
  const ret = `→ \`${formatTypeClean(field.type)}\``;

  let line = `#### ${sig} ${ret}\n`;
  if (field.description) line += `${field.description}\n`;
  line += "\n";
  return line;
}

// ── extract REST endpoints from C# source ────────────────────────────────────

function extractDocComment(lines, methodLine) {
  const parts = [];
  for (let k = methodLine - 1; k >= 0; k--) {
    const trimmed = lines[k].trim();
    if (trimmed.startsWith("/// <summary>")) break;
    if (trimmed.startsWith("///")) {
      const text = trimmed
        .replace(/^\/\/\/\s*/, "")
        .replace(/<\/?summary>/g, "")
        .replace(/<[^>]+>/g, "")
        .trim();
      if (text) parts.unshift(text);
    } else if (
      trimmed.startsWith("[") ||
      trimmed.startsWith("//") ||
      trimmed === ""
    ) {
      continue;
    } else {
      break;
    }
  }
  return parts.join(" ") || null;
}

function extractRestEndpoints(source, filePath) {
  const endpoints = [];
  const lines = source.split("\n");

  const routeMatch = source.match(/\[Route\("([^"]+)"\)\]/);
  const baseRoute = routeMatch ? `/${routeMatch[1]}` : "";

  // Minimal API endpoints (static classes with Handle method)
  if (/public\s+static\s+class/.test(source)) {
    const handleIdx = lines.findIndex((l) => l.includes("Handle"));
    if (handleIdx >= 0) {
      const doc = extractDocComment(lines, handleIdx);
      const className = path.basename(filePath, ".cs");

      const recordMatch = source.match(
        /public\s+record\s+(\w+)\s*\(([^)]+)\)/
      );
      const params = [];
      if (recordMatch) {
        for (const field of recordMatch[2].split(",").map((f) => f.trim())) {
          const parts = field.split(/\s+/);
          if (parts.length >= 2) {
            params.push({
              name: parts[parts.length - 1],
              type: parts.slice(0, -1).join(" "),
            });
          }
        }
      }

      endpoints.push({
        method: "POST",
        path: className.replace("Endpoint", "").toLowerCase(),
        name: className,
        params,
        doc,
      });
    }
    return endpoints;
  }

  // Controller endpoints
  for (let i = 0; i < lines.length; i++) {
    const httpMatch = lines[i].match(
      /\[Http(Get|Post|Put|Delete|Patch)\(?(?:"([^"]*)")?\)?\]/
    );
    if (!httpMatch) continue;

    const method = httpMatch[1].toUpperCase();
    const subRoute = httpMatch[2] || "";
    const routePath = `${baseRoute}${subRoute ? "/" + subRoute : ""}`;

    let j = i + 1;
    while (j < lines.length && lines[j].trim().startsWith("[")) j++;
    const sigMatch = lines[j]?.match(
      /public\s+(?:async\s+)?(?:Task<)?(\w+)>?\s+(\w+)\s*\(/
    );
    if (!sigMatch) continue;

    const doc = extractDocComment(lines, i);
    const params = [];

    const paramBlock = [];
    let depth = 0;
    for (let k = j; k < lines.length; k++) {
      depth += (lines[k].match(/\(/g) || []).length;
      depth -= (lines[k].match(/\)/g) || []).length;
      paramBlock.push(lines[k]);
      if (depth <= 0) break;
    }
    const block = paramBlock.join(" ");
    for (const fm of block.matchAll(/\[From(\w+)\]\s*(\w+(?:\?)?)\s+(\w+)/g)) {
      params.push({ name: fm[3], type: fm[2], source: fm[1].toLowerCase() });
    }
    for (const pp of subRoute.matchAll(/\{(\w+)\}/g)) {
      if (!params.find((p) => p.name === pp[1])) {
        params.push({ name: pp[1], type: "string", source: "path" });
      }
    }

    endpoints.push({
      method,
      path: routePath,
      name: sigMatch[2],
      returnType: sigMatch[1],
      params,
      doc,
    });
  }
  return endpoints;
}

// ── format types from introspection ──────────────────────────────────────────

function formatTypes(schema) {
  const sections = [];
  const builtinScalars = new Set([
    "String",
    "Int",
    "Float",
    "Boolean",
    "ID",
    "DateTime",
    "UUID",
    "Decimal",
    "Long",
    "Short",
    "Byte",
    "Date",
    "TimeSpan",
    "Any",
    "Upload",
  ]);

  const enums = [];
  const objects = [];
  const inputs = [];

  for (const t of schema.types) {
    if (t.name.startsWith("__")) continue;
    if (builtinScalars.has(t.name)) continue;
    if (
      t.name === "GraphQLQueries" ||
      t.name === "GraphQLMutations" ||
      t.name === "GraphQLSubscriptions"
    )
      continue;

    if (t.kind === "ENUM") enums.push(t);
    else if (t.kind === "OBJECT" && t.fields) objects.push(t);
    else if (t.kind === "INPUT_OBJECT" && t.inputFields) inputs.push(t);
  }

  // Enums
  if (enums.length > 0) {
    sections.push(`### Enums\n`);
    for (const e of enums.sort((a, b) => a.name.localeCompare(b.name))) {
      const vals = e.enumValues.map((v) => v.name).join(" | ");
      let line = `**\`${e.name}\`** — \`${vals}\`\n`;
      if (e.description) line = `**\`${e.name}\`** — ${e.description}\n\`${vals}\`\n`;
      sections.push(line);
    }
  }

  // Objects (only DTOs, payloads, records — skip internal framework types)
  const interestingObjects = objects.filter(
    (t) =>
      /Dto|Payload|Record|Info|Entry|Page|Result|Definition|Step|Config|Limit/.test(
        t.name
      ) || t.name === "Skill"
  );
  if (interestingObjects.length > 0) {
    sections.push(`\n### Object Types\n`);
    for (const t of interestingObjects.sort((a, b) =>
      a.name.localeCompare(b.name)
    )) {
      let block = `**\`${t.name}\`**`;
      if (t.description) block += ` — ${t.description}`;
      block += "\n";
      if (t.fields.length > 0) {
        block += `| Field | Type | Description |\n|-------|------|-------------|\n`;
        for (const f of t.fields) {
          const desc = f.description || "";
          block += `| \`${f.name}\` | \`${formatTypeClean(f.type)}\` | ${desc} |\n`;
        }
      }
      block += "\n";
      sections.push(block);
    }
  }

  // Input types
  if (inputs.length > 0) {
    sections.push(`\n### Input Types\n`);
    for (const t of inputs.sort((a, b) => a.name.localeCompare(b.name))) {
      // Skip auto-generated Record inputs
      if (t.name.endsWith("RecordInput")) continue;

      let block = `**\`${t.name}\`**`;
      if (t.description) block += ` — ${t.description}`;
      block += "\n";
      if (t.inputFields.length > 0) {
        block += `| Field | Type | Default | Description |\n|-------|------|---------|-------------|\n`;
        for (const f of t.inputFields) {
          const desc = f.description || "";
          const def = f.defaultValue ? `\`${f.defaultValue}\`` : "";
          block += `| \`${f.name}\` | \`${formatTypeClean(f.type)}\` | ${def} | ${desc} |\n`;
        }
      }
      block += "\n";
      sections.push(block);
    }
  }

  return sections.join("\n");
}

// ── build the document ───────────────────────────────────────────────────────

async function generate() {
  console.log(`Fetching schema from ${BASE_URL}...`);

  const [dashboardSchema, agentSchema] = await Promise.all([
    introspect(DASHBOARD_GQL),
    introspect(AGENT_GQL),
  ]);

  if (!dashboardSchema) {
    console.error(
      "✗ Could not introspect dashboard schema. Is the API running?"
    );
    process.exit(1);
  }

  const sections = [];

  sections.push(`# EnterpriseAgentOs Backend API Reference

> **Auto-generated** from live schema introspection by \`scripts/generate-api-doc.js\`.
> Re-generate: \`node scripts/generate-api-doc.js\`
> Source: \`${BASE_URL}\`

## Overview

| Interface | Endpoint | Auth | Purpose |
|-----------|----------|------|---------|
| Dashboard GraphQL | \`/api/dashboard/graphql\` | Session cookie (\`eaos-session\`) | Operator dashboard |
| Agent GraphQL | \`/api/graphql\` | Agent bearer token | Agent pod → backend |
| REST | Various \`/api/*\` paths | Varies (see below) | Webhooks, OAuth, downloads |

---
`);

  // ── Dashboard GraphQL ──────────────────────────────────────────────────

  sections.push(`## Dashboard GraphQL (\`/api/dashboard/graphql\`)

**Auth:** Session cookie \`eaos-session\` (set by \`GET /api/auth/callback/google\`).
`);

  const dashQueries = dashboardSchema.queryType?.fields || [];
  const dashMutations = dashboardSchema.mutationType?.fields || [];
  const dashSubs = dashboardSchema.subscriptionType?.fields || [];

  if (dashQueries.length > 0) {
    sections.push(`### Queries\n`);
    for (const f of dashQueries) sections.push(formatField(f));
  }

  if (dashMutations.length > 0) {
    sections.push(`### Mutations\n`);
    for (const f of dashMutations) sections.push(formatField(f));
  }

  if (dashSubs.length > 0) {
    sections.push(`### Subscriptions\n`);
    for (const f of dashSubs) sections.push(formatField(f));
  }

  // ── Agent GraphQL ──────────────────────────────────────────────────────

  if (agentSchema) {
    sections.push(`## Agent GraphQL (\`/api/graphql\`)

**Auth:** Bearer token (issued per agent pod).
`);

    const agentQueries = agentSchema.queryType?.fields || [];
    const agentMutations = agentSchema.mutationType?.fields || [];

    if (agentQueries.length > 0) {
      sections.push(`### Queries\n`);
      for (const f of agentQueries) sections.push(formatField(f));
    }

    if (agentMutations.length > 0) {
      sections.push(`### Mutations\n`);
      for (const f of agentMutations) sections.push(formatField(f));
    }
  }

  // ── REST ───────────────────────────────────────────────────────────────

  const controllerFiles = findFiles(API_SRC, /Controller\.cs$/);
  const endpointFiles = findFiles(API_SRC, /Endpoint\.cs$/);
  const allRest = [];

  for (const f of [...controllerFiles, ...endpointFiles]) {
    const src = readIfExists(f);
    if (!src) continue;
    allRest.push(...extractRestEndpoints(src, f));
  }

  if (allRest.length > 0) {
    sections.push(`## REST Endpoints\n`);
    for (const ep of allRest) {
      let line = `#### \`${ep.method} ${ep.path}\`\n`;
      if (ep.doc) line += `${ep.doc}\n\n`;
      if (ep.params.length > 0) {
        line += `| Param | Type | Source |\n|-------|------|--------|\n`;
        for (const p of ep.params) {
          line += `| \`${p.name}\` | \`${p.type}\` | ${p.source || "body"} |\n`;
        }
        line += "\n";
      }
      sections.push(line);
    }
  }

  // ── Types ──────────────────────────────────────────────────────────────

  sections.push(`## Types\n`);
  sections.push(formatTypes(dashboardSchema));

  // ── Write ──────────────────────────────────────────────────────────────

  const content = sections.join("\n");
  fs.writeFileSync(OUT, content, "utf-8");
  const lineCount = content.split("\n").length;
  console.log(`✓ Generated ${OUT} (${lineCount} lines)`);
}

generate().catch((err) => {
  console.error("✗ Generation failed:", err);
  process.exit(1);
});
