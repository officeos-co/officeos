import Link from "next/link";
import { Navbar } from "@/components/sections/navbar";
import { Check, Leaf, Sparkles } from "lucide-react";

export const metadata = {
	title: "Pricing — Office OS",
	description:
		"Transparent pricing that scales with your agent fleet. Free tier included.",
};

const individualPlans = [
	{
		name: "Free",
		icon: Leaf,
		description: "Get started with one agent",
		price: "Free",
		period: null,
		cta: "Start Free",
		ctaHref: "https://dashboard.harrokrog.com",
		highlight: false,
		prefix: null,
		features: [
			"1 concurrent agent",
			"500K credits / month included",
			"Optional pay-as-you-go overage ($5 / 1M credits)",
			"All providers (OpenAI, Anthropic, Gemini, ...)",
			"Full system control via zeroclaw runtime",
			"GraphQL skill gateway",
			"Community support",
		],
	},
	{
		name: "Pro",
		icon: Sparkles,
		description: "Run up to 3 agents",
		price: "$29",
		period: "/ month",
		cta: "Start Free",
		ctaHref: "https://dashboard.harrokrog.com",
		highlight: true,
		prefix: "Everything in Free and:",
		features: [
			"3 concurrent agents",
			"10M credits / month included",
			"Optional pay-as-you-go overage ($3 / 1M credits)",
			"Smart model routing",
			"Priority email support",
			"Custom skill packages",
		],
	},
];

const teamPlans = [
	{
		name: "Team",
		icon: Leaf,
		description: "Scale to 10 agents",
		price: "$99",
		period: "/ month",
		cta: "Start Free",
		ctaHref: "https://dashboard.harrokrog.com",
		highlight: false,
		prefix: "Everything in Pro and:",
		features: [
			"10 concurrent agents",
			"25M credits / month included",
			"Pay-as-you-go overage ($2.50 / 1M credits, no hard cutoff)",
			"SSO (SAML / OIDC)",
			"Priority email support",
		],
	},
	{
		name: "Enterprise",
		icon: Sparkles,
		description: "Unlimited agents, custom budget",
		price: "Custom",
		period: null,
		cta: "Contact Us",
		ctaHref: "mailto:harro@harrokrog.com",
		highlight: true,
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
];

interface Plan {
	name: string;
	icon: typeof Leaf;
	description: string;
	price: string;
	period: string | null;
	cta: string;
	ctaHref: string;
	highlight: boolean;
	prefix: string | null;
	features: string[];
}

function PlanCard({ plan }: { plan: Plan }) {
	const Icon = plan.icon;
	return (
		<div
			className={`rounded-2xl border p-8 flex flex-col gap-6 transition-colors ${
				plan.highlight
					? "border-secondary/40 bg-secondary/[0.03]"
					: "border-border"
			}`}
		>
			<Icon className="h-8 w-8 text-primary" />

			<div>
				<h3 className="text-xl font-semibold text-primary">{plan.name}</h3>
				<p className="text-sm text-muted-foreground mt-1">
					{plan.description}
				</p>
			</div>

			<div className="flex items-baseline gap-1">
				<span className="text-4xl font-bold text-primary">{plan.price}</span>
				{plan.period && (
					<span className="text-sm text-muted-foreground">{plan.period}</span>
				)}
			</div>

			<Link
				href={plan.ctaHref}
				className={`w-full rounded-xl py-3 font-medium text-sm text-center transition-colors ${
					plan.highlight
						? "bg-secondary text-secondary-foreground hover:bg-secondary/90"
						: "bg-primary text-primary-foreground hover:bg-primary/90"
				}`}
			>
				{plan.cta}
			</Link>

			<div className="border-t border-border pt-4 flex-1">
				{plan.prefix && (
					<p className="text-xs text-muted-foreground mb-3">{plan.prefix}</p>
				)}
				<ul className="space-y-2">
					{plan.features.map((f) => (
						<li key={f} className="flex items-start gap-2 text-sm">
							<Check className="h-4 w-4 text-emerald-500 mt-0.5 shrink-0" />
							<span className="text-muted-foreground">{f}</span>
						</li>
					))}
				</ul>
			</div>
		</div>
	);
}

export default function PricingPage() {
	return (
		<div className="min-h-screen bg-background text-primary font-sans">
			<Navbar />

			<main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
				<h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
					Plans that grow with you
				</h1>
				<p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
					Self-hosted Kubernetes deployment. Full system control.
					Scale as your agent fleet grows.
				</p>

				{/* Individual Plans */}
				<div className="mt-16">
					<h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wider mb-6">
						Individual
					</h2>
					<div className="grid grid-cols-1 gap-6 md:grid-cols-2">
						{individualPlans.map((plan) => (
							<PlanCard key={plan.name} plan={plan} />
						))}
					</div>
				</div>

				{/* Team Plans */}
				<div className="mt-16">
					<h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wider mb-6">
						Team &amp; Enterprise
					</h2>
					<div className="grid grid-cols-1 gap-6 md:grid-cols-2">
						{teamPlans.map((plan) => (
							<PlanCard key={plan.name} plan={plan} />
						))}
					</div>
				</div>

				<p className="text-center text-xs text-muted-foreground/60 mt-10">
					Prices shown don&apos;t include applicable tax. Yearly billing saves 20%.
				</p>

				{/* CTA */}
				<div className="mt-24 text-center">
					<h2 className="text-2xl font-bold tracking-tight mb-4">
						Not sure which plan?
					</h2>
					<p className="text-muted-foreground mb-6">
						Start free with one agent. Upgrade anytime as your team grows.
					</p>
					<div className="flex flex-row items-center justify-center gap-3">
						<Link
							href="https://dashboard.harrokrog.com"
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
				Made in Hamburg — &copy; 2026 Office OS GmbH
			</footer>
		</div>
	);
}
