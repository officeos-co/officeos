"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { Check, ArrowLeft, Leaf, Sparkles } from "lucide-react"
import { cn } from "@/lib/utils"

type Tab = "individual" | "team"
type Billing = "monthly" | "yearly"

const individualPlans = [
  {
    key: "free",
    name: "Free",
    icon: Leaf,
    description: "Get started with one agent",
    price: { monthly: 0, yearly: 0 },
    cta: "Current plan",
    current: true,
    features: [
      "1 concurrent agent",
      "500K credits / month included",
      "Optional pay-as-you-go overage ($5 / 1M credits)",
      "All providers (OpenAI, Anthropic, Gemini, …)",
      "Full system control via zeroclaw runtime",
      "GraphQL skill gateway",
      "Community support",
    ],
  },
  {
    key: "pro",
    name: "Pro",
    icon: Sparkles,
    description: "Run up to 3 agents",
    price: { monthly: 4900, yearly: 47000 },
    cta: "Upgrade to Pro",
    current: false,
    prefix: "Everything in Free and:",
    features: [
      "3 concurrent agents",
      "10M credits / month included",
      "Optional pay-as-you-go overage ($3 / 1M credits)",
      "BYOK for 40% discount on token costs",
      "Smart model routing",
      "Priority email support",
      "Custom skill packages",
    ],
  },
]

const teamPlans = [
  {
    key: "team",
    name: "Team",
    icon: Leaf,
    description: "Scale to 10 agents",
    price: { monthly: 19900, yearly: 191000 },
    cta: "Upgrade to Team",
    current: false,
    prefix: "Everything in Pro and:",
    features: [
      "10 concurrent agents",
      "25M credits / month included",
      "Pay-as-you-go overage ($2.50 / 1M credits)",
      "SSO (SAML / OIDC)",
      "Priority email support",
    ],
  },
  {
    key: "enterprise",
    name: "Enterprise",
    icon: Sparkles,
    description: "Unlimited agents, custom budget",
    price: null,
    cta: "Contact us",
    ctaHref: "mailto:harro@officeos.co",
    current: false,
    prefix: "Everything in Team and:",
    features: [
      "Custom concurrent agent limit",
      "Custom token budget",
      "Invoice / PO billing",
      "Custom contract & SLA",
      "Dedicated onboarding",
      "Slack / phone support",
    ],
  },
]

function formatPrice(cents: number) {
  return new Intl.NumberFormat("en", {
    style: "currency",
    currency: "EUR",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(cents / 100)
}

export default function PricingPage() {
  const router = useRouter()
  const [tab, setTab] = useState<Tab>("individual")
  const [billing, setBilling] = useState<Billing>("monthly")

  const plans = tab === "individual" ? individualPlans : teamPlans

  return (
    <div className="min-h-screen bg-background">
      {/* Back */}
      <div className="absolute top-6 left-6">
        <button type="button" onClick={() => router.back()} className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors">
          <ArrowLeft className="size-4" />
          Back
        </button>
      </div>

      <div className="mx-auto max-w-4xl px-6 py-24">
        {/* Heading */}
        <div className="text-center mb-10">
          <h1 className="text-3xl font-bold tracking-tight mb-2">Plans that grow with you</h1>
          <p className="text-muted-foreground">Self-hosted Kubernetes deployment. Full system control.</p>
        </div>

        {/* Tab + billing toggle */}
        <div className="flex flex-col items-center gap-4 mb-10">
          <div className="flex gap-1 rounded-full border border-border bg-muted p-1">
            <button type="button" onClick={() => setTab("individual")}
              className={cn("rounded-full px-5 py-2 text-sm font-medium transition-colors", tab === "individual" ? "bg-background text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground")}>
              Individual
            </button>
            <button type="button" onClick={() => setTab("team")}
              className={cn("rounded-full px-5 py-2 text-sm font-medium transition-colors", tab === "team" ? "bg-background text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground")}>
              Team & Enterprise
            </button>
          </div>
          <div className="flex gap-1 rounded-full border border-border bg-muted p-0.5 text-xs">
            <button type="button" onClick={() => setBilling("monthly")}
              className={cn("rounded-full px-3 py-1 transition-colors", billing === "monthly" ? "bg-background text-foreground shadow-sm" : "text-muted-foreground")}>
              Monthly
            </button>
            <button type="button" onClick={() => setBilling("yearly")}
              className={cn("rounded-full px-3 py-1 flex items-center gap-1 transition-colors", billing === "yearly" ? "bg-background text-foreground shadow-sm" : "text-muted-foreground")}>
              Yearly <span className="font-medium text-emerald-600">· Save 20%</span>
            </button>
          </div>
        </div>

        {/* Cards */}
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
          {plans.map((plan) => {
            const Icon = plan.icon
            const isCustom = plan.price === null
            const price = isCustom ? null : billing === "yearly" ? Math.round(plan.price.yearly / 12) : plan.price.monthly

            return (
              <div key={plan.key} className="rounded-2xl border border-border bg-card p-8 flex flex-col gap-6 hover:border-primary/30 transition-colors">
                <Icon className="size-8 text-primary" />

                <div>
                  <h3 className="text-xl font-semibold">{plan.name}</h3>
                  <p className="text-sm text-muted-foreground mt-1">{plan.description}</p>
                </div>

                <div className="flex items-start gap-2">
                  {isCustom ? (
                    <span className="text-4xl font-bold">Custom</span>
                  ) : (
                    <>
                      <span className="text-4xl font-bold">{price === 0 ? "Free" : formatPrice(price!)}</span>
                      {price !== 0 && (
                        <div className="mt-1 text-xs text-muted-foreground leading-tight">
                          <div>/ month</div>
                          <div>{billing === "yearly" ? "billed annually" : "billed monthly"}</div>
                        </div>
                      )}
                    </>
                  )}
                </div>

                {"ctaHref" in plan && plan.ctaHref ? (
                  <a href={plan.ctaHref} className="w-full rounded-xl bg-primary text-primary-foreground py-3 font-medium text-center hover:bg-primary/90 transition-colors text-sm">
                    {plan.cta}
                  </a>
                ) : plan.current ? (
                  <div className="w-full rounded-xl border border-border text-muted-foreground py-3 font-medium text-sm text-center cursor-default">
                    {plan.cta}
                  </div>
                ) : (
                  <button type="button" className="w-full rounded-xl bg-primary text-primary-foreground py-3 font-medium text-sm hover:bg-primary/90 transition-colors">
                    {plan.cta}
                  </button>
                )}

                <div className="border-t border-border pt-4 flex-1">
                  {"prefix" in plan && plan.prefix && (
                    <p className="text-xs text-muted-foreground mb-3">{plan.prefix}</p>
                  )}
                  <ul className="space-y-2">
                    {plan.features.map((f) => (
                      <li key={f} className="flex items-start gap-2 text-sm">
                        <Check className="size-4 text-emerald-500 mt-0.5 shrink-0" />
                        <span className="text-muted-foreground">{f}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            )
          })}
        </div>

        <p className="text-center text-xs text-muted-foreground/60 mt-10">
          Prices shown don&apos;t include applicable tax.
        </p>
      </div>
    </div>
  )
}
