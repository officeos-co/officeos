"use client";

import { BanIcon, CheckCircle2Icon, ChevronDownIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/utils";

export type PermissionMode = "allow" | "deny";

const modeConfig: Record<
  PermissionMode,
  { label: string; icon: typeof CheckCircle2Icon; className: string }
> = {
  allow: {
    label: "Always allow",
    icon: CheckCircle2Icon,
    className: "text-emerald-700",
  },
  deny: {
    label: "Don't allow",
    icon: BanIcon,
    className: "text-red-600",
  },
};

export function PermissionModeSelect({
  value,
  onChange,
  disabled,
  className,
}: {
  value: PermissionMode;
  onChange: (value: PermissionMode) => void;
  disabled?: boolean;
  className?: string;
}) {
  const selected = modeConfig[value];
  const SelectedIcon = selected.icon;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={disabled}
            className={cn("h-8 gap-1.5 rounded-md px-2.5", className)}
          />
        }
      >
        <SelectedIcon className={cn("size-4", selected.className)} />
        <span className="text-xs font-medium">{selected.label}</span>
        <ChevronDownIcon className="size-3.5 text-muted-foreground" />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-40">
        {(Object.keys(modeConfig) as PermissionMode[]).map((mode) => {
          const option = modeConfig[mode];
          const Icon = option.icon;
          return (
            <DropdownMenuItem key={mode} onClick={() => onChange(mode)}>
              <Icon className={cn("mr-2 size-4", option.className)} />
              {option.label}
            </DropdownMenuItem>
          );
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
