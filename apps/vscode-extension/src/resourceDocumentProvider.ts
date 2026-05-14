import * as vscode from "vscode";
import { OfficeOsCli } from "./officeOsCli";
import {
  ResourceNode,
  resourceDocumentContent,
  resourceDocumentTitle,
} from "./treeProvider";

export class ResourceDocumentProvider
  implements vscode.TextDocumentContentProvider
{
  private readonly onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();
  private readonly documents = new Map<string, string>();
  readonly onDidChange = this.onDidChangeEmitter.event;

  constructor(private cli: OfficeOsCli) {}

  setCli(cli: OfficeOsCli): void {
    this.cli = cli;
  }

  provideTextDocumentContent(uri: vscode.Uri): string {
    return this.documents.get(uri.toString()) ?? "";
  }

  async openResource(node: ResourceNode): Promise<void> {
    const details = await this.cli.describeResource(node.kind, node.name);
    const uri = this.resourceUri(node);
    this.documents.set(uri.toString(), resourceDocumentContent(node, details));
    this.onDidChangeEmitter.fire(uri);

    const document = await vscode.workspace.openTextDocument(uri);
    await vscode.window.showTextDocument(document, { preview: false });
  }

  async openResourceLogs(node: ResourceNode): Promise<void> {
    const logs = await this.cli.resourceLogs(node.kind, node.name);
    const uri = this.resourceLogsUri(node);
    this.documents.set(uri.toString(), logs);
    this.onDidChangeEmitter.fire(uri);

    const document = await vscode.workspace.openTextDocument(uri);
    await vscode.window.showTextDocument(document, { preview: false });
  }

  private resourceUri(node: ResourceNode): vscode.Uri {
    const title = encodeURIComponent(
      resourceDocumentTitle(node).replace(/[\\/]/g, "-"),
    );
    return vscode.Uri.parse(`officeos:/resources/${node.kind}/${title}.json`);
  }

  private resourceLogsUri(node: ResourceNode): vscode.Uri {
    const title = encodeURIComponent(
      resourceDocumentTitle(node).replace(/[\\/]/g, "-"),
    );
    return vscode.Uri.parse(`officeos:/resources/${node.kind}/${title}.log`);
  }
}
