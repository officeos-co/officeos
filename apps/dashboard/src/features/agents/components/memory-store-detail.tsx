"use client";

import { useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { ChevronRightIcon, FileTextIcon } from "lucide-react";
import { useMemoryStore, type MemoryStoreEntry } from "@/features/agents";

export function MemoryStoreDetail({ memoryStoreId }: { memoryStoreId: string }) {
  const { memoryStore, loading } = useMemoryStore(memoryStoreId);
  const entries = memoryStore?.entries ?? [];
  const [selectedKey, setSelectedKey] = useState("");
  const selected = entries.find((entry) => entry.key === selectedKey) ?? entries[0];
  const content = selected?.content ?? "";

  return (
    <div className="flex min-h-0 flex-1 gap-4 pt-4">
      <div className="w-56 shrink-0 overflow-y-auto rounded-xl border border-border bg-card">
        <div className="border-b border-border px-3 py-2.5 text-xs font-medium text-muted-foreground">
          Memories
        </div>
        <div className="p-1.5">
          {loading && (
            <p className="px-2 py-1.5 text-xs text-muted-foreground">Loading...</p>
          )}
          {!loading && entries.length === 0 && (
            <p className="px-2 py-1.5 text-xs text-muted-foreground">No memories yet</p>
          )}
          {entries.map((entry) => (
            <MemoryEntryButton
              key={entry.id}
              entry={entry}
              selected={selected?.id === entry.id}
              onSelect={() => setSelectedKey(entry.key)}
            />
          ))}
        </div>
      </div>

      <div className="flex-1 overflow-y-auto rounded-xl border border-border bg-card">
        <div className="flex items-center gap-2 border-b border-border px-4 py-2.5">
          <FileTextIcon className="size-3.5 text-muted-foreground" />
          <span className="font-mono text-xs">{selected?.key ?? ""}</span>
        </div>
        <div className="prose prose-sm max-w-none p-6 prose-headings:font-semibold prose-headings:text-foreground prose-h1:mt-0 prose-h1:mb-3 prose-h1:text-lg prose-h2:mt-5 prose-h2:mb-2 prose-h2:text-sm prose-p:text-sm prose-p:text-muted-foreground prose-li:text-sm prose-li:text-muted-foreground prose-strong:text-foreground prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:before:content-none prose-code:after:content-none">
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
        </div>
      </div>
    </div>
  );
}

function MemoryEntryButton({
  entry,
  selected,
  onSelect,
}: {
  entry: MemoryStoreEntry;
  selected: boolean;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onSelect}
      className={`flex w-full items-center gap-1.5 rounded-md px-2 py-1.5 text-left text-xs transition-colors ${
        selected
          ? "bg-primary/5 text-foreground"
          : "text-muted-foreground hover:bg-muted/50"
      }`}
    >
      <ChevronRightIcon className="size-3 text-muted-foreground" />
      <FileTextIcon className="size-3.5 shrink-0" />
      <span className="truncate font-mono">{entry.key}</span>
    </button>
  );
}
