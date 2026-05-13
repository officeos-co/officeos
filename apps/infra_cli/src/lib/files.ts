import { readFile } from "node:fs/promises";

export async function readManifestFile(path: string): Promise<string> {
  if (!path) throw new Error("Missing manifest path. Use `-f <file>`.");
  return await readFile(path, "utf8");
}
