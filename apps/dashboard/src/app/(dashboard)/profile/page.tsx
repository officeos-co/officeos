"use client"

import { useEffect, useState } from "react"
import { toast } from "sonner"
import { PageHeader } from "@/components/page-header"
import { PageContainer } from "@/components/page-container"
import { Button } from "@/components/ui/button"
import { HelpTooltip, WithTooltip } from "@/components/ui/help-tooltip"
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
import { Skeleton } from "@/components/ui/skeleton"
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

  if (loading && !profile) {
    return (
      <>
        <PageHeader
          page="Profile"
          subtitle="Update account details and preferences."
          width="narrow"
        />
        <PageContainer width="narrow" className="flex flex-1 flex-col gap-8 pb-4">
          <section>
            <Skeleton className="h-5 w-16 mb-4" />
            <div className="grid grid-cols-[1fr_1fr] gap-4">
              <div className="space-y-2">
                <Skeleton className="h-4 w-20" />
                <div className="flex items-center gap-3">
                  <Skeleton className="size-10 rounded-full" />
                  <Skeleton className="h-9 flex-1 rounded-md" />
                </div>
              </div>
              <div className="space-y-2">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-9 w-full rounded-md" />
              </div>
            </div>
          </section>
          <Skeleton className="h-px w-full" />
          <section>
            <Skeleton className="h-5 w-20 mb-4" />
            <Skeleton className="h-9 w-48 rounded-md" />
          </section>
          <Skeleton className="h-px w-full" />
          <section>
            <Skeleton className="h-5 w-28 mb-4" />
            <div className="space-y-3">
              <Skeleton className="h-8 w-full rounded-md" />
              <Skeleton className="h-8 w-full rounded-md" />
            </div>
          </section>
        </PageContainer>
      </>
    )
  }

  if (!profile) {
    return (
      <>
        <PageHeader
          page="Profile"
          subtitle="Update account details and preferences."
          width="narrow"
        />
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
        page="Profile"
        subtitle="Update account details and preferences."
        width="narrow"
        action={
          <Button size="sm" onClick={handleSave} disabled={saving || loading}>
            {saving ? "Saving…" : "Save"}
          </Button>
        }
      />
      <PageContainer width="narrow" className="flex flex-1 flex-col gap-8 pb-4">
        <section>
          <h2 className="text-base font-semibold mb-4">Profile</h2>
          <div className="space-y-4">
            <div className="grid grid-cols-[1fr_1fr] gap-4">
              <div className="space-y-2">
                <Label>
                  Full name
                  <HelpTooltip>
                    Used for account display in the operator UI.
                  </HelpTooltip>
                </Label>
                <div className="flex items-center gap-3">
                  <div className="flex size-10 items-center justify-center rounded-full bg-muted text-sm font-medium text-muted-foreground shrink-0">
                    {initials}
                  </div>
                  <Input value={fullName} onChange={(e) => setFullName(e.target.value)} />
                </div>
              </div>
              <div className="space-y-2">
                <Label>
                  Display name <span className="text-destructive">*</span>
                  <HelpTooltip>
                    Name shown to teammates and in dashboard activity.
                  </HelpTooltip>
                </Label>
                <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
              </div>
            </div>

            <div className="space-y-2">
              <Label>
                Timezone
                <HelpTooltip>
                  Used for display and scheduling defaults. Cron schedules are
                  still stored as backend schedule expressions.
                </HelpTooltip>
              </Label>
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
              <Label>
                What personal preferences should agents consider?
                <HelpTooltip>
                  These preferences are available to agents as operator context
                  and should not contain secrets.
                </HelpTooltip>
              </Label>
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
              <WithTooltip tooltip="Toggle task completion notifications for long-running agent work.">
                <Switch
                  checked={prefs.taskCompletions}
                  onCheckedChange={(v) => updatePref("taskCompletions", v)}
                />
              </WithTooltip>
            </div>
            <div className="flex items-start justify-between py-4 border-b border-border">
              <div>
                <p className="text-sm font-medium">Email notifications</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Get an email when an agent needs your approval or has completed a scheduled task.
                </p>
              </div>
              <WithTooltip tooltip="Toggle email notifications for approval requests and scheduled task completion.">
                <Switch
                  checked={prefs.email}
                  onCheckedChange={(v) => updatePref("email", v)}
                />
              </WithTooltip>
            </div>
            <div className="flex items-start justify-between py-4">
              <div>
                <p className="text-sm font-medium">Channel messages</p>
                <p className="text-xs text-muted-foreground mt-0.5">
                  Get a push notification when a channel message requires your attention.
                </p>
              </div>
              <WithTooltip tooltip="Toggle push notifications for connected channel messages that need attention.">
                <Switch
                  checked={prefs.channelMessages}
                  onCheckedChange={(v) => updatePref("channelMessages", v)}
                />
              </WithTooltip>
            </div>
          </div>
        </section>
      </PageContainer>
    </>
  )
}
