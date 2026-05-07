"use client";

import { useSearchParams } from "next/navigation";
import { formatDistanceToNow } from "date-fns";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useAtlasHistory } from "@/features/atlas";

export default function AtlasHistoryPage() {
  const searchParams = useSearchParams();
  const connectionId = searchParams.get("connectionId");
  const { history, loading } = useAtlasHistory(connectionId);

  return (
    <>
      <PageHeader
        group="Atlas"
        page="History"
        subtitle="Agent request history for Atlas connectors."
        width="thin"
      />
      <PageContainer width="thin" className="flex flex-1 flex-col pb-4">
        <div className="min-h-0 overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Type</TableHead>
                <TableHead>Entity</TableHead>
                <TableHead>Action</TableHead>
                <TableHead>Timestamp</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Duration</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {history.map((item) => (
                <TableRow key={item.id}>
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
              {!loading && history.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={6}
                    className="py-10 text-center text-muted-foreground"
                  >
                    No Atlas request history yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
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
