import { SectionHeader } from "@/components/section-header";
import { Feature as FeatureComponent } from "@/components/ui/feature-slideshow";

const featureItems = [
	{
		id: 1,
		title: "Connect Every Tool",
		content:
			"Your agents work with Notion, Jira, GitHub, Google, and any service your team already uses. One-click setup from the dashboard.",
	},
	{
		id: 2,
		title: "Teach Your Workflows",
		content:
			"Build custom skills that encode your exact business logic. Not generic templates — your processes, your rules, your data.",
	},
	{
		id: 3,
		title: "Credentials Never Leave",
		content:
			"Everything runs on your infrastructure. API keys are encrypted at rest and injected per-request — agents never see them.",
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
					Agents that actually do the work.
				</h2>
				<p className="text-balance text-center font-medium text-muted-foreground">
					Other platforms give you chatbots. We give you agents that connect to
					your tools, follow your processes, and run on your infrastructure.
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
