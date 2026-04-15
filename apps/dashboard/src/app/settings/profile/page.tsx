"use client";

import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
import { useAuth } from "@/hooks/useAuth";
import { apiFetch } from "@/hooks/useApi";
import { isMockEnabled } from "@/lib/mock-data";
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
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  async function handleExport() {
    setExporting(true);
    try {
      if (isMockEnabled()) {
        const blob = new Blob([JSON.stringify({ mock: true })], { type: "application/json" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = "eaos-export.json";
        a.click();
        URL.revokeObjectURL(url);
        return;
      }
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
      <TopBar title="Profile" />
      <div className="max-w-lg px-6 py-6 space-y-8">
        <section className="space-y-4">
          <div>
            <h2 className="text-sm font-medium">Personal information</h2>
            <p className="text-[12px] text-muted-foreground">Update your display name.</p>
          </div>
          <div className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="display-name" className="text-[12px]">Display name</Label>
              <Input
                id="display-name"
                value={name}
                onChange={(e) => { setName(e.target.value); setSaved(false); }}
                placeholder="Your name"
                className="h-8 text-[13px]"
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="email" className="text-[12px]">Email</Label>
              <Input
                id="email"
                value={user?.email ?? ""}
                readOnly
                disabled
                className="h-8 text-[13px] opacity-50"
              />
            </div>
            <div className="flex items-center gap-3">
              <Button size="sm" onClick={handleSave} className="h-7">Save</Button>
              {saved && <span className="text-[12px] text-emerald-600">Saved</span>}
            </div>
          </div>
        </section>

        <div className="border-t border-border" />

        <section className="space-y-4">
          <div>
            <h2 className="text-sm font-medium">Data</h2>
            <p className="text-[12px] text-muted-foreground">Export or delete your account data.</p>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={handleExport} disabled={exporting} className="h-7 text-[12px]">
              {exporting ? "Exporting…" : "Export data"}
            </Button>
            <Button variant="ghost" size="sm" onClick={handleDelete} disabled={deleting} className="h-7 text-[12px] text-destructive hover:text-destructive">
              {deleting ? "Deleting…" : "Delete account"}
            </Button>
          </div>
        </section>
      </div>
    </div>
  );
}
