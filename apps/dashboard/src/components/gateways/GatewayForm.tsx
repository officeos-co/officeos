import type { FormEvent } from "react";
import Link from "next/link";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

// `GatewayType` is kept as a one-value alias so the rest of the
// codebase that still imports it (generated client types, edit page)
// keeps compiling. The form itself no longer exposes a type picker —
// the OpenClaw (external-gateway) flow was removed.
export type GatewayType = "zeroclaw";

type GatewayFormProps = {
  name: string;
  provider: string;
  model: string;
  errorMessage: string | null;
  isLoading: boolean;
  canSubmit: boolean;
  cancelLabel: string;
  submitLabel: string;
  submitBusyLabel: string;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
  onNameChange: (next: string) => void;
  onProviderChange: (next: string) => void;
  onModelChange: (next: string) => void;
};

export function GatewayForm({
  name,
  provider,
  model,
  errorMessage,
  isLoading,
  canSubmit,
  cancelLabel,
  submitLabel,
  submitBusyLabel,
  onSubmit,
  onCancel,
  onNameChange,
  onProviderChange,
  onModelChange,
}: GatewayFormProps) {
  return (
    <form
      onSubmit={onSubmit}
      className="space-y-6 rounded-xl border border-slate-200 bg-white p-6 shadow-sm"
    >
      <div className="space-y-2">
        <label className="text-sm font-medium text-slate-900">
          Agent name <span className="text-red-500">*</span>
        </label>
        <Input
          value={name}
          onChange={(event) => onNameChange(event.target.value)}
          placeholder="My ZeroClaw agent"
          disabled={isLoading}
        />
        <p className="text-xs text-slate-500">
          Memory (graph) and per-agent vault databases are provisioned
          automatically. You don&apos;t need to pick either.
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-900">
            LLM Provider
          </label>
          <select
            value={provider}
            onChange={(event) => onProviderChange(event.target.value)}
            disabled={isLoading}
            className="flex h-10 w-full rounded-md border border-slate-200 bg-white px-3 py-2 text-sm ring-offset-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-950 focus-visible:ring-offset-2"
          >
            <option value="openrouter">OpenRouter</option>
            <option value="openai">OpenAI</option>
            <option value="anthropic">Anthropic</option>
            <option value="ollama">Ollama (local)</option>
          </select>
          <p className="text-xs text-slate-500">
            Using a provider for the first time?{" "}
            <Link
              href="/settings/providers"
              className="text-blue-600 underline-offset-2 hover:underline"
            >
              Configure your API keys in Settings → Providers
            </Link>
            .
          </p>
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-900">Model</label>
          <Input
            value={model}
            onChange={(event) => onModelChange(event.target.value)}
            placeholder="anthropic/claude-sonnet-4-20250514"
            disabled={isLoading}
          />
          <p className="text-xs text-slate-500">
            Leave empty for the provider default.
          </p>
        </div>
      </div>

      {errorMessage ? (
        <p className="text-sm text-red-500">{errorMessage}</p>
      ) : null}

      <div className="flex justify-end gap-3">
        <Button
          type="button"
          variant="ghost"
          onClick={onCancel}
          disabled={isLoading}
        >
          {cancelLabel}
        </Button>
        <Button type="submit" disabled={isLoading || !canSubmit}>
          {isLoading ? submitBusyLabel : submitLabel}
        </Button>
      </div>
    </form>
  );
}
