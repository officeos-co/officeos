"use client";

import type { ReactNode } from "react";
import { SidebarTrigger } from "@/components/ui/sidebar";

type TopBarProps = {
  title: ReactNode;
  subtitle?: ReactNode;
  action?: ReactNode;
};

export function TopBar({ title, subtitle, action }: TopBarProps) {
  return (
    <div className="sticky top-0 z-10 border-b border-border bg-background/80 backdrop-blur-sm">
      <div className="flex items-center justify-between px-6 py-4">
        <div className="flex items-center gap-3">
          <SidebarTrigger className="-ml-1 h-7 w-7 text-muted-foreground hover:text-foreground lg:hidden" />
          <div>
            <h1 className="text-sm font-medium text-foreground">{title}</h1>
            {subtitle && (
              <p className="text-[12px] text-muted-foreground">{subtitle}</p>
            )}
          </div>
        </div>
        {action && <div className="flex items-center gap-2">{action}</div>}
      </div>
    </div>
  );
}
