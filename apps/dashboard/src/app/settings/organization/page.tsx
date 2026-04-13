"use client";

import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
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

export default function OrganizationPage() {
  const [orgName, setOrgName] = useState("My Organization");
  const [saved, setSaved] = useState(false);

  // Placeholder org ID — in production this comes from the auth context / backend
  const orgId = "org_default";

  function handleSave() {
    // TODO: wire up to backend
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  return (
    <div>
      <TopBar title="Organization" subtitle="Manage your organization settings." />
      <div className="p-6 max-w-2xl">
        <Card>
          <CardHeader>
            <CardTitle>Organization details</CardTitle>
            <CardDescription>
              Update your organization name. The organization ID is assigned automatically.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-5">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="org-name">Organization name</Label>
              <Input
                id="org-name"
                value={orgName}
                onChange={(e) => {
                  setOrgName(e.target.value);
                  setSaved(false);
                }}
                placeholder="My Organization"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="org-id">Organization ID</Label>
              <Input
                id="org-id"
                value={orgId}
                readOnly
                disabled
                className="font-mono text-sm opacity-60 cursor-not-allowed"
              />
              <p className="text-[11px] text-muted-foreground">
                This ID is used internally and cannot be changed.
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
      </div>
    </div>
  );
}
