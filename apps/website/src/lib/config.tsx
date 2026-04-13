import { FirstBentoAnimation } from "@/components/first-bento-animation";
import { FourthBentoAnimation } from "@/components/fourth-bento-animation";
import { SecondBentoAnimation } from "@/components/second-bento-animation";
import { ThirdBentoAnimation } from "@/components/third-bento-animation";
import { FlickeringGrid } from "@/components/ui/flickering-grid";
import { Globe } from "@/components/ui/globe";

export const BLUR_FADE_DELAY = 0.15;

export const siteConfig = {
	name: "Office OS",
	description:
		"Deploy, connect, and control AI agents from one platform. Per-team skills, permissions, and knowledge graph access.",
	cta: "Start Free",
	url: process.env.NEXT_PUBLIC_APP_URL || "http://localhost:3000",
	keywords: [
		"AI agent platform",
		"agent deployment",
		"Kubernetes agents",
		"agent management",
		"enterprise AI",
		"custom skills",
	],
	links: {
		github: "https://github.com/officeos",
		linkedin: "https://linkedin.com/company/officeos",
		twitter: "https://x.com/officeos",
	},
	nav: {
		links: [
			{ id: 1, name: "Home", href: "#hero" },
			{ id: 2, name: "Features", href: "#features" },
			{ id: 3, name: "FAQ", href: "#faq" },
		],
	},
	hero: {
		title:
			"Stop managing agents manually. Deploy, connect, and control them from one place.",
		description:
			"Give every team their own AI agent with the right skills, permissions, and access to your organization's knowledge.",
		cta: {
			primary: {
				text: "Start Free",
				href: "https://dashboard.harrokrog.com",
			},
			secondary: {
				text: "Book a Demo",
				href: "#book-demo",
			},
		},
	},
	companyShowcase: {
		title: "Judged & backed by",
		subtitle: "Hackathon winners — backed by industry leaders in tech and innovation",
		companyLogos: [
			{ id: 1, name: "Disability Tech Denmark", src: "/logos/disability-tech.png", href: "https://disabilitytech.dk/" },
			{ id: 2, name: "Microsoft Denmark", src: "/logos/microsoft.png", href: "https://www.microsoft.com/da-dk/about" },
			{ id: 3, name: "DTU Skylab", src: "/logos/dtu-skylab.png", href: "https://www.skylab.dtu.dk/" },
			{ id: 4, name: "UCPH Lighthouse", src: "/logos/ku-lighthouse.png", href: "https://lighthouse.ku.dk/en/" },
			{ id: 5, name: "AccessibleEU", src: "/logos/accessible-eu.png", href: "https://accessibleeu.eu/" },
			{ id: 6, name: "Danske Iværksættere", src: "/logos/danske-ivaerksaettere.png", href: "https://dkiv.dk/" },
			{ id: 7, name: "TechBBQ", src: "/logos/techbbq.png", href: "https://techbbq.dk/" },
			{ id: 8, name: "Siteimprove", src: "/logos/siteimprove.png", href: "https://siteimprove.ai/" },
			{ id: 9, name: "Elsass Fonden", src: "/logos/elsass-fonden.png", href: "https://www.elsassfonden.dk/" },
			{ id: 10, name: "Bevica Legater", src: "/logos/bevica.png", href: "https://www.bevicafonden.dk/" },
			{ id: 11, name: "Videnscenter om Handicap", src: "/logos/videnscenter-handicap.png", href: "https://videnomhandicap.dk/" },
			{ id: 12, name: "Iværksættere med Handicap", src: "/logos/ivaerksaettere-med-handicap.png", href: "https://www.ivmh.dk/" },
		],
	},
	bentoSection: {
		title: "One dashboard for every agent in your organization.",
		description:
			"Deploy agents per team. Each with their own skills, permissions, and knowledge graph access.",
		items: [
			{
				id: 1,
				content: <FirstBentoAnimation />,
				title: "Agent Deployment",
				description:
					"Deploy a new agent in under a minute. Select a team, assign skills, set permissions — done.",
			},
			{
				id: 2,
				content: <SecondBentoAnimation />,
				title: "Custom Skills",
				description:
					"Write your own skills in TypeScript. Your agents execute real business logic on your infrastructure.",
			},
			{
				id: 3,
				content: (
					<ThirdBentoAnimation
						data={[20, 30, 25, 45, 40, 55, 75]}
						toolTipValues={[
							1234, 1678, 2101, 2534, 2967, 3400, 3833, 4266, 4700, 5133,
						]}
					/>
				),
				title: "Knowledge Graph",
				description:
					"Every agent accesses your organization's knowledge. Per-team and per-org graphs that stay in sync.",
			},
			{
				id: 4,
				content: <FourthBentoAnimation once={false} />,
				title: "Central Credentials",
				description:
					"API keys, tokens, service accounts — managed once, used by all agents.",
			},
		],
	},
	featureSection: {
		title: "Your logic. Your network. Not generic MCP.",
		description:
			"MCP servers call APIs. Your agents need to run actual business logic — on your own infrastructure.",
		items: [
			{
				id: 1,
				title: "Sandboxed V8 Isolates",
				content:
					"Every skill runs in an isolated V8 context. No shared memory, no side effects. Fast cold starts, minimal resource usage.",
			},
			{
				id: 2,
				title: "Full TypeScript Support",
				content:
					"Define skills with full type safety using the Skill SDK. Zod schemas for inputs and outputs. Publish as npm packages.",
			},
			{
				id: 3,
				title: "Run on Your Network",
				content:
					"Skills can access internal APIs, databases, and services that have no public endpoint. Your infrastructure, your rules.",
			},
		],
	},
	growthSection: {
		title: "Micro Footprint, Massive Scale",
		description:
			"Production-ready architecture built on Rust and V8 isolates — not VMs, not containers.",
		items: [
			{
				id: 1,
				content: (
					<div className="relative flex size-full items-center justify-center overflow-hidden">
						<FlickeringGrid
							className="size-full"
							gridGap={4}
							squareSize={2}
							maxOpacity={0.5}
						/>
						<div className="absolute inset-0 flex flex-col items-center justify-center gap-4">
							<div className="text-center">
								<p className="font-mono text-5xl font-bold text-primary">
									100x
								</p>
								<p className="mt-2 text-sm text-muted-foreground">
									less resources than VM-based agents
								</p>
							</div>
							<div className="text-center">
								<p className="font-mono text-5xl font-bold text-primary">
									&lt;5ms
								</p>
								<p className="mt-2 text-sm text-muted-foreground">
									cold start time
								</p>
							</div>
						</div>
					</div>
				),
				title: "Rust-based Runtime",
				description:
					"Megabytes, not gigabytes. Run hundreds of agents where other systems manage five.",
			},
			{
				id: 2,
				content: (
					<div className="relative flex size-full max-w-lg -translate-y-20 items-center justify-center overflow-hidden [mask-image:linear-gradient(to_top,transparent,black_50%)]">
						<Globe className="top-28" />
					</div>
				),
				title: "Deploy Anywhere",
				description:
					"Kubernetes-native. Self-hosted on your infrastructure or managed in our cloud.",
			},
		],
	},
	trustSection: {
		title: "Infrastructure your IT team will trust.",
		points: [
			{
				title: "Kubernetes-native",
				description:
					"Deploys as standard K8s workloads. No custom runtimes, no vendor lock-in.",
			},
			{
				title: "Sandboxed V8 Isolates",
				description:
					"Every skill runs in an isolated V8 context. No shared memory, no side effects.",
			},
			{
				title: "Role-based Permissions",
				description:
					"Control which agents access which skills and data sources at the org level.",
			},
			{
				title: "Audit Logs",
				description:
					"Every tool call, LLM request, and credential access is logged and traceable.",
			},
		],
	},
	faqSection: {
		title: "Frequently Asked Questions",
		description: "Common questions about Office OS and how it works.",
		faQitems: [
			{
				id: 1,
				question: "What is Office OS?",
				answer:
					"Office OS is a platform for deploying and managing AI agents across your organization. Each team gets their own agents with dedicated skills, permissions, and knowledge graph access.",
			},
			{
				id: 2,
				question: "How do skills work?",
				answer:
					"Skills are TypeScript modules executed in sandboxed V8 isolates. You define them with the Skill SDK using Zod schemas, and they run on your infrastructure with access to internal APIs and services.",
			},
			{
				id: 3,
				question: "Can I run agents on my own infrastructure?",
				answer:
					"Yes. Office OS is Kubernetes-native and designed for self-hosted deployment. Agent pods run on your cluster with full access to your network.",
			},
			{
				id: 4,
				question: "How are credentials managed?",
				answer:
					"All credentials are centrally encrypted and managed in the backend. Agent pods never see raw API keys — the backend injects them per-request through a secure proxy.",
			},
			{
				id: 5,
				question: "What LLM providers are supported?",
				answer:
					"OpenAI, Anthropic, Google, xAI, Groq, DeepSeek, OpenRouter, and Ollama. You can configure providers per-agent and switch at any time.",
			},
		],
	},
	ctaSection: {
		id: "cta",
		title: "Ready to deploy your first agent?",
		subtitle:
			"Start free with your own API keys. No credit card required.",
		button: {
			text: "Start Free",
			href: "https://dashboard.harrokrog.com",
		},
		subtext:
			"Free tier includes 3 agents. Bring your own API keys. Cancel anytime.",
	},
	footerLinks: [
		{
			title: "Product",
			links: [
				{ id: 1, title: "Features", url: "#features" },
				{ id: 2, title: "Docs", url: "https://docs.harrokrog.com" },
				{ id: 7, title: "Status", url: "https://status.harrokrog.com" },
			],
		},
		{
			title: "Company",
			links: [
				{ id: 3, title: "About", url: "/about" },
				{ id: 4, title: "Blog", url: "#" },
			],
		},
		{
			title: "Legal",
			links: [
				{ id: 5, title: "Privacy", url: "/privacy" },
				{ id: 6, title: "Terms", url: "/terms" },
			],
		},
	],
};

export type SiteConfig = typeof siteConfig;
