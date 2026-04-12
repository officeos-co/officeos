"use client";

import { useState } from "react";
import { TopBar } from "@/components/TopBar";
import { useAuth } from "@/hooks/useAuth";
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
  const { user } = useAuth();
  const [name, setName] = useState(user?.name ?? "");
  const [saved, setSaved] = useState(false);

  function handleSave() {
    // TODO: wire up to backend
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
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
      </div>
    </div>
  );
}
