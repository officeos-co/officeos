"use client";

import { useMemo, useState } from "react";
import { CheckIcon, DatabaseIcon, GlobeIcon } from "lucide-react";
import { getDialogWidthClassName } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { SearchInput } from "@/components/ui/search-input";
import type { AtlasConnectorType } from "@/features/atlas";
import { buildOAuthUrl } from "@/lib/auth-url";
import { cn } from "@/lib/utils";

const DEFAULT_SELECTED_ENTITIES = [
  "commits",
  "issues",
  "pull_requests",
  "repositories",
];
const DEFAULT_ENTITIES = ["repositories", "issues", "pull_requests", "commits"];

export function AtlasConnectorDialog({
  open,
  onOpenChange,
  connectorType,
  oauthConfigured = false,
  onSave,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  connectorType?: AtlasConnectorType;
  oauthConfigured?: boolean;
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
    () => new Set(DEFAULT_SELECTED_ENTITIES),
  );
  const [saving, setSaving] = useState(false);
  const title = connectorType?.title ?? "GitHub";
  const entityOptions = useMemo(
    () =>
      connectorType?.entities?.length
        ? connectorType.entities
        : DEFAULT_ENTITIES,
    [connectorType?.entities],
  );

  const filtered = useMemo(
    () =>
      entityOptions.filter((entity) =>
        entity.toLowerCase().includes(search.toLowerCase()),
      ),
    [entityOptions, search],
  );

  function setOpen(nextOpen: boolean) {
    onOpenChange(nextOpen);
    if (!nextOpen) setSearch("");
  }

  async function handleSave() {
    const repositories = repositoryInput
      .split(/[,\n]/)
      .map((repo) => repo.trim())
      .filter(Boolean);
    setSaving(true);
    try {
      await onSave({
        workspaceName: "default",
        displayName: title,
        repositories,
        entities: Array.from(selected),
      });
      setOpen(false);
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
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent
        className={getDialogWidthClassName(
          "thin",
          "flex max-h-[calc(100vh-96px)] flex-col overflow-hidden p-6",
        )}
      >
        <DialogTitle className="sr-only">Add connector</DialogTitle>

        <section className="min-h-0 overflow-y-auto pr-1">
          <div className="grid gap-7 md:grid-cols-[minmax(0,1fr)_minmax(18rem,0.8fr)]">
            <div className="min-w-0">
              <h3 className="mb-3 text-sm font-medium">Entities</h3>

              <SearchInput
                placeholder="Search entities..."
                value={search}
                onChange={setSearch}
                className="!flex-none max-w-none"
              />

              <div className="mt-3 grid gap-2">
                {filtered.map((entity) => {
                  const checked = selected.has(entity);
                  return (
                    <button
                      key={entity}
                      type="button"
                      onClick={() => toggle(entity)}
                      className="grid min-h-12 grid-cols-[20px_20px_minmax(0,1fr)] items-center gap-3 rounded-xl border border-border bg-card px-3 py-2 text-left text-sm transition-colors hover:bg-muted/50"
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
                      <span className="truncate capitalize">
                        {entity.replaceAll("_", " ")}
                      </span>
                    </button>
                  );
                })}
              </div>
            </div>

            <div className="min-w-0">
              <h3 className="mb-3 text-sm font-medium">Authentication</h3>
              <Button
                variant="outline"
                className="mb-3 w-full justify-center"
                onClick={() =>
                  window.location.assign(
                    buildOAuthUrl("github", "/atlas/connectors"),
                  )
                }
              >
                <GlobeIcon className="size-4" />
                {oauthConfigured ? "Re-authenticate" : "Connect GitHub"}
              </Button>
              <div
                className={cn(
                  "rounded-lg border px-3 py-2 text-sm font-medium",
                  oauthConfigured
                    ? "border-emerald-200 bg-emerald-50 text-emerald-700"
                    : "border-amber-200 bg-amber-50 text-amber-800",
                )}
              >
                {oauthConfigured
                  ? "GitHub OAuth connected"
                  : "GitHub OAuth is not connected"}
              </div>

              <div className="mt-6 space-y-3">
                <Label htmlFor="github-repositories">Repositories</Label>
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

              <Button
                className="mt-6 w-full justify-center"
                disabled={saving || selected.size === 0 || !repositoryInput.trim()}
                onClick={handleSave}
              >
                {saving ? "Adding" : "Add connector"}
              </Button>
            </div>
          </div>
        </section>
      </DialogContent>
    </Dialog>
  );
}
