import Link from "next/link";
import { siteConfig } from "@/lib/site";
import { Navbar } from "@/components/sections/navbar";
import {
  Network,
  Database,
  FileText,
  Search,
  Pencil,
  Brain,
} from "lucide-react";

export const metadata = {
  title: "Enterprise Knowledge Graph — OfficeOS",
  description:
    "Obsidian-inspired knowledge management for AI agents. Per-agent vaults, CouchDB-backed storage, graph-first discovery, and real-time dashboard editing.",
};

export default function KnowledgeGraphPage() {
  return (
    <div className="min-h-screen bg-background text-primary font-sans">
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
          Enterprise Knowledge Graph
        </h1>
        <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
          Unstructured knowledge defined by relations, not rigid folders. Every
          agent has access to the full context of your organization — structured
          the way your team actually thinks.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          {/* Obsidian-Inspired */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Network className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Obsidian-Inspired Knowledge Management
              </h2>
            </div>
            <p>
              Traditional enterprise knowledge systems force you into rigid
              hierarchies — folders within folders, taxonomies that become
              outdated the moment they&apos;re created. OfficeOS takes a
              fundamentally different approach, inspired by Obsidian&apos;s
              graph-based model.
            </p>
            <p className="mt-4">
              Knowledge is{" "}
              <strong className="text-primary">
                defined by relations, not by location
              </strong>
              . Documents link to other documents. Categories emerge from
              connections, not from predetermined folder structures. Tags
              provide flexible, overlapping classification instead of rigid
              taxonomy trees. This mirrors how organizations actually accumulate
              and reference knowledge — organically, not hierarchically.
            </p>
          </section>

          {/* CouchDB Vault */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Database className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                CouchDB-Backed Vault System
              </h2>
            </div>
            <p>
              Every agent gets a dedicated CouchDB database — its{" "}
              <strong className="text-primary">vault</strong>. When an agent
              boots, it hydrates its workspace by pulling personality files and
              knowledge documents from this vault. Changes sync bidirectionally:
              updates made in the dashboard appear in the agent&apos;s
              workspace, and agent-generated documents persist back to CouchDB.
            </p>
            <p className="mt-4">
              CouchDB&apos;s document-oriented model is a natural fit for
              knowledge management. Each document is versioned,
              conflict-resistant, and replicable. The vault acts as the single
              source of truth for everything an agent knows about its role,
              personality, and organizational context.
            </p>
          </section>

          {/* Personality Files */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <FileText className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Personality Files
              </h2>
            </div>
            <p>
              Each agent&apos;s vault contains structured Markdown files that
              define its behavior and context:
            </p>
            <ul className="mt-4 space-y-3 pl-6 list-disc">
              <li>
                <strong className="text-primary">SOUL.md</strong> — The
                agent&apos;s core personality, communication style, and
                behavioral guidelines. This is the foundational prompt that
                shapes every interaction.
              </li>
              <li>
                <strong className="text-primary">IDENTITY.md</strong> — Role
                definition, responsibilities, and scope. What the agent is, what
                it does, and what it doesn&apos;t do.
              </li>
              <li>
                <strong className="text-primary">AGENTS.md</strong> — Awareness
                of other agents in the organization. Who they are, what they
                handle, and when to route work to them.
              </li>
            </ul>
            <p className="mt-4">
              These files are plain Markdown — readable by humans, editable in
              the dashboard, and consumed by the agent as system context. No
              proprietary formats, no vendor lock-in.
            </p>
          </section>

          {/* Graph-First Discovery */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Search className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Graph-First Discovery
              </h2>
            </div>
            <p>
              Knowledge discovery in OfficeOS works through{" "}
              <strong className="text-primary">
                categories over folders, tags over taxonomy
              </strong>
              . When an agent needs to find relevant context, it traverses the
              knowledge graph by following relations between documents — not by
              navigating a file tree.
            </p>
            <p className="mt-4">
              This approach follows a{" "}
              <strong className="text-primary">
                text first, structure later
              </strong>{" "}
              philosophy. You write knowledge as it comes — notes, procedures,
              policies, context — and the graph structure emerges from the links
              and tags you add naturally. There&apos;s no upfront schema design,
              no migration when your organization&apos;s needs change.
            </p>
          </section>

          {/* Dashboard Editing */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Pencil className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Real-Time Dashboard Editing
              </h2>
            </div>
            <p>
              The Mission Control dashboard provides a full editor for every
              document in an agent&apos;s vault. Update a personality file, add
              a new knowledge document, or edit organizational context — changes
              sync to the agent&apos;s workspace immediately.
            </p>
            <p className="mt-4">
              This means non-technical team members can shape agent behavior
              without touching code, YAML, or configuration files. The knowledge
              that powers your agents is always visible, always editable, and
              always under your organization&apos;s control.
            </p>
          </section>

          {/* Future: Memory */}
          <section>
            <div className="flex items-center gap-3 mb-4">
              <Brain className="h-6 w-6 text-primary" />
              <h2 className="text-2xl font-bold tracking-tight text-primary">
                Long-Term Memory (Roadmap)
              </h2>
            </div>
            <p>
              The vault system is designed to grow beyond static knowledge
              files. The roadmap includes{" "}
              <strong className="text-primary">MEMORY.md</strong> for curated
              long-term memory — facts, preferences, and decisions the agent
              learns over time and persists across conversations.
            </p>
            <p className="mt-4">
              Additional planned capabilities include daily notes for temporal
              context, project references for scoped knowledge, and cross-agent
              knowledge sharing where agents contribute insights back to the
              organizational graph. The goal is a living knowledge system that
              grows smarter as your agents work.
            </p>
          </section>
        </div>

        {/* CTA */}
        <div className="mt-24 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Build your knowledge graph
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
        Made in Hamburg — © 2026 OfficeOS
      </footer>
    </div>
  );
}
