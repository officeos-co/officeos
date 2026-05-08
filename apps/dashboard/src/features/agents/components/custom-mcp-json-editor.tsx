"use client";

import { useEffect, useMemo, useState } from "react";
import dynamic from "next/dynamic";
import { toast } from "sonner";
import { SaveIcon } from "lucide-react";
import type { ReactCodeMirrorProps } from "@uiw/react-codemirror";
import { json as jsonLanguage } from "@codemirror/lang-json";
import {
  defaultHighlightStyle,
  syntaxHighlighting,
} from "@codemirror/language";
import { EditorView } from "@codemirror/view";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import {
  useDeleteIntegration,
  useRegisterIntegration,
  useSaveIntegrationCredential,
} from "../api/useIntegrations";
import type { McpServer } from "../data/integrations";
import {
  CUSTOM_MCP_EXAMPLE_JSON,
  buildInitialCustomMcpServersJson,
  isUnchangedCustomMcpExample,
  parseCustomMcpServersJson,
} from "../data/custom-mcp-import";

const CodeMirror = dynamic<ReactCodeMirrorProps>(
  () => import("@uiw/react-codemirror").then((mod) => mod.default),
  { ssr: false },
);

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
  const registerIntegration = useRegisterIntegration();
  const saveCredential = useSaveIntegrationCredential();
  const deleteIntegration = useDeleteIntegration();
  const [json, setJson] = useState("");
  const [dirty, setDirty] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const editorExtensions = useMemo(
    () => [
      jsonLanguage(),
      syntaxHighlighting(defaultHighlightStyle, { fallback: true }),
      EditorView.lineWrapping,
      EditorView.theme({
        "&": {
          minHeight: "60vh",
          backgroundColor: "transparent",
          color: "var(--foreground)",
          fontSize: "12px",
        },
        ".cm-scroller": {
          minHeight: "60vh",
          fontFamily:
            "var(--font-geist-mono), ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
          lineHeight: "1.5",
        },
        ".cm-content": {
          padding: "12px 0",
        },
        ".cm-line": {
          padding: "0 14px",
        },
        ".cm-gutters": {
          backgroundColor: "var(--muted)",
          color: "var(--muted-foreground)",
          borderRight: "1px solid var(--border)",
        },
        ".cm-activeLine": {
          backgroundColor: "color-mix(in oklab, var(--muted) 55%, transparent)",
        },
        ".cm-activeLineGutter": {
          backgroundColor: "color-mix(in oklab, var(--muted) 80%, transparent)",
          color: "var(--foreground)",
        },
        ".cm-selectionBackground, &.cm-focused .cm-selectionBackground": {
          backgroundColor: "color-mix(in oklab, var(--primary) 22%, transparent)",
        },
        "&.cm-focused": {
          outline: "none",
        },
        ".cm-cursor": {
          borderLeftColor: "var(--foreground)",
        },
        ".cm-matchingBracket": {
          backgroundColor: "color-mix(in oklab, var(--primary) 14%, transparent)",
          outline: "1px solid color-mix(in oklab, var(--primary) 40%, transparent)",
        },
      }),
    ],
    [],
  );

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
        await registerIntegration(server.input);
        if (Object.keys(server.credentials).length > 0) {
          await saveCredential(server.name, server.credentials);
        }
      }

      for (const server of existingCustomServers) {
        if (!nextNames.has(server.name)) {
          await deleteIntegration(server.name);
        }
      }

      toast.success(
        parsedServers.length === 1
          ? `Saved ${parsedServers[0].title}`
          : `Saved ${parsedServers.length} custom MCP integrations`,
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
            {existingCustomServers.length} custom MCP integration
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

      <div
        aria-invalid={Boolean(validationError)}
        className={cn(
          "overflow-hidden rounded-lg border border-input bg-background transition-colors focus-within:border-ring focus-within:ring-3 focus-within:ring-ring/50",
          validationError &&
            "border-destructive focus-within:border-destructive focus-within:ring-destructive/20",
          loading && "opacity-60",
        )}
      >
        <CodeMirror
          value={json}
          height="60vh"
          minHeight="60vh"
          basicSetup={{
            lineNumbers: true,
            foldGutter: true,
            highlightActiveLine: true,
            highlightActiveLineGutter: true,
            bracketMatching: true,
            closeBrackets: true,
            autocompletion: true,
          }}
          extensions={editorExtensions}
          editable={!loading}
          theme="light"
          onChange={(value) => {
            setJson(value);
            setDirty(true);
            setValidationError(null);
          }}
          placeholder={CUSTOM_MCP_EXAMPLE_JSON}
        />
      </div>

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
