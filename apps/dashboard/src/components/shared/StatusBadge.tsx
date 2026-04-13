import { cn } from "@/lib/utils";

const variants: Record<string, string> = {
  running: "bg-emerald-50 text-emerald-700 border-emerald-200",
  ready: "bg-emerald-50 text-emerald-700 border-emerald-200",
  online: "bg-emerald-50 text-emerald-700 border-emerald-200",
  completed: "bg-emerald-50 text-emerald-700 border-emerald-200",
  pending: "bg-amber-50 text-amber-700 border-amber-200",
  building: "bg-amber-50 text-amber-700 border-amber-200",
  "needs credentials": "bg-amber-50 text-amber-700 border-amber-200",
  failed: "bg-red-50 text-red-700 border-red-200",
  not_found: "bg-red-50 text-red-700 border-red-200",
  offline: "bg-red-50 text-red-700 border-red-200",
  stopped: "bg-muted text-muted-foreground border-border",
  unknown: "bg-muted text-muted-foreground border-border",
  "not installed": "bg-muted text-muted-foreground border-border",
};

const fallback = "bg-muted text-muted-foreground border-border";

const dotColor: Record<string, string> = {
  running: "bg-emerald-500",
  ready: "bg-emerald-500",
  online: "bg-emerald-500",
  completed: "bg-emerald-500",
  pending: "bg-amber-500",
  building: "bg-amber-500",
  "needs credentials": "bg-amber-500",
  failed: "bg-red-500",
  not_found: "bg-red-500",
  offline: "bg-red-500",
};

export function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-medium",
        variants[status] ?? fallback,
      )}
    >
      <span
        className={cn(
          "mr-1.5 h-1.5 w-1.5 rounded-full",
          dotColor[status] ?? "bg-muted-foreground",
        )}
      />
      {status}
    </span>
  );
}
