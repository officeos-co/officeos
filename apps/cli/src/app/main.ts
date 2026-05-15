#!/usr/bin/env bun

import { loginCommand, whoamiCommand } from "../features/auth";
import {
  configCommand,
  deleteCommand,
  describeCommand,
  getCommand,
  logsCommand,
  modelsCommand,
  providerCommand,
  providersCommand,
  runCommand,
  sendCommand,
} from "../features/control-plane";
import {
  applyCommand,
  diffCommand,
  validateCommand,
} from "../features/manifests";
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
    case "get":
      await getCommand(args);
      break;
    case "describe":
      await describeCommand(args);
      break;
    case "delete":
      await deleteCommand(args);
      break;
    case "run":
      await runCommand(args);
      break;
    case "send":
      await sendCommand(args);
      break;
    case "logs":
      await logsCommand(args);
      break;
    case "models":
      await modelsCommand(args);
      break;
    case "providers":
      await providersCommand(args);
      break;
    case "provider":
      await providerCommand(args);
      break;
    case "config":
      await configCommand(args);
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
  print("Usage: officeos <command>");
  print("");
  print("Commands:");
  print("  login [--api-url <url>] [--context <name>]");
  print("  whoami");
  print("  validate -f <file>");
  print("  diff -f <file>");
  print("  apply -f <file>");
  print("  get <kind|kind/name> [-o json|yaml|name]");
  print("  describe <kind/name>");
  print("  delete <kind> <name> | delete --all");
  print("  run <agent> --task <text>");
  print("  send <agent> --message <text>");
  print("  logs <kind/name> [--tail <n>] [--since <duration>] [--type <type>] [--severity <level>]");
  print("  models [-o json|yaml|name]");
  print("  providers [-o json|yaml|name]");
  print("  provider auth codex [--no-browser]");
  print("  config get-contexts|current-context|use-context|set-context");
}
