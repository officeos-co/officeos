"use client";

import { FormEvent, use, useMemo, useState } from "react";
import Link from "next/link";
import { FilePlusIcon, FolderPlusIcon } from "lucide-react";
import { toast } from "sonner";
import { PageContainer } from "@/shell/page-container";
import { PageHeader } from "@/shell/page-header";
import { Button } from "@/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import { Input } from "@/ui/input";
import {
  directoryMarkerKey,
  ensureMarkdownFileName,
  isDirectoryMarkerKey,
  joinMemoryPath,
  markdownPlaceholder,
  type MemoryStoreEntry,
  MemoryStoreDetail,
  normalizeMemoryPath,
  useMemoryStore,
  useUpsertMemoryStoreEntry,
} from "@/features/agents";

const EMPTY_MEMORY_STORE_ENTRIES: MemoryStoreEntry[] = [];

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

export default function MemoryStoreDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const { memoryStore, loading, refetch } = useMemoryStore(id);
  const { upsertMemoryStoreEntry, loading: creating } =
    useUpsertMemoryStoreEntry();
  const [selectedKey, setSelectedKey] = useState("");
  const [selectedDirectory, setSelectedDirectory] = useState("");
  const [createMode, setCreateMode] =
    useState<"directory" | "file" | null>(null);
  const entries = memoryStore?.entries ?? EMPTY_MEMORY_STORE_ENTRIES;
  const existingKeys = useMemo(
    () => new Set(entries.map((entry) => normalizeMemoryPath(entry.key))),
    [entries],
  );

  async function handleCreate(name: string) {
    if (!memoryStore || !createMode) return false;

    const segment = normalizeNameSegment(name);
    if (!segment) {
      toast.error("Enter a valid name");
      return false;
    }

    if (createMode === "file") {
      const fileName = ensureMarkdownFileName(segment);
      const key = joinMemoryPath(selectedDirectory, fileName);
      if (existingKeys.has(key)) {
        toast.error("A file with that name already exists");
        return false;
      }

      try {
        await upsertMemoryStoreEntry(
          memoryStore.id,
          key,
          markdownPlaceholder(fileName),
        );
        await refetch();
        setSelectedDirectory(selectedDirectory);
        setSelectedKey(key);
        setCreateMode(null);
        toast.success("Memory file created");
        return true;
      } catch (error) {
        toast.error(graphQLErrorMessage(error, "Failed to create memory file"));
        return false;
      }
    }

    const directory = joinMemoryPath(selectedDirectory, segment);
    const markerKey = directoryMarkerKey(directory);
    const directoryExists = entries.some((entry) => {
      const key = normalizeMemoryPath(entry.key);
      return key === markerKey || key.startsWith(`${directory}/`);
    });
    if (directoryExists) {
      toast.error("A directory with that name already exists");
      return false;
    }

    try {
      await upsertMemoryStoreEntry(memoryStore.id, markerKey, "");
      await refetch();
      setSelectedDirectory(directory);
      setSelectedKey("");
      setCreateMode(null);
      toast.success("Memory directory created");
      return true;
    } catch (error) {
      toast.error(graphQLErrorMessage(error, "Failed to create memory directory"));
      return false;
    }
  }

  return (
    <div className="flex min-h-svh flex-col">
      <PageHeader
        group="Managed Agents"
        page={memoryStore?.displayName ?? "Memory Store"}
        subtitle={
          selectedDirectory
            ? `Selected directory: ${selectedDirectory}`
            : "Memory store details."
        }
        width="wide"
        action={
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCreateMode("directory")}
            >
              <FolderPlusIcon />
              Create directory
            </Button>
            <Button size="sm" onClick={() => setCreateMode("file")}>
              <FilePlusIcon />
              Create file
            </Button>
            <Button
              variant="outline"
              size="sm"
              nativeButton={false}
              render={<Link href="/memory-stores" />}
            >
              All memory stores
            </Button>
          </div>
        }
      />
      <PageContainer width="wide" className="flex min-h-0 flex-1 flex-col pb-4">
        <MemoryStoreDetail
          memoryStore={memoryStore}
          loading={loading}
          selectedKey={selectedKey}
          selectedDirectory={selectedDirectory}
          onSelectedKeyChange={setSelectedKey}
          onSelectedDirectoryChange={setSelectedDirectory}
          onRefetch={refetch}
        />
      </PageContainer>
      <CreateMemoryEntryDialog
        mode={createMode}
        selectedDirectory={selectedDirectory}
        loading={creating}
        onOpenChange={(open) => {
          if (!open) setCreateMode(null);
        }}
        onCreate={handleCreate}
      />
    </div>
  );
}

function CreateMemoryEntryDialog({
  mode,
  selectedDirectory,
  loading,
  onOpenChange,
  onCreate,
}: {
  mode: "directory" | "file" | null;
  selectedDirectory: string;
  loading: boolean;
  onOpenChange: (open: boolean) => void;
  onCreate: (name: string) => Promise<boolean>;
}) {
  const [name, setName] = useState("");
  const open = mode !== null;
  const isFile = mode === "file";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const created = await onCreate(name);
    if (created) setName("");
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) setName("");
        onOpenChange(nextOpen);
      }}
    >
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>
            {isFile ? "Create memory file" : "Create memory directory"}
          </DialogTitle>
          <DialogDescription>
            {selectedDirectory
              ? `Create inside ${selectedDirectory}.`
              : "Create at the memory store root."}
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            autoFocus
            value={name}
            placeholder={isFile ? "notes.md" : "research"}
            onChange={(event) => setName(event.target.value)}
          />
          {isFile && (
            <p className="text-xs text-muted-foreground">
              Files are saved as markdown. The .md extension is added automatically.
            </p>
          )}
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setName("");
                onOpenChange(false);
              }}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={loading || !name.trim()}>
              Create
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function normalizeNameSegment(name: string) {
  const segment = normalizeMemoryPath(name);
  if (!segment || segment.includes("/") || segment === "." || segment === "..") {
    return "";
  }
  if (isDirectoryMarkerKey(segment)) return "";
  return segment;
}
