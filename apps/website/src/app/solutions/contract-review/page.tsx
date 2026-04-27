import Link from "next/link";
import { siteConfig } from "@/lib/site";
import {
  FileCheck,
  Scale,
  GitCompare,
  AlertTriangle,
  Pencil,
  Zap,
} from "lucide-react";
import { Navbar } from "@/components/sections/navbar";

export const metadata = {
  title: "Contract Review Associate — OfficeOS",
  description:
    "An AI agent that reviews contracts against internal compliance guidelines, past agreements, and industry regulations — then hands annotated markup to your legal team.",
};

export default function ContractReview() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <p className="text-sm font-medium text-secondary text-center uppercase tracking-wider">
          Solution
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-center md:text-5xl">
          Contract Review Associate
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          A junior associate that works alongside your legal team. Reads every
          new contract against your compliance guidelines, past agreements, and
          industry regulations — then delivers annotated markup, not a
          replacement for legal judgment.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Scale className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Compliance Checking Against Internal Policies
              </h2>
            </div>
            <p>
              The agent reads new contracts and checks every clause against your{" "}
              <strong className="text-primary">
                internal compliance guidelines
              </strong>{" "}
              stored in the knowledge graph. Liability caps, data processing
              terms, IP assignment clauses, termination conditions — it knows
              what your organization requires and flags anything that deviates.
            </p>
            <p className="mt-4">
              This isn&apos;t generic legal AI. The agent knows <em>your</em>{" "}
              policies, <em>your</em> risk tolerance, and <em>your</em>{" "}
              regulatory environment. A clause that&apos;s fine for one company
              might be a red flag for yours.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <GitCompare className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Historical Contract Comparison
              </h2>
            </div>
            <p>
              The agent compares new contracts against{" "}
              <strong className="text-primary">
                similar deals your company has already signed
              </strong>
              . If a vendor is proposing terms significantly different from what
              you agreed to with comparable vendors, the agent flags it. If a
              clause was negotiated out of a previous deal with the same
              counterparty, it surfaces that precedent.
            </p>
            <p className="mt-4">
              This institutional memory is what makes a senior lawyer effective
              — they remember past deals. Your agent has perfect recall across
              every contract in your knowledge graph.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <AlertTriangle className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Risk Identification
              </h2>
            </div>
            <p>
              Beyond compliance checking, the agent identifies{" "}
              <strong className="text-primary">problematic clauses</strong>{" "}
              based on industry-specific regulations from the knowledge graph.
              Auto-renewal traps, broad indemnification language, ambiguous IP
              ownership, one-sided limitation of liability — the patterns that
              experienced lawyers catch, surfaced automatically for every
              contract.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Pencil className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Annotated Markup for Legal Review
              </h2>
            </div>
            <p>
              The output isn&apos;t a summary or a risk score. It&apos;s an{" "}
              <strong className="text-primary">annotated markup</strong> that
              your legal team can immediately work with — specific clauses
              highlighted, issues explained, alternative formulations suggested,
              relevant precedents from past deals cited. The agent hands off to
              Legal the same way a junior associate would: prepared, annotated,
              ready for senior review.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <FileCheck className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Not a Replacement — A Force Multiplier
              </h2>
            </div>
            <p>
              The agent doesn&apos;t make legal decisions. It does the work that
              a junior associate would do: read the contract, flag the issues,
              prepare the analysis. Your lawyers spend their time on judgment
              calls and negotiations instead of first-pass review. One lawyer
              with this agent reviews contracts at the pace of three.
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
                  A new contract arrives — uploaded to the dashboard or
                  forwarded via email integration.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  2
                </span>
                <p>
                  The agent reads the full document and checks each clause
                  against internal compliance policies, past contracts, and
                  industry regulations from the knowledge graph.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  3
                </span>
                <p>
                  It produces an annotated markup: flagged clauses, risk
                  assessments, suggested alternatives, and relevant precedents.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  4
                </span>
                <p>
                  The markup is delivered to your legal team — ready for senior
                  review, not raw reading.
                </p>
              </div>
            </div>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Give your legal team a dedicated associate
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
