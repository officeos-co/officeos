"use client";

import * as React from "react";
import { CheckIcon, MinusIcon } from "lucide-react";

import { cn } from "@/lib/utils";

function Table({ className, ...props }: React.ComponentProps<"table">) {
  return (
    <div
      data-slot="table-container"
      className="relative w-full overflow-x-auto"
    >
      <table
        data-slot="table"
        className={cn("w-full caption-bottom text-sm", className)}
        {...props}
      />
    </div>
  );
}

function TableHeader({ className, ...props }: React.ComponentProps<"thead">) {
  return (
    <thead
      data-slot="table-header"
      className={cn("[&_tr]:border-b", className)}
      {...props}
    />
  );
}

function TableBody({ className, ...props }: React.ComponentProps<"tbody">) {
  return (
    <tbody
      data-slot="table-body"
      className={cn("[&_tr:last-child]:border-0", className)}
      {...props}
    />
  );
}

function TableFooter({ className, ...props }: React.ComponentProps<"tfoot">) {
  return (
    <tfoot
      data-slot="table-footer"
      className={cn(
        "border-t bg-muted/50 font-medium [&>tr]:last:border-b-0",
        className,
      )}
      {...props}
    />
  );
}

function TableRow({ className, ...props }: React.ComponentProps<"tr">) {
  return (
    <tr
      data-slot="table-row"
      className={cn(
        "group/table-row border-b transition-colors hover:bg-muted/50 has-aria-expanded:bg-muted/50 data-[state=selected]:bg-muted",
        className,
      )}
      {...props}
    />
  );
}

function TableHead({ className, ...props }: React.ComponentProps<"th">) {
  return (
    <th
      data-slot="table-head"
      className={cn(
        "px-4 py-3 text-left align-middle text-xs font-medium whitespace-nowrap text-foreground [&:has([role=checkbox])]:pr-0",
        className,
      )}
      {...props}
    />
  );
}

function TableCell({ className, ...props }: React.ComponentProps<"td">) {
  return (
    <td
      data-slot="table-cell"
      className={cn(
        "px-4 py-3 align-middle whitespace-nowrap [&:has([role=checkbox])]:pr-0",
        className,
      )}
      {...props}
    />
  );
}

function TableSelectionHead({
  className,
  checked = false,
  indeterminate = false,
  "aria-label": ariaLabel = "Select all rows",
  onCheckedChange,
  ...props
}: Omit<React.ComponentProps<"th">, "onChange"> & {
  checked?: boolean;
  indeterminate?: boolean;
  onCheckedChange?: (checked: boolean) => void;
}) {
  return (
    <TableHead className={cn("w-10 px-3", className)} {...props}>
      <TableSelectionCheckbox
        checked={checked}
        indeterminate={indeterminate}
        aria-label={ariaLabel}
        alwaysVisible={checked || indeterminate}
        onCheckedChange={onCheckedChange}
      />
    </TableHead>
  );
}

function TableSelectionCell({
  className,
  checked = false,
  "aria-label": ariaLabel = "Select row",
  onCheckedChange,
  ...props
}: Omit<React.ComponentProps<"td">, "onChange"> & {
  checked?: boolean;
  onCheckedChange?: (checked: boolean) => void;
}) {
  return (
    <TableCell className={cn("w-10 px-3", className)} {...props}>
      <TableSelectionCheckbox
        checked={checked}
        aria-label={ariaLabel}
        alwaysVisible={checked}
        onCheckedChange={onCheckedChange}
      />
    </TableCell>
  );
}

function TableSelectionCheckbox({
  className,
  checked = false,
  indeterminate = false,
  alwaysVisible = false,
  onCheckedChange,
  onClick,
  ...props
}: Omit<React.ComponentProps<"button">, "onChange"> & {
  checked?: boolean;
  indeterminate?: boolean;
  alwaysVisible?: boolean;
  onCheckedChange?: (checked: boolean) => void;
}) {
  return (
    <button
      type="button"
      role="checkbox"
      aria-checked={indeterminate ? "mixed" : checked}
      data-state={checked || indeterminate ? "checked" : "unchecked"}
      className={cn(
        "flex size-4 items-center justify-center rounded border border-border bg-background text-primary transition-opacity focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
        !alwaysVisible &&
          "opacity-0 group-hover/table-row:opacity-100 focus-visible:opacity-100",
        className,
      )}
      onClick={(event) => {
        event.stopPropagation();
        onClick?.(event);
        onCheckedChange?.(!(checked || indeterminate));
      }}
      {...props}
    >
      {indeterminate ? (
        <MinusIcon className="size-3" />
      ) : checked ? (
        <CheckIcon className="size-3" />
      ) : null}
    </button>
  );
}

function TableSelectionToolbar({
  className,
  selectedCount,
  children,
}: React.ComponentProps<"div"> & {
  selectedCount: number;
}) {
  if (selectedCount === 0) return null;

  return (
    <div
      data-slot="table-selection-toolbar"
      className={cn(
        "flex h-9 items-center gap-2 rounded-md border border-border bg-background px-3 text-sm shadow-sm",
        className,
      )}
    >
      <span className="text-muted-foreground">{selectedCount} selected</span>
      {children}
    </div>
  );
}

function TableCaption({
  className,
  ...props
}: React.ComponentProps<"caption">) {
  return (
    <caption
      data-slot="table-caption"
      className={cn("mt-4 text-sm text-muted-foreground", className)}
      {...props}
    />
  );
}

export {
  Table,
  TableHeader,
  TableBody,
  TableFooter,
  TableHead,
  TableRow,
  TableCell,
  TableSelectionHead,
  TableSelectionCell,
  TableSelectionCheckbox,
  TableSelectionToolbar,
  TableCaption,
};
