"use client";

import { SectionHeader } from "@/components/section-header";
import { FlickeringGrid } from "@/components/ui/flickering-grid";
import { Globe } from "@/components/ui/globe";

const items = [
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
						<p className="font-mono text-5xl font-bold text-primary">100x</p>
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
];

export function GrowthSection() {
	return (
		<section
			id="growth"
			className="relative flex w-full flex-col items-center justify-center px-5 md:px-10"
		>
			<div className="relative mx-5 border-x md:mx-10">
				<div className="absolute top-0 -left-4 h-full w-4 bg-[size:10px_10px] text-gray-950/5 [background-image:repeating-linear-gradient(315deg,currentColor_0_1px,#0000_0_50%)] md:-left-14 md:w-14"></div>
				<div className="absolute top-0 -right-4 h-full w-4 bg-[size:10px_10px] text-gray-950/5 [background-image:repeating-linear-gradient(315deg,currentColor_0_1px,#0000_0_50%)] md:-right-14 md:w-14"></div>

				<SectionHeader>
					<h2 className="text-balance text-center font-medium text-3xl tracking-tighter md:text-4xl">
						Micro Footprint, Massive Scale
					</h2>
					<p className="text-balance text-center font-medium text-muted-foreground">
						Production-ready architecture built on Rust and V8 isolates — not
						VMs, not containers.
					</p>
				</SectionHeader>

				<div className="grid grid-cols-1 divide-y md:grid-cols-2 md:divide-x md:divide-y-0">
					{items.map((item) => (
						<div
							key={item.id}
							className="flex min-h-[500px] flex-col items-start justify-end gap-2 p-6"
						>
							{item.content}
							<h3 className="font-semibold text-lg tracking-tighter">
								{item.title}
							</h3>
							<p className="text-muted-foreground">{item.description}</p>
						</div>
					))}
				</div>
			</div>
		</section>
	);
}
