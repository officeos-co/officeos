#!/usr/bin/env node
import {
  readFileSync,
  writeFileSync,
  existsSync,
  readdirSync,
  mkdirSync,
} from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const skillsDir = resolve(__dirname, "../packages/skills");
const outDir = resolve(__dirname, "../dist/manifests");

mkdirSync(outDir, { recursive: true });

const skills = readdirSync(skillsDir).filter((d) =>
  existsSync(resolve(skillsDir, d, "skill.json")),
);

const manifests = [];

for (const name of skills) {
  const dir = resolve(skillsDir, name);
  const skillJson = JSON.parse(
    readFileSync(resolve(dir, "skill.json"), "utf-8"),
  );

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
    credentialFields: Object.entries(skillJson.credentials || {}).map(
      ([key, field]) => ({
        key,
        label: field.label,
        kind: field.kind,
        required: field.required !== false,
        placeholder: field.placeholder || null,
        help: field.help || null,
      }),
    ),
  };

  manifests.push(manifest);
  console.log(`  ${name}: OK`);
}

writeFileSync(resolve(outDir, "all.json"), JSON.stringify(manifests, null, 2));
console.log(`\nBuilt ${manifests.length} manifests to dist/manifests/all.json`);
