"use client";

import { useMemo, useState } from "react";
import { CheckIcon, DatabaseIcon, GlobeIcon, SearchIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { AtlasConnectorType } from "@/features/atlas";
import { cn } from "@/lib/utils";

const SUGGESTED = new Set(["commits", "issues", "pull_requests", "repositories"]);
const DEFAULT_ENTITIES = ["repositories", "issues", "pull_requests", "commits"];

export function GitHubConnectionDialog({
  open,
  onOpenChange,
  connectorType,
  onSave,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  connectorType?: AtlasConnectorType;
  onSave: (values: {
    workspaceName: string;
    displayName: string;
    repositories: string[];
    entities: string[];
  }) => Promise<void>;
}) {
  const [search, setSearch] = useState("");
  const [repositoryInput, setRepositoryInput] = useState("officeos-co/officeos");
  const [selected, setSelected] = useState<Set<string>>(
    () => new Set(["commits", "issues", "pull_requests", "repositories"]),
  );
  const [saving, setSaving] = useState(false);
  const entityOptions = useMemo(
    () => (connectorType?.entities?.length ? connectorType.entities : DEFAULT_ENTITIES),
    [connectorType?.entities],
  );

  const filtered = useMemo(
    () =>
      entityOptions.filter((entity) =>
        entity.toLowerCase().includes(search.toLowerCase()),
      ),
    [entityOptions, search],
  );

  async function handleSave() {
    const repositories = repositoryInput
      .split(/[,\n]/)
      .map((repo) => repo.trim())
      .filter(Boolean);
    setSaving(true);
    try {
      await onSave({
        workspaceName: "default",
        displayName: "GitHub",
        repositories,
        entities: Array.from(selected),
      });
      onOpenChange(false);
    } finally {
      setSaving(false);
    }
  }

  function toggle(entity: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(entity)) next.delete(entity);
      else next.add(entity);
      return next;
    });
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] max-w-5xl overflow-hidden p-0">
        <DialogHeader className="border-b border-border px-6 py-5">
          <div className="flex items-center justify-center gap-4">
            <div className="flex size-14 items-center justify-center rounded-2xl bg-foreground text-background">
              {connectorType?.logo ? (
                <span
                  className="size-9 [&>svg]:size-9 [&>svg]:fill-current"
                  dangerouslySetInnerHTML={{ __html: connectorType.logo }}
                />
              ) : null}
            </div>
            <DialogTitle className="text-2xl">GitHub</DialogTitle>
          </div>
        </DialogHeader>

        <div className="grid max-h-[68vh] min-h-[560px] overflow-hidden md:grid-cols-[1fr_1fr]">
          <div className="overflow-y-auto border-r border-border p-6">
            <div className="mb-4 flex items-center gap-2">
              <h3 className="text-lg font-semibold">Entities</h3>
              <button
                className="rounded-full border border-border px-3 py-1 text-xs text-muted-foreground"
                onClick={() => setSelected(new Set())}
              >
                None
              </button>
              <button
                className="rounded-full border border-primary bg-primary/10 px-3 py-1 text-xs font-medium text-primary"
                onClick={() => setSelected(new Set(["commits", "issues", "pull_requests", "repositories"]))}
              >
                Suggested
              </button>
              <button
                className="rounded-full border border-border px-3 py-1 text-xs text-muted-foreground"
                onClick={() => setSelected(new Set(entityOptions))}
              >
                All
              </button>
            </div>

            <div className="relative mb-5">
              <SearchIcon className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search entities..."
                className="pl-9"
              />
            </div>

            <div className="space-y-1">
              {filtered.map((entity) => {
                const checked = selected.has(entity);
                return (
                  <button
                    key={entity}
                    type="button"
                    onClick={() => toggle(entity)}
                    className="flex w-full items-center gap-3 rounded-md px-2 py-2.5 text-left text-sm hover:bg-muted"
                  >
                    <span
                      className={cn(
                        "flex size-5 items-center justify-center rounded-full border",
                        checked
                          ? "border-primary bg-primary text-primary-foreground"
                          : "border-muted-foreground/40",
                      )}
                    >
                      {checked ? <CheckIcon className="size-3" /> : null}
                    </span>
                    <DatabaseIcon className="size-4 text-muted-foreground" />
                    <span className="capitalize">{entity.replaceAll("_", " ")}</span>
                    {SUGGESTED.has(entity) && (
                      <span className="ml-auto rounded-full bg-primary/10 px-2 py-0.5 text-[10px] font-medium text-primary">
                        Suggested
                      </span>
                    )}
                  </button>
                );
              })}
            </div>
          </div>

          <div className="overflow-y-auto p-6">
            <div className="mb-8 flex items-center gap-4">
              <h3 className="text-lg font-semibold">Authentication</h3>
              <div className="h-px flex-1 bg-border" />
              <span className="rounded-md border border-border px-3 py-2 text-sm">
                OAuth
              </span>
            </div>

            <Button
              className="mb-4 h-12 w-full"
              onClick={() =>
                window.location.assign(
                  `/api/auth/github?returnTo=${encodeURIComponent("/atlas/connectors")}`,
                )
              }
            >
              <GlobeIcon className="size-4" />
              Re-authenticate
            </Button>
            <div className="mb-10 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-700">
              OAuth connected when GitHub auth exists
            </div>

            <div className="space-y-3">
              <Label htmlFor="github-repositories">GitHub Repositories</Label>
              <Input
                id="github-repositories"
                value={repositoryInput}
                onChange={(event) => setRepositoryInput(event.target.value)}
                placeholder="owner/repo, owner/another-repo"
              />
              <p className="text-xs leading-5 text-muted-foreground">
                Atlas V1 supports explicit repositories in owner/repo format.
              </p>
            </div>
          </div>
        </div>

        <div className="border-t border-border p-6">
          <Button
            className="h-12 w-full"
            disabled={saving || selected.size === 0 || !repositoryInput.trim()}
            onClick={handleSave}
          >
            {saving ? "Saving..." : "Save"}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
