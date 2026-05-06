"use client";

import Link from "next/link";
import { ArrowLeftIcon } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import { CustomMcpJsonEditor, useMcpServers } from "@/features/agents";

export default function CustomMcpPage() {
  const { servers, loading } = useMcpServers();

  return (
    <>
      <PageHeader
        group="MCP Servers"
        page="Custom MCP servers"
        width="wide"
        action={
          <Button
            size="sm"
            variant="outline"
            nativeButton={false}
            render={<Link href="/integrations" />}
          >
            <ArrowLeftIcon className="size-4" />
            Back
          </Button>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col pb-4">
        <CustomMcpJsonEditor servers={servers} loading={loading} />
      </PageContainer>
    </>
  );
}
