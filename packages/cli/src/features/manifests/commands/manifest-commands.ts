import { readManifestFile } from "../../../lib/files";
import { requireContext } from "../../../lib/config-store";
import { print, printChanges } from "../../../shell/output";
import {
  applyManifest,
  diffManifest,
  exportAgent,
  validateManifest,
} from "../api/manifests-api";

export async function validateCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const manifest = await readManifestFile(requireFile(args));
  const result = await validateManifest(
    context.apiUrl,
    context.token,
    manifest,
  );
  if (!result.valid) {
    for (const error of result.errors) print(`error: ${error}`);
    process.exitCode = 1;
    return;
  }
  for (const resource of result.resources) print(`valid ${resource}`);
}

export async function diffCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const manifest = await readManifestFile(requireFile(args));
  const result = await diffManifest(context.apiUrl, context.token, manifest);
  printChanges(result.changes);
}

export async function applyCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const manifest = await readManifestFile(requireFile(args));
  const result = await applyManifest(context.apiUrl, context.token, manifest);
  printChanges(result.changes);
}

export async function exportCommand(args: string[]): Promise<void> {
  if (args[0] !== "agent" || !args[1]) {
    throw new Error("Usage: eaos export agent <name>");
  }
  const context = await requireContext();
  print(await exportAgent(context.apiUrl, context.token, args[1]));
}

function requireFile(args: string[]): string {
  const index =
    args.indexOf("-f") >= 0 ? args.indexOf("-f") : args.indexOf("--file");
  if (index < 0 || !args[index + 1])
    throw new Error("Missing manifest path. Use `-f <file>`.");
  return args[index + 1];
}
