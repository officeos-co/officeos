"use client";

import { Children, isValidElement, type ReactNode } from "react";
import { CircleHelpIcon } from "lucide-react";

import { cn } from "@/lib/utils";

function nativeTitle(node: ReactNode): string {
  if (node === null || node === undefined || typeof node === "boolean") {
    return "";
  }

  if (typeof node === "string" || typeof node === "number") {
    return String(node);
  }

  if (Array.isArray(node)) {
    return node.map(nativeTitle).filter(Boolean).join(" ");
  }

  if (isValidElement<{ children?: ReactNode }>(node)) {
    return nativeTitle(node.props.children);
  }

  return "";
}

export function HelpTooltip({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
  focusable?: boolean;
  side?: "top" | "bottom" | "left" | "right";
}) {
  return (
    <span
      title={nativeTitle(children)}
      aria-label={nativeTitle(children)}
      className={cn(
        "inline-flex size-4 items-center justify-center rounded-full text-muted-foreground transition-colors hover:text-foreground",
        className,
      )}
    >
      <CircleHelpIcon className="size-3.5" />
    </span>
  );
}

export function WithTooltip({
  children,
  tooltip,
}: {
  children: ReactNode;
  tooltip: ReactNode;
  side?: "top" | "bottom" | "left" | "right";
}) {
  return (
    <span title={nativeTitle(tooltip)} className="inline-flex">
      {Children.only(children)}
    </span>
  );
}
