"use client";

export const dynamic = "force-dynamic";

import { useState } from "react";
import { useRouter } from "next/navigation";

import { useAuth } from "@/auth/clerk";

import { ApiError } from "@/api/mutator";
import { useCreateGatewayApiV1GatewaysPost } from "@/api/generated/gateways/gateways";
import { useOrganizationMembership } from "@/lib/use-organization-membership";
import { GatewayForm, type GatewayType } from "@/components/gateways/GatewayForm";
import { DashboardPageLayout } from "@/components/templates/DashboardPageLayout";
import {
  DEFAULT_WORKSPACE_ROOT,
  checkGatewayConnection,
  type GatewayCheckStatus,
  validateGatewayUrl,
} from "@/lib/gateway-form";

export default function NewGatewayPage() {
  const { isSignedIn } = useAuth();
  const router = useRouter();

  const { isAdmin } = useOrganizationMembership(isSignedIn);

  const [gatewayType, setGatewayType] = useState<GatewayType>("openclaw");
  const [name, setName] = useState("");
  const [gatewayUrl, setGatewayUrl] = useState("");
  const [gatewayToken, setGatewayToken] = useState("");
  const [disableDevicePairing, setDisableDevicePairing] = useState(false);
  const [workspaceRoot, setWorkspaceRoot] = useState(DEFAULT_WORKSPACE_ROOT);
  const [allowInsecureTls, setAllowInsecureTls] = useState(false);
  const [dockerImage, setDockerImage] = useState("");
  const [provider, setProvider] = useState("openrouter");
  const [model, setModel] = useState("");
  const [memory, setMemory] = useState("sqlite");

  const [gatewayUrlError, setGatewayUrlError] = useState<string | null>(null);
  const [gatewayCheckStatus, setGatewayCheckStatus] =
    useState<GatewayCheckStatus>("idle");
  const [gatewayCheckMessage, setGatewayCheckMessage] = useState<string | null>(
    null,
  );

  const [error, setError] = useState<string | null>(null);

  const createMutation = useCreateGatewayApiV1GatewaysPost<ApiError>({
    mutation: {
      onSuccess: (result) => {
        if (result.status === 200) {
          router.push(`/gateways/${result.data.id}`);
        }
      },
      onError: (err) => {
        setError(err.message || "Something went wrong.");
      },
    },
  });

  const isLoading =
    createMutation.isPending || gatewayCheckStatus === "checking";

  const isZeroClaw = gatewayType === "zeroclaw";

  const canSubmit = isZeroClaw
    ? Boolean(name.trim())
    : Boolean(name.trim()) &&
      Boolean(gatewayUrl.trim()) &&
      Boolean(workspaceRoot.trim());

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!isSignedIn) return;

    if (!name.trim()) {
      setError("Name is required.");
      return;
    }

    if (isZeroClaw) {
      // ZeroClaw: skip URL/workspace validation, just create
      setError(null);
      createMutation.mutate({
        data: {
          name: name.trim(),
          type: "zeroclaw",
          url: "",
          workspace_root: "",
          docker_image: dockerImage.trim() || null,
          provider: provider,
          model: model.trim() || null,
          memory: memory,
        } as any,
      });
      return;
    }

    // OpenClaw: full validation flow
    const gatewayValidation = validateGatewayUrl(gatewayUrl);
    setGatewayUrlError(gatewayValidation);
    if (gatewayValidation) {
      setGatewayCheckStatus("error");
      setGatewayCheckMessage(gatewayValidation);
      return;
    }
    if (!workspaceRoot.trim()) {
      setError("Workspace root is required.");
      return;
    }

    setGatewayCheckStatus("checking");
    setGatewayCheckMessage(null);
    const { ok, message } = await checkGatewayConnection({
      gatewayUrl,
      gatewayToken,
      gatewayDisableDevicePairing: disableDevicePairing,
      gatewayAllowInsecureTls: allowInsecureTls,
    });
    setGatewayCheckStatus(ok ? "success" : "error");
    setGatewayCheckMessage(message);
    if (!ok) {
      return;
    }

    setError(null);
    createMutation.mutate({
      data: {
        name: name.trim(),
        type: "openclaw",
        url: gatewayUrl.trim(),
        token: gatewayToken.trim() || null,
        disable_device_pairing: disableDevicePairing,
        workspace_root: workspaceRoot.trim(),
        allow_insecure_tls: allowInsecureTls,
      } as any,
    });
  };

  return (
    <DashboardPageLayout
      signedOut={{
        message: "Sign in to create a gateway.",
        forceRedirectUrl: "/gateways/new",
      }}
      title={isZeroClaw ? "Create ZeroClaw agent" : "Create gateway"}
      description={
        isZeroClaw
          ? "Launch a ZeroClaw agent as a Docker container."
          : "Configure an OpenClaw gateway for mission control."
      }
      isAdmin={isAdmin}
      adminOnlyMessage="Only organization owners and admins can create gateways."
    >
      <GatewayForm
        gatewayType={gatewayType}
        name={name}
        gatewayUrl={gatewayUrl}
        gatewayToken={gatewayToken}
        disableDevicePairing={disableDevicePairing}
        workspaceRoot={workspaceRoot}
        allowInsecureTls={allowInsecureTls}
        dockerImage={dockerImage}
        provider={provider}
        model={model}
        memory={memory}
        gatewayUrlError={gatewayUrlError}
        gatewayCheckStatus={gatewayCheckStatus}
        gatewayCheckMessage={gatewayCheckMessage}
        errorMessage={error}
        isLoading={isLoading}
        canSubmit={canSubmit}
        workspaceRootPlaceholder={DEFAULT_WORKSPACE_ROOT}
        cancelLabel="Cancel"
        submitLabel={isZeroClaw ? "Launch agent" : "Create gateway"}
        submitBusyLabel={isZeroClaw ? "Launching…" : "Creating…"}
        onSubmit={handleSubmit}
        onCancel={() => router.push("/gateways")}
        onGatewayTypeChange={(next) => {
          setGatewayType(next);
          setError(null);
          setGatewayUrlError(null);
          setGatewayCheckStatus("idle");
          setGatewayCheckMessage(null);
        }}
        onNameChange={setName}
        onGatewayUrlChange={(next) => {
          setGatewayUrl(next);
          setGatewayUrlError(null);
          setGatewayCheckStatus("idle");
          setGatewayCheckMessage(null);
        }}
        onGatewayTokenChange={(next) => {
          setGatewayToken(next);
          setGatewayCheckStatus("idle");
          setGatewayCheckMessage(null);
        }}
        onDisableDevicePairingChange={(next) => {
          setDisableDevicePairing(next);
          setGatewayCheckStatus("idle");
          setGatewayCheckMessage(null);
        }}
        onWorkspaceRootChange={setWorkspaceRoot}
        onAllowInsecureTlsChange={(next) => {
          setAllowInsecureTls(next);
          setGatewayCheckStatus("idle");
          setGatewayCheckMessage(null);
        }}
        onDockerImageChange={setDockerImage}
        onProviderChange={setProvider}
        onModelChange={setModel}
        onMemoryChange={setMemory}
      />
    </DashboardPageLayout>
  );
}
