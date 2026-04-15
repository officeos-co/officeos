/**
 * YAML frontmatter parsing and manipulation.
 *
 * Ported from obsctl's core/frontmatter.py. Handles the `---` delimited
 * YAML block at the top of Obsidian notes without external dependencies.
 */

/**
 * Parse YAML frontmatter from note content.
 * Returns [metadata, body] where metadata is a key-value record
 * and body is the content after the frontmatter block.
 */
export function parseFrontmatter(
  content: string,
): [Record<string, unknown>, string] {
  if (!content || !content.startsWith("---")) {
    return [{}, content ?? ""];
  }

  // Find the closing ---
  const endIndex = content.indexOf("\n---", 3);
  if (endIndex === -1) {
    return [{}, content];
  }

  const yamlBlock = content.slice(4, endIndex).trim();
  const body = content.slice(endIndex + 4).replace(/^\n/, "");

  if (!yamlBlock) {
    return [{}, content];
  }

  try {
    const metadata = parseSimpleYaml(yamlBlock);
    if (Object.keys(metadata).length === 0) {
      return [{}, content];
    }
    return [metadata, body];
  } catch {
    return [{}, content];
  }
}

/**
 * Build a complete note string from metadata dict and body.
 * If metadata is empty, returns just the body.
 */
export function buildNote(
  metadata: Record<string, unknown>,
  body: string,
): string {
  if (!metadata || Object.keys(metadata).length === 0) {
    return body;
  }

  const yaml = serializeSimpleYaml(metadata);
  return `---\n${yaml}\n---\n${body}`;
}

/**
 * Set a property in the note's frontmatter. Creates frontmatter if needed.
 */
export function setProperty(
  content: string,
  key: string,
  value: unknown,
): string {
  const [metadata, body] = parseFrontmatter(content);
  metadata[key] = value;
  return buildNote(metadata, body);
}

/**
 * Remove a property from the note's frontmatter. No-op if not present.
 */
export function removeProperty(content: string, key: string): string {
  const [metadata, body] = parseFrontmatter(content);
  if (Object.keys(metadata).length === 0) return content;
  if (!(key in metadata)) return buildNote(metadata, body);
  delete metadata[key];
  if (Object.keys(metadata).length === 0) return body;
  return buildNote(metadata, body);
}

// ---------------------------------------------------------------------------
// Simple YAML parser (no dependencies)
// ---------------------------------------------------------------------------

/**
 * Parse a simple YAML block into a record. Handles:
 * - key: value (string, number, boolean)
 * - key: [a, b, c] (inline arrays)
 * - key:\n  - item1\n  - item2 (block arrays)
 * - Quoted strings
 *
 * This is intentionally minimal — Obsidian frontmatter is simple YAML.
 */
function parseSimpleYaml(yaml: string): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  const lines = yaml.split("\n");
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];
    const trimmed = line.trim();

    // Skip empty lines and comments
    if (!trimmed || trimmed.startsWith("#")) {
      i++;
      continue;
    }

    const colonIdx = trimmed.indexOf(":");
    if (colonIdx === -1) {
      i++;
      continue;
    }

    const key = trimmed.slice(0, colonIdx).trim();
    const rawValue = trimmed.slice(colonIdx + 1).trim();

    if (rawValue === "" || rawValue === "|" || rawValue === ">") {
      // Check for block list (- item) on next lines
      const items: string[] = [];
      let j = i + 1;
      while (j < lines.length) {
        const nextLine = lines[j];
        const nextTrimmed = nextLine.trim();
        if (nextTrimmed.startsWith("- ")) {
          items.push(parseYamlValue(nextTrimmed.slice(2).trim()));
          j++;
        } else if (nextTrimmed === "" || nextTrimmed.startsWith("#")) {
          j++;
        } else if (nextLine.startsWith("  ") || nextLine.startsWith("\t")) {
          // Continuation of a multi-line value — skip for now
          j++;
        } else {
          break;
        }
      }
      if (items.length > 0) {
        result[key] = items;
        i = j;
      } else {
        result[key] = "";
        i++;
      }
    } else if (rawValue.startsWith("[") && rawValue.endsWith("]")) {
      // Inline array
      const inner = rawValue.slice(1, -1).trim();
      if (inner === "") {
        result[key] = [];
      } else {
        result[key] = inner.split(",").map((s) => parseYamlValue(s.trim()));
      }
      i++;
    } else {
      result[key] = parseYamlValue(rawValue);
      i++;
    }
  }

  return result;
}

/** Parse a single YAML scalar value. */
function parseYamlValue(raw: string): unknown {
  if (raw === "true" || raw === "True" || raw === "TRUE") return true;
  if (raw === "false" || raw === "False" || raw === "FALSE") return false;
  if (raw === "null" || raw === "Null" || raw === "NULL" || raw === "~")
    return null;

  // Quoted string
  if (
    (raw.startsWith('"') && raw.endsWith('"')) ||
    (raw.startsWith("'") && raw.endsWith("'"))
  ) {
    return raw.slice(1, -1);
  }

  // Number
  if (/^-?\d+$/.test(raw)) return parseInt(raw, 10);
  if (/^-?\d+\.\d+$/.test(raw)) return parseFloat(raw);

  return raw;
}

/**
 * Serialize a record to simple YAML. Handles strings, numbers, booleans,
 * arrays (as block lists), and null.
 */
function serializeSimpleYaml(data: Record<string, unknown>): string {
  const lines: string[] = [];

  for (const [key, value] of Object.entries(data)) {
    if (Array.isArray(value)) {
      if (value.length === 0) {
        lines.push(`${key}: []`);
      } else {
        lines.push(`${key}:`);
        for (const item of value) {
          lines.push(`  - ${serializeYamlValue(item)}`);
        }
      }
    } else {
      lines.push(`${key}: ${serializeYamlValue(value)}`);
    }
  }

  return lines.join("\n");
}

/** Serialize a single YAML scalar value. */
function serializeYamlValue(value: unknown): string {
  if (value === null || value === undefined) return "null";
  if (typeof value === "boolean") return value ? "true" : "false";
  if (typeof value === "number") return String(value);
  if (typeof value === "string") {
    // Quote if the string contains special characters
    if (
      value.includes(":") ||
      value.includes("#") ||
      value.includes("[") ||
      value.includes("]") ||
      value.includes("{") ||
      value.includes("}") ||
      value.includes(",") ||
      value.includes("&") ||
      value.includes("*") ||
      value.includes("!") ||
      value.includes("|") ||
      value.includes(">") ||
      value.includes("'") ||
      value.includes('"') ||
      value.includes("%") ||
      value.includes("@") ||
      value.startsWith(" ") ||
      value.endsWith(" ")
    ) {
      return `"${value.replace(/\\/g, "\\\\").replace(/"/g, '\\"')}"`;
    }
    return value;
  }
  return String(value);
}
