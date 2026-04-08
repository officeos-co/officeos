"use client";

import { useSearchParams } from "next/navigation";

import { SignInButton } from "@/auth/clerk";
import { isLocalAuthMode } from "@/auth/localAuth";
import { resolveSignInRedirectUrl } from "@/auth/redirects";
import { LocalAuthLogin } from "@/components/organisms/LocalAuthLogin";

export default function SignInPage() {
  const searchParams = useSearchParams();

  if (isLocalAuthMode()) {
    return <LocalAuthLogin />;
  }

  const _forceRedirectUrl = resolveSignInRedirectUrl(
    searchParams.get("redirect_url"),
  );

  // Google OAuth sign-in — replaces the old Clerk <SignIn/> component.
  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
      <div className="w-full max-w-md rounded-xl border border-slate-200 bg-white p-8 shadow-sm text-center space-y-4">
        <h1 className="text-xl font-semibold text-slate-900">Sign in</h1>
        <p className="text-sm text-slate-600">
          Continue with your Google account.
        </p>
        <SignInButton>
          <button
            type="button"
            className="w-full rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-medium text-white hover:bg-blue-700"
          >
            Sign in with Google
          </button>
        </SignInButton>
      </div>
    </main>
  );
}
