"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { UserPlusIcon } from "lucide-react";
import { PageHeader } from "@/shell/page-header";
import { PageContainer } from "@/shell/page-container";
import { Button } from "@/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import { Label } from "@/ui/label";
import { Skeleton } from "@/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/ui/table";
import { Textarea } from "@/ui/textarea";
import {
  organizationRoleLabel,
  organizationRoleTooltip,
  useInviteMember,
  useOrganization,
} from "@/features/manage";

const MAX_INVITE_EMAILS = 50;
const INVITE_ROLE_OPTIONS = [
  {
    value: "Editor",
    label: "Editor",
    description: organizationRoleTooltip("Editor"),
  },
  {
    value: "Admin",
    label: "Admin",
    description: organizationRoleTooltip("Admin"),
  },
  {
    value: "Viewer",
    label: "Viewer",
    description: organizationRoleTooltip("Viewer"),
  },
] as const;

export default function MembersPage() {
  const router = useRouter();
  const { organization, loading, error } = useOrganization();
  const { inviteMember, loading: inviting } = useInviteMember();
  const [inviteOpen, setInviteOpen] = useState(false);
  const [inviteEmails, setInviteEmails] = useState("");
  const [inviteRole, setInviteRole] =
    useState<(typeof INVITE_ROLE_OPTIONS)[number]["value"]>("Editor");
  const parsedInviteEmails = useMemo(
    () => parseInviteEmails(inviteEmails),
    [inviteEmails],
  );
  const inviteEmailCount = parsedInviteEmails.length;
  const selectedInviteRole = INVITE_ROLE_OPTIONS.find(
    (role) => role.value === inviteRole,
  );
  const hasInvalidInviteEmails = parsedInviteEmails.some(
    (email) => !isLikelyEmail(email),
  );
  const canSendInvite =
    inviteEmailCount > 0 &&
    inviteEmailCount <= MAX_INVITE_EMAILS &&
    !hasInvalidInviteEmails;

  useEffect(() => {
    if (error) {
      toast.error("Failed to load members", { description: error.message });
    }
  }, [error]);

  async function handleInvite() {
    if (!canSendInvite) return;
    for (const email of parsedInviteEmails) {
      await inviteMember({ email, role: inviteRole });
    }
    setInviteEmails("");
    setInviteRole("Editor");
    setInviteOpen(false);
    toast.success(
      inviteEmailCount === 1 ? "Invitation created" : "Invitations created",
    );
  }

  if (loading && !organization) {
    return (
      <>
        <PageHeader
          page="Members"
          subtitle="Manage organization members."
          width="thin"
        />
        <PageContainer width="thin" className="flex flex-1 flex-col gap-3 pb-4">
          {Array.from({ length: 4 }).map((_, index) => (
            <Skeleton key={index} className="h-12 w-full rounded-md" />
          ))}
        </PageContainer>
      </>
    );
  }

  if (!organization) {
    return (
      <>
        <PageHeader
          page="Members"
          subtitle="Manage organization members."
          width="thin"
        />
        <PageContainer width="thin" className="py-20">
          <p className="text-sm text-muted-foreground">
            Unable to load organization members.
          </p>
        </PageContainer>
      </>
    );
  }

  return (
    <>
      <PageHeader
        page="Members"
        subtitle="Manage organization members."
        width="thin"
        action={
          <Button size="sm" onClick={() => setInviteOpen(true)}>
            <UserPlusIcon className="size-3.5" />
            Invite member
          </Button>
        }
      />
      <PageContainer width="thin" className="flex flex-1 flex-col gap-4 pb-4">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Email</TableHead>
              <TableHead>Role</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {organization.members.map((member) => (
              <TableRow
                key={member.id}
                className="cursor-pointer"
                onClick={() => router.push(`/members/${member.id}`)}
              >
                <TableCell className="font-medium">
                  {member.email.split("@")[0]}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {member.email}
                </TableCell>
                <TableCell>
                  <span
                    title={organizationRoleTooltip(member.role)}
                    className="rounded bg-muted px-1.5 py-0.5 text-xs"
                  >
                    {organizationRoleLabel(member.role)}
                  </span>
                </TableCell>
              </TableRow>
            ))}
            {organization.members.length === 0 && (
              <TableRow>
                <TableCell
                  colSpan={3}
                  className="h-24 text-center text-muted-foreground"
                >
                  No members found.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </PageContainer>

      <Dialog open={inviteOpen} onOpenChange={setInviteOpen}>
        <DialogContent className="max-w-[448px]">
          <DialogHeader>
            <DialogTitle>Invite Members</DialogTitle>
            <DialogDescription>
              Enter up to 50 email addresses, separated by commas or new lines
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3 pt-2">
            <div className="space-y-1.5">
              <Textarea
                id="invite-emails"
                autoFocus
                value={inviteEmails}
                onChange={(event) => setInviteEmails(event.target.value)}
                placeholder={
                  "claude.shannon@example.com\nshannon.claude@example.com"
                }
                className="min-h-36 resize-none"
              />
              {inviteEmailCount > MAX_INVITE_EMAILS && (
                <p className="text-xs text-destructive">
                  Enter 50 email addresses or fewer.
                </p>
              )}
              {hasInvalidInviteEmails && (
                <p className="text-xs text-destructive">
                  Remove invalid email addresses before sending.
                </p>
              )}
            </div>
            <div className="flex items-center justify-between gap-3">
              <Label>Role</Label>
              <Select
                value={inviteRole}
                onValueChange={(value) =>
                  value &&
                  setInviteRole(
                    value as (typeof INVITE_ROLE_OPTIONS)[number]["value"],
                  )
                }
              >
                <SelectTrigger className="w-24">
                  <SelectValue>{selectedInviteRole?.label}</SelectValue>
                </SelectTrigger>
                <SelectContent align="end" className="w-[356px]">
                  {INVITE_ROLE_OPTIONS.map((role) => (
                    <SelectItem
                      key={role.value}
                      value={role.value}
                      className="items-start py-2 pr-8 pl-2"
                    >
                      <span className="flex flex-col items-start gap-0.5 whitespace-normal text-left">
                        <span>{role.label}</span>
                        <span className="text-xs leading-4 text-muted-foreground">
                          {role.description}
                        </span>
                      </span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setInviteOpen(false)}
            >
              Cancel
            </Button>
            <Button
              size="sm"
              onClick={handleInvite}
              disabled={inviting || !canSendInvite}
            >
              {inviteEmailCount > 1 ? "Create invites" : "Create invite"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}

function parseInviteEmails(value: string) {
  return value
    .split(/[,\n]+/)
    .map((email) => email.trim())
    .filter(Boolean);
}

function isLikelyEmail(value: string) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value);
}
