"use client";

import { useMemo, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { formatDistanceToNow } from "date-fns";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { DataPagination } from "@/components/ui/data-pagination";
import { EmptyState } from "@/components/ui/empty-state";
import { SearchInput } from "@/components/ui/search-input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  type AtlasConnection,
  type AtlasIndexedRecord,
  parseJsonArray,
  useAtlasConnections,
  useAtlasIndexedRecords,
} from "@/features/atlas";

const PAGE_SIZES = [10, 25, 50] as const;

export default function AtlasExplorerPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const initialConnectionId = searchParams.get("connectionId") ?? "";
  const initialEntity = searchParams.get("entity") ?? "";

  const { connections, loading: connectionsLoading } = useAtlasConnections({
    pollInterval: 5000,
  });
  const [connectionId, setConnectionId] = useState(initialConnectionId);
  const [entity, setEntity] = useState(initialEntity);
  const [search, setSearch] = useState("");
  const [pageSize, setPageSize] = useState<number>(25);
  const [pageIndex, setPageIndex] = useState(0);

  const selectedConnection = useMemo(
    () =>
      connections.find((connection) => connection.id === connectionId) ??
      connections[0] ??
      null,
    [connectionId, connections],
  );
  const entityOptions = useMemo(
    () =>
      selectedConnection
        ? parseJsonArray(selectedConnection.entitiesJson)
        : [],
    [selectedConnection],
  );
  const effectiveEntity = entityOptions.includes(entity)
    ? entity
    : entityOptions[0] ?? "";
  const cursor = pageIndex > 0 ? String(pageIndex * pageSize) : null;
  const { page, loading: recordsLoading } = useAtlasIndexedRecords({
    connectionId: selectedConnection?.id,
    entity: effectiveEntity,
    query: search,
    cursor,
    limit: pageSize,
  });
  const connectorById = useMemo(() => {
    const mapped = new Map<string, AtlasConnection>();
    for (const connection of connections) mapped.set(connection.id, connection);
    return mapped;
  }, [connections]);
  const estimatedTotal =
    pageIndex * pageSize + page.records.length + (page.hasMore ? pageSize : 0);

  function updateConnection(nextConnectionId: string) {
    const nextConnection = connections.find(
      (connection) => connection.id === nextConnectionId,
    );
    const nextEntity = nextConnection
      ? parseJsonArray(nextConnection.entitiesJson)[0] ?? ""
      : "";
    setConnectionId(nextConnectionId);
    setEntity(nextEntity);
    setPageIndex(0);
    replaceUrl(pathname, router, nextConnectionId, nextEntity);
  }

  function updateEntity(nextEntity: string) {
    setEntity(nextEntity);
    setPageIndex(0);
    replaceUrl(pathname, router, selectedConnection?.id ?? "", nextEntity);
  }

  return (
    <>
      <PageHeader
        group="Atlas"
        page="Explorer"
        subtitle="Browse indexed connector data."
        width="wide"
      />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 pb-4">
        <section className="flex min-h-0 min-w-0 flex-col overflow-hidden">
          <div className="flex min-h-14 shrink-0 flex-col gap-2 py-2 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-wrap items-center gap-2">
              <SearchInput
                placeholder="Search indexed data..."
                value={search}
                onChange={(value) => {
                  setSearch(value);
                  setPageIndex(0);
                }}
                className="w-full lg:w-72"
              />
              <Select
                value={selectedConnection?.id ?? "none"}
                onValueChange={(value) => {
                  if (value && value !== "none") updateConnection(value);
                }}
              >
                <SelectTrigger className="w-[220px]">
                  <SelectValue placeholder="Connector" />
                </SelectTrigger>
                <SelectContent>
                  {connections.map((connection) => (
                    <SelectItem key={connection.id} value={connection.id}>
                      {connection.displayName}
                    </SelectItem>
                  ))}
                  {connections.length === 0 ? (
                    <SelectItem value="none" disabled>
                      No connectors
                    </SelectItem>
                  ) : null}
                </SelectContent>
              </Select>
              <Select
                value={effectiveEntity || "none"}
                onValueChange={(value) => {
                  if (value && value !== "none") updateEntity(value);
                }}
              >
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Entity" />
                </SelectTrigger>
                <SelectContent>
                  {entityOptions.map((item) => (
                    <SelectItem key={item} value={item}>
                      {item.replaceAll("_", " ")}
                    </SelectItem>
                  ))}
                  {entityOptions.length === 0 ? (
                    <SelectItem value="none" disabled>
                      No entities
                    </SelectItem>
                  ) : null}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="min-h-0 flex-1 overflow-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Connector</TableHead>
                  <TableHead>Entity</TableHead>
                  <TableHead>Title</TableHead>
                  <TableHead>External updated</TableHead>
                  <TableHead>Indexed updated</TableHead>
                  <TableHead>Metadata</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {page.records.map((record) => {
                  const connector = connectorById.get(record.connectionId);
                  return (
                    <TableRow
                      key={record.id}
                      onClick={() => router.push(`/atlas/explorer/${record.id}`)}
                      className="cursor-pointer"
                    >
                      <TableCell className="font-medium">
                        {connector?.displayName ?? record.connectionId}
                      </TableCell>
                      <TableCell className="font-mono text-xs">
                        {record.entity}
                      </TableCell>
                      <TableCell className="max-w-[20rem]">
                        <div className="truncate font-medium">{record.title}</div>
                        <div className="truncate font-mono text-xs text-muted-foreground">
                          {record.externalId}
                        </div>
                      </TableCell>
                      <TableCell>
                        {record.externalUpdatedAt
                          ? formatDate(record.externalUpdatedAt)
                          : "Unknown"}
                      </TableCell>
                      <TableCell>{formatDate(record.updatedAt)}</TableCell>
                      <TableCell className="max-w-[14rem] truncate text-xs text-muted-foreground">
                        {deriveSourceMetadata(record)}
                      </TableCell>
                    </TableRow>
                  );
                })}
                {!recordsLoading && page.records.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} className="p-0">
                      <EmptyState
                        message={
                          connectionsLoading
                            ? "Loading connectors..."
                            : "No indexed records found."
                        }
                      />
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>

          <div className="shrink-0 py-2">
            <DataPagination
              page={pageIndex}
              pageSize={pageSize}
              total={estimatedTotal}
              pageSizes={PAGE_SIZES}
              onPageChange={(nextPage) => {
                setPageIndex(nextPage);
              }}
              onPageSizeChange={(nextPageSize) => {
                setPageSize(nextPageSize);
                setPageIndex(0);
              }}
            />
          </div>
        </section>
      </PageContainer>
    </>
  );
}

function replaceUrl(
  pathname: string,
  router: ReturnType<typeof useRouter>,
  connectionId: string,
  entity: string,
) {
  const params = new URLSearchParams();
  if (connectionId) params.set("connectionId", connectionId);
  if (entity) params.set("entity", entity);
  router.replace(params.size > 0 ? `${pathname}?${params.toString()}` : pathname);
}

function deriveSourceMetadata(record: AtlasIndexedRecord) {
  const raw = parseJsonObject(record.rawJson);
  if (record.entity === "commits") {
    return stringValue(raw.sha) ?? record.externalId;
  }
  if (record.entity === "repositories") {
    return stringValue(raw.full_name) ?? record.externalId;
  }
  const number = stringValue(raw.number);
  const state = stringValue(raw.state);
  return [number ? `#${number}` : null, state].filter(Boolean).join(" · ") ||
    record.externalId;
}

function parseJsonObject(value: string): Record<string, unknown> {
  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed)
      ? parsed
      : {};
  } catch {
    return {};
  }
}

function stringValue(value: unknown): string | null {
  if (value === null || value === undefined) return null;
  if (typeof value === "string") return value;
  if (typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }
  return null;
}

function formatDate(value: string) {
  try {
    return formatDistanceToNow(new Date(value), { addSuffix: true });
  } catch {
    return "Unknown";
  }
}
