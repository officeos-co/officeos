import Link from "next/link";
import { PenTool, Search, BarChart3, FileText, Users, Zap } from "lucide-react";
import { Navbar } from "@/components/sections/navbar";

export const metadata = {
  title: "Content Strategist — OfficeOS",
  description:
    "An AI agent that researches topics from industry news, customer feedback, and internal knowledge — then develops a differentiated content strategy with drafts ready for review.",
};

export default function ContentStrategy() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <p className="text-sm font-medium text-secondary text-center uppercase tracking-wider">
          Solution
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-center md:text-5xl">
          Content Strategist
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          A content team member that has the entire industry in view.
          Doesn&apos;t write generic blog posts — develops a differentiated
          content strategy and produces drafts a human reviews and approves.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Search className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Research-Driven Topic Discovery
              </h2>
            </div>
            <p>
              The agent independently researches topics from{" "}
              <strong className="text-primary">
                industry news, customer feedback, and internal knowledge
              </strong>
              . It doesn&apos;t brainstorm in a vacuum — it identifies topics
              based on what&apos;s actually happening in your market, what your
              customers are asking about, and what your organization uniquely
              knows.
            </p>
            <p className="mt-4">
              A customer success ticket about a recurring pain point becomes a
              thought leadership article. An internal engineering breakthrough
              becomes a technical deep dive. The agent connects signals that a
              content team working in isolation would miss.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <BarChart3 className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Competitive Content Differentiation
              </h2>
            </div>
            <p>
              The agent doesn&apos;t just find topics — it finds{" "}
              <strong className="text-primary">angles</strong>. For every topic,
              it analyzes what competitors have already published, identifies
              gaps in the conversation, and determines which perspective
              differentiates your content. The goal isn&apos;t more content —
              it&apos;s content that stands out.
            </p>
            <p className="mt-4">
              If three competitors already published &quot;How to implement
              X,&quot; the agent doesn&apos;t suggest a fourth version. It finds
              the angle that leverages your unique expertise and positions your
              organization as the authority.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <FileText className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Multi-Format Content Planning
              </h2>
            </div>
            <p>
              Not every topic deserves the same format. The agent evaluates each
              topic and recommends the right format:{" "}
              <strong className="text-primary">
                LinkedIn post, technical deep dive, case study, newsletter
                segment, or video script
              </strong>
              . It considers your audience, the topic complexity, and where in
              the buyer journey each piece fits.
            </p>
            <p className="mt-4">
              The result is a content calendar that&apos;s strategically planned
              — not a random list of blog post ideas, but a coherent narrative
              arc across formats and channels.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <PenTool className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Draft Generation with Human Review
              </h2>
            </div>
            <p>
              The agent produces full drafts — not outlines, not bullet points.
              These drafts are informed by your knowledge graph, your brand
              voice guidelines, and the competitive analysis it already
              performed. A human reviews, refines, and approves. The creative
              direction stays with your team; the heavy lifting is handled.
            </p>
            <p className="mt-4">
              One content person with this agent produces the output of a small
              content team. Not by cutting corners, but by eliminating the
              research, planning, and first-draft phases that consume most of
              the time.
            </p>
          </section>

          <section>
            <div className="flex items-center gap-3 mb-4">
              <Users className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Full Organizational Context
              </h2>
            </div>
            <p>
              Because the agent draws from the knowledge graph, its content
              reflects the full depth of your organization. Product
              capabilities, customer success stories, engineering insights,
              sales objections — every piece of content is grounded in
              what&apos;s actually true about your company, not generic industry
              talking points.
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
                  The agent scans industry news, customer feedback channels, and
                  internal knowledge for content opportunities.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  2
                </span>
                <p>
                  For each topic, it analyzes competitive content, identifies
                  differentiating angles, and recommends the optimal format.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  3
                </span>
                <p>
                  It produces full drafts informed by your knowledge graph,
                  brand guidelines, and competitive positioning.
                </p>
              </div>
              <div className="flex gap-4">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-secondary/10 text-sm font-bold text-secondary">
                  4
                </span>
                <p>
                  Your team reviews, refines, and publishes. The strategic
                  thinking and heavy lifting are already done.
                </p>
              </div>
            </div>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Deploy a content strategist with industry-wide vision
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
        Made in Hamburg — &copy; 2026 OfficeOS
      </footer>
    </div>
  );
}
