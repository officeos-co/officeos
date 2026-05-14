"use client";

import {
  Activity,
  Boxes,
  Clock3,
  Database,
  FileText,
  Globe2,
  HardDrive,
  PlayCircle,
  Plug,
  RefreshCcw,
  Search,
  Server,
  Settings2,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";
import type { ComponentType } from "react";
import {
  DashboardUser,
  LoadState,
  ResourceCategories,
  ResourceCategory,
  ResourceKind,
  ResourceValue,
  describeResource,
  isActiveResource,
  resourceId,
  resourceHealth,
  resourceHealthReason,
  resourceHealthState,
  resourceName,
  resourceStatus,
  resourceTimestamp,
} from "@/lib/resources";

const iconByKind: Record<ResourceKind, ComponentType<{ className?: string }>> = {
  agents: Boxes,
  runs: PlayCircle,
  channels: Activity,
  routines: Clock3,
  browsers: Globe2,
  memorystores: Database,
  engines: Server,
  providers: Plug,
  models: HardDrive,
};

export default function ResourceDashboardPage() {
  const [user, setUser] = useState<DashboardUser | null>(null);
  const [authChecked, setAuthChecked] = useState(false);
  const [selectedKind, setSelectedKind] = useState<ResourceKind>("agents");
  const [resources, setResources] = useState<Partial<Record<ResourceKind, ResourceValue[]>>>({});
  const [selectedName, setSelectedName] = useState<string>("");
  const [details, setDetails] = useState<ResourceValue | null>(null);
  const [logs, setLogs] = useState("");
  const [query, setQuery] = useState("");
  const [loadState, setLoadState] = useState<LoadState>("idle");
  const [detailState, setDetailState] = useState<LoadState>("idle");
  const [error, setError] = useState<string | null>(null);

  const selectedCategory = ResourceCategories.find((category) => category.kind === selectedKind) ?? ResourceCategories[0];
  const selectedResources = useMemo(() => resources[selectedKind] ?? [], [resources, selectedKind]);
  const effectiveSelectedName = useMemo(() => {
    const selectedExists = selectedResources.some((resource) => resourceKey(resource) === selectedName);
    if (selectedExists) return selectedName;
    return selectedResources[0] ? resourceKey(selectedResources[0]) : "";
  }, [selectedName, selectedResources]);
  const filteredResources = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) return selectedResources;
    return selectedResources.filter((resource) => JSON.stringify(resource).toLowerCase().includes(normalizedQuery));
  }, [query, selectedResources]);

  const loadAllResources = useCallback(async () => {
    setLoadState("loading");
    setError(null);
    try {
      const entries = await Promise.all(
        ResourceCategories.map(async (category) => {
          const value = await fetchJson<unknown>(category.endpoint);
          return [category.kind, normalizeList(value)] as const;
        }),
      );
      const nextResources = Object.fromEntries(entries) as Partial<Record<ResourceKind, ResourceValue[]>>;
      setResources(nextResources);
      setLoadState("ready");
    } catch (err) {
      setError(errorMessage(err));
      setLoadState("error");
    }
  }, []);

  const loadDetailsAndLogs = useCallback(async (category: ResourceCategory, name: string) => {
    setDetailState("loading");
    setDetails(null);
    setLogs("");
    try {
      const known = (resources[category.kind] ?? []).find((resource) => resourceKey(resource) === name) ?? null;
      const canDescribe = category.kind !== "providers" && category.kind !== "models";
      const detail = canDescribe
        ? await fetchJson<ResourceValue>(`/api/v1/resources/${encodeURIComponent(category.kind)}/${encodeURIComponent(name)}`)
        : known;
      const logText = await fetchText(`/api/v1/resources/${encodeURIComponent(category.kind)}/${encodeURIComponent(name)}/logs?tail=200`);
      setDetails(detail);
      setLogs(logText);
      setDetailState("ready");
    } catch (err) {
      setDetails((resources[category.kind] ?? []).find((resource) => resourceKey(resource) === name) ?? null);
      setLogs(errorMessage(err));
      setDetailState("error");
    }
  }, [resources]);

  useEffect(() => {
    let cancelled = false;
    fetchJson<DashboardUser>("/api/v1/me")
      .then((me) => {
        if (cancelled) return;
        setUser(me);
        setAuthChecked(true);
      })
      .catch(() => {
        if (cancelled) return;
        window.location.href = `/login?returnTo=${encodeURIComponent("/")}`;
      });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!authChecked || !user) return;
    queueMicrotask(() => void loadAllResources());
  }, [authChecked, loadAllResources, user]);

  useEffect(() => {
    if (!effectiveSelectedName) return;
    queueMicrotask(() => void loadDetailsAndLogs(selectedCategory, effectiveSelectedName));
  }, [effectiveSelectedName, loadDetailsAndLogs, selectedCategory]);

  if (!authChecked || !user) {
    return <main className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">Loading</main>;
  }

  return (
    <main className="grid min-h-screen grid-cols-[260px_minmax(0,1fr)] bg-background">
      <aside className="border-r border-border bg-panel">
        <div className="border-b border-border px-4 py-4">
          <div className="text-lg font-semibold">OfficeOS</div>
          <div className="truncate text-sm text-muted-foreground">{user.email}</div>
        </div>
        <nav className="p-2">
          {ResourceCategories.map((category) => {
            const Icon = iconByKind[category.kind];
            const count = resources[category.kind]?.length ?? 0;
            const categoryResources = resources[category.kind] ?? [];
            const active = categoryResources.filter(isActiveResource).length;
            const warning = categoryResources.filter((resource) => resourceHealthState(resource) === "orange").length;
            const failed = categoryResources.filter((resource) => resourceHealthState(resource) === "red").length;
            return (
              <button
                className={`mb-1 grid h-11 w-full grid-cols-[20px_minmax(0,1fr)_auto] items-center gap-3 rounded-md px-3 text-left text-sm ${
                  selectedKind === category.kind ? "bg-panel-strong text-foreground" : "text-muted-foreground hover:bg-panel-strong hover:text-foreground"
                }`}
                key={category.kind}
                onClick={() => setSelectedKind(category.kind)}
                type="button"
              >
                <Icon className="size-4" />
                <span className="truncate">{category.label}</span>
                <span className="font-mono text-xs">{active}/{warning}/{failed}/{count}</span>
              </button>
            );
          })}
        </nav>
      </aside>

      <section className="grid min-h-screen grid-rows-[auto_minmax(0,1fr)]">
        <header className="flex h-16 items-center justify-between border-b border-border bg-panel px-5">
          <div>
            <h1 className="text-lg font-semibold">{selectedCategory.label}</h1>
            <div className="text-xs text-muted-foreground">{loadState === "loading" ? "Refreshing" : `${filteredResources.length} shown`}</div>
          </div>
          <div className="flex items-center gap-2">
            <label className="relative block">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <input
                className="h-9 w-72 rounded-md border border-border bg-panel pl-9 pr-3 text-sm outline-none focus:border-primary"
                onChange={(event) => setQuery(event.target.value)}
                value={query}
              />
            </label>
            <button className="inline-flex h-9 items-center gap-2 rounded-md border border-border bg-panel px-3 text-sm hover:bg-panel-strong" onClick={loadAllResources} type="button">
              <RefreshCcw className="size-4" />
              Refresh
            </button>
          </div>
        </header>

        <div className="grid min-h-0 grid-cols-[minmax(460px,1fr)_minmax(360px,42vw)] gap-0">
          <ResourceTable
            category={selectedCategory}
            error={error}
            loadState={loadState}
            resources={filteredResources}
            selectedName={effectiveSelectedName}
            onSelect={setSelectedName}
          />
          <ResourceInspector detailState={detailState} details={details} logs={logs} selectedName={effectiveSelectedName} />
        </div>
      </section>
    </main>
  );
}

function ResourceTable({
  category,
  error,
  loadState,
  resources,
  selectedName,
  onSelect,
}: {
  category: ResourceCategory;
  error: string | null;
  loadState: LoadState;
  resources: ResourceValue[];
  selectedName: string;
  onSelect: (name: string) => void;
}) {
  return (
    <section className="min-h-0 overflow-auto p-4">
      <div className="overflow-hidden rounded-md border border-border bg-panel">
        <table className="w-full table-fixed border-collapse text-sm">
          <thead className="bg-panel-strong text-left text-xs uppercase text-muted-foreground">
            <tr>
              <th className="w-[30%] px-3 py-2 font-medium">Name</th>
              <th className="w-[16%] px-3 py-2 font-medium">Health</th>
              <th className="w-[26%] px-3 py-2 font-medium">Reason</th>
              <th className="w-[14%] px-3 py-2 font-medium">Info</th>
              <th className="w-[14%] px-3 py-2 font-medium">Time</th>
            </tr>
          </thead>
          <tbody>
            {resources.map((resource) => {
              const key = resourceKey(resource);
              return (
                <tr className={`cursor-pointer border-t border-border ${selectedName === key ? "bg-panel-strong" : "hover:bg-panel-strong"}`} key={key} onClick={() => onSelect(key)}>
                  <td className="truncate px-3 py-2 font-medium">{resourceName(resource) || key}</td>
                  <td className="px-3 py-2">
                    <StatusPill resource={resource} />
                  </td>
                  <td className="truncate px-3 py-2 text-muted-foreground">{resourceHealthReason(resource)}</td>
                  <td className="truncate px-3 py-2 text-muted-foreground">{describeResource(category, resource)}</td>
                  <td className="truncate px-3 py-2 font-mono text-xs text-muted-foreground">{resourceTimestamp(resource)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
        {resources.length === 0 ? (
          <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">
            {loadState === "error" ? error : "No resources"}
          </div>
        ) : null}
      </div>
    </section>
  );
}

function ResourceInspector({
  detailState,
  details,
  logs,
  selectedName,
}: {
  detailState: LoadState;
  details: ResourceValue | null;
  logs: string;
  selectedName: string;
}) {
  const [activeTab, setActiveTab] = useState<"logs" | "details">("logs");
  const health = resourceHealth(details);
  const healthState = resourceHealthState(details);
  return (
    <aside className="grid min-h-0 grid-rows-[auto_auto_minmax(0,1fr)] border-l border-border bg-panel">
      {health && healthState !== "green" ? (
        <div className={`m-3 mb-0 rounded-md border p-3 text-sm ${healthState === "red" ? "border-red-200 bg-red-50 text-danger" : "border-amber-200 bg-amber-50 text-amber-800"}`}>
          <div className="font-medium">{health.reason ?? health.status ?? "Needs attention"}</div>
          <div className="mt-1 text-xs">{health.message ?? "Inspect the latest bootstrap run and logs."}</div>
          {health.lastBootstrapRunId ? <div className="mt-2 font-mono text-xs">run/{health.lastBootstrapRunId}</div> : null}
        </div>
      ) : null}
      <div className="flex h-11 items-end gap-1 border-b border-border px-3">
        <InspectorTab active={activeTab === "logs"} icon={FileText} label="Logs" onClick={() => setActiveTab("logs")} />
        <InspectorTab active={activeTab === "details"} icon={Settings2} label="Details" onClick={() => setActiveTab("details")} />
      </div>
      <section className="min-h-0 overflow-auto">
        {activeTab === "details" ? (
          <pre className="p-3 text-xs leading-5">
            {selectedName ? detailState === "loading" ? "Loading" : JSON.stringify(details, null, 2) : ""}
          </pre>
        ) : (
          <pre className="min-h-full whitespace-pre-wrap p-3 text-xs leading-5">
            {selectedName ? detailState === "loading" ? "Loading" : logs || "No logs" : ""}
          </pre>
        )}
      </section>
    </aside>
  );
}

function InspectorTab({
  active,
  icon: Icon,
  label,
  onClick,
}: {
  active: boolean;
  icon: ComponentType<{ className?: string }>;
  label: string;
  onClick: () => void;
}) {
  return (
    <button
      className={`inline-flex h-9 items-center gap-2 rounded-t-md border border-b-0 px-3 text-sm ${
        active ? "border-border bg-background text-foreground" : "border-transparent text-muted-foreground hover:bg-panel-strong hover:text-foreground"
      }`}
      onClick={onClick}
      type="button"
    >
      <Icon className="size-4" />
      {label}
    </button>
  );
}

function StatusPill({ resource }: { resource: ResourceValue }) {
  const status = resourceStatus(resource);
  const state = resourceHealthState(resource);
  const color = state === "green"
    ? "bg-green-500"
    : state === "orange"
      ? "bg-amber-500"
      : state === "red"
        ? "bg-red-500"
        : "bg-muted-foreground";
  return (
    <span className="inline-flex max-w-full items-center gap-2 rounded px-2 py-1 text-xs text-foreground">
      <span className={`size-2 shrink-0 rounded-full ${color}`} />
      <span className="truncate">{status || "-"}</span>
    </span>
  );
}

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url, { credentials: "include" });
  if (!response.ok) throw new Error(await responseError(response));
  return (await response.json()) as T;
}

async function fetchText(url: string): Promise<string> {
  const response = await fetch(url, { credentials: "include" });
  if (!response.ok) throw new Error(await responseError(response));
  return await response.text();
}

async function responseError(response: Response): Promise<string> {
  const text = await response.text();
  if (!text) return `${response.status} ${response.statusText}`;
  try {
    const parsed = JSON.parse(text) as { error?: string; message?: string };
    return parsed.error ?? parsed.message ?? text;
  } catch {
    return `${response.status} ${response.statusText}`;
  }
}

function normalizeList(value: unknown): ResourceValue[] {
  const list = Array.isArray(value) ? value : [value];
  return list.filter((entry): entry is ResourceValue => Boolean(entry) && typeof entry === "object");
}

function resourceKey(resource: ResourceValue): string {
  return resourceId(resource) || resourceName(resource);
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
