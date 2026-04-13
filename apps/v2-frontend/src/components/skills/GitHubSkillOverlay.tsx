"use client";

import { useState } from "react";
import { Modal } from "@/components/shared/Modal";
import { useCustomSkills } from "@/hooks/useCustomSkills";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

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
          <p className="text-sm text-muted-foreground">
            Repository connected. The tool will be built automatically.
          </p>
          <div className="flex justify-end">
            <Button size="sm" onClick={handleClose}>Done</Button>
          </div>
        </div>
      ) : (
        <form onSubmit={onSubmit} className="flex flex-col gap-4">
          <div className="space-y-2">
            <Label htmlFor="repo-url">GitHub repo URL</Label>
            <Input
              id="repo-url"
              autoFocus
              value={repoUrl}
              onChange={(e) => setRepoUrl(e.target.value)}
              placeholder="https://github.com/user/my-tool"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="repo-branch">Branch (optional)</Label>
            <Input
              id="repo-branch"
              value={branch}
              onChange={(e) => setBranch(e.target.value)}
              placeholder="main"
            />
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" size="sm" onClick={handleClose}>Cancel</Button>
            <Button type="submit" size="sm" disabled={submitting}>
              {submitting ? "Connecting..." : "Connect"}
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}
