import Image from "next/image";
import Link from "next/link";
import { Navbar } from "@/components/sections/navbar";
import { FooterSection } from "@/components/sections/footer-section";

export const metadata = {
  title: "Network — OfficeOS",
  description:
    "The organizations and programs that have shaped our founder's journey — from Microsoft to EU accessibility initiatives.",
};

const network = [
  {
    name: "Microsoft Denmark",
    src: "/logos/microsoft.png",
    href: "https://www.microsoft.com/da-dk/about",
    description:
      "Hosted the Disability Tech Hackathon 2026 where our founder won first place. Through the Microsoft for Startups Founders Hub, provides cloud credits and mentorship.",
  },
  {
    name: "Disability Tech Denmark",
    src: "/logos/disability-tech.png",
    href: "https://disabilitytech.dk/",
    description:
      "Organized the hackathon at Microsoft Denmark that brought together accessibility-focused entrepreneurs and enterprise partners.",
  },
  {
    name: "DTU Skylab",
    src: "/logos/dtu-skylab.png",
    href: "https://www.skylab.dtu.dk/",
    description:
      "Denmark's leading university incubator at the Technical University of Denmark. Early-stage mentorship and workspace for our founder.",
  },
  {
    name: "UCPH Lighthouse",
    src: "/logos/ku-lighthouse.png",
    href: "https://lighthouse.ku.dk/en/",
    description:
      "University of Copenhagen's startup program. Go-to-market support and academic network access.",
  },
  {
    name: "AccessibleEU",
    src: "/logos/accessible-eu.png",
    href: "https://accessibleeu.eu/",
    description:
      "European Commission initiative for digital accessibility. Part of the ecosystem shaping accessible enterprise technology.",
  },
  {
    name: "Siteimprove",
    src: "/logos/siteimprove.png",
    href: "https://siteimprove.ai/",
    description:
      "Global leader in digital accessibility and content optimization. Hackathon judge and accessibility domain partner.",
  },
  {
    name: "TechBBQ",
    src: "/logos/techbbq.png",
    href: "https://techbbq.dk/",
    description:
      "Scandinavia's largest startup and innovation summit. Platform for connecting with the Nordic tech ecosystem.",
  },
  {
    name: "Danske Ivaerksaettere",
    src: "/logos/danske-ivaerksaettere.png",
    href: "https://dkiv.dk/",
    description:
      "Denmark's largest entrepreneur network. Business development resources and community.",
  },
  {
    name: "Elsass Fonden",
    src: "/logos/elsass-fonden.png",
    href: "https://www.elsassfonden.dk/",
    description:
      "Danish foundation supporting people with cerebral palsy. Hackathon partner focused on accessibility innovation.",
  },
  {
    name: "Bevica Legater",
    src: "/logos/bevica.png",
    href: "https://www.bevicafonden.dk/",
    description:
      "Foundation supporting projects that improve quality of life for people with disabilities. Hackathon partner.",
  },
  {
    name: "Videnscenter om Handicap",
    src: "/logos/videnscenter-handicap.png",
    href: "https://videnomhandicap.dk/",
    description:
      "Danish knowledge center for disability research. Domain expertise for accessible technology design.",
  },
  {
    name: "Ivaerksaettere med Handicap",
    src: "/logos/ivaerksaettere-med-handicap.png",
    href: "https://www.ivmh.dk/",
    description:
      "Network for entrepreneurs with disabilities. Community and advocacy partner.",
  },
];

export default function Sponsors() {
  return (
    <div className="relative mx-auto max-w-7xl border-x">
      <div className="absolute top-0 left-6 z-10 block h-full w-px border-border border-l" />
      <div className="absolute top-0 right-6 z-10 block h-full w-px border-border border-r" />
      <Navbar />

      <main className="flex min-h-screen w-full flex-col items-center divide-y divide-border">
        {/* Header */}
        <div className="w-full px-6 pt-20 pb-16 md:pt-28">
          <div className="mx-auto max-w-3xl text-center">
            <h1 className="text-4xl font-bold tracking-tight md:text-5xl">
              Our Network
            </h1>
            <p className="mt-4 text-lg text-muted-foreground">
              The organizations and programs that have shaped our
              founder&apos;s journey — and continue to support the mission
              behind OfficeOS.
            </p>
          </div>
        </div>

        {/* Hackathon highlight */}
        <div className="w-full px-6 py-16">
          <div className="mx-auto max-w-4xl">
            <div className="overflow-hidden rounded-xl border border-border">
              <Image
                src="/GroupPhoto.jpeg"
                alt="Disability Tech Hackathon at Microsoft Denmark"
                width={1280}
                height={853}
                className="w-full h-auto"
              />
            </div>
            <div className="mt-6 text-center">
              <p className="text-sm font-medium uppercase tracking-[0.15em] text-secondary">
                Disability Tech Hackathon 2026
              </p>
              <h2 className="mt-2 text-2xl font-bold tracking-tight md:text-3xl">
                First Place — Microsoft Denmark
              </h2>
              <p className="mt-3 mx-auto max-w-2xl text-muted-foreground">
                Our founder Harro Krog won first place at the Disability Tech
                Hackathon hosted at Microsoft Denmark in Lyngby. The win earned
                a spot in the Microsoft for Startups Founders Hub — providing
                cloud credits, mentorship, and enterprise access that now powers
                OfficeOS.
              </p>
            </div>
          </div>
        </div>

        {/* Network grid */}
        <div className="w-full px-6 py-16">
          <div className="mx-auto max-w-5xl">
            <h2 className="text-center text-2xl font-bold tracking-tight mb-12">
              Organizations &amp; Programs
            </h2>
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {network.map((org) => (
                <a
                  key={org.name}
                  href={org.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="group flex flex-col gap-4 rounded-lg border border-border p-6 transition-colors hover:bg-muted/50"
                >
                  <div className="flex h-10 items-center">
                    <Image
                      src={org.src}
                      alt={org.name}
                      width={120}
                      height={40}
                      className="max-h-8 w-auto object-contain"
                    />
                  </div>
                  <div>
                    <h3 className="font-semibold text-primary">{org.name}</h3>
                    <p className="mt-1 text-sm text-muted-foreground leading-relaxed">
                      {org.description}
                    </p>
                  </div>
                </a>
              ))}
            </div>
          </div>
        </div>

        {/* CTA */}
        <div className="w-full px-6 py-16 text-center">
          <h2 className="text-2xl font-bold tracking-tight mb-4">
            Interested in partnering?
          </h2>
          <p className="text-muted-foreground mb-6">
            We&apos;re always looking for organizations that share our vision.
          </p>
          <div className="flex flex-row items-center justify-center gap-3">
            <Link
              href="https://dashboard.officeos.co"
              className="flex h-9 items-center justify-center whitespace-nowrap rounded-full bg-secondary px-6 text-sm font-normal text-primary-foreground tracking-wide shadow-sm transition-all hover:bg-secondary/80 active:scale-95"
            >
              Start Free
            </Link>
            <Link
              href="/"
              className="flex h-9 items-center justify-center whitespace-nowrap rounded-full border border-border px-6 text-sm font-normal tracking-wide transition-all hover:bg-muted active:scale-95"
            >
              Back to Home
            </Link>
          </div>
        </div>

        <FooterSection />
      </main>
    </div>
  );
}
