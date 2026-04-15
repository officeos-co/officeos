"use client"

import { useState } from "react"
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
import { PlusIcon, Trash2Icon } from "lucide-react"

type Member = {
  id: string
  name: string
  email: string
  role: "Owner" | "Admin" | "Member"
  joinedAgo: string
}

const mockMembers: Member[] = [
  { id: "1", name: "Harro Krog", email: "harro@officeos.co", role: "Owner", joinedAgo: "6 months ago" },
  { id: "2", name: "Anna Schmidt", email: "anna@officeos.co", role: "Admin", joinedAgo: "3 months ago" },
  { id: "3", name: "Max Weber", email: "max@officeos.co", role: "Member", joinedAgo: "1 month ago" },
]

export default function TeamPage() {
  const [members, setMembers] = useState(mockMembers)
  const [inviteOpen, setInviteOpen] = useState(false)
  const [inviteEmail, setInviteEmail] = useState("")
  const [orgName, setOrgName] = useState("Acme Corp")

  function handleInvite() {
    const newMember: Member = {
      id: `m_${Date.now()}`,
      name: inviteEmail.split("@")[0],
      email: inviteEmail,
      role: "Member",
      joinedAgo: "just now",
    }
    setMembers([...members, newMember])
    setInviteEmail("")
    setInviteOpen(false)
  }

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
        {/* Organization */}
        <section>
          <h3 className="text-sm font-semibold mb-3">Organization</h3>
          <div className="space-y-2 max-w-sm">
            <Label>Organization name</Label>
            <Input value={orgName} onChange={(e) => setOrgName(e.target.value)} />
          </div>
        </section>

        <Separator />

        {/* Members */}
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
                  <td className="px-0 py-2.5 font-medium">{m.name}</td>
                  <td className="px-0 py-2.5 text-muted-foreground">{m.email}</td>
                  <td className="px-0 py-2.5">
                    <span className={`rounded bg-muted px-1.5 py-0.5 text-xs ${m.role === "Owner" ? "font-medium" : ""}`}>
                      {m.role}
                    </span>
                  </td>
                  <td className="px-0 py-2.5 text-muted-foreground">{m.joinedAgo}</td>
                  <td className="px-0 py-2.5">
                    {m.role !== "Owner" && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                        onClick={() => setMembers(members.filter((x) => x.id !== m.id))}
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

      {/* Invite dialog */}
      <Dialog open={inviteOpen} onOpenChange={setInviteOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Invite team member</DialogTitle>
            <DialogDescription>They'll receive an email invitation to join your organization.</DialogDescription>
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
