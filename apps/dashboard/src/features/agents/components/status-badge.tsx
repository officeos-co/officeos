const statusStyles: Record<
  string,
  { bg: string; text: string; label: string }
> = {
  running: { bg: "bg-emerald-100", text: "text-emerald-700", label: "RUNNING" },
  pending: { bg: "bg-amber-100", text: "text-amber-700", label: "PENDING" },
  booting: { bg: "bg-blue-100", text: "text-blue-700", label: "BOOTING" },
  stopped: { bg: "bg-zinc-100", text: "text-zinc-500", label: "STOPPED" },
  failed: { bg: "bg-red-100", text: "text-red-700", label: "FAILED" },
};

export function StatusBadge({ status }: { status: string }) {
  const style = statusStyles[status] ?? statusStyles.stopped;
  return (
    <span
      className={`inline-flex rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest ${style.bg} ${style.text}`}
    >
      {style.label}
    </span>
  );
}
