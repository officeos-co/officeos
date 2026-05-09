import type { LucideIcon } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

export function OverviewCardGrid({
  className,
  ...props
}: React.ComponentPropsWithoutRef<"div">) {
  return (
    <div
      className={cn("grid gap-3 sm:grid-cols-2 lg:grid-cols-4", className)}
      {...props}
    />
  );
}

export function OverviewCard({
  label,
  value,
  icon: Icon,
  loading = false,
  tone = "default",
}: {
  label: string;
  value: number | string;
  icon: LucideIcon;
  loading?: boolean;
  tone?: "default" | "destructive";
}) {
  return (
    <div className="rounded-lg border border-border bg-card p-4">
      <div className="flex items-center justify-between gap-3">
        <div className="min-w-0">
          <p className="text-sm text-muted-foreground">{label}</p>
          {loading ? (
            <Skeleton className="mt-2 h-7 w-12" />
          ) : (
            <p className="mt-1 text-2xl font-semibold tracking-tight">
              {value}
            </p>
          )}
        </div>
        <div
          className={cn(
            "flex size-8 shrink-0 items-center justify-center rounded-md bg-muted text-muted-foreground",
            tone === "destructive" && "bg-destructive/10 text-destructive",
          )}
        >
          <Icon className="size-4" />
        </div>
      </div>
    </div>
  );
}
