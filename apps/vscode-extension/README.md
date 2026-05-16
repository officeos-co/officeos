# OfficeOS VS Code Extension

Browse OfficeOS control-plane resources from a VS Code Activity Bar view, including agents, routines, credentials, integrations, and providers.

## Development

```bash
npm install
npm run compile
```

Open this folder in VS Code and press `F5` to launch the Extension Development Host.

The extension uses the `officeos` CLI from `PATH`. In this repository it falls back to:

```bash
bun ../../apps/cli/src/app/main.ts
```

```bash
code --extensionDevelopmentPath="$PWD" --new-window
```
