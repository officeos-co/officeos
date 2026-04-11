"use client";

import { useRef, useState } from "react";
import { Modal } from "./Modal";
import { useCustomSkills } from "@/hooks/useCustomSkills";

type Props = {
  open: boolean;
  onClose: () => void;
};

export function UploadSkillOverlay({ open, onClose }: Props) {
  const { upload } = useCustomSkills();
  const fileRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<{ name: string; buildStatus: string; buildError?: string } | null>(null);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setError("Select a .zip file");
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      const res = await upload(file);
      setResult(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Upload failed");
    } finally {
      setSubmitting(false);
    }
  };

  const handleClose = () => {
    setFile(null);
    setError(null);
    setResult(null);
    onClose();
  };

  return (
    <Modal open={open} title="Upload skill" onClose={handleClose}>
      {result ? (
        <div className="flex flex-col gap-4">
          <p className="text-sm">
            Skill <strong>{result.name}</strong> — build status:{" "}
            <span className={result.buildStatus === "ready" ? "text-green-300" : result.buildStatus === "failed" ? "text-red-300" : "text-yellow-300"}>
              {result.buildStatus}
            </span>
          </p>
          {result.buildError && (
            <p className="text-xs text-red-400">{result.buildError}</p>
          )}
          <div className="flex justify-end">
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
          <p className="text-sm text-[var(--eaos-text-muted)]">
            Upload a .zip containing <code>skill.ts</code> and <code>package.json</code>.
          </p>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-[var(--eaos-text-muted)]">Skill archive (.zip)</span>
            <input
              ref={fileRef}
              type="file"
              accept=".zip"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 text-sm file:mr-3 file:rounded file:border-0 file:bg-white/10 file:px-3 file:py-1 file:text-sm file:text-white"
            />
          </label>

          {error && <div className="text-sm text-red-400">{error}</div>}

          <div className="mt-2 flex justify-end gap-2">
            <button type="button" onClick={handleClose} className="rounded-md border border-[var(--eaos-border)] px-4 py-2 text-sm hover:bg-black/40">
              Cancel
            </button>
            <button type="submit" disabled={submitting || !file} className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black disabled:opacity-50">
              {submitting ? "Uploading..." : "Upload"}
            </button>
          </div>
        </form>
      )}
    </Modal>
  );
}
