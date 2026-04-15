"use client";

import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
import { useAuth } from "@/hooks/useAuth";
import { apiFetch } from "@/hooks/useApi";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

export default function ProfilePage() {
  const { user, logout } = useAuth();
  const [name, setName] = useState(user?.name ?? "");
  const [saved, setSaved] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [deleting, setDeleting] = useState(false);

  function handleSave() {
    // TODO: wire up to backend
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  async function handleExport() {
    setExporting(true);
    try {
      const res = await fetch("/api/gdpr/export", { credentials: "include" });
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "eaos-export.json";
      a.click();
      URL.revokeObjectURL(url);
    } finally {
      setExporting(false);
    }
  }

  async function handleDelete() {
    if (!window.confirm("Permanently delete your account and all associated data? This cannot be undone.")) return;
    setDeleting(true);
    try {
      await apiFetch("/api/gdpr/purge", { method: "DELETE" });
      logout();
    } finally {
      setDeleting(false);
    }
  }

  return (
    <div>
      <TopBar title="Profile" subtitle="Manage your personal account settings." />
      <div className="p-6 max-w-2xl">
        <Card>
          <CardHeader>
            <CardTitle>Personal information</CardTitle>
            <CardDescription>
              Update your display name. Your email is managed by your auth provider.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-5">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="display-name">Display name</Label>
              <Input
                id="display-name"
                value={name}
                onChange={(e) => {
                  setName(e.target.value);
                  setSaved(false);
                }}
                placeholder="Your name"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                value={user?.email ?? ""}
                readOnly
                disabled
                className="opacity-60 cursor-not-allowed"
              />
              <p className="text-[11px] text-muted-foreground">
                Email cannot be changed here.
              </p>
            </div>
            <div className="flex items-center gap-3">
              <Button onClick={handleSave}>Save changes</Button>
              {saved && (
                <span className="text-sm text-emerald-500">Saved</span>
              )}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Your data</CardTitle>
            <CardDescription>
              Export or permanently delete all data associated with your account.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <p className="text-sm font-medium">Export data</p>
              <p className="text-[12px] text-muted-foreground">
                Download a JSON file containing all your account data.
              </p>
              <div>
                <Button variant="outline" onClick={handleExport} disabled={exporting}>
                  {exporting ? "Exporting…" : "Export data"}
                </Button>
              </div>
            </div>
            <div className="flex flex-col gap-1.5">
              <p className="text-sm font-medium">Delete account</p>
              <p className="text-[12px] text-muted-foreground">
                Permanently remove your account and all associated data. This action cannot be undone.
              </p>
              <div>
                <Button variant="destructive" onClick={handleDelete} disabled={deleting}>
                  {deleting ? "Deleting…" : "Delete account"}
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
