"use client";

import { use } from "react";
import Link from "next/link";
import { PageContainer } from "@/components/page-container";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { MemoryStoreDetail, useMemoryStore } from "@/features/agents";

export default function MemoryStoreDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const { memoryStore } = useMemoryStore(id);

  return (
    <div className="flex min-h-svh flex-col">
      <PageHeader
        group="Managed Agents"
        page={memoryStore?.displayName ?? "Memory Store"}
        subtitle="Memory store details."
        width="wide"
        action={
          <Button variant="outline" size="sm" nativeButton={false} render={<Link href="/memory-stores" />}>
            All memory stores
          </Button>
        }
      />
      <PageContainer width="wide" className="flex min-h-0 flex-1 flex-col pb-4">
        <MemoryStoreDetail memoryStoreId={id} />
      </PageContainer>
    </div>
  );
}
