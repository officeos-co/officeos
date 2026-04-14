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
			className="relative flex w-full flex-col items-center justify-center gap-6 px-6 py-10 pt-20"
		>
			<div className="text-center">
				<h3 className="text-xl font-medium text-primary tracking-tight">
					Judged & backed by
				</h3>
				<p className="mt-1 text-sm text-muted-foreground">
					Hackathon winners — backed by industry leaders in tech and innovation
				</p>
			</div>
			<div className="z-20 grid w-full max-w-7xl grid-cols-2 items-center justify-center overflow-hidden border-border border-y sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6">
				{companyLogos.map((logo) => (
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
