import { cn } from "@/lib/utils";

const variants: Record<string, string> = {
  running: "text-emerald-600",
  ready: "text-emerald-600",
  online: "text-emerald-600",
  completed: "text-emerald-600",
  pending: "text-amber-600",
  building: "text-amber-600",
  "needs credentials": "text-amber-600",
  failed: "text-red-600",
  not_found: "text-red-600",
  offline: "text-red-600",
  stopped: "text-muted-foreground",
  unknown: "text-muted-foreground",
  "not installed": "text-muted-foreground",
  approved: "text-emerald-600",
  rejected: "text-red-600",
};

const fallback = "text-muted-foreground";

const dotColor: Record<string, string> = {
  running: "bg-emerald-500",
  ready: "bg-emerald-500",
  online: "bg-emerald-500",
  completed: "bg-emerald-500",
  approved: "bg-emerald-500",
  pending: "bg-amber-500",
  building: "bg-amber-500",
  "needs credentials": "bg-amber-500",
  failed: "bg-red-500",
  not_found: "bg-red-500",
  offline: "bg-red-500",
  rejected: "bg-red-500",
};

export function StatusBadge({ status }: { status: string }) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 text-[12px] font-medium",
        variants[status] ?? fallback,
      )}
    >
      <span
        className={cn(
          "h-1.5 w-1.5 rounded-full",
          dotColor[status] ?? "bg-muted-foreground/50",
        )}
      />
      {status}
    </span>
  );
}
