import Link from "next/link";
import { getSiteConfig } from "@/lib/site";
import {
  MessageCircle,
  Brain,
  ArrowRightLeft,
  Clock,
  Shield,
  Zap,
} from "lucide-react";
import { Navbar } from "@/components/sections/navbar";

export const metadata = {
  title: "Customer Success Agent — OfficeOS",
  description:
    "An AI agent that handles inbound customer requests across Email, WhatsApp, and Chat — not with templates, but with real understanding of each customer's context.",
};

export default function CustomerSuccess() {
  const siteConfig = getSiteConfig();
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <p className="text-sm font-medium text-secondary text-center uppercase tracking-wider">
          Solution
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-center md:text-5xl">
          Customer Success Agent
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          A dedicated support agent per customer. Not a chatbot with templates —
          an agent that understands the actual problem, pulls the right context,
          and knows when to hand off to a human.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          <section>
            <div className="flex items-center gap-3 mb-4">
              <MessageCircle className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Multi-Channel, One Agent
              </h2>
            </div>
            <p>
              The agent handles inbound customer requests across{" "}
              <strong className="text-primary">
                Email, WhatsApp, and Chat
              </strong>
              . It doesn&apos;t matter how a customer reaches out — the agent
              maintains full context across all channels. A customer who emails
              on Monday and follows up on WhatsApp on Wednesday gets a coherent
              experience, not a cold restart.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Brain className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Real Understanding, Not Templates
              </h2>
            </div>
            <p>
              This agent doesn&apos;t pattern-match against a FAQ database. It{" "}
              <strong className="text-primary">
                understands the actual problem
              </strong>{" "}
              and pulls relevant context from the knowledge graph: past tickets
              from this customer, their specific contract terms, product
              documentation that applies to their setup, even internal
              engineering notes about known issues.
            </p>
            <p className="mt-4">
              Every response is individually crafted. A customer with a premium
              SLA asking about downtime gets a different response than a
              free-tier user asking the same question — because the agent knows
              the context of each relationship.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <ArrowRightLeft className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Intelligent Escalation
              </h2>
            </div>
            <p>
              The agent recognizes when a human needs to take over — emotionally
              charged situations, complex edge cases, requests that require
              business decisions. When it escalates, it doesn&apos;t just
              forward the message. It hands off with{" "}
              <strong className="text-primary">full context</strong>: a summary
              of the issue, what it already tried, relevant customer history,
              and a suggested resolution. Your support team picks up
              mid-conversation, not from scratch.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Clock className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                24/7 Like a Dedicated Support Rep
              </h2>
            </div>
            <p>
              The agent works around the clock. A customer in Tokyo asking a
              question at 3 AM your time gets the same quality of response as
              one reaching out during business hours. Not a &quot;we&apos;ll get
              back to you&quot; auto-reply — an actual resolution, with full
              access to knowledge and tools.
            </p>
            <p className="mt-4">
              Think of it as a dedicated support person per customer. One that
              remembers every interaction, has read every document, and never
              has a bad day.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Shield className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Knowledge Graph Context
              </h2>
            </div>
            <p>
              The agent draws from your entire organizational knowledge when
              formulating responses. Product docs, engineering runbooks, past
              incident reports, contract specifics, onboarding guides —
              everything in the knowledge graph is available as context. As your
              team adds knowledge, every customer interaction gets better.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Zap className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                How It Works
              </h2>
            </div>
            <div className="space-y-4">
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  1
                </span>
                <p>Customer sends a message via Email, WhatsApp, or Chat.</p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  2
                </span>
                <p>
                  The agent identifies the customer, pulls their history,
                  contract terms, and relevant product context from the
                  knowledge graph.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  3
                </span>
                <p>
                  It formulates an individual response — not a template, but an
                  answer that addresses their specific situation.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  4
                </span>
                <p>
                  If the issue needs human judgment, the agent escalates with
                  full context so your team can resolve it immediately.
                </p>
              </div>
            </div>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Give every customer a dedicated support agent
          </h2>
          <div className="flex flex-row items-center justify-center gap-3">
            <Link
              href={siteConfig.dashboardUrl}
              className="flex h-9 items-center justify-center whitespace-nowrap rounded-full bg-secondary px-6 text-sm font-normal text-primary-foreground tracking-wide shadow-sm transition-all hover:bg-secondary/80 active:scale-95"
            >
              Start Free
            </Link>
            <Link
              href="/"
              className="flex h-9 items-center justify-center whitespace-nowrap rounded-full border border-border px-6 text-sm font-normal tracking-wide transition-all hover:bg-muted active:scale-95"
            >
              Back to Home
            </Link>
          </div>
        </div>
      </main>

      <footer className="border-t border-border py-8 text-center text-sm text-muted-foreground">
        Made in Hamburg — &copy; 2026 OfficeOS
      </footer>
    </div>
  );
}
