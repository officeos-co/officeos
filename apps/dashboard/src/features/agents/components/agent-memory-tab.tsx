"use client";

import { useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
export type FileNode = {
  name: string;
  type: "file" | "folder";
  children?: FileNode[];
  content?: string;
};
import { useAgent } from "../api/useAgents";
import { ChevronRightIcon, FileTextIcon, FolderIcon } from "lucide-react";

function FileTreeItem({
  node,
  depth,
  selectedPath,
  onSelect,
  parentPath,
}: {
  node: FileNode;
  depth: number;
  selectedPath: string;
  onSelect: (path: string, content: string) => void;
  parentPath: string;
}) {
  const [open, setOpen] = useState(depth < 2);
  const path = parentPath ? `${parentPath}/${node.name}` : node.name;

  if (node.type === "folder") {
    return (
      <div>
        <button
          type="button"
          onClick={() => setOpen(!open)}
          className="flex w-full items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-xs hover:bg-muted/50 transition-colors"
          style={{ paddingLeft: `${depth * 12 + 8}px` }}
        >
          <ChevronRightIcon
            className={`size-3 text-muted-foreground shrink-0 transition-transform ${open ? "rotate-90" : ""}`}
          />
          <FolderIcon className="size-3.5 text-muted-foreground shrink-0" />
          <span className="font-mono truncate">{node.name}</span>
        </button>
        {open &&
          node.children?.map((child) => (
            <FileTreeItem
              key={child.name}
              node={child}
              depth={depth + 1}
              selectedPath={selectedPath}
              onSelect={onSelect}
              parentPath={path}
            />
          ))}
      </div>
    );
  }

  const isSelected = selectedPath === path;
  return (
    <button
      type="button"
      onClick={() => onSelect(path, node.content ?? "")}
      className={`flex w-full items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-xs transition-colors ${
        isSelected
          ? "bg-primary/5 text-foreground"
          : "hover:bg-muted/50 text-muted-foreground"
      }`}
      style={{ paddingLeft: `${depth * 12 + 8 + 16}px` }}
    >
      <FileTextIcon className="size-3.5 shrink-0" />
      <span className="font-mono truncate">{node.name}</span>
    </button>
  );
}

export function AgentMemoryTab({ agentId }: { agentId: string }) {
  const { agent, loading } = useAgent(agentId);
  const fileTree: FileNode[] = (agent?.memories ?? []).map((m) => ({
    name: m.key,
    type: "file" as const,
    content: m.content,
  }));
  const [selectedPath, setSelectedPath] = useState("");
  const [content, setContent] = useState("");

  function handleSelect(path: string, fileContent: string) {
    setSelectedPath(path);
    setContent(fileContent);
  }

  return (
    <div className="flex gap-4 pt-4 min-h-0">
      {/* File browser */}
      <div className="w-56 shrink-0 overflow-y-auto rounded-xl border border-border bg-card">
        <div className="px-3 py-2.5 border-b border-border text-xs font-medium text-muted-foreground">
          Memories
        </div>
        <div className="p-1.5">
          {loading && (
            <p className="px-2 py-1.5 text-xs text-muted-foreground">Loading…</p>
          )}
          {!loading && fileTree.length === 0 && (
            <p className="px-2 py-1.5 text-xs text-muted-foreground">No memories yet</p>
          )}
          {fileTree.map((node) => (
            <FileTreeItem
              key={node.name}
              node={node}
              depth={0}
              selectedPath={selectedPath}
              onSelect={handleSelect}
              parentPath=""
            />
          ))}
        </div>
      </div>

      {/* File content */}
      <div className="flex-1 rounded-xl border border-border bg-card overflow-y-auto">
        <div className="px-4 py-2.5 border-b border-border flex items-center gap-2">
          <FileTextIcon className="size-3.5 text-muted-foreground" />
          <span className="font-mono text-xs">{selectedPath}</span>
        </div>
        <div className="p-6 prose prose-sm max-w-none prose-headings:font-semibold prose-headings:text-foreground prose-h1:text-lg prose-h1:mt-0 prose-h1:mb-3 prose-h2:text-sm prose-h2:mt-5 prose-h2:mb-2 prose-p:text-sm prose-p:text-muted-foreground prose-li:text-sm prose-li:text-muted-foreground prose-strong:text-foreground prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:before:content-none prose-code:after:content-none">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
        </div>
      </div>
    </div>
  );
}
