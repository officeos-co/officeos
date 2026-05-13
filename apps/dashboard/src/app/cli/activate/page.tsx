"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuthContext } from "@/contexts/AuthContext";
import { Button } from "@/ui/button";

export default function CliActivatePage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const { authenticated, loading, user } = useAuthContext();
  const [state, setState] = useState<"idle" | "submitting" | "complete" | "error">("idle");
  const [error, setError] = useState<string | null>(null);
  const code = useMemo(() => searchParams.get("code") ?? "", [searchParams]);

  useEffect(() => {
    if (!loading && !authenticated) {
      router.replace(`/login?returnTo=${encodeURIComponent(`/cli/activate?code=${code}`)}`);
    }
  }, [authenticated, code, loading, router]);

  async function authorize() {
    setState("submitting");
    setError(null);
    const response = await fetch("/api/cli/device/authorize", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ userCode: code }),
    });
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { error?: string } | null;
      setError(body?.error ?? "Could not authorize this device code.");
      setState("error");
      return;
    }
    setState("complete");
  }

  if (loading || !authenticated) {
    return (
      <main className="flex min-h-svh items-center justify-center">
        <p className="text-sm text-muted-foreground">Loading...</p>
      </main>
    );
  }

  return (
    <main className="flex min-h-svh items-center justify-center p-6">
      <section className="w-full max-w-sm space-y-6">
        <div className="space-y-2 text-center">
          <h1 className="text-2xl font-semibold tracking-tight">Authorize EAOS CLI</h1>
          <p className="text-sm text-muted-foreground">{user?.email}</p>
        </div>
        <div className="rounded-md border p-4 text-center">
          <div className="text-xs uppercase text-muted-foreground">Device code</div>
          <div className="mt-2 font-mono text-2xl font-semibold">{code || "Missing"}</div>
        </div>
        {state === "complete" ? (
          <p className="text-center text-sm text-muted-foreground">CLI authorization complete. You can return to your terminal.</p>
        ) : (
          <Button className="w-full" disabled={!code || state === "submitting"} onClick={authorize}>
            {state === "submitting" ? "Authorizing..." : "Authorize"}
          </Button>
        )}
        {error ? <p className="text-center text-sm text-destructive">{error}</p> : null}
      </section>
    </main>
  );
}
