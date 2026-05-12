"use client";

import { useState, type FormEvent } from "react";
import { apolloClient } from "@/lib/graphql/client";
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
import { usePurgeMyData } from "../api/useGdpr";

export function OrganizationDeleteDialog({
  open,
  onOpenChange,
  organizationName,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  organizationName: string;
}) {
  const { purgeData, loading } = usePurgeMyData();
  const [confirmation, setConfirmation] = useState("");
  const confirmed = confirmation === organizationName;

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!confirmed || loading) return;
    const purged = await purgeData();
    if (!purged) return;
    await apolloClient.clearStore();
    window.location.href = "/login";
  }

  function setOpen(next: boolean) {
    if (loading) return;
    if (!next) setConfirmation("");
    onOpenChange(next);
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent showCloseButton={!loading}>
        <DialogHeader>
          <DialogTitle>Delete organization and account</DialogTitle>
          <DialogDescription>
            This permanently deletes your account and the data owned by it.
          </DialogDescription>
        </DialogHeader>
        <form className="space-y-4" onSubmit={submit}>
          <div className="space-y-1.5">
            <Label htmlFor="delete-organization-confirmation">
              Type {organizationName} to confirm
            </Label>
            <Input
              id="delete-organization-confirmation"
              value={confirmation}
              onChange={(event) => setConfirmation(event.target.value)}
              autoComplete="off"
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
            <Button
              type="submit"
              variant="destructive"
              disabled={loading || !confirmed}
            >
              Delete account
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
