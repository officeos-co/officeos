"use client";

import { useState } from "react";
import { Modal } from "./Modal";
import { useRunners, type CreateRunnerResult } from "@/hooks/useRunners";

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
          <p className="text-sm text-[var(--eaos-text-muted)]">
            Your runner <strong>{result.name}</strong> has been created. Copy the command below to
            start it. The registration token will only be shown once.
          </p>

          <div className="rounded-md border border-[var(--eaos-border)] bg-black/40 p-3">
            <code className="block whitespace-pre-wrap break-all text-xs text-green-300">
              docker run \{"\n"}
              {"  "}-e PLATFORM_URL=https://api.harrokrog.com \{"\n"}
              {"  "}-e REGISTRATION_TOKEN={result.registrationToken} \{"\n"}
              {"  "}harkro123/skill-runner
            </code>
          </div>

          <div className="flex justify-end gap-2">
            <button
              onClick={copyCommand}
              className="rounded-md border border-[var(--eaos-border)] px-4 py-2 text-sm hover:bg-black/40"
            >
              {copied ? "Copied!" : "Copy command"}
            </button>
            <button
              onClick={handleClose}
              className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black"
            >
              Done
            </button>
          </div>
        </div>
      ) : (
        <form onSubmit={onSubmit} className="flex flex-col gap-4">
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-[var(--eaos-text-muted)]">Name</span>
            <input
              autoFocus
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 outline-none focus:border-white"
              placeholder="production-runner"
            />
          </label>

          {error && <div className="text-sm text-red-400">{error}</div>}

          <div className="mt-2 flex justify-end gap-2">
            <button
              type="button"
              onClick={handleClose}
              className="rounded-md border border-[var(--eaos-border)] px-4 py-2 text-sm hover:bg-black/40"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black disabled:opacity-50"
            >
              {submitting ? "Creating..." : "Create"}
            </button>
          </div>
        </form>
      )}
    </Modal>
  );
}
