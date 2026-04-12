import { siteConfig } from "@/lib/config";

export function CompanyShowcase() {
	const { companyShowcase } = siteConfig;
	return (
		<section
			id="company"
			className="relative flex w-full flex-col items-center justify-center gap-10 px-6 py-10 pt-20"
		>
			<p className="font-medium text-muted-foreground">
				Built by engineers from
			</p>
			<div className="z-20 grid w-full max-w-7xl grid-cols-2 items-center justify-center overflow-hidden border-border border-y md:grid-cols-4">
				{companyShowcase.companyLogos.map((logo) => (
					<div
						className="group relative flex h-28 w-full items-center justify-center p-4 before:absolute before:top-0 before:-left-1 before:z-10 before:h-screen before:w-px before:bg-border before:content-[''] after:absolute after:-top-1 after:left-0 after:z-10 after:h-px after:w-screen after:bg-border after:content-['']"
						key={logo.id}
					>
						<div className="flex h-10 w-28 items-center justify-center rounded-md border border-dashed border-muted-foreground/20 bg-muted/50">
							<span className="text-xs text-muted-foreground">
								{logo.name}
							</span>
						</div>
					</div>
				))}
			</div>
		</section>
	);
}
