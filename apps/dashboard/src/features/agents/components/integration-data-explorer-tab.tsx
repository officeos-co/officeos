"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
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
import type { McpServer } from "@/features/agents/data/integrations";
import {
  type AtlasConnection,
  parseJsonArray,
  useIntegrationIndexedRecords,
} from "@/features/atlas";

export function IntegrationDataExplorerTab({
  integration,
  connections,
  indexedModeEnabled,
  onEnableIndexedMode,
}: {
  integration: McpServer;
  connections: AtlasConnection[];
  indexedModeEnabled: boolean;
  onEnableIndexedMode: () => void;
}) {
  const [connectionId, setConnectionId] = useState("");
  const [entity, setEntity] = useState("");
  const [search, setSearch] = useState("");
  const filteredConnections = useMemo(
    () =>
      connections.filter((connection) =>
        providerMatches(
          connection.provider,
          integration.provider || integration.name,
        ),
      ),
    [connections, integration.name, integration.provider],
  );
  const selectedConnection =
    filteredConnections.find((connection) => connection.id === connectionId) ??
    filteredConnections[0] ??
    null;
  const entityOptions = selectedConnection
    ? parseJsonArray(selectedConnection.entitiesJson)
    : [];
  const effectiveEntity = entityOptions.includes(entity)
    ? entity
    : (entityOptions[0] ?? "");
  const { page, loading } = useIntegrationIndexedRecords({
    connectionId: selectedConnection?.id,
    entity: effectiveEntity,
    query: search,
    limit: 25,
  });

  if (filteredConnections.length === 0) {
    return (
      <div className="pt-4">
        <EmptyState message="No indexed data sources are configured for this integration." />
      </div>
    );
  }

  return (
    <section className="space-y-3 pt-4">
      {!indexedModeEnabled && (
        <div className="rounded-lg border border-sky-200 bg-sky-50 p-4">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div>
              <div className="text-sm font-medium text-sky-900">
                Indexed mode is not enabled
              </div>
              <p className="mt-1 text-xs text-sky-800">
                Enable indexed mode when agents should use this indexed data at
                runtime.
              </p>
            </div>
            <Button size="sm" onClick={onEnableIndexedMode}>
              Enable indexed mode
            </Button>
          </div>
        </div>
      )}

      <div className="flex flex-col gap-2 lg:flex-row lg:items-center">
        <SearchInput
          placeholder="Search indexed data..."
          value={search}
          onChange={setSearch}
          className="lg:w-72"
        />
        <Select
          value={selectedConnection?.id ?? "none"}
          onValueChange={(value) => {
            setConnectionId(value ?? "");
            setEntity("");
          }}
        >
          <SelectTrigger className="lg:w-[220px]">
            <SelectValue>{selectedConnection?.displayName}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {filteredConnections.map((connection) => (
              <SelectItem key={connection.id} value={connection.id}>
                {connection.displayName}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={effectiveEntity || "none"}
          onValueChange={(value) => setEntity(value ?? "")}
        >
          <SelectTrigger className="lg:w-[180px]">
            <SelectValue>
              {effectiveEntity
                ? effectiveEntity.replaceAll("_", " ")
                : "Entity"}
            </SelectValue>
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

      <div className="overflow-hidden rounded-lg border border-border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Title</TableHead>
              <TableHead>Entity</TableHead>
              <TableHead>External ID</TableHead>
              <TableHead>Indexed</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {page.records.map((record) => (
              <TableRow key={record.id}>
                <TableCell className="max-w-[20rem]">
                  <div className="truncate font-medium">{record.title}</div>
                  <div className="truncate text-xs text-muted-foreground">
                    {record.searchText}
                  </div>
                </TableCell>
                <TableCell className="font-mono text-xs">
                  {record.entity}
                </TableCell>
                <TableCell className="max-w-[12rem] truncate font-mono text-xs">
                  {record.externalId}
                </TableCell>
                <TableCell>{formatDate(record.updatedAt)}</TableCell>
              </TableRow>
            ))}
            {!loading && page.records.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} className="p-0">
                  <EmptyState message="No indexed records found." />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
    </section>
  );
}

function providerMatches(left: string, right: string) {
  const normalize = (value: string) =>
    value.toLowerCase().replace(/[^a-z0-9]/g, "");
  return normalize(left) === normalize(right);
}

function formatDate(value: string) {
  try {
    return new Intl.DateTimeFormat(undefined, {
      month: "short",
      day: "numeric",
      hour: "numeric",
      minute: "2-digit",
    }).format(new Date(value));
  } catch {
    return "Unknown";
  }
}
