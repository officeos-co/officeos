"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import {
  BracesIcon,
  CheckCircle2Icon,
  PlusIcon,
} from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { PageContainer } from "@/components/page-container";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  CredentialDialog,
  ConnectorDirectoryDialog,
  sortIntegrations,
  useIntegrations,
  useSetSkillCredentials,
} from "@/features/agents";
import type { McpServer } from "@/features/agents/data/integrations";
import { buildOAuthUrl } from "@/lib/auth-url";
import { Skeleton } from "@/components/ui/skeleton";

export default function IntegrationsPage() {
  const { integrations, loading } = useIntegrations();
  const setCredentials = useSetSkillCredentials();
  const [directoryOpen, setDirectoryOpen] = useState(false);
  const [configSlug, setConfigSlug] = useState<string | null>(null);

  const sorted = useMemo(
    () => sortIntegrations(integrations),
    [integrations],
  );
  const configured = sorted.filter((server) => server.configured);
  const configIntegration = configSlug
    ? integrations.find((server) => server.name === configSlug)
    : null;

  function startSetup(server: McpServer) {
    if (server.oauthProvider) {
      window.location.assign(buildOAuthUrl(server.oauthProvider, "/integrations"));
      return;
    }
    setConfigSlug(server.name);
  }

  return (
    <>
      <PageHeader
        group="Managed Agents"
        page="Connectors"
        subtitle="Manage MCP connector resources agents can attach during creation."
        width="wide"
        action={
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              variant="outline"
              nativeButton={false}
              render={<Link href="/integrations/custom-mcp" />}
            >
              <BracesIcon className="size-4" />
              Custom MCP
            </Button>
            <Button size="sm" onClick={() => setDirectoryOpen(true)}>
              <PlusIcon className="size-4" />
              Add connector
            </Button>
          </div>
        }
      />
      <PageContainer width="wide" className="flex flex-1 flex-col pb-4">
        <div className="min-h-0 overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-12" />
                <TableHead>Name</TableHead>
                <TableHead>Category</TableHead>
                <TableHead>Tools</TableHead>
                <TableHead>Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading &&
                configured.length === 0 &&
                Array.from({ length: 6 }).map((_, index) => (
                  <TableRow key={`connector-skeleton-${index}`}>
                    <TableCell>
                      <Skeleton className="size-8 rounded-lg" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-4 w-36" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-4 w-24" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-4 w-12" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-5 w-20 rounded-full" />
                    </TableCell>
                  </TableRow>
                ))}
              {configured.map((server) => (
                <TableRow key={server.name}>
                  <TableCell>
                    <span
                      className="flex size-8 items-center justify-center rounded-lg border border-border bg-muted/40 [&>img]:size-5 [&>img]:object-contain [&>svg]:size-5"
                      dangerouslySetInnerHTML={{ __html: server.logo }}
                    />
                  </TableCell>
                  <TableCell>
                    <Link
                      href={`/integrations/${server.name}`}
                      className="font-medium hover:underline"
                    >
                      {server.title}
                    </Link>
                    {server.subtitle && (
                      <div className="text-xs text-muted-foreground">
                        {server.subtitle}
                      </div>
                    )}
                  </TableCell>
                  <TableCell>{server.category || "Connector"}</TableCell>
                  <TableCell>{server.tools.length}</TableCell>
                  <TableCell>
                    <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700">
                      <CheckCircle2Icon className="size-3" />
                      Connected
                    </span>
                  </TableCell>
                </TableRow>
              ))}
              {!loading && configured.length === 0 && (
                <TableRow>
                  <TableCell
                    colSpan={5}
                    className="py-10 text-center text-muted-foreground"
                  >
                    No connectors connected yet.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      </PageContainer>

      <ConnectorDirectoryDialog
        open={directoryOpen}
        onOpenChange={setDirectoryOpen}
        integrations={sorted}
        onConnect={startSetup}
      />

      {configIntegration && !configIntegration.oauthProvider && (
        <CredentialDialog
          open={!!configSlug}
          onOpenChange={(open) => {
            if (!open) setConfigSlug(null);
          }}
          name={configIntegration.title}
          slug={configIntegration.name}
          logo={configIntegration.logo}
          credentials={configIntegration.credentialFields}
          onSave={async (values) => {
            await setCredentials(configIntegration.name, values);
            setConfigSlug(null);
          }}
        />
      )}
    </>
  );
}
