"use client";

export const dynamic = "force-dynamic";

import { useState } from "react";
import { useRouter } from "next/navigation";

import { useAuth } from "@/auth/clerk";

import { ApiError } from "@/api/mutator";
import { useCreateGatewayApiV1GatewaysPost } from "@/api/generated/gateways/gateways";
import { useOrganizationMembership } from "@/lib/use-organization-membership";
import { GatewayForm } from "@/components/gateways/GatewayForm";
import { DashboardPageLayout } from "@/components/templates/DashboardPageLayout";

export default function NewAgentPage() {
  const { isSignedIn } = useAuth();
  const router = useRouter();

  const { isAdmin } = useOrganizationMembership(isSignedIn);

  const [name, setName] = useState("");
  const [provider, setProvider] = useState("openrouter");
  const [model, setModel] = useState("");
  const [error, setError] = useState<string | null>(null);

  const createMutation = useCreateGatewayApiV1GatewaysPost<ApiError>({
    mutation: {
      onSuccess: (result) => {
        if (result.status === 200) {
          router.push(`/agents/${result.data.id}`);
        }
      },
      onError: (err) => {
        setError(err.message || "Something went wrong.");
      },
    },
  });

  const isLoading = createMutation.isPending;
  const canSubmit = Boolean(name.trim());

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!isSignedIn) return;
    if (!name.trim()) {
      setError("Name is required.");
      return;
    }
    setError(null);
    createMutation.mutate({
      data: {
        name: name.trim(),
        type: "zeroclaw",
        url: "",
        workspace_root: "",
        provider: provider,
        model: model.trim() || null,
      } as any,
    });
  };

  return (
    <DashboardPageLayout
      signedOut={{
        message: "Sign in to create an agent.",
        forceRedirectUrl: "/agents/new",
      }}
      title="Create agent"
      description="Launch a ZeroClaw agent. Memory, per-agent vault databases, and the docker image are all provisioned automatically. Configure provider API keys in Settings → Providers."
      isAdmin={isAdmin}
      adminOnlyMessage="Only organization owners and admins can create agents."
    >
      <GatewayForm
        name={name}
        provider={provider}
        model={model}
        errorMessage={error}
        isLoading={isLoading}
        canSubmit={canSubmit}
        cancelLabel="Cancel"
        submitLabel="Launch agent"
        submitBusyLabel="Launching…"
        onSubmit={handleSubmit}
        onCancel={() => router.push("/agents")}
        onNameChange={setName}
        onProviderChange={setProvider}
        onModelChange={setModel}
      />
    </DashboardPageLayout>
  );
}
