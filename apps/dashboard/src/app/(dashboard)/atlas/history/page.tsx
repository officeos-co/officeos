"use client";

import { useMemo, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { formatDistanceToNow } from "date-fns";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { DataPagination } from "@/components/ui/data-pagination";
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
import { useAtlasConnections, useAtlasHistory } from "@/features/atlas";

const ALL_CONNECTORS = "All";
const PAGE_SIZES = [10, 25, 50] as const;

export default function AtlasHistoryPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const initialConnectionId = searchParams.get("connectionId") ?? ALL_CONNECTORS;
  const { connections } = useAtlasConnections({ pollInterval: 5000 });
  const { history, loading } = useAtlasHistory(null, { pollInterval: 5000 });
  const [search, setSearch] = useState("");
  const [connectionFilter, setConnectionFilter] = useState(initialConnectionId);
  const [pageSize, setPageSize] = useState<number>(25);
  const [page, setPage] = useState(0);

  const connectorById = useMemo(() => {
    const mapped = new Map<string, string>();
    for (const connection of connections) {
      mapped.set(connection.id, connection.displayName);
    }
    return mapped;
  }, [connections]);

  const filtered = useMemo(() => {
    const needle = search.trim().toLowerCase();
    return history.filter((item) => {
      if (
        connectionFilter !== ALL_CONNECTORS &&
        item.connectionId !== connectionFilter
      ) {
        return false;
      }
      if (!needle) return true;
      return [
        item.type,
        item.entity,
        item.action,
        item.error ?? "",
        item.paramsJson,
      ].some((value) => value.toLowerCase().includes(needle));
    });
  }, [connectionFilter, history, search]);

  const paged = filtered.slice(page * pageSize, (page + 1) * pageSize);

  function updateConnectionFilter(value: string) {
    setConnectionFilter(value);
    setPage(0);
    if (value === ALL_CONNECTORS) {
      router.replace(pathname);
      return;
    }
    const params = new URLSearchParams({ connectionId: value });
    router.replace(`${pathname}?${params.toString()}`);
  }

  return (
    <>
      <PageHeader
        group="Atlas"
        page="History"
        subtitle="Agent request history for Atlas connectors."
        width="thin"
      />
      <PageContainer width="thin" className="flex flex-1 flex-col gap-4 pb-4">
        <section className="flex min-h-0 min-w-0 flex-col overflow-hidden">
          <div className="flex min-h-14 shrink-0 items-center justify-between gap-3 py-2">
            <div className="flex items-center gap-2">
              <SearchInput
                placeholder="Search history..."
                value={search}
                onChange={(value) => {
                  setSearch(value);
                  setPage(0);
                }}
              />
              <Select
                value={connectionFilter}
                onValueChange={(value) => {
                  if (value) updateConnectionFilter(value);
                }}
              >
                <SelectTrigger className="w-[220px]">
                  <SelectValue placeholder="Connector">
                    {connectionFilter === ALL_CONNECTORS
                      ? "All connectors"
                      : connections.find(
                          (connection) => connection.id === connectionFilter,
                        )?.displayName ?? "Unknown connector"}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={ALL_CONNECTORS}>All connectors</SelectItem>
                  {connectionFilter !== ALL_CONNECTORS &&
                  !connections.some(
                    (connection) => connection.id === connectionFilter,
                  ) ? (
                    <SelectItem value={connectionFilter}>
                      Unknown connector
                    </SelectItem>
                  ) : null}
                  {connections.map((connection) => (
                    <SelectItem key={connection.id} value={connection.id}>
                      {connection.displayName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="min-h-0 flex-1 overflow-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Connector</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Entity</TableHead>
                  <TableHead>Action</TableHead>
                  <TableHead>Timestamp</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Duration</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {paged.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell className="font-medium">
                      {connectorById.get(item.connectionId) ?? item.connectionId}
                    </TableCell>
                    <TableCell>{item.type}</TableCell>
                    <TableCell className="font-mono text-xs">
                      {item.entity}
                    </TableCell>
                    <TableCell className="font-mono text-xs">
                      {item.action}
                    </TableCell>
                    <TableCell>{formatDate(item.createdAt)}</TableCell>
                    <TableCell>
                      <span
                        className={
                          item.success ? "text-emerald-700" : "text-red-700"
                        }
                      >
                        {item.success ? "Success" : "Failed"}
                      </span>
                    </TableCell>
                    <TableCell>{item.durationMs}ms</TableCell>
                  </TableRow>
                ))}
                {!loading && filtered.length === 0 && (
                  <TableRow>
                    <TableCell
                      colSpan={7}
                      className="py-10 text-center text-muted-foreground"
                    >
                      No Atlas request history found.
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </div>

          <div className="shrink-0 py-2">
            <DataPagination
              page={page}
              pageSize={pageSize}
              total={filtered.length}
              pageSizes={PAGE_SIZES}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size);
                setPage(0);
              }}
            />
          </div>
        </section>
      </PageContainer>
    </>
  );
}

function formatDate(value: string) {
  try {
    return formatDistanceToNow(new Date(value), { addSuffix: true });
  } catch {
    return "Unknown";
  }
}
