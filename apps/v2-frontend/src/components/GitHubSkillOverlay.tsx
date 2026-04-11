"use client";

import { useState } from "react";
import { Modal } from "./Modal";
import { useCustomSkills } from "@/hooks/useCustomSkills";

type Props = {
  open: boolean;
  onClose: () => void;
};

export function GitHubSkillOverlay({ open, onClose }: Props) {
  const { connectGitHub } = useCustomSkills();
  const [repoUrl, setRepoUrl] = useState("");
  const [branch, setBranch] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!repoUrl.trim()) {
      setError("Repository URL is required");
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      await connectGitHub(repoUrl.trim(), branch.trim() || undefined);
      setDone(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to connect");
    } finally {
      setSubmitting(false);
    }
  };

  const handleClose = () => {
    setRepoUrl("");
    setBranch("");
    setError(null);
    setDone(false);
    onClose();
  };

  return (
    <Modal open={open} title="Connect GitHub repo" onClose={handleClose}>
      {done ? (
        <div className="flex flex-col gap-4">
          <p className="text-sm text-[var(--eaos-text-muted)]">
            Repository connected. The skill will be built automatically.
          </p>
          <div className="flex justify-end">
            <button onClick={handleClose} className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black">
              Done
            </button>
          </div>
        </div>
      ) : (
        <form onSubmit={onSubmit} className="flex flex-col gap-4">
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-[var(--eaos-text-muted)]">GitHub repo URL</span>
            <input
              autoFocus
              value={repoUrl}
              onChange={(e) => setRepoUrl(e.target.value)}
              className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 outline-none focus:border-white"
              placeholder="https://github.com/user/my-skill"
            />
          </label>

          <label className="flex flex-col gap-1 text-sm">
            <span className="text-[var(--eaos-text-muted)]">Branch (optional)</span>
            <input
              value={branch}
              onChange={(e) => setBranch(e.target.value)}
              className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 outline-none focus:border-white"
              placeholder="main"
            />
          </label>

          {error && <div className="text-sm text-red-400">{error}</div>}

          <div className="mt-2 flex justify-end gap-2">
            <button type="button" onClick={handleClose} className="rounded-md border border-[var(--eaos-border)] px-4 py-2 text-sm hover:bg-black/40">
              Cancel
            </button>
            <button type="submit" disabled={submitting} className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black disabled:opacity-50">
              {submitting ? "Connecting..." : "Connect"}
            </button>
          </div>
        </form>
      )}
    </Modal>
  );
}
