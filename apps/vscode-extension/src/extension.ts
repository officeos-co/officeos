import * as vscode from "vscode";
import { OfficeOsCli } from "./officeOsCli";
import { ResourceDocumentProvider } from "./resourceDocumentProvider";
import {
  OfficeOsTreeProvider,
  ResourceNode,
  resourceDeleteName,
  resourceRef,
} from "./treeProvider";

export function activate(context: vscode.ExtensionContext): void {
  let cli = createCli(context);
  const treeProvider = new OfficeOsTreeProvider(cli, context.extensionPath);
  const documentProvider = new ResourceDocumentProvider(cli);
  let terminal: vscode.Terminal | undefined;

  context.subscriptions.push(
    vscode.window.registerTreeDataProvider("officeos.resources", treeProvider),
    vscode.workspace.registerTextDocumentContentProvider(
      "officeos",
      documentProvider,
    ),
    vscode.commands.registerCommand("officeos.refresh", () =>
      treeProvider.refresh(),
    ),
    vscode.commands.registerCommand("officeos.reloadResource", () =>
      treeProvider.refresh(),
    ),
    vscode.commands.registerCommand("officeos.login", async () => {
      await runWithErrors(async () => {
        const command = await cli.terminalCommand(["login"]);
        terminal ??= vscode.window.createTerminal({ name: "OfficeOS" });
        terminal.sendText(command);
        terminal.show();
      });
    }),
    vscode.commands.registerCommand("officeos.currentContext", async () => {
      await runWithErrors(async () => {
        const currentContext = await cli.currentContext();
        void vscode.window.showInformationMessage(
          currentContext
            ? `OfficeOS context: ${currentContext}`
            : "OfficeOS has no current context.",
        );
      });
    }),
    vscode.commands.registerCommand("officeos.useContext", async () => {
      await runWithErrors(async () => {
        const contexts = await cli.getContexts();
        if (contexts.length === 0) {
          void vscode.window.showWarningMessage(
            "OfficeOS has no configured contexts.",
          );
          return;
        }

        const selected = await vscode.window.showQuickPick(contexts, {
          placeHolder: "Select OfficeOS context",
        });
        if (!selected) return;

        await cli.useContext(selected);
        treeProvider.refresh();
        void vscode.window.showInformationMessage(
          `OfficeOS context set to ${selected}.`,
        );
      });
    }),
    vscode.commands.registerCommand(
      "officeos.showResourceLogs",
      async (node?: ResourceNode) => {
        await runWithErrors(async () => {
          if (!node) {
            throw new Error("Select an OfficeOS resource first.");
          }

          await documentProvider.openResourceLogs(node);
        });
      },
    ),
    vscode.commands.registerCommand(
      "officeos.describeResource",
      async (node?: ResourceNode) => {
        await runWithErrors(async () => {
          if (!node) {
            throw new Error("Select an OfficeOS resource first.");
          }

          await documentProvider.openResource(node);
        });
      },
    ),
    vscode.commands.registerCommand(
      "officeos.copyResourceName",
      async (node?: ResourceNode) => {
        await runWithErrors(async () => {
          if (!node) {
            throw new Error("Select an OfficeOS resource first.");
          }

          if (!node.name) {
            throw new Error("OfficeOS resource has no name to copy.");
          }

          await vscode.env.clipboard.writeText(node.name);
        });
      },
    ),
    vscode.commands.registerCommand(
      "officeos.deleteResource",
      async (node?: ResourceNode) => {
        await runWithErrors(async () => {
          if (!node) {
            throw new Error("Select an OfficeOS resource first.");
          }

          const deleteName = resourceDeleteName(node);
          if (!deleteName) {
            throw new Error("OfficeOS resource has no name to delete.");
          }

          const ref = resourceRef(node);
          const confirmed = await vscode.window.showWarningMessage(
            `Delete ${ref}?`,
            { modal: true },
            "Delete",
          );
          if (confirmed !== "Delete") return;

          await cli.deleteResource(node.kind, deleteName);
          treeProvider.refresh();
          void vscode.window.showInformationMessage(`Deleted ${ref}.`);
        });
      },
    ),
    vscode.workspace.onDidChangeConfiguration((event) => {
      if (!event.affectsConfiguration("officeos.cliPath")) return;
      cli = createCli(context);
      treeProvider.setCli(cli);
      documentProvider.setCli(cli);
    }),
    {
      dispose: () => terminal?.dispose(),
    },
  );
}

export function deactivate(): void {
  // VS Code disposes registered subscriptions for us.
}

function createCli(context: vscode.ExtensionContext): OfficeOsCli {
  const configuredCliPath = vscode.workspace
    .getConfiguration("officeos")
    .get<string>("cliPath");
  return new OfficeOsCli({
    extensionPath: context.extensionPath,
    configuredCliPath,
  });
}

async function runWithErrors(work: () => Promise<void>): Promise<void> {
  try {
    await work();
  } catch (error) {
    void vscode.window.showErrorMessage(
      error instanceof Error ? error.message : String(error),
    );
  }
}
