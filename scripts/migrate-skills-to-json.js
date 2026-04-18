#!/usr/bin/env node
/**
 * Migrates skill metadata from skill.ts into skill.json, README.md, CHANGELOG.md, and LICENSE.
 * Run: node scripts/migrate-skills-to-json.js
 */
const fs = require("fs");
const path = require("path");

const SKILLS_DIR = path.join(__dirname, "..", "packages", "skills");
const CATEGORIES = JSON.parse(
  fs.readFileSync(path.join(__dirname, "skill-categories.json"), "utf-8")
);

const YEAR = new Date().getFullYear();
const TODAY = new Date().toISOString().split("T")[0];

const MIT_LICENSE = `MIT License

Copyright (c) ${YEAR} OfficeOS

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

function extractString(src, key) {
  // Match:  key: "value"  or  key: 'value'  or  key:\n    "value"
  // Also handles multi-line string concatenation with description
  const patterns = [
    // key: "value" on same line
    new RegExp(`${key}:\\s*"([^"]*)"`, "s"),
    // key: 'value' on same line
    new RegExp(`${key}:\\s*'([^']*)'`, "s"),
    // key:\n    "value" (next line)
    new RegExp(`${key}:\\s*\\n\\s*"([^"]*)"`, "s"),
    // key:\n    'value' (next line)
    new RegExp(`${key}:\\s*\\n\\s*'([^']*)'`, "s"),
    // template literal
    new RegExp(`${key}:\\s*\`([^\`]*)\``, "s"),
  ];
  for (const re of patterns) {
    const m = src.match(re);
    if (m) return m[1];
  }
  return null;
}

function extractLogo(src) {
  // Logo is always an SVG string, can be very long
  const m = src.match(/logo:\s*["'`](<svg[\s\S]*?<\/svg>)["'`]/);
  return m ? m[1] : null;
}

function extractDescription(src) {
  // Try: description: "..." or description:\n    "..."
  // Handle the common pattern: description:\n    "long string",
  const patterns = [
    /description:\s*\n\s*"((?:[^"\\]|\\.)*)"/s,
    /description:\s*"((?:[^"\\]|\\.)*)"/s,
    /description:\s*\n\s*'((?:[^'\\]|\\.)*)'/s,
    /description:\s*'((?:[^'\\]|\\.)*)'/s,
    /description:\s*`((?:[^`\\]|\\.)*)`/s,
  ];
  for (const re of patterns) {
    const m = src.match(re);
    if (m) return m[1];
  }
  return null;
}

function extractCredentials(src) {
  // Find credentials: { ... } using brace depth counting
  const credStart = src.match(/credentials:\s*\{/);
  if (!credStart) return {};

  const startIdx = credStart.index + credStart[0].length - 1;
  let depth = 1;
  let i = startIdx + 1;
  while (i < src.length && depth > 0) {
    if (src[i] === "{") depth++;
    else if (src[i] === "}") depth--;
    i++;
  }
  const credBlock = src.substring(startIdx, i);

  // If empty credentials: {}
  if (credBlock.trim() === "{}") return {};

  // Parse individual credential entries using a simpler approach
  // Match: key: { label: "...", kind: "...", placeholder: "...", help: "..." }
  const credentials = {};
  const entryRegex = /(\w+):\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}/g;

  // Get the inner content (strip outer braces)
  const inner = credBlock.slice(1, -1);

  let match;
  while ((match = entryRegex.exec(inner)) !== null) {
    const credName = match[1];
    const credBody = match[2];

    const cred = {};
    const labelM = credBody.match(/label:\s*"((?:[^"\\]|\\.)*)"/);
    if (labelM) cred.label = labelM[1];

    const kindM = credBody.match(/kind:\s*"((?:[^"\\]|\\.)*)"/);
    if (kindM) cred.kind = kindM[1];

    const placeholderM = credBody.match(/placeholder:\s*"((?:[^"\\]|\\.)*)"/);
    if (placeholderM) cred.placeholder = placeholderM[1];

    const helpM = credBody.match(/help:\s*"((?:[^"\\]|\\.)*)"/);
    if (helpM) cred.help = helpM[1];

    if (Object.keys(cred).length > 0) {
      credentials[credName] = cred;
    }
  }

  return credentials;
}

function generateReadme(name, title, description, categories, credentials) {
  const credSection = Object.keys(credentials).length > 0
    ? `## Credentials\n\n| Key | Label | Kind |\n|-----|-------|------|\n${Object.entries(credentials).map(([k, v]) => `| \`${k}\` | ${v.label || k} | ${v.kind || "text"} |`).join("\n")}\n`
    : "";

  return `# ${title}

${description}

## Categories

${categories.map(c => `- ${c}`).join("\n")}

${credSection}
## Installation

This skill is included in [EnterpriseAgentOS](https://officeos.co) and requires no separate installation.

## License

MIT
`;
}

function generateChangelog(title) {
  return `# Changelog

## 1.0.0 (${TODAY})

- Initial release of ${title} skill
`;
}

// ── Main ──────────────────────────────────────────────────────────────────────

const dirs = fs.readdirSync(SKILLS_DIR).filter((d) => {
  const p = path.join(SKILLS_DIR, d);
  return fs.statSync(p).isDirectory() && fs.existsSync(path.join(p, "skill.ts"));
});

let count = 0;
const errors = [];

for (const dir of dirs) {
  const skillDir = path.join(SKILLS_DIR, dir);
  const skillTs = fs.readFileSync(path.join(skillDir, "skill.ts"), "utf-8");

  // Only look at the defineSkill block for metadata extraction
  const defineStart = skillTs.indexOf("defineSkill(");
  if (defineStart === -1) {
    errors.push(`${dir}: no defineSkill() found`);
    continue;
  }
  const defineBlock = skillTs.substring(defineStart);

  const name = extractString(defineBlock, "name");
  const title = extractString(defineBlock, "title");
  const logo = extractLogo(defineBlock);
  const description = extractDescription(defineBlock);
  const credentials = extractCredentials(defineBlock);
  const categories = CATEGORIES[dir] || [];

  if (!name || !title) {
    errors.push(`${dir}: could not extract name or title`);
    continue;
  }

  if (!description) {
    errors.push(`${dir}: could not extract description`);
  }

  // skill.json
  const manifest = {
    name,
    title,
    version: "1.0.0",
    description: description || "",
    logo: logo || "",
    categories,
    keywords: [],
    author: { name: "OfficeOS Team", url: "https://officeos.co" },
    license: "MIT",
    repository: `https://github.com/officeos-co/skill-${name}`,
    contributors: [
      { name: "Harro Krog", url: "https://github.com/HarKro753" },
    ],
    credentials,
  };

  fs.writeFileSync(
    path.join(skillDir, "skill.json"),
    JSON.stringify(manifest, null, 2) + "\n"
  );

  // README.md
  if (!fs.existsSync(path.join(skillDir, "README.md"))) {
    fs.writeFileSync(
      path.join(skillDir, "README.md"),
      generateReadme(name, title, description || "", categories, credentials)
    );
  }

  // CHANGELOG.md
  if (!fs.existsSync(path.join(skillDir, "CHANGELOG.md"))) {
    fs.writeFileSync(
      path.join(skillDir, "CHANGELOG.md"),
      generateChangelog(title)
    );
  }

  // LICENSE
  if (!fs.existsSync(path.join(skillDir, "LICENSE"))) {
    fs.writeFileSync(path.join(skillDir, "LICENSE"), MIT_LICENSE);
  }

  count++;
  console.log(`✓ ${dir} → skill.json, README.md, CHANGELOG.md, LICENSE`);
}

console.log(`\nMigrated ${count} skills.`);
if (errors.length) {
  console.log(`\nWarnings:`);
  errors.forEach((e) => console.log(`  ⚠ ${e}`));
}
