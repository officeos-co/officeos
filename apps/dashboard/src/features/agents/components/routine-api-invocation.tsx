"use client";

import { useMemo, useState } from "react";
import dynamic from "next/dynamic";
import { CheckIcon, CopyIcon } from "lucide-react";
import type { ReactCodeMirrorProps } from "@uiw/react-codemirror";
import {
  defaultHighlightStyle,
  syntaxHighlighting,
} from "@codemirror/language";
import { EditorView } from "@codemirror/view";
import { Button } from "@/ui/button";
import { WithTooltip } from "@/ui/help-tooltip";
import { cn } from "@/lib/utils";

const CodeMirror = dynamic<ReactCodeMirrorProps>(
  () => import("@uiw/react-codemirror").then((mod) => mod.default),
  { ssr: false },
);

const SECRET_PLACEHOLDER = "YOUR_ROUTINE_SECRET";

function endpointPath(triggerId: string) {
  return `/api/agent-routines/triggers/${triggerId}/invoke`;
}

function buildCurlExample(endpoint: string, secret: string) {
  return `curl -X POST '${endpoint}' \\
  -H 'Content-Type: application/json' \\
  -H 'X-Agent-Routine-Secret: ${secret}' \\
  -d '{"source":"external-system","payload":{"environment":"prod"}}'`;
}

function buildPythonExample(endpoint: string, secret: string) {
  return `import requests

response = requests.post(
    "${endpoint}",
    headers={
        "Content-Type": "application/json",
        "X-Agent-Routine-Secret": "${secret}",
    },
    json={
        "source": "external-system",
        "payload": {"environment": "prod"},
    },
)
response.raise_for_status()
print(response.json())`;
}

export function RoutineApiInvocation({
  triggerId,
  triggerName,
  secret,
  className,
}: {
  triggerId: string;
  triggerName?: string;
  secret?: string;
  className?: string;
}) {
  const [copied, setCopied] = useState<string | null>(null);
  const endpoint = endpointPath(triggerId);
  const displayedSecret = secret ?? SECRET_PLACEHOLDER;
  const codeExtensions = useMemo(
    () => [
      syntaxHighlighting(defaultHighlightStyle, { fallback: true }),
      EditorView.lineWrapping,
      EditorView.theme({
        "&": {
          backgroundColor: "transparent",
          color: "var(--foreground)",
          fontSize: "12px",
        },
        ".cm-scroller": {
          fontFamily:
            "var(--font-geist-mono), ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
          lineHeight: "1.5",
        },
        ".cm-content": {
          padding: "10px 0",
        },
        ".cm-line": {
          padding: "0 12px",
        },
        ".cm-gutters": {
          display: "none",
        },
        "&.cm-focused": {
          outline: "none",
        },
      }),
    ],
    [],
  );

  async function copy(value: string, key: string) {
    await navigator.clipboard.writeText(value);
    setCopied(key);
    window.setTimeout(() => {
      setCopied((current) => (current === key ? null : current));
    }, 1500);
  }

  return (
    <div className={cn("space-y-3", className)}>
      <div>
        <div className="text-sm font-medium">
          {triggerName ? `${triggerName} API call` : "API call"}
        </div>
        <p className="mt-1 text-xs text-muted-foreground">
          Send a POST request with JSON payload and the routine secret header.
        </p>
      </div>

      <CopyRow
        label="Endpoint"
        value={endpoint}
        copied={copied === "endpoint"}
        onCopy={() => copy(endpoint, "endpoint")}
      />

      <CodeExample
        label="curl"
        value={buildCurlExample(endpoint, displayedSecret)}
        copied={copied === "curl"}
        extensions={codeExtensions}
        onCopy={() => copy(buildCurlExample(endpoint, displayedSecret), "curl")}
      />

      <CodeExample
        label="Python"
        value={buildPythonExample(endpoint, displayedSecret)}
        copied={copied === "python"}
        extensions={codeExtensions}
        onCopy={() => copy(buildPythonExample(endpoint, displayedSecret), "python")}
      />
    </div>
  );
}

function CopyRow({
  label,
  value,
  copied,
  onCopy,
}: {
  label: string;
  value: string;
  copied: boolean;
  onCopy: () => void;
}) {
  return (
    <div className="space-y-1.5">
      <div className="text-xs font-medium text-muted-foreground">{label}</div>
      <div className="flex items-center gap-2">
        <code className="min-w-0 flex-1 break-all rounded-md bg-muted px-3 py-2 font-mono text-xs">
          {value}
        </code>
        <WithTooltip tooltip={`Copy ${label.toLowerCase()}.`}>
          <Button
            type="button"
            variant="outline"
            size="icon"
            className="size-8 shrink-0"
            onClick={onCopy}
          >
            {copied ? (
              <CheckIcon className="size-3.5" />
            ) : (
              <CopyIcon className="size-3.5" />
            )}
          </Button>
        </WithTooltip>
      </div>
    </div>
  );
}

function CodeExample({
  label,
  value,
  copied,
  extensions,
  onCopy,
}: {
  label: string;
  value: string;
  copied: boolean;
  extensions: ReactCodeMirrorProps["extensions"];
  onCopy: () => void;
}) {
  return (
    <div className="overflow-hidden rounded-lg border border-border">
      <div className="flex items-center justify-between gap-2 border-b border-border bg-muted/40 px-3 py-2">
        <div className="text-xs font-medium text-muted-foreground">{label}</div>
        <WithTooltip tooltip={`Copy ${label} example.`}>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            className="size-7 text-muted-foreground"
            onClick={onCopy}
          >
            {copied ? (
              <CheckIcon className="size-3.5" />
            ) : (
              <CopyIcon className="size-3.5" />
            )}
          </Button>
        </WithTooltip>
      </div>
      <CodeMirror
        value={value}
        minHeight="88px"
        basicSetup={{
          lineNumbers: false,
          foldGutter: false,
          highlightActiveLine: false,
          highlightActiveLineGutter: false,
        }}
        extensions={extensions}
        editable={false}
        theme="light"
      />
    </div>
  );
}
