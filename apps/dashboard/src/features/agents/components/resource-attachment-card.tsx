"use client";

import type { ReactNode } from "react";
import Link from "next/link";
import { ExternalLinkIcon, Trash2Icon } from "lucide-react";
import { Button } from "@/ui/button";
import { Label } from "@/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import { Textarea } from "@/ui/textarea";

export type ResourceOption = {
  id: string;
  label: string;
  logo?: string;
};

export function ResourceAttachmentCard({
  title,
  icon,
  selectorLabel,
  selectorPlaceholder,
  manageHref,
  manageLabel,
  options,
  value,
  instructions,
  onValueChange,
  onInstructionsChange,
  onRemove,
}: {
  title: string;
  icon?: ReactNode;
  selectorLabel: string;
  selectorPlaceholder: string;
  manageHref: string;
  manageLabel: string;
  options: ResourceOption[];
  value: string;
  instructions: string;
  onValueChange: (value: string) => void;
  onInstructionsChange: (value: string) => void;
  onRemove: () => void;
}) {
  const selectedOption = options.find((option) => option.id === value);
  const selectedOptionLogo = selectedOption?.logo;

  return (
    <div className="rounded-xl border border-border p-4">
      <div className="mb-8 flex items-center justify-between gap-3">
        <h3 className="inline-flex items-center gap-2 text-sm font-medium">
          {icon && (
            <span className="flex size-7 items-center justify-center rounded-md bg-muted text-muted-foreground">
              {icon}
            </span>
          )}
          {title}
        </h3>
        <Button variant="ghost" size="icon-sm" onClick={onRemove}>
          <Trash2Icon className="size-4" />
        </Button>
      </div>

      <div className="mb-2 flex items-end justify-between gap-3">
        <Label>
          {selectorLabel} <span className="text-destructive">*</span>
        </Label>
        <Link
          href={manageHref}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1 text-sm underline underline-offset-4 hover:text-foreground"
        >
          {manageLabel}
          <ExternalLinkIcon className="size-3.5" />
        </Link>
      </div>
      <Select value={value} onValueChange={(next) => next && onValueChange(next)}>
        <SelectTrigger className="w-full">
          <SelectValue placeholder={selectorPlaceholder}>
            <span className="inline-flex min-w-0 items-center gap-2">
              {selectedOptionLogo ? (
                <span
                  aria-hidden="true"
                  className="flex size-4 shrink-0 items-center justify-center [&>img]:size-4 [&>img]:object-contain [&>svg]:size-4"
                  dangerouslySetInnerHTML={{ __html: selectedOptionLogo }}
                />
              ) : null}
              <span className="truncate">{selectedOption?.label}</span>
            </span>
          </SelectValue>
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem key={option.id} value={option.id}>
              <span className="inline-flex min-w-0 items-center gap-2">
                {option.logo ? (
                  <span
                    aria-hidden="true"
                    className="flex size-4 shrink-0 items-center justify-center [&>img]:size-4 [&>img]:object-contain [&>svg]:size-4"
                    dangerouslySetInnerHTML={{ __html: option.logo }}
                  />
                ) : null}
                <span className="truncate">{option.label}</span>
              </span>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <div className="mt-6 space-y-2">
        <Label>Instructions (optional)</Label>
        <Textarea
          value={instructions}
          onChange={(event) => onInstructionsChange(event.target.value)}
          placeholder="Tell the agent what this resource contains and when to use it."
          rows={4}
        />
      </div>
    </div>
  );
}
