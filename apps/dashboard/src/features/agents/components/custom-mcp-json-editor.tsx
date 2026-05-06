"use client";

import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { SaveIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  useDeleteMcpServer,
  useRegisterMcpServer,
  useSaveMcpCredential,
} from "../api/useIntegrations";
import type { McpServer } from "../data/integrations";
import {
  CUSTOM_MCP_EXAMPLE_JSON,
  buildInitialCustomMcpServersJson,
  isUnchangedCustomMcpExample,
  parseCustomMcpServersJson,
} from "../data/custom-mcp-import";

function graphQLErrorMessage(error: unknown, fallback: string) {
  if (
    typeof error === "object" &&
    error !== null &&
    "graphQLErrors" in error &&
    Array.isArray(error.graphQLErrors)
  ) {
    const first = error.graphQLErrors[0] as { message?: unknown } | undefined;
    if (typeof first?.message === "string") return first.message;
  }
  return fallback;
}

export function CustomMcpJsonEditor({
  servers,
  loading = false,
}: {
  servers: McpServer[];
  loading?: boolean;
}) {
  const registerMcpServer = useRegisterMcpServer();
  const saveCredential = useSaveMcpCredential();
  const deleteMcpServer = useDeleteMcpServer();
  const [json, setJson] = useState("");
  const [dirty, setDirty] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const existingCustomServers = useMemo(
    () => servers.filter((server) => !server.isBuiltin),
    [servers],
  );

  useEffect(() => {
    if (dirty) return;
    setJson(buildInitialCustomMcpServersJson(servers));
    setValidationError(null);
  }, [dirty, servers]);

  async function handleSave() {
    if (isUnchangedCustomMcpExample(json)) {
      const message = "Change the example JSON before saving.";
      setValidationError(message);
      toast.error(message);
      return;
    }

    let parsedServers;
    try {
      parsedServers = parseCustomMcpServersJson(json);
      setValidationError(null);
    } catch (error) {
      const message =
        error instanceof Error ? error.message : "Invalid MCP JSON.";
      setValidationError(message);
      toast.error(message);
      return;
    }

    const nextNames = new Set(parsedServers.map((server) => server.name));

    setSaving(true);
    try {
      for (const server of parsedServers) {
        await registerMcpServer(server.input);
        if (Object.keys(server.credentials).length > 0) {
          await saveCredential(server.name, server.credentials);
        }
      }

      for (const server of existingCustomServers) {
        if (!nextNames.has(server.name)) {
          await deleteMcpServer(server.name);
        }
      }

      toast.success(
        parsedServers.length === 1
          ? `Saved ${parsedServers[0].title}`
          : `Saved ${parsedServers.length} custom MCP servers`,
      );
      setDirty(false);
    } catch (error) {
      toast.error(graphQLErrorMessage(error, "Failed to save MCP JSON."));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-3">
      <div className="flex items-center justify-between gap-3">
        <div className="min-w-0">
          <h2 className="text-sm font-medium">Configuration JSON</h2>
          <p className="mt-1 text-xs text-muted-foreground">
            {existingCustomServers.length} custom MCP server
            {existingCustomServers.length === 1 ? "" : "s"} configured
          </p>
        </div>
        <Button
          size="sm"
          disabled={saving || loading || json.trim().length === 0}
          onClick={handleSave}
        >
          <SaveIcon className="size-4" />
          {saving ? "Saving" : "Save"}
        </Button>
      </div>

      <Textarea
        className="min-h-[60vh] flex-1 resize-y font-mono text-xs leading-5"
        value={json}
        onChange={(event) => {
          setJson(event.target.value);
          setDirty(true);
          setValidationError(null);
        }}
        aria-invalid={Boolean(validationError)}
        disabled={loading}
        spellCheck={false}
        placeholder={CUSTOM_MCP_EXAMPLE_JSON}
      />

      {validationError ? (
        <p className="text-xs text-destructive">{validationError}</p>
      ) : (
        <p className="text-xs text-muted-foreground">
          Blank env values define required credentials without replacing saved
          secrets. Add a value only when you want to save or rotate it.
        </p>
      )}
    </div>
  );
}
