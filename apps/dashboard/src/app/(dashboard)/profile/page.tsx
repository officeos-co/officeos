"use client"

import { useEffect, useState } from "react"
import { toast } from "sonner"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
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
import { Loader2Icon } from "lucide-react"
import { useProfile, useUpdateProfile, type NotificationPrefs } from "@/features/manage"

export default function ProfilePage() {
  const { profile, loading, error } = useProfile()
  const { updateProfile } = useUpdateProfile()

  const [fullName, setFullName] = useState("")
  const [displayName, setDisplayName] = useState("")
  const [timezone, setTimezone] = useState("")
  const [preferences, setPreferences] = useState("")
  const [prefs, setPrefs] = useState<NotificationPrefs>({
    taskCompletions: false,
    email: false,
    channelMessages: false,
  })
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (error) toast.error("Failed to load profile", { description: error.message })
  }, [error])

  useEffect(() => {
    if (!loading && profile) {
      setFullName(profile.name ?? "")
      setDisplayName(profile.displayName ?? "")
      setTimezone(profile.timezone ?? "")
      setPreferences(profile.preferences ?? "")
      setPrefs(profile.notificationPrefs)
    }
  }, [loading, profile])

  async function handleSave() {
    setSaving(true)
    try {
      await updateProfile({
        name: fullName,
        displayName,
        timezone: timezone || null,
        preferences,
        notificationPrefs: prefs,
      })
      toast.success("Profile saved")
    } catch (err) {
      toast.error("Failed to save profile", {
        description: err instanceof Error ? err.message : "Unknown error",
      })
    } finally {
      setSaving(false)
    }
  }

  async function updatePref(key: keyof NotificationPrefs, value: boolean) {
    const next = { ...prefs, [key]: value }
    setPrefs(next)
    try {
      await updateProfile({ notificationPrefs: next })
    } catch {
      setPrefs(prefs)
      toast.error("Failed to update notification preference")
    }
  }

  if (loading) {
    return (
      <>
        <PageHeader group="Manage" page="Profile" />
        <div className="flex items-center justify-center py-20">
          <Loader2Icon className="size-6 animate-spin text-muted-foreground" />
        </div>
      </>
    )
  }

  if (!profile) {
    return (
      <>
        <PageHeader group="Manage" page="Profile" />
        <div className="flex items-center justify-center py-20">
          <p className="text-sm text-muted-foreground">Unable to load profile.</p>
        </div>
      </>
    )
  }

  const initials = (fullName || profile.email || "?")
    .split(/\s+|@/)
    .map((n) => n[0])
    .filter(Boolean)
    .join("")
    .toUpperCase()
    .slice(0, 2)

  return (
    <>
      <PageHeader
        group="Manage"
        page="Profile"
        action={
          <Button size="sm" onClick={handleSave} disabled={saving || loading}>
            {saving ? "Saving…" : "Save"}
          </Button>
        }
      />
      <div className="flex flex-1 flex-col gap-8 p-4 pt-0 max-w-3xl mx-auto w-full">
        <section>
          <h2 className="text-base font-semibold mb-4">Profile</h2>
          <div className="space-y-4">
            <div className="grid grid-cols-[1fr_1fr] gap-4">
              <div className="space-y-2">
                <Label>Full name</Label>
                <div className="flex items-center gap-3">
                  <div className="flex size-10 items-center justify-center rounded-full bg-muted text-sm font-medium text-muted-foreground shrink-0">
                    {initials}
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
              <Label>Timezone</Label>
              <Select value={timezone || undefined} onValueChange={(v) => { if (v) setTimezone(v) }}>
                <SelectTrigger>
                  <SelectValue placeholder="Select your timezone" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="UTC">UTC</SelectItem>
                  <SelectItem value="Europe/Amsterdam">Europe/Amsterdam</SelectItem>
                  <SelectItem value="Europe/London">Europe/London</SelectItem>
                  <SelectItem value="America/New_York">America/New_York</SelectItem>
                  <SelectItem value="America/Los_Angeles">America/Los_Angeles</SelectItem>
                  <SelectItem value="Asia/Tokyo">Asia/Tokyo</SelectItem>
                  <SelectItem value="Asia/Singapore">Asia/Singapore</SelectItem>
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
              <Switch
                checked={prefs.taskCompletions}
                onCheckedChange={(v) => updatePref("taskCompletions", v)}
              />
            </div>
            <div className="flex items-start justify-between py-4 border-b border-border">
              <div>
                <p className="text-sm font-medium">Email notifications</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Get an email when an agent needs your approval or has completed a scheduled task.
                </p>
              </div>
              <Switch
                checked={prefs.email}
                onCheckedChange={(v) => updatePref("email", v)}
              />
            </div>
            <div className="flex items-start justify-between py-4">
              <div>
                <p className="text-sm font-medium">Channel messages</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Get a push notification when a channel message requires your attention.
                </p>
              </div>
              <Switch
                checked={prefs.channelMessages}
                onCheckedChange={(v) => updatePref("channelMessages", v)}
              />
            </div>
          </div>
        </section>
      </div>
    </>
  )
}
