"use client";

import Link from "next/link";
import { useState } from "react";
import { PlusIcon } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
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
} from "@/components/ui/table";
import {
  ChannelOnboardingDialog,
  useChannelConnections,
} from "@/features/agents";
import { useAnalytics } from "@/features/analytics";
import { Skeleton } from "@/components/ui/skeleton";
import type { Channel } from "@/features/agents/data/channels";

export default function ChannelsPage() {
  const { connections, channelTypes, loading } = useChannelConnections();
  const { trackChannelConnected } = useAnalytics();
  const [pickerOpen, setPickerOpen] = useState(false);
  const [onboardingChannel, setOnboardingChannel] = useState<Channel | null>(
    null,
  );

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Channels"
        subtitle="Manage communication channels agents can mount into sessions."
        width="thin"
        action={
          <Button size="sm" onClick={() => setPickerOpen(true)}>
            <PlusIcon className="size-4" />
            Add channel
          </Button>
        }
      />
      <PageContainer width="thin" className="flex flex-1 flex-col pb-4">
        <div className="min-h-0 overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
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
              {connections.map((connection) => (
                <TableRow key={connection.id}>
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
                  <TableCell>
                    <Link
                      href={`/channels/${connection.id}`}
                      className="font-medium hover:underline"
                    >
                      {connection.displayName}
                    </Link>
                  </TableCell>
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
              {!loading && connections.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={5}
                    className="py-10 text-center text-muted-foreground"
                  >
                    No channels yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
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
