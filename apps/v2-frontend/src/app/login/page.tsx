"use client";

import { Suspense, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/button";
import { AlertCircle } from "lucide-react";

function LoginContent() {
  const { isAuthenticated, loading } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const error = searchParams.get("error");

  useEffect(() => {
    if (!loading && isAuthenticated) {
      router.push("/");
    }
  }, [loading, isAuthenticated, router]);

  return (
    <div className="flex h-screen w-screen flex-col items-center justify-center">
      <div className="flex w-full max-w-sm flex-col items-center gap-8">
        <div className="flex items-center gap-3">
          <div className="grid h-10 w-10 place-items-center rounded-xl bg-primary text-lg font-bold text-primary-foreground">
            E
          </div>
          <h1 className="text-2xl font-semibold tracking-tight">
            EnterpriseAgentOS
          </h1>
        </div>

        <p className="text-sm text-muted-foreground">
          Sign in to access Mission Control
        </p>

        {error && (
          <div className="flex w-full items-start gap-3 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3">
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
            <div className="flex-1">
              <p className="text-sm font-medium text-destructive">Sign-in failed</p>
              <p className="mt-1 text-xs text-muted-foreground">{error}</p>
            </div>
          </div>
        )}

        <Button
          variant="outline"
          size="lg"
          onClick={() => {
            // Go directly to the backend — cookies must be set by the backend origin
            const backendUrl = window.location.hostname === "localhost"
              ? ""
              : "https://api.harrokrog.com";
            window.location.href = `${backendUrl}/api/auth/google`;
          }}
          className="gap-3"
        >
          <svg className="h-5 w-5" viewBox="0 0 24 24">
            <path
              fill="#4285F4"
              d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z"
            />
            <path
              fill="#34A853"
              d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
            />
            <path
              fill="#FBBC05"
              d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
            />
            <path
              fill="#EA4335"
              d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
            />
          </svg>
          Sign in with Google
        </Button>
      </div>
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense>
      <LoginContent />
    </Suspense>
  );
}
