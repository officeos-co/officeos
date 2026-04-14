"use client";

import { SectionHeader } from "@/components/section-header";
import { FirstBentoAnimation } from "@/components/first-bento-animation";
import { SecondBentoAnimation } from "@/components/second-bento-animation";
import { ThirdBentoAnimation } from "@/components/third-bento-animation";
import { FourthBentoAnimation } from "@/components/fourth-bento-animation";

const items = [
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
];

export function BentoSection() {
	return (
		<section
			id="bento"
			className="relative flex w-full flex-col items-center justify-center px-5 md:px-10"
		>
			<div className="relative mx-5 border-x md:mx-10">
				<div className="absolute top-0 -left-4 h-full w-4 bg-[size:10px_10px] text-primary/5 [background-image:repeating-linear-gradient(315deg,currentColor_0_1px,#0000_0_50%)] md:-left-14 md:w-14"></div>
				<div className="absolute top-0 -right-4 h-full w-4 bg-[size:10px_10px] text-primary/5 [background-image:repeating-linear-gradient(315deg,currentColor_0_1px,#0000_0_50%)] md:-right-14 md:w-14"></div>

				<SectionHeader>
					<h2 className="text-balance pb-1 text-center font-medium text-3xl tracking-tighter md:text-4xl">
						One dashboard for every agent in your organization.
					</h2>
					<p className="text-balance text-center font-medium text-muted-foreground">
						Deploy agents per team. Each with their own skills, permissions, and
						knowledge graph access.
					</p>
				</SectionHeader>

				<div className="grid grid-cols-1 overflow-hidden md:grid-cols-2">
					{items.map((item) => (
						<div
							key={item.id}
							className="group group relative flex max-h-[400px] min-h-[600px] cursor-pointer flex-col items-start justify-end p-0.5 before:absolute before:top-0 before:-left-0.5 before:z-10 before:h-screen before:w-px before:bg-border before:content-[''] after:absolute after:-top-0.5 after:left-0 after:z-10 after:h-px after:w-screen after:bg-border after:content-[''] md:min-h-[500px]"
						>
							<div className="relative flex size-full h-full items-center justify-center overflow-hidden">
								{item.content}
							</div>
							<div className="flex-1 flex-col gap-2 p-6">
								<h3 className="font-semibold text-lg tracking-tighter">
									{item.title}
								</h3>
								<p className="text-muted-foreground">{item.description}</p>
							</div>
						</div>
					))}
				</div>
			</div>
		</section>
	);
}
