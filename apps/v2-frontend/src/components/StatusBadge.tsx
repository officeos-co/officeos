import { cn } from "@/lib/utils";

const variants: Record<string, string> = {
  running: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
  ready: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
  online: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
  completed: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20",
  pending: "bg-amber-500/10 text-amber-400 border-amber-500/20",
  building: "bg-amber-500/10 text-amber-400 border-amber-500/20",
  "needs credentials": "bg-amber-500/10 text-amber-400 border-amber-500/20",
  failed: "bg-red-500/10 text-red-400 border-red-500/20",
  not_found: "bg-red-500/10 text-red-400 border-red-500/20",
  offline: "bg-red-500/10 text-red-400 border-red-500/20",
  stopped: "bg-muted text-muted-foreground border-border",
  unknown: "bg-muted text-muted-foreground border-border",
  "not installed": "bg-muted text-muted-foreground border-border",
};

const fallback = "bg-muted text-muted-foreground border-border";

export function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-medium",
        variants[status] ?? fallback
      )}
    >
      <span
        className={cn(
          "mr-1.5 h-1.5 w-1.5 rounded-full",
          status === "running" || status === "ready" || status === "online" || status === "completed"
            ? "bg-emerald-400"
            : status === "pending" || status === "building" || status === "needs credentials"
              ? "bg-amber-400"
              : status === "failed" || status === "not_found" || status === "offline"
                ? "bg-red-400"
                : "bg-muted-foreground"
        )}
      />
      {status}
    </span>
  );
}
