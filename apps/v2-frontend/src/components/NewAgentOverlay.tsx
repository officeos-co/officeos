import { useState } from "react";
import { Modal } from "./Modal";
import { useAgents } from "../hooks/useAgents";

type NewAgentOverlayProps = {
  open: boolean;
  onClose: () => void;
};

export function NewAgentOverlay({ open, onClose }: NewAgentOverlayProps) {
  const { create } = useAgents();
  const [name, setName] = useState("");
  const [model, setModel] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!name.trim()) {
      setError("Name is required");
      return;
    }
    setSubmitting(true);
    try {
      await create({ name: name.trim(), model: model.trim() || undefined });
      setName("");
      setModel("");
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create agent");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Modal open={open} title="New agent" onClose={onClose}>
      <form onSubmit={onSubmit} className="flex flex-col gap-4">
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-[var(--eaos-text-muted)]">Name</span>
          <input
            autoFocus
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 outline-none focus:border-white"
            placeholder="my-agent"
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-[var(--eaos-text-muted)]">Model (optional)</span>
          <input
            value={model}
            onChange={(e) => setModel(e.target.value)}
            className="rounded-md border border-[var(--eaos-border)] bg-black/40 px-3 py-2 outline-none focus:border-white"
            placeholder="claude-sonnet-4-6"
          />
        </label>

        {error && <div className="text-sm text-red-400">{error}</div>}

        <div className="mt-2 flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
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
    </Modal>
  );
}
