"use client";

import { useEffect, useMemo, useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import {
  ChevronRightIcon,
  EyeIcon,
  FileTextIcon,
  FolderIcon,
  PencilIcon,
  RotateCcwIcon,
  SaveIcon,
  Trash2Icon,
} from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";
import {
  type MemoryStore,
  type MemoryStoreEntry,
  useDeleteMemoryStoreEntry,
  useUpsertMemoryStoreEntry,
} from "../api/useAgentResources";

export const MEMORY_DIRECTORY_MARKER = ".directory";

type TreeNode = {
  name: string;
  path: string;
  type: "directory" | "file";
  entry?: MemoryStoreEntry;
  children: TreeNode[];
};

type MutableTreeNode = Omit<TreeNode, "children"> & {
  children: Map<string, MutableTreeNode>;
};

function graphQLErrorMessage(error: unknown, fallback: string) {
  if (
    typeof error === "object" &&
    error !== null &&
    "graphQLErrors" in error &&
    Array.isArray(error.graphQLErrors)
  ) {
    const first = error.graphQLErrors[0] as { message?: unknown } | undefined;
    if (typeof first?.message === "string") return first.message;
  }
  return fallback;
}

export function normalizeMemoryPath(path: string) {
  return path
    .replaceAll("\\", "/")
    .split("/")
    .map((part) => part.trim())
    .filter(Boolean)
    .join("/");
}

export function joinMemoryPath(...parts: string[]) {
  return normalizeMemoryPath(parts.join("/"));
}

export function parentMemoryDirectory(key: string) {
  const parts = normalizeMemoryPath(key).split("/").filter(Boolean);
  parts.pop();
  return parts.join("/");
}

export function isDirectoryMarkerKey(key: string) {
  const parts = normalizeMemoryPath(key).split("/");
  return parts.at(-1) === MEMORY_DIRECTORY_MARKER;
}

export function directoryMarkerKey(directory: string) {
  return joinMemoryPath(directory, MEMORY_DIRECTORY_MARKER);
}

export function ensureMarkdownFileName(name: string) {
  const trimmed = name.trim();
  return trimmed.toLowerCase().endsWith(".md") ? trimmed : `${trimmed}.md`;
}

export function markdownPlaceholder(fileName: string) {
  const title = fileName.replace(/\.md$/i, "").trim() || "Untitled";
  return `# ${title}\n`;
}

export function MemoryStoreDetail({
  memoryStore,
  loading,
  selectedKey,
  selectedDirectory,
  onSelectedKeyChange,
  onSelectedDirectoryChange,
  onRefetch,
}: {
  memoryStore: MemoryStore | null;
  loading: boolean;
  selectedKey: string;
  selectedDirectory: string;
  onSelectedKeyChange: (key: string) => void;
  onSelectedDirectoryChange: (directory: string) => void;
  onRefetch: () => Promise<unknown>;
}) {
  const entries = memoryStore?.entries ?? [];
  const visibleEntries = useMemo(
    () => entries.filter((entry) => !isDirectoryMarkerKey(entry.key)),
    [entries],
  );
  const tree = useMemo(() => buildMemoryTree(entries), [entries]);
  const selected =
    visibleEntries.find((entry) => entry.key === selectedKey) ?? null;
  const [expandedDirectories, setExpandedDirectories] = useState<Set<string>>(
    () => new Set([""]),
  );
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState("");
  const { upsertMemoryStoreEntry, loading: saving } = useUpsertMemoryStoreEntry();
  const { deleteMemoryStoreEntry, loading: deleting } =
    useDeleteMemoryStoreEntry();
  const dirty = selected ? draft !== selected.content : false;

  useEffect(() => {
    if (selectedKey && !visibleEntries.some((entry) => entry.key === selectedKey)) {
      onSelectedKeyChange("");
    }
  }, [onSelectedKeyChange, selectedKey, visibleEntries]);

  useEffect(() => {
    setDraft(selected?.content ?? "");
    setEditing(false);
  }, [selected?.id, selected?.content]);

  useEffect(() => {
    const directories = new Set<string>([""]);
    for (const directory of [selectedDirectory, parentMemoryDirectory(selectedKey)]) {
      const parts = normalizeMemoryPath(directory).split("/").filter(Boolean);
      let current = "";
      for (const part of parts) {
        current = joinMemoryPath(current, part);
        directories.add(current);
      }
    }
    setExpandedDirectories((current) => new Set([...current, ...directories]));
  }, [selectedDirectory, selectedKey]);

  async function handleSave() {
    if (!memoryStore || !selected || !dirty) return;

    try {
      await upsertMemoryStoreEntry(memoryStore.id, selected.key, draft);
      await onRefetch();
      toast.success("Memory file saved");
    } catch (error) {
      toast.error(graphQLErrorMessage(error, "Failed to save memory file"));
    }
  }

  async function handleDeleteFile() {
    if (!memoryStore || !selected) return;
    const confirmed = window.confirm(`Delete ${selected.key}?`);
    if (!confirmed) return;

    try {
      await deleteMemoryStoreEntry(memoryStore.id, selected.key);
      onSelectedKeyChange("");
      onSelectedDirectoryChange(parentMemoryDirectory(selected.key));
      await onRefetch();
      toast.success("Memory file deleted");
    } catch (error) {
      toast.error(graphQLErrorMessage(error, "Failed to delete memory file"));
    }
  }

  async function handleDeleteDirectory() {
    if (!memoryStore || !selectedDirectory) return;
    const prefix = `${selectedDirectory}/`;
    const keys = entries
      .filter(
        (entry) =>
          normalizeMemoryPath(entry.key) === directoryMarkerKey(selectedDirectory) ||
          normalizeMemoryPath(entry.key).startsWith(prefix),
      )
      .map((entry) => entry.key);
    if (keys.length === 0) return;

    const confirmed = window.confirm(`Delete ${selectedDirectory} and its contents?`);
    if (!confirmed) return;

    try {
      await Promise.all(keys.map((key) => deleteMemoryStoreEntry(memoryStore.id, key)));
      onSelectedKeyChange("");
      onSelectedDirectoryChange(parentMemoryDirectory(selectedDirectory));
      await onRefetch();
      toast.success("Memory directory deleted");
    } catch (error) {
      toast.error(graphQLErrorMessage(error, "Failed to delete memory directory"));
    }
  }

  return (
    <div className="flex min-h-0 flex-1 overflow-hidden rounded-xl border border-border bg-card">
      <div className="w-80 shrink-0 overflow-y-auto border-r border-border">
        <div className="p-1.5">
          {loading && (
            <p className="px-2 py-1.5 text-xs text-muted-foreground">Loading...</p>
          )}
          {!loading && entries.length === 0 && (
            <p className="px-2 py-1.5 text-xs text-muted-foreground">Empty</p>
          )}
          {!loading && entries.length > 0 && (
            <MemoryTree
              nodes={tree}
              expandedDirectories={expandedDirectories}
              selectedKey={selectedKey}
              selectedDirectory={selectedDirectory}
              onToggleDirectory={(path) => {
                setExpandedDirectories((current) => {
                  const next = new Set(current);
                  if (next.has(path)) next.delete(path);
                  else next.add(path);
                  return next;
                });
              }}
              onSelectDirectory={(path) => {
                onSelectedKeyChange("");
                onSelectedDirectoryChange(path);
              }}
              onSelectFile={(entry) => {
                onSelectedKeyChange(entry.key);
                onSelectedDirectoryChange(parentMemoryDirectory(entry.key));
              }}
            />
          )}
        </div>
      </div>

      <div className="min-w-0 flex-1 overflow-y-auto">
        {selected ? (
          <>
            <div className="flex min-h-12 items-center gap-2 border-b border-border px-4 py-2.5">
              <FileTextIcon className="size-3.5 text-muted-foreground" />
              <span className="min-w-0 flex-1 truncate font-mono text-xs">
                {selected.key}
              </span>
              <Button
                variant={editing ? "outline" : "secondary"}
                size="sm"
                onClick={() => setEditing(false)}
              >
                <EyeIcon />
                Preview
              </Button>
              <Button
                variant={editing ? "secondary" : "outline"}
                size="sm"
                onClick={() => setEditing(true)}
              >
                <PencilIcon />
                Edit
              </Button>
              {dirty && (
                <Button variant="outline" size="sm" onClick={() => setDraft(selected.content)}>
                  <RotateCcwIcon />
                  Revert
                </Button>
              )}
              <Button
                size="sm"
                disabled={!dirty || saving}
                onClick={() => void handleSave()}
              >
                <SaveIcon />
                Save
              </Button>
              <Button
                variant="destructive"
                size="sm"
                disabled={deleting}
                onClick={() => void handleDeleteFile()}
              >
                <Trash2Icon />
                Delete
              </Button>
            </div>
            {editing ? (
              <div className="p-4">
                <Textarea
                  value={draft}
                  onChange={(event) => setDraft(event.target.value)}
                  className="min-h-[32rem] resize-none font-mono text-xs leading-6"
                />
              </div>
            ) : selected.key.toLowerCase().endsWith(".md") ? (
              <div className="prose prose-sm max-w-none p-6 prose-headings:font-semibold prose-headings:text-foreground prose-h1:mt-0 prose-h1:mb-3 prose-h1:text-lg prose-h2:mt-5 prose-h2:mb-2 prose-h2:text-sm prose-p:text-sm prose-p:text-muted-foreground prose-li:text-sm prose-li:text-muted-foreground prose-strong:text-foreground prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:before:content-none prose-code:after:content-none">
                <ReactMarkdown remarkPlugins={[remarkGfm]}>{selected.content}</ReactMarkdown>
              </div>
            ) : (
              <pre className="m-0 whitespace-pre-wrap p-6 font-mono text-xs leading-6 text-muted-foreground">
                {selected.content}
              </pre>
            )}
          </>
        ) : (
          <div className="flex h-full min-h-[28rem] items-center justify-center px-6 text-center">
            <div>
              <FolderIcon className="mx-auto mb-3 size-8 text-muted-foreground" />
              <h2 className="text-lg font-semibold tracking-tight">
                {selectedDirectory ? selectedDirectory : "Root directory"}
              </h2>
              <p className="mt-2 text-sm text-muted-foreground">
                Create a markdown file or select an existing file to edit.
              </p>
              {selectedDirectory && (
                <Button
                  className="mt-4"
                  variant="destructive"
                  size="sm"
                  disabled={deleting}
                  onClick={() => void handleDeleteDirectory()}
                >
                  <Trash2Icon />
                  Delete directory
                </Button>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function MemoryTree({
  nodes,
  expandedDirectories,
  selectedKey,
  selectedDirectory,
  onToggleDirectory,
  onSelectDirectory,
  onSelectFile,
}: {
  nodes: TreeNode[];
  expandedDirectories: Set<string>;
  selectedKey: string;
  selectedDirectory: string;
  onToggleDirectory: (path: string) => void;
  onSelectDirectory: (path: string) => void;
  onSelectFile: (entry: MemoryStoreEntry) => void;
}) {
  const rootSelected = !selectedKey && selectedDirectory === "";

  return (
    <div>
      <button
        type="button"
        onClick={() => onSelectDirectory("")}
        className={cn(
          "mb-1 flex w-full items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-xs transition-colors",
          rootSelected
            ? "bg-primary/5 text-foreground"
            : "text-muted-foreground hover:bg-muted/50",
        )}
      >
        <FolderIcon className="size-3.5 shrink-0" />
        <span className="truncate font-mono">/</span>
      </button>
      {nodes.map((node) => (
        <MemoryTreeNode
          key={node.path}
          node={node}
          depth={0}
          expandedDirectories={expandedDirectories}
          selectedKey={selectedKey}
          selectedDirectory={selectedDirectory}
          onToggleDirectory={onToggleDirectory}
          onSelectDirectory={onSelectDirectory}
          onSelectFile={onSelectFile}
        />
      ))}
    </div>
  );
}

function MemoryTreeNode({
  node,
  depth,
  expandedDirectories,
  selectedKey,
  selectedDirectory,
  onToggleDirectory,
  onSelectDirectory,
  onSelectFile,
}: {
  node: TreeNode;
  depth: number;
  expandedDirectories: Set<string>;
  selectedKey: string;
  selectedDirectory: string;
  onToggleDirectory: (path: string) => void;
  onSelectDirectory: (path: string) => void;
  onSelectFile: (entry: MemoryStoreEntry) => void;
}) {
  const expanded = expandedDirectories.has(node.path);
  const selected =
    node.type === "file"
      ? selectedKey === node.entry?.key
      : !selectedKey && selectedDirectory === node.path;

  return (
    <div>
      <button
        type="button"
        onClick={() => {
          if (node.type === "directory") {
            onSelectDirectory(node.path);
            onToggleDirectory(node.path);
          } else if (node.entry) {
            onSelectFile(node.entry);
          }
        }}
        className={cn(
          "flex w-full items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-xs transition-colors",
          selected
            ? "bg-primary/5 text-foreground"
            : "text-muted-foreground hover:bg-muted/50",
        )}
        style={{ paddingLeft: `${0.5 + depth * 0.875}rem` }}
      >
        {node.type === "directory" ? (
          <>
            <ChevronRightIcon
              className={cn(
                "size-3 shrink-0 text-muted-foreground transition-transform",
                expanded && "rotate-90",
              )}
            />
            <FolderIcon className="size-3.5 shrink-0" />
          </>
        ) : (
          <>
            <span className="size-3 shrink-0" />
            <FileTextIcon className="size-3.5 shrink-0" />
          </>
        )}
        <span className="truncate font-mono">{node.name}</span>
      </button>
      {node.type === "directory" && expanded && (
        <div>
          {node.children.map((child) => (
            <MemoryTreeNode
              key={child.path}
              node={child}
              depth={depth + 1}
              expandedDirectories={expandedDirectories}
              selectedKey={selectedKey}
              selectedDirectory={selectedDirectory}
              onToggleDirectory={onToggleDirectory}
              onSelectDirectory={onSelectDirectory}
              onSelectFile={onSelectFile}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function buildMemoryTree(entries: MemoryStoreEntry[]) {
  const root: MutableTreeNode = {
    name: "",
    path: "",
    type: "directory",
    children: new Map(),
  };

  for (const entry of entries) {
    const key = normalizeMemoryPath(entry.key);
    const parts = key.split("/").filter(Boolean);
    if (parts.length === 0) continue;

    const fileName = parts.at(-1) ?? "";
    const directoryParts = isDirectoryMarkerKey(key) ? parts.slice(0, -1) : parts.slice(0, -1);
    let current = root;
    let currentPath = "";

    for (const part of directoryParts) {
      currentPath = joinMemoryPath(currentPath, part);
      const existing = current.children.get(currentPath);
      if (existing) {
        current = existing;
        continue;
      }

      const next: MutableTreeNode = {
        name: part,
        path: currentPath,
        type: "directory",
        children: new Map(),
      };
      current.children.set(currentPath, next);
      current = next;
    }

    if (!isDirectoryMarkerKey(key)) {
      current.children.set(key, {
        name: fileName,
        path: key,
        type: "file",
        entry,
        children: new Map(),
      });
    }
  }

  return sortTreeChildren(root);
}

function sortTreeChildren(node: MutableTreeNode): TreeNode[] {
  return [...node.children.values()]
    .sort((a, b) => {
      if (a.type !== b.type) return a.type === "directory" ? -1 : 1;
      return a.name.localeCompare(b.name);
    })
    .map((child) => ({
      name: child.name,
      path: child.path,
      type: child.type,
      entry: child.entry,
      children: sortTreeChildren(child),
    }));
}
