"use client";

import { use, useMemo, useState } from "react";
import Link from "next/link";
import { notFound, useRouter } from "next/navigation";
import { ArrowLeftIcon } from "lucide-react";
import { PageContainer } from "@/shell/page-container";
import { Button } from "@/ui/button";
import { Skeleton } from "@/ui/skeleton";
import {
  CredentialSetup,
  useIntegration,
  useIntegrationCatalog,
  useSaveIntegrationCredential,
} from "@/features/agents";

function IntegrationSetupSkeleton() {
  return (
    <PageContainer width="narrow" className="flex flex-1 items-start py-8">
      <div className="w-full rounded-lg border border-border bg-background p-6 shadow-sm">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-4 h-64 w-full rounded-lg" />
      </div>
    </PageContainer>
  );
}

export default function IntegrationSetupPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const router = useRouter();
  const { slug } = use(params);
  const [selectedName, setSelectedName] = useState(slug);
  const { integration, loading } = useIntegration(slug);
  const { integrations, loading: catalogLoading } = useIntegrationCatalog();
  const saveCredential = useSaveIntegrationCredential();
  const selectedIntegration = useMemo(
    () =>
      integrations.find((candidate) => candidate.name === selectedName) ??
      integration,
    [integration, integrations, selectedName],
  );

  if (!integration || integrations.length === 0) {
    if (loading || catalogLoading) return <IntegrationSetupSkeleton />;
    return notFound();
  }

  return (
    <PageContainer width="narrow" className="flex flex-1 items-start py-8">
      <div className="w-full rounded-lg border border-border bg-background p-6 shadow-sm">
        <div className="mb-5 flex justify-end">
          <Button
            size="sm"
            variant="outline"
            nativeButton={false}
            render={<Link href={`/integrations/${integration.name}`} />}
          >
            <ArrowLeftIcon className="size-4" />
            Back
          </Button>
        </div>
        <CredentialSetup
          integrations={integrations}
          selectedName={selectedIntegration?.name ?? selectedName}
          onSelectedNameChange={setSelectedName}
          returnTo={(server) => `/integrations/${server.name}`}
          onSave={(server, values) => saveCredential(server.name, values)}
          onSaved={() =>
            router.push(`/integrations/${selectedIntegration?.name ?? selectedName}`)
          }
        />
      </div>
    </PageContainer>
  );
}
