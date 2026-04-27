import Link from "next/link";
import { siteConfig } from "@/lib/site";
import {
  Search,
  Database,
  BarChart3,
  MessageSquare,
  Calendar,
  Brain,
} from "lucide-react";
import { Navbar } from "@/components/sections/navbar";

export const metadata = {
  title: "Sales Lead Researcher — OfficeOS",
  description:
    "An AI agent that independently researches every lead before every sales call — LinkedIn, company news, funding rounds, CRM history, and full organizational context.",
};

export default function SalesIntelligence() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <p className="text-sm font-medium text-secondary text-center uppercase tracking-wider">
          Solution
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-center md:text-5xl">
          Sales Lead Researcher
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          A research analyst that works exclusively for your sales team. Before
          every call, every meeting, every outreach — it builds a briefing no
          junior rep could match.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Search className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Deep Lead Research, Every Time
              </h2>
            </div>
            <p>
              Before every sales call, the agent independently researches the
              lead: LinkedIn profile, company news, funding rounds, past
              interactions from your CRM, and industry context. It doesn&apos;t
              wait for someone to ask — it runs every day for every upcoming
              meeting on your team&apos;s calendar.
            </p>
            <p className="mt-4">
              The result is a briefing that a junior sales rep could never
              produce on their own, because the agent pulls from the{" "}
              <strong className="text-primary">
                entire organizational knowledge graph
              </strong>
              . Past deal notes, competitor positioning, internal product
              updates, customer success tickets — everything relevant to this
              specific lead, assembled in minutes.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Database className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Full CRM Context
              </h2>
            </div>
            <p>
              The agent connects to your CRM and pulls the complete history —
              past conversations, deal stages, contract values, objections
              raised, features requested. When your rep walks into a call, they
              know exactly where the relationship stands and what matters to
              this specific contact.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <BarChart3 className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Competitive Positioning Briefs
              </h2>
            </div>
            <p>
              The agent doesn&apos;t just research the lead — it researches what
              the lead is evaluating. If a competitor was mentioned in previous
              conversations, if the prospect&apos;s company recently signed with
              an alternative, if there are public signals about their tech stack
              — the briefing includes specific competitive positioning tailored
              to this deal.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Brain className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Knowledge Graph Integration
              </h2>
            </div>
            <p>
              This is where OfficeOS is fundamentally different from a research
              tool. The agent has access to your organization&apos;s full
              knowledge graph — product roadmaps, pricing decisions, engineering
              capabilities, customer success stories. It generates{" "}
              <strong className="text-primary">
                personalized talking points
              </strong>{" "}
              that connect what the lead cares about to what your company
              actually delivers.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Calendar className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Runs Automatically, Every Day
              </h2>
            </div>
            <p>
              Configure it once and the agent checks your team&apos;s calendar
              daily. For every meeting with an external contact, it runs a full
              research cycle and delivers the briefing before the call. No
              manual triggers, no reminders — like having a dedicated research
              analyst that never forgets, never calls in sick, and improves as
              your knowledge graph grows.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <MessageSquare className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                How It Works
              </h2>
            </div>
            <div className="space-y-4">
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  1
                </span>
                <p>
                  The agent scans your team&apos;s calendar for upcoming
                  meetings with external contacts.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  2
                </span>
                <p>
                  For each meeting, it researches the lead across LinkedIn,
                  company news, funding databases, and your CRM.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  3
                </span>
                <p>
                  It enriches the research with internal context from the
                  knowledge graph — past deals, product fit, competitive angles.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  4
                </span>
                <p>
                  A structured briefing is delivered to the rep before the call
                  — ready to use, not a wall of text.
                </p>
              </div>
            </div>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Give your sales team a research analyst
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
