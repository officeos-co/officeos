"use client";

import Link from "next/link";
import { ArrowLeftIcon } from "lucide-react";
import { PageHeader } from "@/shell/page-header";
import { PageContainer } from "@/shell/page-container";
import { Button } from "@/ui/button";
import { CustomMcpJsonEditor, useIntegrations } from "@/features/agents";

export default function CustomMcpPage() {
  const { integrations, loading } = useIntegrations();

  return (
    <>
      <PageHeader
        group="Integrations"
        page="Custom MCP integrations"
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
        <CustomMcpJsonEditor servers={integrations} loading={loading} />
      </PageContainer>
    </>
  );
}
