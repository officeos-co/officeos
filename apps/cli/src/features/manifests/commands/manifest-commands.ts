import { readManifestFile } from "../../../lib/files";
import { requireContext } from "../../../lib/config-store";
import { print, printChanges } from "../../../shell/output";
import {
  applyManifest,
  diffManifest,
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
    for (const error of result.errors) print(`error: ${error.kind}/${error.name}: ${error.message}`);
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

function requireFile(args: string[]): string {
  const index =
    args.indexOf("-f") >= 0 ? args.indexOf("-f") : args.indexOf("--file");
  if (index < 0 || !args[index + 1])
    throw new Error("Missing manifest path. Use `-f <file>`.");
  return args[index + 1];
}
