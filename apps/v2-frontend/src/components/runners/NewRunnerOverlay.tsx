"use client";

import { useState } from "react";
import { Modal } from "@/components/shared/Modal";
import { useRunners, type CreateRunnerResult } from "@/hooks/useRunners";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Copy, Check } from "lucide-react";

type NewRunnerOverlayProps = {
  open: boolean;
  onClose: () => void;
};

export function NewRunnerOverlay({ open, onClose }: NewRunnerOverlayProps) {
  const { create } = useRunners();

  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<CreateRunnerResult | null>(null);
  const [copied, setCopied] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!name.trim()) {
      setError("Name is required");
      return;
    }
    setSubmitting(true);
    try {
      const res = await create(name.trim());
      setResult(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create runner");
    } finally {
      setSubmitting(false);
    }
  };

  const handleClose = () => {
    setName("");
    setError(null);
    setResult(null);
    setCopied(false);
    onClose();
  };

  const copyCommand = () => {
    if (!result) return;
    const cmd = `docker run -e PLATFORM_URL=https://api.harrokrog.com -e REGISTRATION_TOKEN=${result.registrationToken} harkro123/skill-runner`;
    navigator.clipboard.writeText(cmd);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <Modal open={open} title={result ? "Runner created" : "New runner"} onClose={handleClose}>
      {result ? (
        <div className="flex flex-col gap-4">
          <p className="text-sm text-muted-foreground">
            Runner <strong className="text-foreground">{result.name}</strong> created.
            Copy the command below. The registration token is shown only once.
          </p>

          <div className="rounded-lg border border-border bg-secondary p-3">
            <code className="block whitespace-pre-wrap break-all font-mono text-xs text-emerald-600">
              docker run \{"\n"}
              {"  "}-e PLATFORM_URL=https://api.harrokrog.com \{"\n"}
              {"  "}-e REGISTRATION_TOKEN={result.registrationToken} \{"\n"}
              {"  "}harkro123/skill-runner
            </code>
          </div>

          <div className="flex justify-end gap-2">
            <Button variant="outline" size="sm" onClick={copyCommand}>
              {copied ? <Check className="mr-1.5 h-3.5 w-3.5" /> : <Copy className="mr-1.5 h-3.5 w-3.5" />}
              {copied ? "Copied" : "Copy command"}
            </Button>
            <Button size="sm" onClick={handleClose}>Done</Button>
          </div>
        </div>
      ) : (
        <form onSubmit={onSubmit} className="flex flex-col gap-4">
          <div className="space-y-2">
            <Label htmlFor="runner-name">Name</Label>
            <Input
              id="runner-name"
              autoFocus
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="production-runner"
            />
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" size="sm" onClick={handleClose}>
              Cancel
            </Button>
            <Button type="submit" size="sm" disabled={submitting}>
              {submitting ? "Creating..." : "Create"}
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}
