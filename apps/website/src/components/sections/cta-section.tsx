import Link from "next/link";
import { siteConfig } from "@/lib/config";

export function CTASection() {
	const { ctaSection } = siteConfig;

	return (
		<section
			id="cta"
			className="flex w-full flex-col items-center justify-center"
		>
			<div className="w-full">
				<div className="relative z-20 flex h-[400px] w-full flex-col items-center justify-center gap-6 overflow-hidden rounded-xl border border-border bg-accent shadow-xl md:h-[400px]">
					<h1 className="max-w-xs text-center font-medium text-4xl text-primary tracking-tighter md:max-w-xl md:text-5xl">
						{ctaSection.title}
					</h1>
					<p className="max-w-md text-center text-muted-foreground">
						{ctaSection.subtitle}
					</p>
					<div className="flex flex-col items-center justify-center gap-3">
						<Link
							href={ctaSection.button.href}
							className="flex h-10 w-fit items-center justify-center rounded-full border border-white/[0.12] bg-secondary px-6 font-normal text-sm tracking-wide shadow-[inset_0_1px_2px_rgba(255,255,255,0.25),0_3px_3px_-1.5px_rgba(16,24,40,0.06),0_1px_1px_rgba(16,24,40,0.08)] transition-all ease-out hover:bg-secondary/80 active:scale-95"
						>
							{ctaSection.button.text}
						</Link>
						<span className="text-sm text-muted-foreground">
							{ctaSection.subtext}
						</span>
					</div>
				</div>
			</div>
		</section>
	);
}
