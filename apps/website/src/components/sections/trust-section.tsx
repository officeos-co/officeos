"use client";

import { motion } from "motion/react";
import { ShieldCheck, Lock, ScrollText, ScanEye } from "lucide-react";
import { SectionHeader } from "@/components/section-header";

const trustPoints = [
	{
		title: "Self-Hosted",
		description:
			"Runs entirely on your infrastructure. Data never leaves your network — no third-party cloud dependency.",
	},
	{
		title: "Sandboxed Execution",
		description:
			"Every skill runs in an isolated V8 context. No shared memory, no cross-agent side effects.",
	},
	{
		title: "Credential Isolation",
		description:
			"API keys and secrets live in your backend. Agent pods have zero access to raw credentials.",
	},
	{
		title: "Full Audit Trail",
		description:
			"Every tool call, LLM request, and credential access is logged, traceable, and exportable.",
	},
];

const icons = [ShieldCheck, ScanEye, Lock, ScrollText];

export function TrustSection() {
	return (
		<section
			id="trust"
			className="relative flex w-full flex-col items-center justify-center px-5 md:px-10"
		>
			<SectionHeader>
				<h2 className="text-balance text-center font-medium text-3xl tracking-tighter md:text-4xl">
					Infrastructure your IT team will trust.
				</h2>
			</SectionHeader>

			<div className="grid w-full max-w-5xl grid-cols-1 gap-6 p-10 sm:grid-cols-2 lg:grid-cols-4">
				{trustPoints.map((point, i) => {
					const Icon = icons[i];
					return (
						<motion.div
							key={point.title}
							initial={{ opacity: 0, y: 16 }}
							whileInView={{ opacity: 1, y: 0 }}
							viewport={{ once: true }}
							transition={{ duration: 0.4, delay: i * 0.1 }}
							className="flex flex-col gap-3 rounded-lg border border-border bg-accent/50 p-6"
						>
							<Icon className="h-6 w-6 text-secondary" />
							<h3 className="font-semibold text-primary">{point.title}</h3>
							<p className="text-sm text-muted-foreground">
								{point.description}
							</p>
						</motion.div>
					);
				})}
			</div>
		</section>
	);
}
