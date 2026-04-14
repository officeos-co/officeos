import { SectionHeader } from "@/components/section-header";
import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from "@/components/ui/accordion";

const faqItems = [
	{
		question: "What is Office OS?",
		answer:
			"Office OS is an AI agent platform — not an office suite or Microsoft Office replacement. It lets you deploy autonomous AI agents that work across your company: researching leads, handling support, monitoring competitors, reviewing contracts, and more. Each agent gets dedicated skills, permissions, and access to your organization's knowledge graph.",
	},
	{
		question: "How do skills work?",
		answer:
			"Skills are TypeScript modules executed in sandboxed V8 isolates. You define them with the Skill SDK using Zod schemas, and they run on your infrastructure with access to internal APIs and services.",
	},
	{
		question: "Can I run agents on my own infrastructure?",
		answer:
			"Yes. Office OS is Kubernetes-native and designed for self-hosted deployment. Agent pods run on your cluster with full access to your network.",
	},
	{
		question: "How are credentials managed?",
		answer:
			"All credentials are centrally encrypted and managed in the backend. Agent pods never see raw API keys — the backend injects them per-request through a secure proxy.",
	},
	{
		question: "What LLM providers are supported?",
		answer:
			"OpenAI, Anthropic, Google, xAI, Groq, DeepSeek, OpenRouter, and Ollama. You can configure providers per-agent and switch at any time.",
	},
];

export function FAQSection() {
	return (
		<section
			id="faq"
			className="relative flex w-full flex-col items-center justify-center gap-10 pb-10"
		>
			<SectionHeader>
				<h2 className="text-balance text-center font-medium text-3xl tracking-tighter md:text-4xl">
					Frequently Asked Questions
				</h2>
				<p className="text-balance text-center font-medium text-muted-foreground">
					Common questions about Office OS and how it works.
				</p>
			</SectionHeader>

			<div className="mx-auto w-full max-w-3xl px-10">
				<Accordion
					type="single"
					collapsible
					className="grid w-full gap-2 border-b-0"
				>
					{faqItems.map((faq, index) => (
						<AccordionItem
							key={index}
							value={index.toString()}
							className="grid gap-2 border-0"
						>
							<AccordionTrigger className="cursor-pointer rounded-lg border border-border bg-accent px-4 py-3.5 no-underline hover:no-underline data-[state=open]:ring data-[state=open]:ring-primary/20">
								{faq.question}
							</AccordionTrigger>
							<AccordionContent className="rounded-lg border bg-accent p-3 text-primary">
								<p className="font-medium text-primary leading-relaxed">
									{faq.answer}
								</p>
							</AccordionContent>
						</AccordionItem>
					))}
				</Accordion>
			</div>
		</section>
	);
}
