"use client";

import { useState } from "react";
import {
  ArrowLeftIcon,
  ExternalLinkIcon,
  PlusIcon,
} from "lucide-react";
import { Button } from "@/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import { SearchInput } from "@/ui/search-input";
import { getDialogWidthClassName } from "@/shell/page-container";
import type { McpServer } from "../data/integrations";

export function ConnectorDirectoryDialog({
  open,
  onOpenChange,
  integrations,
  onConnect,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  integrations: McpServer[];
  onConnect: (server: McpServer) => void;
}) {
  const [search, setSearch] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [selectedServerName, setSelectedServerName] = useState<string | null>(
    null,
  );
  const selected = selectedServerName
    ? integrations.find((server) => server.name === selectedServerName)
    : null;
  const categories = Array.from(
    new Set(integrations.map((server) => server.category).filter(Boolean)),
  ).sort();
  const filtered = integrations.filter((server) => {
    if (selectedCategory && server.category !== selectedCategory) return false;
    if (!search.trim()) return true;
    const needle = search.toLowerCase();
    return (
      server.title.toLowerCase().includes(needle) ||
      server.description.toLowerCase().includes(needle) ||
      server.name.toLowerCase().includes(needle)
    );
  });

  function setOpen(nextOpen: boolean) {
    onOpenChange(nextOpen);
    if (!nextOpen) {
      setSearch("");
      setSelectedCategory(null);
      setSelectedServerName(null);
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent
        className={getDialogWidthClassName(
          "thin",
          "flex h-[min(760px,calc(100vh-96px))] flex-col overflow-hidden p-6",
        )}
      >
        {!selected && (
          <DialogHeader>
            <DialogTitle>Add connector</DialogTitle>
            <DialogDescription>
              Connect an MCP server that agents can attach during creation.
            </DialogDescription>
          </DialogHeader>
        )}

        {selected ? (
          <ConnectorDetail
            server={selected}
            onBack={() => setSelectedServerName(null)}
            onConnect={onConnect}
          />
        ) : (
          <section className="mt-4 flex min-h-0 flex-1 flex-col gap-4 overflow-hidden">
            <SearchInput
              placeholder="Search connectors..."
              value={search}
              onChange={setSearch}
              className="!flex-none max-w-none"
            />
            <div className="shrink-0 overflow-x-auto pb-1">
              <div className="flex items-start gap-2">
                <button
                  type="button"
                  className={
                    selectedCategory === null
                      ? "min-h-9 shrink-0 rounded-full border border-primary bg-primary px-4 py-2 text-left text-xs font-medium leading-4 text-primary-foreground"
                      : "min-h-9 shrink-0 rounded-full border border-border px-4 py-2 text-left text-xs font-medium leading-4 text-muted-foreground hover:bg-muted hover:text-foreground"
                  }
                  onClick={() => setSelectedCategory(null)}
                >
                  All
                </button>
                {categories.map((category) => (
                  <button
                    key={category}
                    type="button"
                    className={
                      selectedCategory === category
                        ? "min-h-9 max-w-44 shrink-0 rounded-full border border-primary bg-primary px-4 py-2 text-left text-xs font-medium leading-4 text-primary-foreground"
                        : "min-h-9 max-w-44 shrink-0 rounded-full border border-border px-4 py-2 text-left text-xs font-medium leading-4 text-muted-foreground hover:bg-muted hover:text-foreground"
                    }
                    onClick={() => setSelectedCategory(category)}
                  >
                    <span className="line-clamp-2">{category}</span>
                  </button>
                ))}
              </div>
            </div>
            <div className="grid min-h-0 flex-1 grid-cols-1 content-start items-start gap-3 overflow-y-auto pr-1 md:grid-cols-2">
              {filtered.map((server) => (
                <button
                  key={server.name}
                  type="button"
                  className="grid min-h-20 grid-cols-[32px_minmax(0,1fr)_20px] items-start gap-3 overflow-hidden rounded-xl border border-border bg-card p-3 text-left transition-colors hover:bg-muted/50"
                  onClick={() => setSelectedServerName(server.name)}
                >
                  <span
                    className="flex size-8 shrink-0 items-center justify-center rounded-lg border border-border bg-muted/40 [&>img]:size-5 [&>img]:object-contain [&>svg]:size-5"
                    dangerouslySetInnerHTML={{ __html: server.logo }}
                  />
                  <span className="min-w-0">
                    <span className="flex min-w-0 items-center gap-2">
                      <span className="truncate font-medium">
                        {server.title}
                      </span>
                      {server.configured && (
                        <span className="shrink-0 text-xs text-muted-foreground">
                          Connected
                        </span>
                      )}
                    </span>
                    <span className="mt-1 line-clamp-2 block overflow-hidden text-xs leading-[18px] text-muted-foreground">
                      {server.subtitle || "Connector"}
                    </span>
                  </span>
                  <PlusIcon className="size-4 shrink-0 text-muted-foreground" />
                </button>
              ))}
            </div>
          </section>
        )}
      </DialogContent>
    </Dialog>
  );
}

function ConnectorDetail({
  server,
  onBack,
  onConnect,
}: {
  server: McpServer;
  onBack: () => void;
  onConnect: (server: McpServer) => void;
}) {
  const needsSetup =
    Boolean(server.oauthProvider) || server.credentialFields.length > 0;
  const connectorUrl = server.url || server.command;

  return (
    <section className="mt-4 min-h-0 flex-1 overflow-y-auto pr-1">
      <button
        type="button"
        className="mb-4 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        onClick={onBack}
      >
        <ArrowLeftIcon className="size-4" />
        Back
      </button>
      <div className="flex items-start gap-3">
        <span
          className="flex size-12 shrink-0 items-center justify-center rounded-lg border border-border bg-muted/40 [&>img]:size-7 [&>img]:object-contain [&>svg]:size-7"
          dangerouslySetInnerHTML={{ __html: server.logo }}
        />
        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-4">
            <div>
              <h2 className="text-xl font-semibold">{server.title}</h2>
              <p className="text-sm text-muted-foreground">
                {server.subtitle || server.category || "Connector"}
              </p>
            </div>
            <Button
              size="sm"
              variant={server.configured ? "outline" : "default"}
              onClick={() => onConnect(server)}
            >
              {server.configured ? "Reconnect" : needsSetup ? "Connect" : "Add"}
            </Button>
          </div>
        </div>
      </div>

      <p className="mt-6 max-w-3xl text-sm leading-6 text-foreground/70">
        {server.description}
      </p>

      {server.authorName && (
        <p className="mt-5 text-sm text-muted-foreground">
          Developed by{" "}
          {server.authorUrl ? (
            <a
              href={server.authorUrl}
              target="_blank"
              rel="noreferrer"
              className="text-foreground underline underline-offset-4"
            >
              {server.authorName}
            </a>
          ) : (
            <span className="text-foreground">{server.authorName}</span>
          )}
        </p>
      )}

      {server.tools.length > 0 && (
        <div className="mt-7">
          <div className="mb-3 flex items-center gap-2 text-sm font-medium">
            Tools
            <span className="rounded bg-muted px-1.5 py-0.5 text-xs">
              {server.tools.length}
            </span>
          </div>
          <div className="flex flex-wrap gap-2">
            {server.tools.map((tool) => (
              <span
                key={tool.name}
                className="rounded-full border border-border bg-muted/40 px-3 py-1 font-mono text-xs"
              >
                {tool.name}
              </span>
            ))}
          </div>
        </div>
      )}

      <div className="mt-7 border-t border-border pt-5">
        <h3 className="mb-4 text-sm font-medium">Details</h3>
        <dl className="grid gap-x-10 gap-y-4 text-sm sm:grid-cols-2">
          <div>
            <dt className="text-muted-foreground">Author</dt>
            <dd className="mt-1">{server.authorName || "Unknown"}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Connector URL</dt>
            <dd className="mt-1 truncate font-mono text-xs">
              {connectorUrl || "Managed connector"}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Category</dt>
            <dd className="mt-1">{server.category || "Connector"}</dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Identifier</dt>
            <dd className="mt-1 font-mono text-xs">{server.name}</dd>
          </div>
        </dl>
        <div className="mt-5 flex flex-wrap gap-3 text-sm">
          {server.documentationUrl && (
            <a
              href={server.documentationUrl}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1 underline underline-offset-4"
            >
              Documentation
              <ExternalLinkIcon className="size-3.5" />
            </a>
          )}
          {server.repositoryUrl && (
            <a
              href={server.repositoryUrl}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-1 underline underline-offset-4"
            >
              Source
              <ExternalLinkIcon className="size-3.5" />
            </a>
          )}
        </div>
      </div>
    </section>
  );
}
