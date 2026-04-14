import Image from "next/image";
import { OrbitingCircles } from "@/components/ui/orbiting-circle";

function ChannelIcon({ src, alt }: { src: string; alt: string }) {
	return (
		<div className="flex size-10 items-center justify-center">
			<Image src={src} alt={alt} width={32} height={32} className="size-8" />
		</div>
	);
}

export function SecondBentoAnimation() {
	return (
		<div className="relative flex h-full w-full items-center justify-center overflow-hidden">
			<div className="pointer-events-none absolute bottom-0 left-0 z-20 h-20 w-full bg-gradient-to-t from-background to-transparent"></div>
			<div className="pointer-events-none absolute top-0 left-0 z-20 h-20 w-full bg-gradient-to-b from-background to-transparent"></div>

			<div className="absolute top-1/2 left-1/2 z-30 flex size-16 -translate-x-1/2 -translate-y-1/2 items-center justify-center gap-2 rounded-full bg-secondary p-2 md:top-auto md:bottom-0">
				<span className="font-mono text-sm font-bold text-white">OS</span>
			</div>
			<div className="relative flex h-full w-full items-center justify-center overflow-hidden">
				<div className="relative flex h-full w-full translate-y-0 items-center justify-center md:translate-y-32">
					<OrbitingCircles
						index={0}
						iconSize={60}
						radius={100}
						reverse
						speed={1}
					>
						<ChannelIcon src="/logos/slack.svg" alt="Slack" />
						<ChannelIcon src="/logos/email.svg" alt="Email" />
						<ChannelIcon src="/logos/teams.svg" alt="Teams" />
					</OrbitingCircles>

					<OrbitingCircles index={1} iconSize={60} speed={0.5}>
						<ChannelIcon src="/logos/whatsapp.svg" alt="WhatsApp" />
						<ChannelIcon src="/logos/discord.svg" alt="Discord" />
						<ChannelIcon src="/logos/telegram.svg" alt="Telegram" />
					</OrbitingCircles>

					<OrbitingCircles
						index={2}
						iconSize={60}
						radius={230}
						reverse
						speed={0.5}
					>
						<ChannelIcon src="/logos/github.svg" alt="GitHub" />
						<ChannelIcon src="/logos/notion.svg" alt="Notion" />
						<ChannelIcon src="/logos/linear.svg" alt="Linear" />
					</OrbitingCircles>
				</div>
			</div>
		</div>
	);
}
