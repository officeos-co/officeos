#!/usr/bin/env bun

import { loginCommand, whoamiCommand } from "../features/auth";
import { applyCommand, diffCommand, exportCommand, validateCommand } from "../features/manifests";
import { print } from "../shell/output";

const [command, ...args] = process.argv.slice(2);

try {
  switch (command) {
    case "login":
      await loginCommand(args);
      break;
    case "whoami":
      await whoamiCommand();
      break;
    case "validate":
      await validateCommand(args);
      break;
    case "diff":
      await diffCommand(args);
      break;
    case "apply":
      await applyCommand(args);
      break;
    case "export":
      await exportCommand(args);
      break;
    case "help":
    case undefined:
      help();
      break;
    default:
      throw new Error(`Unknown command '${command}'.`);
  }
} catch (error) {
  process.stderr.write(`error: ${(error as Error).message}\n`);
  process.exitCode = 1;
}

function help(): void {
  print("Usage: eaos <command>");
  print("");
  print("Commands:");
  print("  login [--api-url <url>] [--context <name>]");
  print("  whoami");
  print("  validate -f <file>");
  print("  diff -f <file>");
  print("  apply -f <file>");
  print("  export agent <name>");
}
