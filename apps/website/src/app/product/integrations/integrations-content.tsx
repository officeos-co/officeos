"use client";

import { useMemo, useState } from "react";
import {
	Globe,
	MessageSquare,
	Plug,
	Search,
	Server,
	Terminal,
	Workflow,
} from "lucide-react";
import { Input } from "@/components/ui/input";

const integrationSections = [
	{
		title: "First-Party Integrations",
		icon: Plug,
		keywords: ["github", "notion", "google", "obsidian", "skills", "api"],
		body: (
			<>
				<p>
					OfficeOS ships with a growing set of first-party skills built by
					the core team:{" "}
					<strong className="text-primary">
						GitHub, Notion, Google, and Obsidian
					</strong>
					. These aren&apos;t generic API clients — they&apos;re purpose-built
					integrations that expose the actions agents actually need: creating
					issues, querying databases, managing documents, and searching
					knowledge bases.
				</p>
				<p className="mt-4">
					Every first-party skill is built with the same{" "}
					<strong className="text-primary">@harro/skill-sdk</strong> that you
					use for custom skills. There&apos;s no privileged internal API, no
					special treatment. First-party skills are simply well-tested,
					well-maintained packages that ship with the platform.
				</p>
			</>
		),
	},
	{
		title: "Browser Automation",
		icon: Globe,
		keywords: ["playwright", "browser", "chrome", "screenshots", "cookies"],
		body: (
			<>
				<p>
					The browser skill uses{" "}
					<strong className="text-primary">Playwright</strong> to give agents
					headless Chrome control — navigating pages, filling forms, clicking
					buttons, extracting content, and taking screenshots. It&apos;s a
					system skill, meaning it&apos;s always available without manual
					installation or credential configuration.
				</p>
				<p className="mt-4">
					Each agent gets{" "}
					<strong className="text-primary">
						per-agent session persistence
					</strong>
					. The backend transparently manages browser sessions and persists
					cookies in Postgres. Agents are session-unaware — they just browse
					the web, and the platform handles login state, session cookies, and
					context across interactions.
				</p>
			</>
		),
	},
	{
		title: "Channel Integration",
		icon: MessageSquare,
		keywords: ["slack", "email", "whatsapp", "teams", "messages", "channels"],
		body: (
			<>
				<p>
					Agents communicate through the channels your team already uses:{" "}
					<strong className="text-primary">Slack, Email, and WhatsApp</strong>.
					Instead of requiring users to switch to a new interface, agents meet
					your team where they work.
				</p>
				<p className="mt-4">
					A support agent can respond to customer emails, a project manager
					agent can post updates in Slack channels, and a notification agent
					can send WhatsApp alerts. Channel integration is bidirectional —
					agents can both send and receive messages.
				</p>
			</>
		),
	},
	{
		title: "Unified Skill Interface",
		icon: Terminal,
		keywords: ["skill_exec", "graphql", "tool", "cli", "credentials"],
		body: (
			<>
				<p>
					Agents call every integration through a single{" "}
					<strong className="text-primary">skill_exec</strong> tool that
					presents a CLI-like interface over GraphQL. Whether it&apos;s
					creating a GitHub issue, querying a Notion database, or browsing a
					webpage, the interface is the same.
				</p>
				<p className="mt-4">
					This unified interface means the LLM doesn&apos;t need to learn
					different tool formats for different integrations. Every integration
					also benefits from central credential management — API keys stored
					once, used everywhere.
				</p>
			</>
		),
	},
	{
		title: "Self-Hosted Runners",
		icon: Server,
		keywords: ["docker", "runner", "on-premise", "network", "device flow"],
		body: (
			<>
				<p>
					For integrations that need to access on-premise systems, OfficeOS
					provides <strong className="text-primary">self-hosted runners</strong>{" "}
					— Docker containers that run inside your network and poll the
					backend for jobs. No tunnels, no firewall changes, no inbound ports
					required.
				</p>
				<p className="mt-4">
					Runner authentication uses{" "}
					<strong className="text-primary">
						RFC 8628 device authorization flow
					</strong>{" "}
					— the same pattern used by GitHub CLI and Docker Desktop. Skills sync
					automatically to runners.
				</p>
			</>
		),
	},
	{
		title: "Universal Extensibility",
		icon: Workflow,
		keywords: ["rest", "graphql", "zod", "sdk", "custom skills"],
		body: (
			<>
				<p>
					Any software that exposes a{" "}
					<strong className="text-primary">GraphQL or REST API</strong> can be
					integrated as a skill. The SDK provides a straightforward pattern:
					define your actions, declare input/output schemas with Zod, specify
					required credentials, and publish.
				</p>
				<p className="mt-4">
					OfficeOS agents have{" "}
					<strong className="text-primary">full system control</strong> — they
					can interact with any API, any internal service, any tool that your
					organization runs.
				</p>
			</>
		),
	},
];

export function IntegrationsContent() {
	const [query, setQuery] = useState("");
	const normalizedQuery = query.trim().toLowerCase();

	const filteredSections = useMemo(() => {
		if (!normalizedQuery) return integrationSections;

		return integrationSections.filter((section) =>
			[section.title, ...section.keywords]
				.join(" ")
				.toLowerCase()
				.includes(normalizedQuery),
		);
	}, [normalizedQuery]);

	return (
		<>
			<div className="relative mt-10 max-w-xl mx-auto">
				<Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
				<Input
					type="search"
					value={query}
					onChange={(event) => setQuery(event.target.value)}
					placeholder="Search integrations..."
					className="h-11 rounded-full pl-10"
				/>
			</div>

			<div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
				{filteredSections.map((section) => {
					const Icon = section.icon;
					return (
						<section key={section.title}>
							<div className="flex items-center gap-3 mb-4">
								<Icon className="h-6 w-6 text-primary" />
								<h2 className="text-2xl font-bold tracking-tight text-primary">
									{section.title}
								</h2>
							</div>
							{section.body}
						</section>
					);
				})}

				{filteredSections.length === 0 && (
					<div className="rounded-lg border border-border p-8 text-center text-sm text-muted-foreground">
						No integrations matched your search.
					</div>
				)}
			</div>
		</>
	);
}
