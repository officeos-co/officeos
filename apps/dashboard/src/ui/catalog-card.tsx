"use client";

import type { KeyboardEvent, ReactNode } from "react";
import { cn } from "@/lib/utils";

type CatalogCardProps = {
  logo: string;
  title: string;
  subtitle?: string;
  description?: string;
  action?: ReactNode;
  meta?: ReactNode;
  selected?: boolean;
  variant?: "marketplace" | "compact";
  onClick?: () => void;
};

export function CatalogCard({
  logo,
  title,
  subtitle,
  description,
  action,
  meta,
  selected = false,
  variant = "marketplace",
  onClick,
}: CatalogCardProps) {
  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (!onClick) return;
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      onClick();
    }
  }

  if (variant === "compact") {
    return (
      <div
        role={onClick ? "button" : undefined}
        tabIndex={onClick ? 0 : undefined}
        onClick={onClick}
        onKeyDown={onKeyDown}
        className={cn(
          "flex items-center gap-2.5 rounded-lg border bg-card px-3 py-2 text-sm transition-[border-color,background-color]",
          selected ? "border-primary bg-primary/5" : "border-border",
          onClick && "cursor-pointer hover:bg-accent/20",
        )}
      >
        <div
          className="size-[18px] shrink-0 [&>img]:size-[18px] [&>img]:object-contain [&>svg]:size-[18px]"
          dangerouslySetInnerHTML={{ __html: logo }}
        />
        <span className="min-w-0 flex-1 truncate font-medium text-foreground">
          {title}
        </span>
        {action}
      </div>
    );
  }

  return (
    <div
      role={onClick ? "button" : undefined}
      tabIndex={onClick ? 0 : undefined}
      onClick={onClick}
      onKeyDown={onKeyDown}
      className={cn(
        "group flex flex-col rounded-xl border border-border bg-card p-4 text-left text-card-foreground shadow-[0_1px_2px_rgba(0,0,0,0.025)] transition-[border-color,background-color,box-shadow]",
        onClick &&
          "cursor-pointer hover:border-foreground/15 hover:bg-accent/20 hover:shadow-sm",
      )}
    >
      <div className="flex items-start gap-3">
        <div
          className="size-9 shrink-0 [&>img]:size-9 [&>img]:object-contain [&>svg]:size-9"
          dangerouslySetInnerHTML={{ __html: logo }}
        />
        <div className="min-w-0 flex-1 pt-0.5">
          <h3 className="truncate text-sm font-semibold leading-5 text-foreground">
            {title}
          </h3>
          {subtitle && (
            <p className="truncate text-xs leading-5 text-muted-foreground">
              {subtitle}
            </p>
          )}
        </div>
        {action}
      </div>

      {description && (
        <p className="mt-4 line-clamp-2 text-sm leading-6 text-foreground/70">
          {description}
        </p>
      )}

      {meta && <div className="mt-auto flex flex-wrap gap-1.5 pt-4">{meta}</div>}
    </div>
  );
}

export function CatalogMeta({ children }: { children: ReactNode }) {
  return (
    <span className="inline-flex items-center rounded-md bg-muted px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
      {children}
    </span>
  );
}
