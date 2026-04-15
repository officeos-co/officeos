/**
 * Wikilink extraction and replacement for Obsidian note content.
 *
 * Ported from obsctl's core/wikilinks.py. Handles [[wikilinks]] with
 * headings, display text, and code block protection.
 */

const FENCED_CODE_RE = /```[\s\S]*?```/g;
const INLINE_CODE_RE = /`[^`]+`/g;
const WIKILINK_RE = /\[\[([^\]]+?)\]\]/g;

/**
 * Extract wikilink targets from text, ignoring code blocks and inline code.
 *
 * Handles: [[Note]], [[Note|Display]], [[Note#Heading]], [[Note#Heading|Display]].
 * Ignores wikilinks inside fenced code blocks and inline code.
 * Ignores empty [[]] and whitespace-only wikilinks.
 */
export function extractWikilinks(text: string): string[] {
  // Remove fenced code blocks
  let clean = text.replace(FENCED_CODE_RE, "");
  // Remove inline code
  clean = clean.replace(INLINE_CODE_RE, "");

  const results: string[] = [];
  let match: RegExpExecArray | null;
  const re = new RegExp(WIKILINK_RE.source, "g");

  while ((match = re.exec(clean)) !== null) {
    const inner = match[1];
    // Strip display text (after |)
    const notePart = inner.split("|")[0];
    // Strip heading (after #)
    const noteName = notePart.split("#")[0].trim();
    if (noteName) {
      results.push(noteName);
    }
  }

  return results;
}

/**
 * Replace wikilinks targeting oldName with newName in text.
 *
 * Handles all variants: [[Old]], [[Old|display]], [[Old#heading]], [[Old#heading|display]].
 * Respects code blocks: wikilinks inside fenced or inline code are NOT modified.
 * Matching is case-insensitive. The newName's exact casing is always used.
 */
export function replaceWikilinks(
  text: string,
  oldName: string,
  newName: string,
): { text: string; count: number } {
  if (!text) return { text: "", count: 0 };

  // Protect code blocks with placeholders
  const protectedRegions: string[] = [];
  const PLACEHOLDER_PREFIX = "\x00PROTECTED_";

  function protect(match: string): string {
    const idx = protectedRegions.length;
    protectedRegions.push(match);
    return `${PLACEHOLDER_PREFIX}${idx}\x00`;
  }

  let working = text.replace(FENCED_CODE_RE, (m) => protect(m));
  working = working.replace(INLINE_CODE_RE, (m) => protect(m));

  // Build regex for [[oldName...]] with optional #heading and |display
  const escaped = oldName.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const pattern = new RegExp(
    `\\[\\[(${escaped})((?:#[^\\]|]*)?)((?:\\|[^\\]]*)?)\\]\\]`,
    "gi",
  );

  let count = 0;
  working = working.replace(pattern, (_match, _name, heading, display) => {
    count++;
    return `[[${newName}${heading}${display}]]`;
  });

  // Restore protected regions (reverse order for safety)
  for (let i = protectedRegions.length - 1; i >= 0; i--) {
    working = working.replace(`${PLACEHOLDER_PREFIX}${i}\x00`, protectedRegions[i]);
  }

  return { text: working, count };
}
