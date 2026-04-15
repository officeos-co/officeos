/**
 * Vault rule guardrails — check notes against vault design conventions.
 *
 * Ported from obsctl's core/guardrails.py.
 *
 * Rules:
 * 1. Folder placement — warn when target folder does not exist in vault
 * 2. Properties system — warn when note has no `categories` in frontmatter
 * 3. Placement rules  — warn on folder ↔ category mismatch (References)
 */

import type { VaultCredentials } from "./types.ts";
import { parseFrontmatter } from "./frontmatter.ts";
import { listNotes } from "./client.ts";

export interface Violation {
  rule_name: string;
  message: string;
  reference: string;
}

/** Extract the parent folder from a vault path, or undefined if root. */
function parentFolder(path: string): string | undefined {
  const idx = path.lastIndexOf("/");
  return idx >= 0 ? path.slice(0, idx) : undefined;
}

/**
 * Run all vault rules against a note path and content.
 *
 * @param path Target vault path (e.g. "References/My Note.md")
 * @param content Full note content including frontmatter
 * @param existingFolders Set of folder paths that currently exist in the vault
 */
export function checkRules(
  path: string,
  content: string,
  existingFolders: Set<string>,
): Violation[] {
  const violations: Violation[] = [];
  const targetFolder = parentFolder(path);
  const [metadata] = parseFrontmatter(content);

  checkFolderPlacement(targetFolder, existingFolders, violations);
  checkMissingCategories(path, metadata, violations);
  checkCategoryMismatch(targetFolder, metadata, violations);

  return violations;
}

function checkFolderPlacement(
  targetFolder: string | undefined,
  existingFolders: Set<string>,
  violations: Violation[],
): void {
  if (!targetFolder) return;
  if (existingFolders.has(targetFolder)) return;

  const folderList =
    existingFolders.size > 0
      ? [...existingFolders].sort().join(", ")
      : "(none)";

  violations.push({
    rule_name: "Folder placement",
    message:
      `Folder "${targetFolder}" does not exist in the vault.\n` +
      `  Existing folders: ${folderList}\n` +
      `  Creating a new folder is usually a mistake — ` +
      `notes go in existing infrastructure folders.`,
    reference: 'vault design rules → "Folders are for infrastructure, not organization"',
  });
}

function checkMissingCategories(
  path: string,
  metadata: Record<string, unknown>,
  violations: Violation[],
): void {
  if ("categories" in metadata) return;

  const basename = path.split("/").pop() ?? path;
  const name = basename.endsWith(".md") ? basename.slice(0, -3) : basename;

  violations.push({
    rule_name: "Properties system",
    message:
      `Note "${name}" has no \`categories\` property in frontmatter.\n` +
      `  Every note should have categories for classification.`,
    reference: 'vault design rules → "Properties are for meaning"',
  });
}

function checkCategoryMismatch(
  targetFolder: string | undefined,
  metadata: Record<string, unknown>,
  violations: Violation[],
): void {
  const categories = metadata.categories;
  if (!Array.isArray(categories) || categories.length === 0) return;

  // Extract category names, stripping wikilink brackets: "[[References]]" → "References"
  const catNames = new Set<string>();
  for (const cat of categories) {
    if (typeof cat === "string") {
      const stripped = cat.trim();
      if (stripped.startsWith("[[") && stripped.endsWith("]]")) {
        catNames.add(stripped.slice(2, -2));
      } else {
        catNames.add(stripped);
      }
    }
  }

  const hasReferencesCat = catNames.has("References");
  const inReferencesFolder =
    !!targetFolder && targetFolder.split("/")[0] === "References";

  if (hasReferencesCat && !inReferencesFolder) {
    violations.push({
      rule_name: "Placement rules",
      message:
        'Note is categorized as "[[References]]" but is being placed at root.\n' +
        "  Reference notes (external world) belong in References/.",
      reference:
        'vault design rules → "Root = your world, References = external world"',
    });
  } else if (!hasReferencesCat && inReferencesFolder) {
    violations.push({
      rule_name: "Placement rules",
      message:
        'Note is in References/ but categories do not include "[[References]]".\n' +
        "  Notes in References/ should be categorized as References.",
      reference:
        'vault design rules → "Root = your world, References = external world"',
    });
  }
}

/** Derive the set of existing folders from the vault's current notes. */
export async function getExistingFolders(
  fetchFn: typeof globalThis.fetch,
  creds: VaultCredentials,
): Promise<Set<string>> {
  const notes = await listNotes(fetchFn, creds);
  const folders = new Set<string>();
  for (const note of notes) {
    const folder = parentFolder(note.path);
    if (folder) folders.add(folder);
  }
  return folders;
}

/** Format a violation for agent-readable output. */
export function formatViolation(v: Violation): string {
  const lines = [`⚠ Rule: ${v.rule_name}`];
  for (const line of v.message.split("\n")) {
    lines.push(`  ${line}`);
  }
  lines.push(`  See: ${v.reference}`);
  return lines.join("\n");
}

/**
 * Run guardrails and return a warnings array.
 *
 * Unlike the Python CLI (which prompts interactively), the skill returns
 * violations as warnings in the response payload — the agent decides what to
 * do.  Pass strict=true to throw instead of returning warnings.
 */
export async function enforceGuardrails(
  fetchFn: typeof globalThis.fetch,
  creds: VaultCredentials,
  path: string,
  content: string,
  strict = false,
): Promise<string[]> {
  const existingFolders = await getExistingFolders(fetchFn, creds);
  const violations = checkRules(path, content, existingFolders);

  if (violations.length === 0) return [];

  const warnings = violations.map(formatViolation);

  if (strict) {
    throw new Error(
      `Vault rule violations (strict mode):\n${warnings.join("\n\n")}`,
    );
  }

  return warnings;
}
