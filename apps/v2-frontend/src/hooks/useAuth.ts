"use client";

import { useCallback, useEffect, useState } from "react";

export type User = {
  id: string;
  email: string;
  name: string | null;
  avatarUrl: string | null;
};

export function useAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  const check = useCallback(async () => {
    try {
      const res = await fetch("/api/auth/me");
      if (res.ok) {
        setUser(await res.json());
      } else {
        setUser(null);
      }
    } catch {
      setUser(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    check();
  }, [check]);

  const logout = useCallback(async () => {
    await fetch("/api/auth/logout", { method: "POST" });
    setUser(null);
    window.location.href = "/login";
  }, []);

  return {
    user,
    loading,
    isAuthenticated: user !== null,
    logout,
    refetch: check,
  };
}
