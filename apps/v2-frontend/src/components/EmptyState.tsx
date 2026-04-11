import type { ReactNode } from "react";

type EmptyStateProps = {
  title: string;
  description?: string;
  action?: ReactNode;
};

export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <div className="mx-8 my-16 grid place-items-center rounded-lg border border-dashed border-border bg-card/50 px-6 py-20 text-center">
      <div className="text-base font-medium">{title}</div>
      {description && (
        <div className="mt-2 max-w-sm text-sm text-muted-foreground">
          {description}
        </div>
      )}
      {action && <div className="mt-6">{action}</div>}
    </div>
  );
}
