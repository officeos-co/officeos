import * as vscode from "vscode";
import {
  OfficeOsCli,
  ResourceKind,
  ResourceKinds,
  resourceId,
  resourceName,
  singularKind,
} from "./officeOsCli";

interface ResourceCategory {
  readonly label: string;
  readonly cliKind: ResourceKind;
  readonly icon: string;
}

const Categories: readonly ResourceCategory[] = [
  { label: "Agents", cliKind: "agents", icon: "hubot" },
  { label: "Runs", cliKind: "runs", icon: "play-circle" },
  { label: "Channels", cliKind: "channels", icon: "broadcast" },
  { label: "Routines", cliKind: "routines", icon: "clock" },
  { label: "Memory Stores", cliKind: "memorystores", icon: "database" },
  { label: "Engines", cliKind: "engines", icon: "server-process" },
  { label: "Providers", cliKind: "providers", icon: "plug" },
  { label: "Models", cliKind: "models", icon: "symbol-method" },
] as const;

export type OfficeOsNode =
  | CategoryNode
  | ResourceNode
  | FieldNode
  | MessageNode;

export class CategoryNode {
  readonly nodeType = "category";

  constructor(readonly category: ResourceCategory) {}
}

export class ResourceNode {
  readonly nodeType = "resource";
  readonly kind: ResourceKind;
  readonly name: string;

  constructor(
    readonly category: ResourceCategory,
    readonly value: unknown,
  ) {
    this.kind = category.cliKind;
    this.name = readResourceName(value);
  }
}

export class FieldNode {
  readonly nodeType = "field";

  constructor(
    readonly key: string,
    readonly value: unknown,
  ) {}
}

class MessageNode {
  readonly nodeType = "message";

  constructor(readonly message: string) {}
}

export class OfficeOsTreeProvider implements vscode.TreeDataProvider<OfficeOsNode> {
  private readonly onDidChangeTreeDataEmitter = new vscode.EventEmitter<
    OfficeOsNode | undefined
  >();
  readonly onDidChangeTreeData = this.onDidChangeTreeDataEmitter.event;

  constructor(private cli: OfficeOsCli) {}

  setCli(cli: OfficeOsCli): void {
    this.cli = cli;
    this.refresh();
  }

  refresh(node?: OfficeOsNode): void {
    this.onDidChangeTreeDataEmitter.fire(node);
  }

  getTreeItem(element: OfficeOsNode): vscode.TreeItem {
    switch (element.nodeType) {
      case "category":
        return this.categoryTreeItem(element);
      case "resource":
        return this.resourceTreeItem(element);
      case "field":
        return this.fieldTreeItem(element);
      case "message":
        return this.messageTreeItem(element);
    }
  }

  async getChildren(element?: OfficeOsNode): Promise<OfficeOsNode[]> {
    if (!element) {
      return Categories.map((category) => new CategoryNode(category));
    }

    if (element.nodeType === "category") {
      return await this.loadCategory(element.category);
    }

    if (element.nodeType === "resource") {
      return valueToFields(element.value);
    }

    if (element.nodeType === "field") {
      return valueToFields(element.value);
    }

    return [];
  }

  private async loadCategory(
    category: ResourceCategory,
  ): Promise<OfficeOsNode[]> {
    try {
      const resources = await this.cli.listResources(category.cliKind);
      if (resources.length === 0) {
        return [new MessageNode(`No ${category.label.toLowerCase()} found`)];
      }

      return resources.map((resource) => new ResourceNode(category, resource));
    } catch (error) {
      void vscode.window.showErrorMessage(
        `OfficeOS ${category.label} failed to load: ${errorMessage(error)}`,
      );
      return [new MessageNode("Unable to load resources")];
    }
  }

  private categoryTreeItem(node: CategoryNode): vscode.TreeItem {
    const item = new vscode.TreeItem(
      node.category.label,
      vscode.TreeItemCollapsibleState.Collapsed,
    );
    item.contextValue = "officeosCategory";
    item.iconPath = new vscode.ThemeIcon(node.category.icon);
    item.tooltip = `${node.category.label} from officeos ${node.category.cliKind}`;
    return item;
  }

  private resourceTreeItem(node: ResourceNode): vscode.TreeItem {
    const label = node.name || "(unnamed)";
    const item = new vscode.TreeItem(
      label,
      vscode.TreeItemCollapsibleState.Collapsed,
    );
    item.contextValue = "officeosResource";
    item.iconPath = new vscode.ThemeIcon(node.category.icon);
    item.description = resourceDescription(node.value);
    item.tooltip = `${node.kind}/${label}`;
    item.command = {
      command: "officeos.describeResource",
      title: "Describe Resource",
      arguments: [node],
    };
    return item;
  }

  private fieldTreeItem(node: FieldNode): vscode.TreeItem {
    const hasChildren = isExpandable(node.value);
    const item = new vscode.TreeItem(
      node.key,
      hasChildren
        ? vscode.TreeItemCollapsibleState.Collapsed
        : vscode.TreeItemCollapsibleState.None,
    );
    item.contextValue = "officeosField";
    item.description = fieldDescription(node.value);
    item.tooltip = fieldTooltip(node.value);
    item.iconPath = new vscode.ThemeIcon(
      hasChildren ? "symbol-object" : "symbol-field",
    );
    return item;
  }

  private messageTreeItem(node: MessageNode): vscode.TreeItem {
    const item = new vscode.TreeItem(
      node.message,
      vscode.TreeItemCollapsibleState.None,
    );
    item.contextValue = "officeosMessage";
    item.iconPath = new vscode.ThemeIcon("info");
    return item;
  }
}

export function resourceRef(node: ResourceNode): string {
  return `${node.kind}/${node.name}`;
}

export function resourceDocumentTitle(node: ResourceNode): string {
  const kind = resourceKind(node.value, node.kind);
  return `${kind} ${node.name || resourceId(node.value) || "(unnamed)"}`;
}

export function resourceDocumentContent(
  node: ResourceNode,
  details: unknown,
): string {
  return JSON.stringify(
    {
      kind: resourceKind(details, node.kind),
      name: readResourceName(details) || node.name,
      resource: details,
    },
    null,
    2,
  );
}

function valueToFields(value: unknown): OfficeOsNode[] {
  if (Array.isArray(value)) {
    return value.map((entry, index) => new FieldNode(`[${index}]`, entry));
  }

  if (!value || typeof value !== "object") {
    return [];
  }

  return Object.entries(value as Record<string, unknown>).map(
    ([key, entry]) => new FieldNode(key, entry),
  );
}

function readResourceName(value: unknown): string {
  if (!value || typeof value !== "object") {
    return String(value);
  }

  const direct = resourceName(value);
  if (direct) return direct;

  const record = value as Record<string, unknown>;
  const metadata = record.metadata;
  if (metadata && typeof metadata === "object") {
    const name = (metadata as Record<string, unknown>).name;
    if (typeof name === "string") return name;
  }

  return "";
}

function resourceKind(value: unknown, fallback: string): string {
  if (value && typeof value === "object") {
    const kind = (value as Record<string, unknown>).kind;
    if (typeof kind === "string" && kind.length > 0) {
      return kind;
    }
  }

  return ResourceKinds.includes(fallback as ResourceKind)
    ? singularKind(fallback)
    : fallback;
}

function resourceDescription(value: unknown): string | undefined {
  if (!value || typeof value !== "object") {
    return undefined;
  }

  const record = value as Record<string, unknown>;
  for (const key of [
    "phase",
    "status",
    "enabled",
    "type",
    "provider",
    "model",
    "engine",
  ]) {
    const valueForKey = record[key];
    if (
      valueForKey !== undefined &&
      valueForKey !== null &&
      String(valueForKey).length > 0
    ) {
      return `${key}: ${String(valueForKey)}`;
    }
  }

  return undefined;
}

function fieldDescription(value: unknown): string | undefined {
  if (Array.isArray(value)) {
    return `${value.length} item${value.length === 1 ? "" : "s"}`;
  }

  if (value && typeof value === "object") {
    return "object";
  }

  if (value === undefined) {
    return "undefined";
  }

  return truncate(String(value), 80);
}

function fieldTooltip(value: unknown): string {
  if (value === undefined) {
    return "undefined";
  }

  if (typeof value === "string") {
    return value;
  }

  return truncate(JSON.stringify(value, null, 2) ?? String(value), 2000);
}

function isExpandable(value: unknown): boolean {
  return Array.isArray(value)
    ? value.length > 0
    : Boolean(value && typeof value === "object");
}

function truncate(value: string, maxLength: number): string {
  return value.length > maxLength
    ? `${value.slice(0, maxLength - 1)}...`
    : value;
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
