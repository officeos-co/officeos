import path from "node:path";
import * as vscode from "vscode";
import {
  OfficeOsCli,
  ResourceDescriptor,
  ResourceKind,
  resourceId,
  resourceName,
  singularKind,
} from "./officeOsCli";

interface ResourceCategory {
  readonly label: string;
  readonly cliKind: ResourceKind;
  readonly icon: string;
  readonly capabilities: readonly string[];
}

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

  constructor(
    private cli: OfficeOsCli,
    private readonly extensionPath: string,
  ) {}

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
      return await this.loadCategories();
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

  private async loadCategories(): Promise<OfficeOsNode[]> {
    try {
      return (await this.cli.listResourceCatalog()).map(
        (resource) => new CategoryNode(toResourceCategory(resource)),
      );
    } catch (error) {
      void vscode.window.showErrorMessage(
        `OfficeOS resources failed to load: ${errorMessage(error)}`,
      );
      return [new MessageNode("Unable to load resource catalog")];
    }
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
    item.contextValue =
      node.kind === "agents"
        ? "officeosAgentResource"
        : canDeleteResource(node)
          ? "officeosDeletableResource"
          : "officeosResource";
    item.iconPath = stateIconPath(this.extensionPath, resourceState(node.value));
    item.description = resourceDescription(node.value);
    item.tooltip = resourceTooltip(node, label);
    item.command = {
      command: "officeos.showResourceLogs",
      title: "Show Resource Logs",
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

export function resourceDeleteName(node: ResourceNode): string {
  return node.name || resourceId(node.value);
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

  return singularKind(fallback);
}

function resourceDescription(value: unknown): string | undefined {
  if (!value || typeof value !== "object") {
    return undefined;
  }

  const record = value as Record<string, unknown>;
  const health = resourceHealth(record);
  if (health?.state || health?.reason) {
    return [health.state, health.reason].filter(Boolean).join(": ");
  }

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

function resourceTooltip(node: ResourceNode, label: string): string {
  const health = resourceHealth(node.value);
  if (!health) {
    return `${node.kind}/${label}`;
  }

  const details = [
    `${node.kind}/${label}`,
    health.status ? `status: ${health.status}` : undefined,
    health.state ? `state: ${health.state}` : undefined,
    health.reason ? `reason: ${health.reason}` : undefined,
    health.message,
  ].filter(Boolean);
  return details.join("\n");
}

type ResourceState = "green" | "orange" | "red" | "idle" | "neutral";

interface ResourceHealth {
  status?: string;
  state?: string;
  reason?: string;
  message?: string;
}

function resourceHealth(value: unknown): ResourceHealth | undefined {
  if (!value || typeof value !== "object") {
    return undefined;
  }

  const health = (value as Record<string, unknown>).health;
  return health && typeof health === "object"
    ? (health as ResourceHealth)
    : undefined;
}

function resourceState(value: unknown): ResourceState {
  const health = resourceHealth(value);
  if (health?.state === "green" || health?.state === "orange" || health?.state === "red" || health?.state === "idle") {
    return health.state;
  }

  const status = resourceStatus(value).toLowerCase();
  if (["active", "running", "enabled", "configured", "ready", "succeeded", "healthy", "completed"].includes(status)) {
    return "green";
  }

  if (status === "idle") {
    return "idle";
  }

  if (["pending", "queued", "booting", "restarting", "working", "degraded"].includes(status)) {
    return "orange";
  }

  if (["error", "failed", "disabled", "unconfigured", "canceled", "cancelled"].includes(status)) {
    return "red";
  }

  return "neutral";
}

function resourceStatus(value: unknown): string {
  if (!value || typeof value !== "object") {
    return "";
  }

  const record = value as Record<string, unknown>;
  const health = resourceHealth(record);
  if (health?.status) return health.status;
  if (typeof record.status === "string") return record.status;
  if (typeof record.phase === "string") return record.phase;
  if (typeof record.enabled === "boolean") return record.enabled ? "enabled" : "disabled";
  if (typeof record.configured === "boolean") return record.configured ? "configured" : "unconfigured";
  return "";
}

function stateIconPath(extensionPath: string, state: ResourceState): vscode.Uri {
  return vscode.Uri.file(path.join(extensionPath, "resources", `state-${state}.svg`));
}

function canDeleteResource(node: ResourceNode): boolean {
  return node.category.capabilities.some(
    (capability) => capability.toLowerCase() === "delete",
  );
}

function toResourceCategory(resource: ResourceDescriptor): ResourceCategory {
  return {
    label: resource.displayName || resource.kind,
    cliKind: resource.kind,
    icon: resource.icon || "folder",
    capabilities: resource.capabilities,
  };
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
