"use client";

import Link from "next/link";
import { siteConfig } from "@/lib/site";
import { ArrowRight } from "lucide-react";
import { useCalModal } from "@/hooks/use-cal-modal";
import { AgentShowcase } from "@/components/agent-showcase";

export function HeroSection() {
  const { openCalModal } = useCalModal();

  return (
    <section id="hero" className="relative w-full">
      <div className="relative flex w-full flex-col items-center px-6">
        <div className="absolute inset-0">
          <div className="absolute inset-0 -z-10 h-[600px] w-full rounded-b-xl [background:radial-gradient(125%_125%_at_50%_10%,var(--background)_40%,var(--secondary)_100%)] md:h-[800px]"></div>
        </div>
        <div className="relative z-10 mx-auto flex h-full w-full max-w-3xl flex-col items-center justify-center gap-10 pt-32">
          <div className="flex flex-col items-center justify-center gap-5">
            <h1 className="text-balance text-center text-3xl font-medium tracking-tighter text-primary md:text-4xl lg:text-5xl xl:text-6xl">
              The AI workforce for your company
            </h1>
            <p className="text-balance text-center text-base font-medium leading-relaxed tracking-tight text-muted-foreground md:text-lg">
              Employees that work 24/7, know everything about your company, and
              never need onboarding.
            </p>
          </div>
          <div className="flex flex-row items-center justify-center gap-2.5">
            <Link
              href={siteConfig.dashboardUrl}
              className="btn-glow flex h-9 items-center justify-center whitespace-nowrap rounded-full bg-secondary px-6 text-sm font-normal tracking-wide text-primary-foreground transition-all ease-out hover:bg-secondary/80 active:scale-95"
            >
              Start Free
            </Link>
            <button
              type="button"
              onClick={openCalModal}
              className="flex h-10 items-center justify-center gap-2 whitespace-nowrap rounded-full border border-border bg-background px-5 text-sm font-normal tracking-wide text-primary transition-all ease-out hover:bg-muted active:scale-95"
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
