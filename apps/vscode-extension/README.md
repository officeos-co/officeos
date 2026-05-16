# OfficeOS VS Code Extension

Browse OfficeOS control-plane resources from a VS Code Activity Bar view, including agents, routines, credentials, integrations, and providers.

## Development

```bash
npm install
npm run compile
```

Run `../../scripts/dev.bash` from this folder's parent repository to package and
install the extension into the normal VS Code profile, then open the main
workspace. Re-run the script whenever you want VS Code to pick up the latest
extension build.

Open this folder in VS Code and press `F5` only when you specifically need the
Extension Development Host debugger.

The extension uses the `officeos` CLI from `PATH`. In this repository it falls back to:

```bash
bun ../../apps/cli/src/app/main.ts
```

```bash
code --extensionDevelopmentPath="$PWD" --new-window
```
