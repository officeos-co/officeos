"use client"

import { useState } from "react"
import { PageHeader } from "@/components/page-header"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Switch } from "@/components/ui/switch"
import { Separator } from "@/components/ui/separator"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"

export default function ProfilePage() {
  const [fullName, setFullName] = useState("Harro Krog")
  const [displayName, setDisplayName] = useState("Harro")
  const [workFunction, setWorkFunction] = useState("")
  const [preferences, setPreferences] = useState("")
  const [notifyCompletions, setNotifyCompletions] = useState(false)
  const [notifyEmails, setNotifyEmails] = useState(false)
  const [notifyDispatch, setNotifyDispatch] = useState(false)

  return (
    <>
      <PageHeader group="Manage" page="Profile" />
      <div className="flex flex-1 flex-col gap-8 p-4 pt-0 max-w-3xl mx-auto w-full">
        {/* Profile section */}
        <section>
          <h2 className="text-base font-semibold mb-4">Profile</h2>
          <div className="space-y-4">
            <div className="grid grid-cols-[1fr_1fr] gap-4">
              <div className="space-y-2">
                <Label>Full name</Label>
                <div className="flex items-center gap-3">
                  <div className="flex size-10 items-center justify-center rounded-full bg-muted text-sm font-medium text-muted-foreground shrink-0">
                    {fullName.split(" ").map((n) => n[0]).join("").toUpperCase().slice(0, 2)}
                  </div>
                  <Input value={fullName} onChange={(e) => setFullName(e.target.value)} />
                </div>
              </div>
              <div className="space-y-2">
                <Label>Display name <span className="text-destructive">*</span></Label>
                <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
              </div>
            </div>

            <div className="space-y-2">
              <Label>What best describes your work?</Label>
              <Select value={workFunction} onValueChange={(v) => { if (v) setWorkFunction(v) }}>
                <SelectTrigger>
                  <SelectValue placeholder="Select your work function" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="engineering">Engineering</SelectItem>
                  <SelectItem value="product">Product</SelectItem>
                  <SelectItem value="design">Design</SelectItem>
                  <SelectItem value="data">Data Science</SelectItem>
                  <SelectItem value="operations">Operations</SelectItem>
                  <SelectItem value="marketing">Marketing</SelectItem>
                  <SelectItem value="sales">Sales</SelectItem>
                  <SelectItem value="support">Support</SelectItem>
                  <SelectItem value="other">Other</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>What personal preferences should agents consider?</Label>
              <p className="text-xs text-muted-foreground">Your preferences will apply to all agents.</p>
              <Textarea
                value={preferences}
                onChange={(e) => setPreferences(e.target.value)}
                placeholder="e.g. keep explanations brief and to the point"
                rows={4}
              />
            </div>
          </div>
        </section>

        <Separator />

        {/* Notifications section */}
        <section>
          <h2 className="text-base font-semibold mb-4">Notifications</h2>
          <div className="space-y-0">
            <div className="flex items-start justify-between py-4 border-b border-border">
              <div>
                <p className="text-sm font-medium">Task completions</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Get notified when an agent has finished a task. Useful for long-running tool calls and research.
                </p>
              </div>
              <Switch checked={notifyCompletions} onCheckedChange={setNotifyCompletions} />
            </div>
            <div className="flex items-start justify-between py-4 border-b border-border">
              <div>
                <p className="text-sm font-medium">Email notifications</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Get an email when an agent needs your approval or has completed a scheduled task.
                </p>
              </div>
              <Switch checked={notifyEmails} onCheckedChange={setNotifyEmails} />
            </div>
            <div className="flex items-start justify-between py-4">
              <div>
                <p className="text-sm font-medium">Channel messages</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Get a push notification when a channel message requires your attention.
                </p>
              </div>
              <Switch checked={notifyDispatch} onCheckedChange={setNotifyDispatch} />
            </div>
          </div>
        </section>
      </div>
    </>
  )
}
