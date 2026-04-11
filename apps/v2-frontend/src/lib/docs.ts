import fs from "fs";
import path from "path";

const DOCS_DIR = path.resolve(process.cwd(), "..", "..", "docs");

export type DocEntry = {
  slug: string;
  title: string;
  subtitle: string;
};

function extractTitleAndSubtitle(content: string): { title: string; subtitle: string } {
  const lines = content.split("\n");
  let title = "";
  let subtitle = "";
  for (const line of lines) {
    const trimmed = line.trim();
    if (!title && trimmed.startsWith("# ")) {
      title = trimmed.replace(/^#\s+/, "");
    } else if (title && !subtitle && trimmed.startsWith(">")) {
      subtitle = trimmed.replace(/^>\s*/, "");
    }
    if (title && subtitle) break;
  }
  return { title: title || "Untitled", subtitle };
}

export function listDocs(): DocEntry[] {
  const files = fs.readdirSync(DOCS_DIR).filter((f) => f.endsWith(".md")).sort();
  return files.map((file) => {
    const slug = file.replace(/\.md$/, "");
    const content = fs.readFileSync(path.join(DOCS_DIR, file), "utf-8");
    const { title, subtitle } = extractTitleAndSubtitle(content);
    return { slug, title, subtitle };
  });
}

export function getDoc(slug: string): { content: string; title: string; subtitle: string } | null {
  const filePath = path.join(DOCS_DIR, `${slug}.md`);
  if (!fs.existsSync(filePath)) return null;
  const content = fs.readFileSync(filePath, "utf-8");
  const { title, subtitle } = extractTitleAndSubtitle(content);
  return { content, title, subtitle };
}
