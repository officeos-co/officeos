export function print(message = ""): void {
  process.stdout.write(`${message}\n`);
}

export function printJson(value: unknown): void {
  print(JSON.stringify(value, null, 2));
}

export function printChanges(changes: Array<{ kind: string; name: string; action: string; message?: string | null }>): void {
  for (const change of changes) {
    const suffix = change.message ? ` - ${change.message}` : "";
    print(`${change.action.padEnd(8)} ${change.kind}/${change.name}${suffix}`);
  }
}
