import { SectionHeader } from "@/components/section-header";
import { Feature as FeatureComponent } from "@/components/ui/feature-slideshow";

const featureItems = [
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
];

export function FeatureSection() {
	return (
		<section
			id="features"
			className="relative flex w-full flex-col items-center justify-center gap-5"
		>
			<SectionHeader>
				<h2 className="text-balance text-center font-medium text-3xl tracking-tighter md:text-4xl">
					Your logic. Your network. Not generic MCP.
				</h2>
				<p className="text-balance text-center font-medium text-muted-foreground">
					MCP servers call APIs. Your agents need to run actual business logic —
					on your own infrastructure.
				</p>
			</SectionHeader>
			<div className="flex h-full w-full items-center justify-center lg:h-[450px]">
				<FeatureComponent
					collapseDelay={5000}
					linePosition="bottom"
					featureItems={featureItems}
					lineColor="bg-secondary"
				/>
			</div>
		</section>
	);
}
