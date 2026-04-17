"use client"

import { useEffect, useState } from "react"
import { toast } from "sonner"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { PlusIcon, Trash2Icon, Loader2Icon } from "lucide-react"
import {
  useOrganization,
  useInviteMember,
  useRemoveMember,
  useRenameOrg,
} from "@/hooks/useOrganization"

export default function TeamPage() {
  const { organization, loading, error } = useOrganization()
  const { inviteMember } = useInviteMember()
  const { removeMember } = useRemoveMember()
  const { renameOrg } = useRenameOrg()

  const [inviteOpen, setInviteOpen] = useState(false)
  const [inviteEmail, setInviteEmail] = useState("")
  const [orgName, setOrgName] = useState("")

  useEffect(() => {
    if (error) toast.error("Failed to load team", { description: error.message })
  }, [error])

  useEffect(() => {
    if (!loading && organization) setOrgName(organization.name)
  }, [loading, organization])

  async function handleInvite() {
    if (!inviteEmail.includes("@")) return
    await inviteMember({ email: inviteEmail, role: "Member" })
    setInviteEmail("")
    setInviteOpen(false)
  }

  async function handleRemove(memberId: string) {
    await removeMember(memberId)
  }

  async function handleRenameBlur() {
    if (organization && orgName && orgName !== organization.name) {
      await renameOrg(orgName)
    }
  }

  if (loading) {
    return (
      <>
        <PageHeader group="Manage" page="Team" />
        <div className="flex items-center justify-center py-20">
          <Loader2Icon className="size-6 animate-spin text-muted-foreground" />
        </div>
      </>
    )
  }

  if (!organization) {
    return (
      <>
        <PageHeader group="Manage" page="Team" />
        <div className="flex items-center justify-center py-20">
          <p className="text-sm text-muted-foreground">Unable to load team information.</p>
        </div>
      </>
    )
  }

  const members = organization.members

  return (
    <>
      <PageHeader
        group="Manage"
        page="Team"
        action={
          <Button size="sm" onClick={() => setInviteOpen(true)}>
            <PlusIcon className="size-3.5" />
            Invite member
          </Button>
        }
      />
      <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-3xl mx-auto w-full">
        <section>
          <h3 className="text-sm font-semibold mb-3">Organization</h3>
          <div className="space-y-2 max-w-sm">
            <Label>Organization name</Label>
            <Input
              value={orgName}
              onChange={(e) => setOrgName(e.target.value)}
              onBlur={handleRenameBlur}
            />
          </div>
        </section>

        <Separator />

        <section>
          <h3 className="text-sm font-semibold mb-3">Members ({members.length})</h3>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left">
                <th className="px-0 py-2.5 font-medium">Name</th>
                <th className="px-0 py-2.5 font-medium">Email</th>
                <th className="px-0 py-2.5 font-medium">Role</th>
                <th className="px-0 py-2.5 font-medium">Joined</th>
                <th className="px-0 py-2.5 w-[40px]" />
              </tr>
            </thead>
            <tbody>
              {members.map((m) => (
                <tr key={m.id} className="border-b last:border-0">
                  <td className="px-0 py-2.5 font-medium">{m.name ?? m.email.split("@")[0]}</td>
                  <td className="px-0 py-2.5 text-muted-foreground">{m.email}</td>
                  <td className="px-0 py-2.5">
                    <span className={`rounded bg-muted px-1.5 py-0.5 text-xs ${m.role === "Owner" ? "font-medium" : ""}`}>
                      {m.role}
                    </span>
                    {m.status === "invited" && (
                      <span className="ml-2 rounded bg-amber-100 text-amber-900 px-1.5 py-0.5 text-xs">
                        invited
                      </span>
                    )}
                  </td>
                  <td className="px-0 py-2.5 text-muted-foreground">{m.joinedAgo}</td>
                  <td className="px-0 py-2.5">
                    {m.role !== "Owner" && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                        onClick={() => handleRemove(m.id)}
                      >
                        <Trash2Icon className="size-3.5" />
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      </div>

      <Dialog open={inviteOpen} onOpenChange={setInviteOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Invite team member</DialogTitle>
            <DialogDescription>They&apos;ll receive an email invitation to join your organization.</DialogDescription>
          </DialogHeader>
          <div className="space-y-3 pt-2">
            <div className="space-y-1.5">
              <Label>Email address</Label>
              <Input
                type="email"
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
                placeholder="colleague@company.com"
              />
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" size="sm" onClick={() => setInviteOpen(false)}>Cancel</Button>
            <Button size="sm" onClick={handleInvite} disabled={!inviteEmail.includes("@")}>Send invite</Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
