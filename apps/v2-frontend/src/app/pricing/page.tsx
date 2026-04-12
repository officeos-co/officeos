import Link from "next/link";
import { TopBar } from "@/components/TopBar";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Check } from "lucide-react";

interface PlanFeature {
  text: string;
}

interface PricingPlan {
  name: string;
  price: string;
  priceNote?: string;
  description: string;
  badge?: string;
  features: PlanFeature[];
  cta: string;
  ctaVariant: "default" | "outline" | "secondary";
  disabled?: boolean;
}

const plans: PricingPlan[] = [
  {
    name: "Free",
    price: "$0",
    priceNote: "forever",
    description: "Get started with one agent. No credit card required.",
    features: [
      { text: "1 concurrent agent" },
      { text: "2M tokens / month included" },
      { text: "All providers (OpenAI, Anthropic, Gemini, …)" },
      { text: "Full system control via zeroclaw runtime" },
      { text: "GraphQL skill gateway" },
      { text: "Community support" },
    ],
    cta: "Get started",
    ctaVariant: "outline",
  },
  {
    name: "Team",
    price: "$249",
    priceNote: "/ month",
    description: "Scale to 10 agents with generous token budget and overage billing.",
    badge: "Most popular",
    features: [
      { text: "10 concurrent agents" },
      { text: "25M tokens / month included" },
      { text: "Pay-as-you-go overage (no hard cutoff mid-task)" },
      { text: "Overage rate: model cost × 1.3" },
      { text: "Smart model routing (Haiku → Sonnet → Opus)" },
      { text: "Priority email support" },
    ],
    cta: "Subscribe",
    ctaVariant: "default",
    disabled: true, // Stripe not yet configured
  },
  {
    name: "Enterprise",
    price: "Custom",
    description: "Unlimited agents, custom token budgets, and dedicated support.",
    features: [
      { text: "Custom concurrent agent limit" },
      { text: "Custom token budget" },
      { text: "Invoice / PO billing" },
      { text: "Custom contract & SLA" },
      { text: "Dedicated onboarding" },
      { text: "Slack / phone support" },
    ],
    cta: "Contact us",
    ctaVariant: "secondary",
  },
];

export default function PricingPage() {
  return (
    <div>
      <TopBar
        title="Pricing"
        subtitle="Simple, transparent pricing. Scale as your agent fleet grows."
      />

      <div className="p-6">
        {/* Header */}
        <div className="mb-10 text-center">
          <h2 className="text-2xl font-semibold tracking-tight">
            Choose your plan
          </h2>
          <p className="mt-2 text-sm text-muted-foreground">
            All plans include self-hosted Kubernetes deployment and full system
            control. Upgrade or downgrade at any time.
          </p>
        </div>

        {/* Plan cards */}
        <div className="mx-auto grid max-w-5xl grid-cols-1 gap-6 md:grid-cols-3">
          {plans.map((plan) => (
            <Card
              key={plan.name}
              className={
                plan.badge
                  ? "relative border-primary ring-1 ring-primary"
                  : "relative"
              }
            >
              {plan.badge && (
                <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                  <Badge className="bg-primary text-primary-foreground">
                    {plan.badge}
                  </Badge>
                </div>
              )}

              <CardHeader className="pb-4">
                <CardTitle className="text-lg">{plan.name}</CardTitle>
                <div className="flex items-baseline gap-1">
                  <span className="text-3xl font-bold">{plan.price}</span>
                  {plan.priceNote && (
                    <span className="text-sm text-muted-foreground">
                      {plan.priceNote}
                    </span>
                  )}
                </div>
                <CardDescription>{plan.description}</CardDescription>
              </CardHeader>

              <CardContent className="flex flex-col gap-6">
                {/* Feature list */}
                <ul className="flex flex-col gap-2.5">
                  {plan.features.map((feature) => (
                    <li key={feature.text} className="flex items-start gap-2.5">
                      <Check className="mt-0.5 h-4 w-4 shrink-0 text-emerald-500" />
                      <span className="text-[13px] text-muted-foreground">
                        {feature.text}
                      </span>
                    </li>
                  ))}
                </ul>

                {/* CTA */}
                {plan.name === "Enterprise" ? (
                  <a
                    href="mailto:harro@harrokrog.com"
                    className={cn(
                      buttonVariants({ variant: plan.ctaVariant }),
                      "w-full justify-center",
                    )}
                  >
                    {plan.cta}
                  </a>
                ) : plan.disabled ? (
                  <span
                    className={cn(
                      buttonVariants({ variant: plan.ctaVariant }),
                      "w-full justify-center cursor-not-allowed opacity-50",
                    )}
                    title="Stripe billing coming soon"
                    aria-disabled="true"
                  >
                    {plan.cta}
                  </span>
                ) : (
                  <Link
                    href="/agents?new=1"
                    className={cn(
                      buttonVariants({ variant: plan.ctaVariant }),
                      "w-full justify-center",
                    )}
                  >
                    {plan.cta}
                  </Link>
                )}

                {plan.disabled && (
                  <p className="text-center text-[11px] text-muted-foreground/60">
                    Stripe integration coming soon
                  </p>
                )}
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Overage note */}
        <div className="mx-auto mt-10 max-w-2xl rounded-lg border border-border bg-muted/30 p-5">
          <h3 className="mb-2 text-sm font-semibold">How overage billing works</h3>
          <p className="text-[13px] text-muted-foreground">
            We follow the Cursor model — agents are never cut off mid-task. Once
            your included token budget is exhausted, usage continues and is billed
            at{" "}
            <span className="font-medium text-foreground">model cost × 1.3</span>{" "}
            via Stripe metered billing. Smart model routing automatically uses
            Haiku for simple tasks (~$0.50/M blended) and escalates to Sonnet
            (~$9/M) or Opus (~$45/M) only when needed.
          </p>
        </div>
      </div>
    </div>
  );
}
