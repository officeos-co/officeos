import type { ComponentPropsWithoutRef } from "react";
import { cn } from "@/lib/utils";

export type PageWidth = "full" | "wide" | "thin" | "narrow";

const pageWidthClassNames: Record<PageWidth, string> = {
  full: "max-w-[1600px]",
  wide: "max-w-6xl",
  thin: "max-w-4xl",
  narrow: "max-w-3xl",
};

export function getPageWidthClassName(
  width: PageWidth = "full",
  className?: string,
) {
  return cn("mx-auto w-full", pageWidthClassNames[width], className);
}

const dialogWidthClassNames: Record<PageWidth, string> = {
  full: "w-[calc(100vw-2rem)] sm:w-[calc(100vw-4rem)] max-w-none sm:max-w-[1600px]",
  wide: "w-[calc(100vw-2rem)] sm:w-[calc(100vw-4rem)] max-w-none sm:max-w-6xl",
  thin: "w-[calc(100vw-2rem)] sm:w-[calc(100vw-4rem)] max-w-none sm:max-w-4xl",
  narrow: "w-[calc(100vw-2rem)] sm:w-[calc(100vw-4rem)] max-w-none sm:max-w-3xl",
};

export function getDialogWidthClassName(
  width: PageWidth = "narrow",
  className?: string,
) {
  return cn(dialogWidthClassNames[width], className);
}

type PageContainerProps = ComponentPropsWithoutRef<"div"> & {
  width?: PageWidth;
};

export function PageContainer({
  width = "full",
  className,
  ...props
}: PageContainerProps) {
  return (
    <div
      className={getPageWidthClassName(width, className)}
      {...props}
    />
  );
}
