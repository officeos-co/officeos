"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { PlusIcon, Trash2Icon } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";
import { SearchInput } from "@/components/ui/search-input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  TableSelectionCell,
  TableSelectionHead,
  TableSelectionToolbar,
} from "@/components/ui/table";
import {
  ChannelOnboardingDialog,
  useChannelConnections,
  useDeleteChannelConnection,
} from "@/features/agents";
import { useAnalytics } from "@/features/analytics";
import { Skeleton } from "@/components/ui/skeleton";
import type { Channel } from "@/features/agents/data/channels";

export default function ChannelsPage() {
  const router = useRouter();
  const { connections, channelTypes, loading } = useChannelConnections();
  const { deleteChannelConnection } = useDeleteChannelConnection();
  const { trackChannelConnected } = useAnalytics();
  const [search, setSearch] = useState("");
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [pickerOpen, setPickerOpen] = useState(false);
  const [onboardingChannel, setOnboardingChannel] = useState<Channel | null>(
    null,
  );

  const filtered = useMemo(() => {
    const query = search.toLowerCase();
    return connections.filter((connection) => {
      if (!query) return true;
      const status = connection.enabled ? "enabled" : "disabled";
      return (
        connection.id.toLowerCase().includes(query) ||
        connection.displayName.toLowerCase().includes(query) ||
        connection.typeDisplayName.toLowerCase().includes(query) ||
        status.includes(query)
      );
    });
  }, [connections, search]);
  const filteredIds = useMemo(
    () => filtered.map((connection) => connection.id),
    [filtered],
  );
  const selectedVisibleCount = filteredIds.filter((id) =>
    selectedIds.has(id),
  ).length;
  const allVisibleSelected =
    filteredIds.length > 0 && selectedVisibleCount === filteredIds.length;
  const someVisibleSelected = selectedVisibleCount > 0 && !allVisibleSelected;

  function toggleConnection(connectionId: string, checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) next.add(connectionId);
      else next.delete(connectionId);
      return next;
    });
  }

  function toggleVisibleConnections(checked: boolean) {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      for (const id of filteredIds) {
        if (checked) next.add(id);
        else next.delete(id);
      }
      return next;
    });
  }

  async function deleteSelectedConnections() {
    const ids = Array.from(selectedIds);
    await Promise.all(ids.map((id) => deleteChannelConnection(id)));
    setSelectedIds(new Set());
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Channels"
        subtitle="Manage communication channels agents can mount into sessions."
        width="wide"
        action={
          <Button size="sm" onClick={() => setPickerOpen(true)}>
            <PlusIcon className="size-4" />
            Add channel
          </Button>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col gap-4 pb-4">
        <div className="flex min-h-9 items-center justify-between gap-2">
          <SearchInput
            placeholder="Search channels..."
            value={search}
            onChange={setSearch}
          />
          <TableSelectionToolbar selectedCount={selectedIds.size}>
            <Button
              variant="destructive"
              size="sm"
              onClick={deleteSelectedConnections}
            >
              <Trash2Icon className="size-4" />
              Delete
            </Button>
          </TableSelectionToolbar>
        </div>

        <Table>
          <TableHeader>
            <TableRow>
              <TableSelectionHead
                checked={allVisibleSelected}
                indeterminate={someVisibleSelected}
                onCheckedChange={toggleVisibleConnections}
              />
              <TableHead className="w-12" />
              <TableHead>Name</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Created</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading &&
              connections.length === 0 &&
              Array.from({ length: 6 }).map((_, index) => (
                <TableRow key={`channel-skeleton-${index}`}>
                  <TableCell className="w-10 px-3">
                    <Skeleton className="size-4 rounded" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="size-8 rounded-lg" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-40" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-28" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-5 w-16 rounded-full" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-32" />
                  </TableCell>
                </TableRow>
              ))}
            {filtered.map((connection) => (
              <TableRow
                key={connection.id}
                data-state={selectedIds.has(connection.id) ? "selected" : undefined}
                onClick={() => router.push(`/channels/${connection.id}`)}
                className="cursor-pointer"
              >
                <TableSelectionCell
                  checked={selectedIds.has(connection.id)}
                  aria-label={`Select ${connection.displayName}`}
                  onCheckedChange={(checked) =>
                    toggleConnection(connection.id, checked)
                  }
                />
                <TableCell>
                  {connection.logo ? (
                    <span
                      className="flex size-8 items-center justify-center rounded-lg border border-border bg-muted/40 [&>svg]:size-5"
                      dangerouslySetInnerHTML={{ __html: connection.logo }}
                    />
                  ) : (
                    <span className="block size-8 rounded-lg bg-muted" />
                  )}
                </TableCell>
                <TableCell>{connection.displayName}</TableCell>
                <TableCell>{connection.typeDisplayName}</TableCell>
                <TableCell>
                  <span
                    className={
                      connection.enabled
                        ? "rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700"
                        : "rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground"
                    }
                  >
                    {connection.enabled ? "Enabled" : "Disabled"}
                  </span>
                </TableCell>
                <TableCell>{formatDate(connection.createdAt)}</TableCell>
              </TableRow>
            ))}
            {!loading && filtered.length === 0 && (
              <TableRow>
                <TableCell colSpan={6} className="p-0">
                  <EmptyState message="No channels found." />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>

      <Dialog open={pickerOpen} onOpenChange={setPickerOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Add channel</DialogTitle>
            <DialogDescription>
              Create a channel resource that can be attached to an agent
              session.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            {channelTypes.map((channel) => (
              <button
                key={channel.slug}
                type="button"
                className="flex w-full items-start gap-3 rounded-lg border border-border p-3 text-left transition-colors hover:bg-muted/50"
                onClick={() => {
                  setPickerOpen(false);
                  setOnboardingChannel(channel);
                }}
              >
                <span
                  className="size-7 shrink-0 [&>svg]:size-7"
                  dangerouslySetInnerHTML={{ __html: channel.logo }}
                />
                <span className="min-w-0">
                  <span className="block text-sm font-medium">
                    {channel.name}
                  </span>
                  <span className="line-clamp-2 text-xs text-muted-foreground">
                    {channel.description || "Create a channel connection."}
                  </span>
                </span>
              </button>
            ))}
            {!loading && channelTypes.length === 0 && (
              <p className="py-6 text-center text-sm text-muted-foreground">
                No channel types available.
              </p>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {onboardingChannel && (
        <ChannelOnboardingDialog
          open={!!onboardingChannel}
          onOpenChange={(open) => {
            if (!open) setOnboardingChannel(null);
          }}
          channel={onboardingChannel}
          onComplete={() => {
            trackChannelConnected(onboardingChannel.slug);
            setOnboardingChannel(null);
          }}
        />
      )}
    </>
  );
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleString();
}
