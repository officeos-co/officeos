"use client";

import { Button } from "@/components/ui/button";
import { siteConfig, navConfig } from "@/lib/config";

export function Navbar() {
  return (
    <nav className="sticky top-0 z-50 w-full border-b border-border/40 bg-background/80 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-6">
        <a href="#" className="font-mono text-lg font-bold tracking-tight">
          {siteConfig.name}
        </a>
        <Button
          size="sm"
          onClick={() =>
            document.getElementById("cta")?.scrollIntoView({ behavior: "smooth" })
          }
        >
          {navConfig.cta}
        </Button>
      </div>
    </nav>
  );
}
