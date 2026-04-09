"use client";

export const dynamic = "force-dynamic";

import { useMemo } from "react";
import { useParams, useRouter } from "next/navigation";

import { getSessionToken, useAuth } from "@/auth/clerk";
import { DashboardPageLayout } from "@/components/templates/DashboardPageLayout";
import { Button } from "@/components/ui/button";

import { ApiError } from "@/api/mutator";
import { getApiBaseUrl } from "@/lib/api-base";
import {
  type gatewaysStatusApiV1GatewaysStatusGetResponse,
  type getGatewayApiV1GatewaysGatewayIdGetResponse,
  useGatewaysStatusApiV1GatewaysStatusGet,
  useGetGatewayApiV1GatewaysGatewayIdGet,
} from "@/api/generated/gateways/gateways";
import { formatTimestamp } from "@/lib/formatters";
import { useOrganizationMembership } from "@/lib/use-organization-membership";

const maskToken = (value?: string | null) => {
  if (!value) return "—";
  if (value.length <= 8) return "••••";
  return `••••${value.slice(-4)}`;
};

export default function AgentDetailPage() {
  const router = useRouter();
  const params = useParams();
  const { isSignedIn } = useAuth();
  const agentIdParam = params?.agentId;
  const agentId = Array.isArray(agentIdParam) ? agentIdParam[0] : agentIdParam;

  const { isAdmin } = useOrganizationMembership(isSignedIn);

  // Backend still calls the underlying entity a "gateway" — we keep the
  // generated hooks as-is and only rename at the presentation layer.
  const gatewayQuery = useGetGatewayApiV1GatewaysGatewayIdGet<
    getGatewayApiV1GatewaysGatewayIdGetResponse,
    ApiError
  >(agentId ?? "", {
    query: {
      enabled: Boolean(isSignedIn && isAdmin && agentId),
      refetchInterval: 30_000,
    },
  });

  const gateway =
    gatewayQuery.data?.status === 200 ? gatewayQuery.data.data : null;

  const statusParams = gateway
    ? {
        gateway_url: gateway.url,
        gateway_token: gateway.token ?? undefined,
        gateway_disable_device_pairing: gateway.disable_device_pairing,
        gateway_allow_insecure_tls: gateway.allow_insecure_tls,
      }
    : {};

  const statusQuery = useGatewaysStatusApiV1GatewaysStatusGet<
    gatewaysStatusApiV1GatewaysStatusGetResponse,
    ApiError
  >(statusParams, {
    query: {
      enabled: Boolean(isSignedIn && isAdmin && gateway),
      refetchInterval: 15_000,
    },
  });

  const status =
    statusQuery.data?.status === 200 ? statusQuery.data.data : null;
  const isConnected = status?.connected ?? false;

  const title = useMemo(
    () => (gateway?.name ? gateway.name : "Agent"),
    [gateway?.name],
  );

  return (
    <DashboardPageLayout
      signedOut={{
        message: "Sign in to view an agent.",
        forceRedirectUrl: `/agents/${agentId}`,
      }}
      title={title}
      description="Agent configuration and connection details."
      headerActions={
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={() => router.push("/agents")}>
            Back to agents
          </Button>
          {agentId ? (
            <Button
              variant="outline"
              onClick={async () => {
                // The UI proxy requires a short-lived gateway-scoped
                // JWT. Mint it with the user's normal dashboard
                // session bearer, pass it in the URL query string
                // (the proxy promotes it to a Path-scoped cookie on
                // the first request so the SPA's internal fetches
                // inherit auth automatically).
                try {
                  const base = getApiBaseUrl();
                  const token = getSessionToken();
                  const mintResp = await fetch(
                    `${base}/api/gateways/${agentId}/ui-token`,
                    {
                      method: "POST",
                      headers: token
                        ? { Authorization: `Bearer ${token}` }
                        : {},
                    },
                  );
                  if (!mintResp.ok) {
                    throw new Error(
                      `Failed to mint UI token: ${mintResp.status}`,
                    );
                  }
                  const { token: uiToken } = await mintResp.json();
                  window.open(
                    `${base}/api/gateways/${agentId}/ui/?t=${encodeURIComponent(uiToken)}`,
                    "_blank",
                    "noopener,noreferrer",
                  );
                } catch (err) {
                  console.error("Open agent dashboard failed:", err);
                }
              }}
            >
              Open agent dashboard
            </Button>
          ) : null}
          {isAdmin && agentId ? (
            <Button onClick={() => router.push(`/agents/${agentId}/edit`)}>
              Edit agent
            </Button>
          ) : null}
        </div>
      }
      isAdmin={isAdmin}
      adminOnlyMessage="Only organization owners and admins can access agents."
    >
      {gatewayQuery.isLoading ? (
        <div className="rounded-xl border border-slate-200 bg-white p-6 text-sm text-slate-500 shadow-sm">
          Loading agent…
        </div>
      ) : gatewayQuery.error ? (
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-6 text-sm text-rose-700">
          {gatewayQuery.error.message}
        </div>
      ) : gateway ? (
        <div className="grid gap-6 lg:grid-cols-2">
          <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
            <div className="flex items-center justify-between">
              <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
                Connection
              </p>
              <div className="flex items-center gap-2 text-xs text-slate-500">
                <span
                  className={`h-2 w-2 rounded-full ${
                    statusQuery.isLoading
                      ? "bg-slate-300"
                      : isConnected
                        ? "bg-emerald-500"
                        : "bg-rose-500"
                  }`}
                />
                <span>
                  {statusQuery.isLoading
                    ? "Checking"
                    : isConnected
                      ? "Online"
                      : "Offline"}
                </span>
              </div>
            </div>
            <div className="mt-4 space-y-3 text-sm text-slate-700">
              <div>
                <p className="text-xs uppercase text-slate-400">Agent URL</p>
                <p className="mt-1 text-sm font-medium text-slate-900">
                  {gateway.url}
                </p>
              </div>
              <div>
                <p className="text-xs uppercase text-slate-400">Token</p>
                <p className="mt-1 text-sm font-medium text-slate-900">
                  {maskToken(gateway.token)}
                </p>
              </div>
              <div>
                <p className="text-xs uppercase text-slate-400">
                  Device pairing
                </p>
                <p className="mt-1 text-sm font-medium text-slate-900">
                  {gateway.disable_device_pairing ? "Disabled" : "Required"}
                </p>
              </div>
            </div>
          </div>

          <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
              Runtime
            </p>
            <div className="mt-4 space-y-3 text-sm text-slate-700">
              <div>
                <p className="text-xs uppercase text-slate-400">
                  Workspace root
                </p>
                <p className="mt-1 text-sm font-medium text-slate-900">
                  {gateway.workspace_root}
                </p>
              </div>
              <div className="grid gap-3 sm:grid-cols-2">
                <div>
                  <p className="text-xs uppercase text-slate-400">Created</p>
                  <p className="mt-1 text-sm font-medium text-slate-900">
                    {formatTimestamp(gateway.created_at)}
                  </p>
                </div>
                <div>
                  <p className="text-xs uppercase text-slate-400">Updated</p>
                  <p className="mt-1 text-sm font-medium text-slate-900">
                    {formatTimestamp(gateway.updated_at)}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      ) : null}
    </DashboardPageLayout>
  );
}
