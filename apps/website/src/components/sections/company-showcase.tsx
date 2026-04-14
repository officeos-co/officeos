"use client";

import Image from "next/image";

const companyLogos = [
	{
		id: 1,
		name: "Disability Tech Denmark",
		src: "/logos/disability-tech.png",
		href: "https://disabilitytech.dk/",
	},
	{
		id: 2,
		name: "Microsoft Denmark",
		src: "/logos/microsoft.png",
		href: "https://www.microsoft.com/da-dk/about",
	},
	{
		id: 3,
		name: "DTU Skylab",
		src: "/logos/dtu-skylab.png",
		href: "https://www.skylab.dtu.dk/",
	},
	{
		id: 4,
		name: "UCPH Lighthouse",
		src: "/logos/ku-lighthouse.png",
		href: "https://lighthouse.ku.dk/en/",
	},
	{
		id: 5,
		name: "AccessibleEU",
		src: "/logos/accessible-eu.png",
		href: "https://accessibleeu.eu/",
	},
	{
		id: 6,
		name: "Danske Ivaerksaettere",
		src: "/logos/danske-ivaerksaettere.png",
		href: "https://dkiv.dk/",
	},
	{
		id: 7,
		name: "TechBBQ",
		src: "/logos/techbbq.png",
		href: "https://techbbq.dk/",
	},
	{
		id: 8,
		name: "Siteimprove",
		src: "/logos/siteimprove.png",
		href: "https://siteimprove.ai/",
	},
	{
		id: 9,
		name: "Elsass Fonden",
		src: "/logos/elsass-fonden.png",
		href: "https://www.elsassfonden.dk/",
	},
	{
		id: 10,
		name: "Bevica Legater",
		src: "/logos/bevica.png",
		href: "https://www.bevicafonden.dk/",
	},
	{
		id: 11,
		name: "Videnscenter om Handicap",
		src: "/logos/videnscenter-handicap.png",
		href: "https://videnomhandicap.dk/",
	},
	{
		id: 12,
		name: "Ivaerksaettere med Handicap",
		src: "/logos/ivaerksaettere-med-handicap.png",
		href: "https://www.ivmh.dk/",
	},
];

export function CompanyShowcase() {
	return (
		<section
			id="company"
			className="relative flex w-full flex-col items-center justify-center gap-4 px-12 py-8 md:px-20 lg:px-32"
		>
			<p className="text-xs font-medium uppercase tracking-[0.2em] text-muted-foreground">
				Judged & backed by
			</p>

			<div
				className="relative w-full overflow-hidden"
				style={
					{ "--duration": "30s", "--gap": "3rem" } as React.CSSProperties
				}
			>
				{/* Left fade */}
				<div className="pointer-events-none absolute inset-y-0 left-0 z-10 w-32 bg-gradient-to-r from-background to-transparent" />
				{/* Right fade */}
				<div className="pointer-events-none absolute inset-y-0 right-0 z-10 w-32 bg-gradient-to-l from-background to-transparent" />

				<div className="flex animate-marquee items-center gap-[var(--gap)]">
					{[...companyLogos, ...companyLogos].map((logo, i) => (
						<a
							href={logo.href}
							target="_blank"
							rel="noopener noreferrer"
							className="flex shrink-0 items-center justify-center opacity-40 transition-opacity hover:opacity-80"
							key={`${logo.id}-${i}`}
						>
							<Image
								src={logo.src}
								alt={logo.name}
								width={120}
								height={40}
								className="max-h-7 w-auto object-contain"
							/>
						</a>
					))}
				</div>
			</div>
		</section>
	);
}
