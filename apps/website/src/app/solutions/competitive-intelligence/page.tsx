import Link from "next/link";
import { Radar, TrendingUp, Users, FileText, Target, Zap } from "lucide-react";
import { Navbar } from "@/components/sections/navbar";

export const metadata = {
  title: "Competitive Intelligence Analyst — OfficeOS",
  description:
    "An AI agent that monitors 20+ competitors daily — press releases, product launches, job postings, social media, earnings calls — and delivers strategic analysis, not summaries.",
};

export default function CompetitiveIntelligence() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <p className="text-sm font-medium text-secondary text-center uppercase tracking-wider">
          Solution
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-center md:text-5xl">
          Competitive Intelligence Analyst
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          A strategy analyst that never sleeps. Monitors 20+ competitors daily
          and delivers real analysis with strategic assessments — not summaries.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Radar className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Daily Monitoring Across All Channels
              </h2>
            </div>
            <p>
              The agent monitors your competitors daily across every signal that
              matters:{" "}
              <strong className="text-primary">
                press releases, product launches, job postings, social media,
                and earnings calls
              </strong>
              . Not once a week, not when someone remembers to check — every
              single day, across every competitor you track.
            </p>
            <p className="mt-4">
              Job postings alone tell a story. If a competitor starts hiring 15
              ML engineers, they&apos;re probably building something. The agent
              connects these dots and surfaces them before they become obvious.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <TrendingUp className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Strategic Analysis, Not Summaries
              </h2>
            </div>
            <p>
              The agent doesn&apos;t write generic summaries. It produces{" "}
              <strong className="text-primary">
                real analysis with strategic assessments
              </strong>
              . Instead of &quot;Competitor X launched a new feature,&quot; you
              get: &quot;Competitor X hired 15 ML engineers and just launched a
              recommendation engine — this likely targets the same enterprise
              segment as our Product Z. Their pricing at $X/seat undercuts us by
              30%.&quot;
            </p>
            <p className="mt-4">
              These are assessments a strategy analyst would make, delivered
              every morning with the context of your organization&apos;s
              positioning, product roadmap, and customer base from the knowledge
              graph.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Users className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Department-Specific Routing
              </h2>
            </div>
            <p>
              Not every competitive signal is relevant to every team. The agent
              independently evaluates which department each insight matters to.
              A competitor&apos;s new pricing model goes to Sales and Product. A
              new patent filing goes to Engineering. A senior hire in their
              go-to-market team goes to Marketing.
            </p>
            <p className="mt-4">
              Each team gets exactly the intelligence they need — no noise, no
              irrelevant alerts, no 30-page reports that nobody reads.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Target className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Trend Detection and Early Warnings
              </h2>
            </div>
            <p>
              Individual signals are useful. Patterns across time are powerful.
              The agent tracks trends — a competitor consistently hiring in a
              specific domain, gradually repositioning their messaging, slowly
              expanding into your market segment. These slow-moving threats are
              the ones that blindside companies. The agent makes them visible
              early.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <FileText className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Full Organizational Context
              </h2>
            </div>
            <p>
              Because the agent has access to your knowledge graph, its analysis
              isn&apos;t generic. It knows your product roadmap, your pricing,
              your customer segments, your strengths and weaknesses. When a
              competitor makes a move, the analysis is always relative to{" "}
              <strong className="text-primary">your</strong> position — not
              abstract market commentary.
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
                <p>
                  Configure which competitors to track and which channels to
                  monitor (web, social, job boards, filings).
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  2
                </span>
                <p>
                  The agent scans daily, identifies new signals, and evaluates
                  strategic relevance against your knowledge graph.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  3
                </span>
                <p>
                  Strategic analysis is generated — not summaries, but
                  assessments with implications for your specific business.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  4
                </span>
                <p>
                  Insights are routed to the right department and accumulated
                  over time for trend analysis.
                </p>
              </div>
            </div>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Deploy a strategy analyst that never sleeps
          </h2>
          <div className="flex flex-row items-center justify-center gap-3">
            <Link
              href="https://dashboard.officeos.co"
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
        Made in Hamburg — &copy; 2026 OfficeOS GmbH
      </footer>
    </div>
  );
}
