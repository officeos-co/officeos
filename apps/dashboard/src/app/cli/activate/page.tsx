"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import type { DashboardUser } from "@/lib/resources";

export default function CliActivatePage() {
  const params = useSearchParams();
  const router = useRouter();
  const code = useMemo(() => params.get("code") ?? "", [params]);
  const [user, setUser] = useState<DashboardUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [status, setStatus] = useState<"idle" | "submitting" | "complete" | "error">("idle");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch("/api/v1/me", { credentials: "include" })
      .then(async (response) => (response.ok ? ((await response.json()) as DashboardUser) : null))
      .then((me) => {
        if (cancelled) return;
        setUser(me);
        setLoading(false);
      })
      .catch(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!loading && !user) router.replace(`/login?returnTo=${encodeURIComponent(`/cli/activate?code=${code}`)}`);
  }, [code, loading, router, user]);

  async function authorize() {
    setStatus("submitting");
    setError(null);
    const response = await fetch("/api/v1/auth/device/authorize", {
      method: "POST",
      credentials: "include",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ userCode: code }),
    });
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { error?: string } | null;
      setError(body?.error ?? "Authorization failed.");
      setStatus("error");
      return;
    }
    setStatus("complete");
  }

  if (loading || !user) {
    return <main className="flex min-h-screen items-center justify-center text-sm text-muted-foreground">Loading</main>;
  }

  return (
    <main className="flex min-h-screen items-center justify-center px-6">
      <section className="w-full max-w-sm rounded-md border border-border bg-panel p-6 text-center shadow-sm">
        <h1 className="text-xl font-semibold">Authorize CLI</h1>
        <p className="mt-1 text-sm text-muted-foreground">{user.email}</p>
        <div className="my-6 rounded-md border border-border bg-panel-strong p-4">
          <div className="text-xs uppercase text-muted-foreground">Device code</div>
          <div className="mt-2 font-mono text-2xl font-semibold">{code || "Missing"}</div>
        </div>
        {status === "complete" ? (
          <p className="text-sm text-muted-foreground">Complete.</p>
        ) : (
          <button className="h-10 w-full rounded-md bg-primary px-4 text-primary-foreground disabled:opacity-60" disabled={!code || status === "submitting"} onClick={authorize} type="button">
            {status === "submitting" ? "Authorizing" : "Authorize"}
          </button>
        )}
        {error ? <p className="mt-4 text-sm text-danger">{error}</p> : null}
      </section>
    </main>
  );
}
