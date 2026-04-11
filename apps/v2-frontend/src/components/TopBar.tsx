import type { ReactNode } from "react";

type TopBarProps = {
  title: ReactNode;
  subtitle?: ReactNode;
  action?: ReactNode;
};

export function TopBar({ title, subtitle, action }: TopBarProps) {
  return (
    <div className="sticky top-0 z-10 border-b border-border bg-card/80 px-8 py-5 backdrop-blur-md">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-lg font-semibold tracking-tight">{title}</h1>
          {subtitle && (
            <p className="mt-0.5 text-[13px] text-muted-foreground">{subtitle}</p>
          )}
        </div>
        {action}
      </div>
    </div>
  );
}
