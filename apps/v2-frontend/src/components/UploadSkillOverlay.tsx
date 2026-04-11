"use client";

import { useRef, useState } from "react";
import { Modal } from "./Modal";
import { useCustomSkills } from "@/hooks/useCustomSkills";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";

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
          <p className="text-sm text-muted-foreground">
            Skill <strong className="text-foreground">{result.name}</strong> — build status:{" "}
            <span className={
              result.buildStatus === "ready" ? "text-emerald-400" :
              result.buildStatus === "failed" ? "text-destructive" : "text-amber-400"
            }>
              {result.buildStatus}
            </span>
          </p>
          {result.buildError && (
            <p className="text-xs text-destructive">{result.buildError}</p>
          )}
          <div className="flex justify-end">
            <Button size="sm" onClick={handleClose}>Done</Button>
          </div>
        </div>
      ) : (
        <form onSubmit={onSubmit} className="flex flex-col gap-4">
          <p className="text-sm text-muted-foreground">
            Upload a <code className="rounded bg-secondary px-1 py-0.5 text-xs">.zip</code> containing{" "}
            <code className="rounded bg-secondary px-1 py-0.5 text-xs">skill.ts</code> and{" "}
            <code className="rounded bg-secondary px-1 py-0.5 text-xs">package.json</code>.
          </p>
          <div className="space-y-2">
            <Label>Skill archive (.zip)</Label>
            <input
              ref={fileRef}
              type="file"
              accept=".zip"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className="w-full rounded-md border border-input bg-secondary px-3 py-2 text-sm file:mr-3 file:rounded file:border-0 file:bg-primary/10 file:px-3 file:py-1 file:text-sm file:text-primary"
            />
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" size="sm" onClick={handleClose}>Cancel</Button>
            <Button type="submit" size="sm" disabled={submitting || !file}>
              {submitting ? "Uploading..." : "Upload"}
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}
