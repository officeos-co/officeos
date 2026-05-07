"use client";

import Link from "next/link";
import { ExternalLinkIcon, Trash2Icon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";

export type ResourceOption = {
  id: string;
  label: string;
};

export function ResourceAttachmentCard({
  title,
  selectorLabel,
  selectorPlaceholder,
  manageHref,
  manageLabel,
  options,
  value,
  access,
  instructions,
  onValueChange,
  onAccessChange,
  onInstructionsChange,
  onRemove,
}: {
  title: string;
  selectorLabel: string;
  selectorPlaceholder: string;
  manageHref: string;
  manageLabel: string;
  options: ResourceOption[];
  value: string;
  access: string;
  instructions: string;
  onValueChange: (value: string) => void;
  onAccessChange: (value: string) => void;
  onInstructionsChange: (value: string) => void;
  onRemove: () => void;
}) {
  return (
    <div className="rounded-xl border border-border p-4">
      <div className="mb-8 flex items-center justify-between gap-3">
        <h3 className="text-sm font-medium">{title}</h3>
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
          className="inline-flex items-center gap-1 text-sm underline underline-offset-4 hover:text-foreground"
        >
          {manageLabel}
          <ExternalLinkIcon className="size-3.5" />
        </Link>
      </div>
      <Select value={value} onValueChange={(next) => next && onValueChange(next)}>
        <SelectTrigger className="w-full">
          <SelectValue placeholder={selectorPlaceholder} />
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem key={option.id} value={option.id}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <div className="mt-6 space-y-2">
        <Label>Access</Label>
        <Select value={access} onValueChange={(next) => next && onAccessChange(next)}>
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="read_write">Read & write</SelectItem>
            <SelectItem value="read_only">Read only</SelectItem>
          </SelectContent>
        </Select>
      </div>

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
