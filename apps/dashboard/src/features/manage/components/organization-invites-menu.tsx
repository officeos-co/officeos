"use client";

import { InboxIcon } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from "@/ui/dropdown-menu";
import {
  useAcceptOrganizationInvite,
  useDeclineOrganizationInvite,
  usePendingOrganizationInvites,
} from "../api/useOrganization";

export function OrganizationInvitesMenu() {
  const { invites, loading } = usePendingOrganizationInvites();
  const { acceptOrganizationInvite, loading: accepting } =
    useAcceptOrganizationInvite();
  const { declineOrganizationInvite, loading: declining } =
    useDeclineOrganizationInvite();
  const busy = accepting || declining;

  async function accept(memberId: string, organizationName: string) {
    await acceptOrganizationInvite(memberId);
    toast.success(`Joined ${organizationName}`);
  }

  async function decline(memberId: string, organizationName: string) {
    await declineOrganizationInvite(memberId);
    toast.success(`Declined ${organizationName}`);
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger render={<Button size="sm" variant="outline" />}>
        <span className="relative inline-flex">
          <InboxIcon className="size-3.5" />
          {invites.length > 0 && (
            <span className="absolute -top-1 -right-1 size-2 rounded-full bg-destructive" />
          )}
        </span>
        Messages
        {invites.length > 0 && (
          <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] leading-none text-muted-foreground">
            {invites.length}
          </span>
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent
        side="bottom"
        align="end"
        sideOffset={6}
        className="w-72 rounded-xl p-2"
      >
        <div className="px-1 pb-2 text-xs font-medium text-muted-foreground">
          Organization invitations
        </div>
        {loading ? (
          <div className="px-1 py-6 text-center text-sm text-muted-foreground">
            Loading invites...
          </div>
        ) : invites.length === 0 ? (
          <div className="px-1 py-6 text-center text-sm text-muted-foreground">
            No pending invites.
          </div>
        ) : (
          <div className="space-y-2">
            {invites.map((invite) => (
              <div key={invite.id} className="rounded-md border p-2">
                <div className="truncate text-sm font-medium">
                  {invite.organizationName}
                </div>
                <div className="mt-0.5 text-xs text-muted-foreground">
                  Invited as {invite.role}
                </div>
                <div className="mt-2 flex justify-end gap-2">
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() =>
                      void decline(invite.id, invite.organizationName)
                    }
                    disabled={busy}
                  >
                    Decline
                  </Button>
                  <Button
                    size="sm"
                    onClick={() =>
                      void accept(invite.id, invite.organizationName)
                    }
                    disabled={busy}
                  >
                    Accept
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
