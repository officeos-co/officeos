"use client";

import { useState } from "react";
import Image from "next/image";
import { PageHeader } from "@/components/page-header";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { SettingsIcon, Trash2Icon } from "lucide-react";
import { toast } from "sonner";
import { useProviders, type Provider } from "@/features/manage";

/** Logo map — provider name (lowercase) → public asset path */
const LOGOS: Record<string, string> = {
  openai: "/openai.svg",
  anthropic: "/anthropic.svg",
  google: "/google.svg",
  xai: "/xai.svg",
};

function ProviderLogo({ name, size = 24 }: { name: string; size?: number }) {
  const src = LOGOS[name.toLowerCase()];
  if (!src) return null;
  return (
    <Image
      src={src}
      alt={name}
      width={size}
      height={size}
      className="shrink-0 invert dark:invert-0"
    />
  );
}

export default function ProvidersPage() {
  const { providers, loading, setProviderKey, clearProviderKey } =
    useProviders();

  const [activeProvider, setActiveProvider] = useState<Provider | null>(null);
  const [isEditing, setIsEditing] = useState(false);

  const [apiKey, setApiKey] = useState("");
  const [saving, setSaving] = useState(false);

  function openSetup(provider: Provider) {
    setActiveProvider(provider);
    setIsEditing(false);
    setApiKey("");
  }

  function openEdit(provider: Provider) {
    setActiveProvider(provider);
    setIsEditing(true);
    setApiKey("");
  }

  function closeDialog() {
    setActiveProvider(null);
  }

  async function handleConnect() {
    if (!apiKey.trim()) {
      toast.error("API key is required");
      return;
    }
    if (!activeProvider) return;

    setSaving(true);
    try {
      await setProviderKey(activeProvider.name, apiKey.trim());
      toast.success(
        isEditing
          ? `${activeProvider.displayName} updated`
          : `${activeProvider.displayName} connected`,
      );
      closeDialog();
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "Failed to save";
      toast.error(msg);
    } finally {
      setSaving(false);
    }
  }

  async function handleRemove(name: string) {
    try {
      await clearProviderKey(name);
      toast.success("Provider key removed");
    } catch {
      toast.error("Failed to remove provider key");
    }
  }

  const configured = providers.filter((p) => p.configured);
  const unconfigured = providers.filter((p) => !p.configured);

  if (loading) {
    return (
      <>
        <PageHeader group="Manage" page="Providers" />
        <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-3xl mx-auto w-full">
          <Skeleton className="h-4 w-40 mb-3" />
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-16 w-full rounded-lg" />
          ))}
        </div>
      </>
    );
  }

  return (
    <>
      <PageHeader group="Manage" page="Providers" />
      <div className="flex flex-1 flex-col gap-6 p-4 pt-0 max-w-3xl mx-auto w-full">
        {/* Configured providers */}
        {configured.length > 0 && (
          <section>
            <h3 className="text-sm font-semibold mb-3">Configured Providers</h3>
            <div className="space-y-2">
              {configured.map((p) => (
                <div
                  key={p.id}
                  className="flex items-center gap-3 rounded-lg border bg-card p-4"
                >
                  <ProviderLogo name={p.name} />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-sm">
                        {p.displayName}
                      </span>
                      <span className="rounded bg-primary/10 px-1.5 py-0.5 text-xs font-medium text-primary">
                        Connected
                      </span>
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {p.models.length} model
                      {p.models.length !== 1 ? "s" : ""} available
                    </span>
                  </div>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    onClick={() => openEdit(p)}
                  >
                    <SettingsIcon className="size-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    onClick={() => handleRemove(p.name)}
                  >
                    <Trash2Icon className="size-4" />
                  </Button>
                </div>
              ))}
            </div>
          </section>
        )}

        {configured.length > 0 && unconfigured.length > 0 && <Separator />}

        {/* Available providers */}
        {unconfigured.length > 0 && (
          <section>
            <h3 className="text-sm font-semibold mb-1">Add Provider</h3>
            <p className="text-xs text-muted-foreground mb-3">
              Connect your own API keys for self-hosted deployments.
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
              {unconfigured.map((p) => (
                <button
                  key={p.id}
                  onClick={() => openSetup(p)}
                  className="flex items-center gap-3 rounded-lg border bg-card p-4 text-left transition-colors hover:bg-accent cursor-pointer"
                >
                  <ProviderLogo name={p.name} />
                  <div className="flex-1 min-w-0">
                    <span className="font-medium text-sm">{p.displayName}</span>
                    <br />
                    <span className="text-xs text-muted-foreground">
                      {p.models.length} model
                      {p.models.length !== 1 ? "s" : ""}
                    </span>
                  </div>
                  <span className="text-xs text-muted-foreground">Connect</span>
                </button>
              ))}
            </div>
          </section>
        )}
      </div>

      {/* Setup / Edit Dialog */}
      <Dialog
        open={activeProvider !== null}
        onOpenChange={(o) => !o && closeDialog()}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            {activeProvider && (
              <div className="flex items-center gap-2 mb-1">
                <ProviderLogo name={activeProvider.name} size={20} />
                <DialogTitle>Set up {activeProvider.displayName}</DialogTitle>
              </div>
            )}
            <DialogDescription>
              {activeProvider &&
                `Connect to ${activeProvider.displayName} and access its models.`}
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4">
            {/* API Key */}
            <div className="space-y-1.5">
              <Label className="text-sm font-semibold">API Key</Label>
              <Input
                type="password"
                placeholder="sk-..."
                value={apiKey}
                onChange={(e) => setApiKey(e.target.value)}
              />
              <p className="text-xs text-muted-foreground">
                Paste your API key from {activeProvider?.displayName} to access
                your models.
              </p>
            </div>

            {/* Models (read-only) */}
            {activeProvider && activeProvider.models.length > 0 && (
              <>
                <Separator />
                <div className="space-y-1.5">
                  <Label className="text-sm font-semibold">Models</Label>
                  <p className="text-xs text-muted-foreground">
                    Models that will be available once connected.
                  </p>
                  <div className="space-y-1 rounded-lg border bg-card p-1">
                    {activeProvider.models.map((model) => (
                      <div
                        key={model}
                        className="rounded-md px-3 py-2 text-sm text-muted-foreground"
                      >
                        {model}
                      </div>
                    ))}
                  </div>
                </div>
              </>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={closeDialog}>
              Cancel
            </Button>
            <Button onClick={handleConnect} disabled={saving}>
              {saving ? "Saving..." : isEditing ? "Save" : "Connect"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
