import fs from "fs";
import path from "path";
import matter from "gray-matter";

export interface ChangelogEntry {
  title: string;
  date: string;
  version?: string;
  tags?: string[];
  content: string;
}

export function getChangelogEntries(): ChangelogEntry[] {
  const changelogDir = path.join(process.cwd(), "changelog");

  if (!fs.existsSync(changelogDir)) {
    return [];
  }

  const files = fs
    .readdirSync(changelogDir)
    .filter((f) => f.endsWith(".md"))
    .sort()
    .reverse();

  return files.map((filename) => {
    const raw = fs.readFileSync(path.join(changelogDir, filename), "utf-8");
    const { data, content } = matter(raw);
    return {
      title: data.title,
      date: data.date,
      version: data.version,
      tags: data.tags,
      content,
    };
  });
}
