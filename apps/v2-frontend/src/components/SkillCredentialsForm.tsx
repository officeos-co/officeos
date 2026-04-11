"use client";

import { useEffect, useState } from "react";
import { Save, Check } from "lucide-react";
import type { CredentialField } from "@/hooks/useSkills";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

type Props = {
  fields: CredentialField[];
  onSubmit: (values: Record<string, string>) => Promise<void>;
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
        <div className="rounded-md border border-emerald-500/20 bg-emerald-500/5 px-3 py-2 text-xs text-emerald-400">
          Credentials are configured. Submitting will overwrite them.
        </div>
      )}

      {fields.map((f) => (
        <div key={f.key} className="space-y-1.5">
          <Label>
            {f.label}
            {f.required && <span className="text-destructive"> *</span>}
          </Label>
          {f.kind === "textarea" ? (
            <textarea
              rows={6}
              value={values[f.key] ?? ""}
              onChange={(e) => setValues({ ...values, [f.key]: e.target.value })}
              placeholder={f.placeholder ?? undefined}
              className="flex w-full resize-y rounded-md border border-input bg-transparent px-3 py-2 font-mono text-xs shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            />
          ) : (
            <Input
              type={f.kind === "password" ? "password" : "text"}
              value={values[f.key] ?? ""}
              onChange={(e) => setValues({ ...values, [f.key]: e.target.value })}
              placeholder={f.placeholder ?? undefined}
            />
          )}
          {f.help && (
            <p className="text-[11px] text-muted-foreground">{f.help}</p>
          )}
        </div>
      ))}

      {error && (
        <p className="text-sm text-destructive">{error}</p>
      )}

      <div className="flex items-center justify-between">
        {saved ? (
          <span className="flex items-center gap-1 text-xs text-emerald-400">
            <Check className="h-3 w-3" />
            Saved
          </span>
        ) : (
          <span className="text-[11px] text-muted-foreground">
            Stored encrypted. Used only for upstream API calls.
          </span>
        )}
        <Button type="submit" size="sm" disabled={!canSubmit}>
          <Save className="mr-1.5 h-3.5 w-3.5" />
          {saving ? "Saving..." : "Save credentials"}
        </Button>
      </div>
    </form>
  );
}
