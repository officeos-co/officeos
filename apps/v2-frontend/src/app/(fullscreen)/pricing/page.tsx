"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Check, ArrowLeft, Leaf, Sparkles } from "lucide-react";
import { cn } from "@/lib/utils";

type Tab = "individual" | "team";
type Billing = "monthly" | "yearly";

const individualPlans = [
  {
    key: "free",
    name: "Free",
    icon: Leaf,
    description: "Get started with one agent",
    monthlyPrice: 0,
    yearlyPrice: 0,
    priceNote: "forever",
    hasBillingToggle: false,
    cta: "Get started",
    ctaHref: "/agents",
    disabled: false,
    features: {
      base: [
        "1 concurrent agent",
        "2M tokens / month included",
        "All providers (OpenAI, Anthropic, Gemini, …)",
        "Full system control via zeroclaw runtime",
        "GraphQL skill gateway",
        "Community support",
      ],
    },
  },
  {
    key: "pro",
    name: "Pro",
    icon: Sparkles,
    description: "Run up to 3 agents",
    monthlyPrice: 29,
    yearlyPrice: 24,
    hasBillingToggle: true,
    cta: "Subscribe",
    disabled: true,
    features: {
      prefix: "Everything in Free and:",
      base: [
        "3 concurrent agents",
        "10M tokens / month included",
        "Smart model routing",
        "Priority email support",
        "Custom skill packages",
      ],
    },
  },
] as const;

const teamPlans = [
  {
    key: "team",
    name: "Team",
    icon: Leaf,
    description: "Scale to 10 agents",
    monthlyPrice: 249,
    yearlyPrice: 207,
    hasBillingToggle: true,
    cta: "Subscribe",
    disabled: true,
    features: {
      prefix: "Everything in Pro and:",
      base: [
        "10 concurrent agents",
        "25M tokens / month included",
        "Pay-as-you-go overage (no hard cutoff mid-task)",
        "SSO (SAML / OIDC)",
        "Priority email support",
      ],
    },
  },
  {
    key: "enterprise",
    name: "Enterprise",
    icon: Sparkles,
    description: "Unlimited agents, custom budget",
    monthlyPrice: null,
    yearlyPrice: null,
    hasBillingToggle: false,
    cta: "Contact us",
    ctaHref: "mailto:harro@harrokrog.com",
    disabled: false,
    features: {
      prefix: "Everything in Team and:",
      base: [
        "Custom concurrent agent limit",
        "Custom token budget",
        "Invoice / PO billing",
        "Custom contract & SLA",
        "Dedicated onboarding",
        "Slack / phone support",
      ],
    },
  },
] as const;

function BillingToggle({
  billing,
  onChange,
}: {
  billing: Billing;
  onChange: (b: Billing) => void;
}) {
  return (
    <div className="flex gap-1 rounded-full border border-border bg-muted p-0.5 w-fit text-xs">
      <button
        type="button"
        onClick={() => onChange("monthly")}
        className={cn(
          "rounded-full px-3 py-1 transition-colors",
          billing === "monthly"
            ? "bg-background text-foreground shadow-sm"
            : "text-muted-foreground hover:text-foreground",
        )}
      >
        Monthly
      </button>
      <button
        type="button"
        onClick={() => onChange("yearly")}
        className={cn(
          "rounded-full px-3 py-1 flex items-center gap-1 transition-colors",
          billing === "yearly"
            ? "bg-background text-foreground shadow-sm"
            : "text-muted-foreground hover:text-foreground",
        )}
      >
        Yearly{" "}
        <span className="text-primary font-medium">· Save 17%</span>
      </button>
    </div>
  );
}

type AnyPlan = (typeof individualPlans)[number] | (typeof teamPlans)[number];

function PlanCard({
  plan,
  billing,
  onBillingChange,
}: {
  plan: AnyPlan;
  billing: Billing;
  onBillingChange: (b: Billing) => void;
}) {
  const Icon = plan.icon;
  const isCustom = plan.monthlyPrice === null;
  const price = isCustom
    ? null
    : billing === "yearly" && plan.hasBillingToggle
      ? plan.yearlyPrice
      : plan.monthlyPrice;

  return (
    <div className="rounded-2xl border border-border bg-card p-8 flex flex-col gap-6 hover:border-primary/30 transition-colors shadow-sm">
      {/* Icon */}
      <Icon className="h-8 w-8 text-primary" />

      {/* Billing toggle */}
      {plan.hasBillingToggle && (
        <BillingToggle billing={billing} onChange={onBillingChange} />
      )}

      {/* Title + description */}
      <div>
        <h3 className="text-xl font-semibold text-foreground">{plan.name}</h3>
        <p className="text-sm text-muted-foreground mt-1">{plan.description}</p>
      </div>

      {/* Price */}
      <div className="flex items-start gap-2">
        {isCustom ? (
          <span className="text-4xl font-bold text-foreground">Custom</span>
        ) : (
          <>
            <span className="text-4xl font-bold text-foreground">{price === 0 ? "Free" : price}</span>
            {price !== 0 && (
              <div className="mt-1 text-xs text-muted-foreground leading-tight">
                <div>EUR / month</div>
                <div>
                  {billing === "yearly" && plan.hasBillingToggle
                    ? "billed annually"
                    : "billed monthly"}
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* CTA */}
      {"ctaHref" in plan && plan.ctaHref ? (
        <a
          href={plan.ctaHref}
          className="w-full rounded-xl bg-primary text-primary-foreground py-3 font-medium text-center hover:bg-primary/90 transition-colors text-sm"
        >
          {plan.cta}
        </a>
      ) : plan.disabled ? (
        <div className="relative group">
          <button
            type="button"
            disabled
            className="w-full rounded-xl border border-border text-muted-foreground py-3 font-medium text-sm cursor-not-allowed bg-muted/50"
          >
            {plan.cta}
          </button>
          <div className="absolute -top-8 left-1/2 -translate-x-1/2 whitespace-nowrap rounded bg-foreground/80 px-2 py-1 text-xs text-background opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
            Coming soon
          </div>
        </div>
      ) : null}

      {/* Features */}
      <div className="border-t border-border pt-4 flex-1">
        {"prefix" in plan.features && plan.features.prefix && (
          <p className="text-xs text-muted-foreground mb-3">{plan.features.prefix}</p>
        )}
        <ul className="space-y-2">
          {plan.features.base.map((f) => (
            <li key={f} className="flex items-start gap-2 text-sm">
              <Check className="h-4 w-4 text-emerald-500 mt-0.5 shrink-0" />
              <span className="text-muted-foreground">{f}</span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

export default function PricingPage() {
  const router = useRouter();
  const [tab, setTab] = useState<Tab>("individual");
  const [billing, setBilling] = useState<Billing>("monthly");

  const plans = tab === "individual" ? individualPlans : teamPlans;

  return (
    <div className="min-h-screen bg-background text-foreground">
      {/* Back button */}
      <div className="absolute top-6 left-6">
        <button
          type="button"
          onClick={() => router.push("/agents")}
          className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          Back
        </button>
      </div>

      <div className="mx-auto max-w-4xl px-6 py-24">
        {/* Heading */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold tracking-tight mb-3 text-foreground">
            Plans that grow with you
          </h1>
          <p className="text-muted-foreground text-base">
            Self-hosted Kubernetes deployment. Full system control. Scale as your agent fleet grows.
          </p>
        </div>

        {/* Tab toggle */}
        <div className="flex justify-center mb-10">
          <div className="flex gap-1 rounded-full border border-border bg-muted p-1">
            <button
              type="button"
              onClick={() => setTab("individual")}
              className={cn(
                "rounded-full px-5 py-2 text-sm font-medium transition-colors",
                tab === "individual"
                  ? "bg-background text-foreground shadow-sm"
                  : "text-muted-foreground hover:text-foreground",
              )}
            >
              Individual
            </button>
            <button
              type="button"
              onClick={() => setTab("team")}
              className={cn(
                "rounded-full px-5 py-2 text-sm font-medium transition-colors",
                tab === "team"
                  ? "bg-background text-foreground shadow-sm"
                  : "text-muted-foreground hover:text-foreground",
              )}
            >
              Team and Enterprise
            </button>
          </div>
        </div>

        {/* Plan cards */}
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {plans.map((plan) => (
            <PlanCard
              key={plan.key}
              plan={plan}
              billing={billing}
              onBillingChange={setBilling}
            />
          ))}
        </div>

        {/* Footer note */}
        <p className="text-center text-xs text-muted-foreground/60 mt-10">
          Prices shown don&apos;t include applicable tax.
        </p>
      </div>
    </div>
  );
}
