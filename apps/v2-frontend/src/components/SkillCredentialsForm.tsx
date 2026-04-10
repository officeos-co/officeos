"use client";

import { useEffect, useState } from "react";
import { Save, Check } from "lucide-react";
import type { CredentialField } from "@/hooks/useSkills";

type Props = {
  fields: CredentialField[];
  /** Called on submit with the raw field values. */
  onSubmit: (values: Record<string, string>) => Promise<void>;
  /** Already-configured? If true the form renders placeholders only. */
  configured: boolean;
};

export function SkillCredentialsForm({ fields, onSubmit, configured }: Props) {
  const [values, setValues] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    setValues(Object.fromEntries(fields.map((f) => [f.key, ""])));
  }, [fields]);

  const canSubmit =
    !saving &&
    fields
      .filter((f) => f.required)
      .every((f) => (values[f.key] ?? "").trim().length > 0);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    setSaved(false);
    try {
      await onSubmit(values);
      setSaved(true);
      setValues(Object.fromEntries(fields.map((f) => [f.key, ""])));
      setTimeout(() => setSaved(false), 2500);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save credentials");
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={submit} className="flex flex-col gap-3">
      {configured && (
        <div className="rounded-md border border-emerald-500/30 bg-emerald-500/5 px-3 py-2 text-xs text-emerald-200">
          Credentials are currently configured. Submitting the form will overwrite them.
          Leaving fields blank will not clear them.
        </div>
      )}

      {fields.map((f) => {
        const inputClass =
          "rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 text-sm outline-none focus:border-white";
        return (
          <label key={f.key} className="flex flex-col gap-1 text-sm">
            <span className="text-[var(--eaos-text-muted)]">
              {f.label}
              {f.required && <span className="text-red-400">*</span>}
            </span>
            {f.kind === "textarea" ? (
              <textarea
                rows={6}
                value={values[f.key] ?? ""}
                onChange={(e) => setValues({ ...values, [f.key]: e.target.value })}
                placeholder={f.placeholder ?? undefined}
                className={`${inputClass} resize-y font-mono text-xs`}
              />
            ) : (
              <input
                type={f.kind === "password" ? "password" : "text"}
                value={values[f.key] ?? ""}
                onChange={(e) => setValues({ ...values, [f.key]: e.target.value })}
                placeholder={f.placeholder ?? undefined}
                className={inputClass}
              />
            )}
            {f.help && (
              <span className="text-[11px] text-[var(--eaos-text-muted)]">{f.help}</span>
            )}
          </label>
        );
      })}

      {error && (
        <div className="rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-xs text-red-300">
          {error}
        </div>
      )}

      <div className="flex items-center justify-between">
        {saved ? (
          <span className="flex items-center gap-1 text-xs text-emerald-300">
            <Check className="h-3 w-3" />
            Saved
          </span>
        ) : (
          <span className="text-[11px] text-[var(--eaos-text-muted)]">
            Stored encrypted. Used only for calls to the upstream API.
          </span>
        )}
        <button
          type="submit"
          disabled={!canSubmit}
          className="flex items-center gap-1 rounded-md bg-white px-4 py-2 text-sm font-medium text-black disabled:opacity-50"
        >
          <Save className="h-4 w-4" />
          {saving ? "Saving…" : "Save credentials"}
        </button>
      </div>
    </form>
  );
}
