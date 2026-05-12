"use client";

import { useState, type FormEvent } from "react";
import { toast } from "sonner";
import { getDialogWidthClassName } from "@/shell/page-container";
import { Button } from "@/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import { Input } from "@/ui/input";
import { Label } from "@/ui/label";
import { useCreateOrganization } from "../api/useOrganization";

export function OrganizationCreateDialog({
  open,
  onOpenChange,
  defaultName,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  defaultName: string;
}) {
  const { createOrganization, loading } = useCreateOrganization();
  const [name, setName] = useState(defaultName);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed || loading) return;
    await createOrganization(trimmed);
    toast.success("Organization set up");
    onOpenChange(false);
  }

  function setOpen(next: boolean) {
    if (loading) return;
    if (next) setName(defaultName);
    onOpenChange(next);
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent
        className={getDialogWidthClassName("narrow")}
        showCloseButton={!loading}
      >
        <DialogHeader>
          <DialogTitle className="text-xl">
            Let&apos;s get your organization details
          </DialogTitle>
          <DialogDescription>
            Set up the organization your team will collaborate in.
          </DialogDescription>
        </DialogHeader>
        <form className="space-y-4" onSubmit={submit}>
          <div className="space-y-1.5">
            <Label htmlFor="setup-organization-name">Organization name</Label>
            <Input
              id="setup-organization-name"
              autoFocus
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="Acme Operations"
            />
          </div>
          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => setOpen(false)}
              disabled={loading}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={loading || !name.trim()}>
              Set up organization
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
