import Link from "next/link";
import { buildOAuthUrl } from "@/lib/auth-url";
import { getEnvConfig } from "@/lib/env";

export const dynamic = "force-dynamic";

type LoginPageProps = {
  searchParams: Promise<{
    returnTo?: string | string[];
    error?: string | string[];
  }>;
};

export default async function LoginPage({ searchParams }: LoginPageProps) {
  const params = await searchParams;
  const returnTo = safeReturnTo(singleValue(params.returnTo));
  const error = singleValue(params.error);
  const { apiUrl } = getEnvConfig();

  return (
    <main className="flex min-h-screen items-center justify-center px-6">
      <section className="w-full max-w-sm rounded-md border border-border bg-panel p-6 shadow-sm">
        <div className="mb-6 space-y-1 text-center">
          <Link href="/" className="text-lg font-semibold">
            OfficeOS
          </Link>
          <h1 className="text-xl font-semibold">Sign in</h1>
        </div>
        <div className="space-y-3">
          <a className="flex h-10 items-center justify-center gap-2 rounded-md border border-border hover:bg-panel-strong" href={buildOAuthUrl(apiUrl, "google", returnTo)}>
            <GoogleIcon />
            Google
          </a>
          <a className="flex h-10 items-center justify-center gap-2 rounded-md border border-border hover:bg-panel-strong" href={buildOAuthUrl(apiUrl, "github", returnTo)}>
            <GitHubIcon />
            GitHub
          </a>
        </div>
        {error ? <p className="mt-4 text-center text-sm text-danger">{error}</p> : null}
      </section>
    </main>
  );
}

function singleValue(value: string | string[] | undefined): string | null {
  if (Array.isArray(value)) return value[0] ?? null;
  return value ?? null;
}

function safeReturnTo(value: string | null): string {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.includes("://")) return "/";
  return value;
}

function GitHubIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-4" fill="currentColor" aria-hidden="true">
      <path d="M12 2C6.48 2 2 6.58 2 12.25c0 4.53 2.87 8.37 6.84 9.73.5.09.68-.22.68-.49 0-.24-.01-1.04-.01-1.89-2.78.62-3.37-1.22-3.37-1.22-.45-1.18-1.11-1.49-1.11-1.49-.91-.64.07-.63.07-.63 1 .07 1.53 1.06 1.53 1.06.89 1.56 2.34 1.11 2.91.85.09-.66.35-1.11.64-1.37-2.22-.26-4.56-1.14-4.56-5.07 0-1.12.39-2.03 1.03-2.75-.1-.26-.45-1.31.1-2.71 0 0 .84-.28 2.75 1.04A9.38 9.38 0 0 1 12 6.96c.85 0 1.71.12 2.51.35 1.9-1.32 2.74-1.04 2.74-1.04.55 1.4.2 2.45.1 2.71.64.72 1.03 1.63 1.03 2.75 0 3.94-2.34 4.8-4.57 5.06.36.32.68.94.68 1.9 0 1.37-.01 2.48-.01 2.82 0 .27.18.59.69.49A10.16 10.16 0 0 0 22 12.25C22 6.58 17.52 2 12 2Z" />
    </svg>
  );
}

function GoogleIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-4" aria-hidden="true">
      <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z" fill="#4285F4" />
      <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
      <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05" />
      <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
    </svg>
  );
}
