"use client";

import Link from "next/link";
import { getSiteConfig } from "@/lib/site";
import { ArrowRight } from "lucide-react";
import { useCalModal } from "@/hooks/use-cal-modal";
import { AgentShowcase } from "@/components/agent-showcase";

export function HeroSection() {
  const siteConfig = getSiteConfig();
  const { openCalModal } = useCalModal();

  return (
    <section id="hero" className="relative w-full">
      <div className="relative flex w-full flex-col items-center px-6">
        <div className="absolute inset-0">
          <div className="absolute inset-0 -z-10 h-[600px] w-full rounded-b-xl [background:radial-gradient(125%_125%_at_50%_10%,var(--background)_40%,var(--secondary)_100%)] md:h-[800px]"></div>
        </div>
        <div className="relative z-10 mx-auto flex h-full w-full max-w-5xl flex-col items-center justify-center gap-11 pt-32 md:pt-36">
          <div className="flex flex-col items-center justify-center gap-6">
            <h1 className="max-w-4xl text-balance text-center text-4xl font-medium leading-[1.04] tracking-tight text-primary md:text-5xl lg:text-6xl">
              Open-source infrastructure to scale AI agents
            </h1>
            <p className="max-w-2xl text-balance text-center text-base font-medium leading-8 text-muted-foreground md:text-lg">
              Deploy, host, and manage agents with tools, memory, credentials,
              logs, and isolated workspaces from one control plane.
            </p>
          </div>
          <div className="flex flex-row items-center justify-center gap-3">
            <Link
              href={siteConfig.dashboardUrl}
              className="btn-glow flex h-11 items-center justify-center whitespace-nowrap rounded-full bg-secondary px-8 text-base font-medium text-primary-foreground transition-all ease-out hover:bg-secondary/80 active:scale-95"
            >
              Start Free
            </Link>
            <button
              type="button"
              onClick={openCalModal}
              className="flex h-11 items-center justify-center gap-2 whitespace-nowrap rounded-full border border-border bg-background px-7 text-base font-medium text-primary transition-all ease-out hover:bg-muted active:scale-95"
            >
              Book a Demo
              <ArrowRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>
      <div className="relative mt-10 px-6">
        <AgentShowcase />
      </div>
    </section>
  );
}
