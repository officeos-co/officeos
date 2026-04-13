"use client";

import { useCallback, useEffect, useState } from "react";
import type { User } from "@/types/auth";

export type { User } from "@/types/auth";

export function useAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const check = useCallback(async () => {
    setError(null);
    try {
      const res = await fetch("/api/auth/me");
      if (res.ok) {
        setUser(await res.json());
      } else if (res.status === 401) {
        setUser(null);
      } else {
        const text = await res.text().catch(() => "");
        setUser(null);
        setError(`Authentication check failed (${res.status})${text ? `: ${text}` : ""}`);
      }
    } catch (err) {
      setUser(null);
      setError(err instanceof Error ? err.message : "Network error — could not reach the server");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    check();
  }, [check]);

  const logout = useCallback(async () => {
    try {
      await fetch("/api/auth/logout", { method: "POST" });
    } catch {
      // Logout best-effort
    }
    setUser(null);
    window.location.href = "/login";
  }, []);

  return {
    user,
    loading,
    error,
    isAuthenticated: user !== null,
    logout,
    refetch: check,
  };
}
