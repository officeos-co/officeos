"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { toast } from "sonner"
import { Check, ArrowLeft, Leaf, Sparkles, Loader2 } from "lucide-react"
import { cn } from "@/lib/utils"
import { isDevelopment } from "@/lib/env"
import { useBilling, usePlanLimits, usePlanPrices } from "@/features/manage"

function formatCredits(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(0)}M`
  if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`
  return n.toString()
}

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
  const [tab, setTab] = useState<"individual" | "team">("individual")
  const [billing, setBilling] = useState<"monthly" | "yearly">("monthly")
  const { billing: currentBilling, loading: billingLoading, error: billingError } = useBilling()
  const { planLimits, loading: limitsLoading, error: limitsError } = usePlanLimits()
  const { prices, loading: pricesLoading, error: pricesError } = usePlanPrices()

  const loading = billingLoading || limitsLoading || pricesLoading

  useEffect(() => {
    if (isDevelopment()) {
      window.location.href = "https://officeos.co/pricing"
      return
    }
  }, [])

  useEffect(() => {
    if (billingError) toast.error("Failed to load billing", { description: billingError.message })
    if (limitsError) toast.error("Failed to load plan limits", { description: limitsError.message })
    if (pricesError) toast.error("Failed to load plan prices", { description: pricesError.message })
  }, [billingError, limitsError, pricesError])

  const currentPlan = currentBilling?.plan

  const individualPlans = planLimits && prices
    ? [
        {
          key: "free",
          name: "Free",
          icon: Leaf,
          description: "Get started with one agent",
          price: prices.free ?? { monthly: 0, yearly: 0 },
          features: [
            `${planLimits.individualFree.concurrentAgents} concurrent agent`,
            `${formatCredits(planLimits.individualFree.creditsPerMonth)} credits / month included`,
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
          price: prices.pro ?? { monthly: 0, yearly: 0 },
          prefix: "Everything in Free and:",
          features: [
            `${planLimits.individualPro.concurrentAgents} concurrent agents`,
            `${formatCredits(planLimits.individualPro.creditsPerMonth)} credits / month included`,
            "Optional pay-as-you-go overage ($3 / 1M credits)",
            "BYOK for 40% discount on token costs",
            "Smart model routing",
            "Priority email support",
            "Custom skill packages",
          ],
        },
      ]
    : []

  const teamPlans = planLimits && prices
    ? [
        {
          key: "team",
          name: "Team",
          icon: Leaf,
          description: "Scale to 10 agents",
          price: prices.team ?? { monthly: 0, yearly: 0 },
          prefix: "Everything in Pro and:",
          features: [
            `${planLimits.orgTeam.concurrentAgents} concurrent agents`,
            `${formatCredits(planLimits.orgTeam.creditsPerMonth)} credits / month included`,
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
          ctaHref: "mailto:harro@officeos.co",
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
    : []

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

        {/* Tab toggle */}
        <div className="flex justify-center mb-10">
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
        </div>

        {loading ? (
          <div className="flex justify-center py-20">
            <Loader2 className="size-6 animate-spin text-muted-foreground" />
          </div>
        ) : (
          /* Cards */
          <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
            {plans.map((plan) => {
              const Icon = plan.icon
              const isCustom = plan.price === null
              const price = isCustom ? null : billing === "yearly" ? Math.round(plan.price!.yearly / 12) : plan.price!.monthly
              const isCurrent = plan.key === currentPlan

              return (
                <div key={plan.key} className="rounded-2xl border border-border bg-card p-8 flex flex-col gap-6 hover:border-primary/30 transition-colors">
                  <Icon className="size-8 text-primary" />

                  {plan.price !== null && plan.price.monthly !== 0 && (
                    <div className="flex gap-1 rounded-full border border-border bg-muted p-0.5 w-fit text-xs">
                      <button type="button" onClick={() => setBilling("monthly")}
                        className={cn("rounded-full px-3 py-1 transition-colors", billing === "monthly" ? "bg-background text-foreground shadow-sm" : "text-muted-foreground")}>
                        Monthly
                      </button>
                      <button type="button" onClick={() => setBilling("yearly")}
                        className={cn("rounded-full px-3 py-1 flex items-center gap-1 transition-colors", billing === "yearly" ? "bg-background text-foreground shadow-sm" : "text-muted-foreground")}>
                        Yearly <span className="font-medium text-emerald-600">· Save 20%</span>
                      </button>
                    </div>
                  )}

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
                      Contact us
                    </a>
                  ) : isCurrent ? (
                    <div className="w-full rounded-xl border border-border text-muted-foreground py-3 font-medium text-sm text-center cursor-default">
                      Current plan
                    </div>
                  ) : (
                    <div className="w-full rounded-xl border border-dashed border-border text-muted-foreground py-3 font-medium text-sm text-center cursor-default">
                      Coming soon
                    </div>
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
        )}

        <p className="text-center text-xs text-muted-foreground/60 mt-10">
          Prices shown don&apos;t include applicable tax.
        </p>
      </div>
    </div>
  )
}
