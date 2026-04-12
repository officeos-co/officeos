import Image from "next/image";
import { siteConfig } from "@/lib/config";

export function CompanyShowcase() {
	const { companyShowcase } = siteConfig;
	return (
		<section
			id="company"
			className="relative flex w-full flex-col items-center justify-center gap-6 px-6 py-10 pt-20"
		>
			<div className="text-center">
				<h3 className="text-xl font-medium text-primary tracking-tight">
					{companyShowcase.title}
				</h3>
				<p className="mt-1 text-sm text-muted-foreground">
					{companyShowcase.subtitle}
				</p>
			</div>
			<div className="z-20 grid w-full max-w-7xl grid-cols-2 items-center justify-center overflow-hidden border-border border-y sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
				{companyShowcase.companyLogos.map((logo) => (
					<a
						href={logo.href}
						target="_blank"
						rel="noopener noreferrer"
						className="group relative flex h-28 w-full items-center justify-center p-4 before:absolute before:top-0 before:-left-1 before:z-10 before:h-screen before:w-px before:bg-border before:content-[''] after:absolute after:-top-1 after:left-0 after:z-10 after:h-px after:w-screen after:bg-border after:content-[''] opacity-50 hover:opacity-100 transition-opacity"
						key={logo.id}
					>
						<Image
							src={logo.src}
							alt={logo.name}
							width={160}
							height={60}
							className="max-h-10 w-auto object-contain"
						/>
					</a>
				))}
			</div>
		</section>
	);
}
