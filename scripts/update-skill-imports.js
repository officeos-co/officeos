#!/usr/bin/env node
/**
 * Updates each skill.ts to import metadata from skill.json
 * and spread it into defineSkill() instead of inline fields.
 *
 * Replaces: name, title, logo, description, credentials, doc
 * With: ...manifest, doc,
 * Keeps: actions (and everything after)
 */
import { readdirSync, readFileSync, writeFileSync, existsSync } from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const skillsDir = resolve(__dirname, "../packages/skills");

const skillDirs = readdirSync(skillsDir).filter((d) =>
  existsSync(resolve(skillsDir, d, "skill.ts")) &&
  existsSync(resolve(skillsDir, d, "skill.json"))
);

let updated = 0;
let skipped = 0;

for (const name of skillDirs) {
  const filePath = resolve(skillsDir, name, "skill.ts");
  let src = readFileSync(filePath, "utf-8");

  // Skip if already imports skill.json
  if (src.includes("skill.json")) {
    console.log(`SKIP (already has skill.json): ${name}`);
    skipped++;
    continue;
  }

  // 1. Add manifest import after the SDK import line
  const sdkImportRegex = /^(import\s+\{[^}]*\}\s+from\s+"@harro\/skill-sdk";?\s*\n)/m;
  if (!sdkImportRegex.test(src)) {
    console.log(`SKIP (no SDK import found): ${name}`);
    skipped++;
    continue;
  }
  src = src.replace(sdkImportRegex, `$1import manifest from "./skill.json" with { type: "json" };\n`);

  // 2. Replace everything between `defineSkill({` and `actions` with `...manifest, doc,`
  const defineStart = src.indexOf("defineSkill({");
  if (defineStart === -1) {
    console.log(`SKIP (no defineSkill found): ${name}`);
    skipped++;
    continue;
  }

  const afterBrace = defineStart + "defineSkill({".length;
  const rest = src.slice(afterBrace);

  // Find `actions:` or `actions :`  at 2-space indent (top-level field inside defineSkill)
  const actionsMatch = rest.match(/\n(\s*actions\s*:\s*)/);
  if (!actionsMatch) {
    console.log(`SKIP (no actions field found): ${name}`);
    skipped++;
    continue;
  }

  const actionsIndex = rest.indexOf(actionsMatch[0]);
  const afterActions = rest.slice(actionsIndex);

  src = src.slice(0, afterBrace) + "\n  ...manifest,\n  doc," + afterActions;

  // 3. Remove the `import doc from "./SKILL.md"` if it's now redundant — actually we still need it
  // doc is still imported, just referenced in defineSkill. Keep it.

  writeFileSync(filePath, src);
  console.log(`UPDATED: ${name}`);
  updated++;
}

console.log(`\nDone. Updated: ${updated}, Skipped: ${skipped}`);
