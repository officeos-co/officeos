const colors: Record<string, string> = {
  running: "border-green-500/40 text-green-300",
  pending: "border-yellow-500/40 text-yellow-300",
  failed: "border-red-500/40 text-red-300",
};
const fallback = "border-[var(--eaos-border)] text-[var(--eaos-text-muted)]";

export function StatusBadge({ status }: { status: string }) {
  return (
    <span className={`rounded-full border px-2 py-0.5 text-xs ${colors[status] ?? fallback}`}>
      {status}
    </span>
  );
}
