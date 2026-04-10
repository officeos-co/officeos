import type { ReactNode } from "react";

type TopBarProps = {
  title: string;
  subtitle?: ReactNode;
  action?: ReactNode;
};

export function TopBar({ title, subtitle, action }: TopBarProps) {
  return (
    <div className="sticky top-0 z-10 border-b border-[var(--eaos-border)] bg-[var(--eaos-bg)]/90 px-8 py-6 backdrop-blur">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">{title}</h1>
          {subtitle && (
            <p className="mt-1 text-sm text-[var(--eaos-text-muted)]">{subtitle}</p>
          )}
        </div>
        {action}
      </div>
    </div>
  );
}
