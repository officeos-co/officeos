"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";

import { setSessionToken } from "@/auth/clerk";
import { getApiBaseUrl } from "@/lib/api-base";

export default function AuthCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const code = searchParams.get("code");
    if (!code) {
      setError("No authorization code received from Google.");
      return;
    }

    const exchangeCode = async () => {
      try {
        const baseUrl = getApiBaseUrl();
        const resp = await fetch(`${baseUrl}/api/auth/google/callback`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ code }),
        });

        if (!resp.ok) {
          const data = await resp.json().catch(() => ({}));
          setError(data.detail || "Authentication failed.");
          return;
        }

        const data = await resp.json();
        setSessionToken(data.token);
        router.push("/boards");
      } catch (err) {
        setError(err instanceof Error ? err.message : "Authentication failed.");
      }
    };

    exchangeCode();
  }, [searchParams, router]);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="rounded-xl border border-red-200 bg-red-50 p-6 text-center">
          <h2 className="text-lg font-semibold text-red-800">
            Authentication failed
          </h2>
          <p className="mt-2 text-sm text-red-600">{error}</p>
          <button
            onClick={() => router.push("/")}
            className="mt-4 rounded-lg bg-red-600 px-4 py-2 text-sm text-white hover:bg-red-700"
          >
            Back to home
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center">
      <p className="text-sm text-slate-500">Signing in...</p>
    </div>
  );
}
