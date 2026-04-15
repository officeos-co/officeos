"use client";

import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

export default function OrganizationPage() {
  const [orgName, setOrgName] = useState("My Organization");
  const [saved, setSaved] = useState(false);

  const orgId = "org_default";

  function handleSave() {
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  return (
    <div>
      <TopBar title="Organization" />
      <div className="max-w-lg px-6 py-6 space-y-4">
        <div>
          <h2 className="text-sm font-medium">Organization details</h2>
          <p className="text-[12px] text-muted-foreground">Update your organization name.</p>
        </div>
        <div className="space-y-3">
          <div className="space-y-1.5">
            <Label htmlFor="org-name" className="text-[12px]">Organization name</Label>
            <Input
              id="org-name"
              value={orgName}
              onChange={(e) => { setOrgName(e.target.value); setSaved(false); }}
              placeholder="My Organization"
              className="h-8 text-[13px]"
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="org-id" className="text-[12px]">Organization ID</Label>
            <Input
              id="org-id"
              value={orgId}
              readOnly
              disabled
              className="h-8 text-[13px] font-mono opacity-50"
            />
          </div>
          <div className="flex items-center gap-3">
            <Button size="sm" onClick={handleSave} className="h-7">Save</Button>
            {saved && <span className="text-[12px] text-emerald-600">Saved</span>}
          </div>
        </div>
      </div>
    </div>
  );
}
