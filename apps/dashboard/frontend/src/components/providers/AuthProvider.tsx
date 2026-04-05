"use client";

import type { ReactNode } from "react";

import { SignedOut, SignInButton } from "@/auth/clerk";

export function AuthProvider({ children }: { children: ReactNode }) {
  return (
    <>
      {children}
    </>
  );
}
