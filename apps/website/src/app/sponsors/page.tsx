import Image from "next/image";
import Link from "next/link";
import { Navbar } from "@/components/sections/navbar";
import { FooterSection } from "@/components/sections/footer-section";

export const metadata = {
  title: "Sponsors — OfficeOS",
  description:
    "The organizations and partners backing OfficeOS — from Microsoft to EU accessibility initiatives.",
};

const sponsors = [
  {
    name: "Microsoft Denmark",
    src: "/logos/microsoft.png",
    href: "https://www.microsoft.com/da-dk/about",
    description:
      "Hosted the Disability Tech Hackathon 2026 at Microsoft Denmark HQ in Lyngby. OfficeOS won first place, earning a spot in the Microsoft for Startups Founders Hub.",
    highlight: true,
  },
  {
    name: "Disability Tech Denmark",
    src: "/logos/disability-tech.png",
    href: "https://disabilitytech.dk/",
    description:
      "Organized the hackathon that brought together accessibility-focused startups and enterprise partners. Core partner in shaping our accessibility-first approach.",
  },
  {
    name: "DTU Skylab",
    src: "/logos/dtu-skylab.png",
    href: "https://www.skylab.dtu.dk/",
    description:
      "Denmark's leading university incubator at the Technical University of Denmark. Provided early-stage mentorship and workspace.",
  },
  {
    name: "UCPH Lighthouse",
    src: "/logos/ku-lighthouse.png",
    href: "https://lighthouse.ku.dk/en/",
    description:
      "University of Copenhagen's startup program. Supported go-to-market strategy and academic network access.",
  },
  {
    name: "AccessibleEU",
    src: "/logos/accessible-eu.png",
    href: "https://accessibleeu.eu/",
    description:
      "European Commission initiative for digital accessibility. Recognized OfficeOS as a key player in accessible enterprise AI.",
  },
  {
    name: "Siteimprove",
    src: "/logos/siteimprove.png",
    href: "https://siteimprove.ai/",
    description:
      "Global leader in digital accessibility and content optimization. Strategic partner for accessibility tooling integration.",
  },
  {
    name: "TechBBQ",
    src: "/logos/techbbq.png",
    href: "https://techbbq.dk/",
    description:
      "Scandinavia's largest startup and innovation summit. Showcased OfficeOS to the Nordic tech ecosystem.",
  },
  {
    name: "Danske Ivaerksaettere",
    src: "/logos/danske-ivaerksaettere.png",
    href: "https://dkiv.dk/",
    description:
      "Denmark's largest entrepreneur network. Provided business development resources and community access.",
  },
  {
    name: "Elsass Fonden",
    src: "/logos/elsass-fonden.png",
    href: "https://www.elsassfonden.dk/",
    description:
      "Danish foundation supporting people with cerebral palsy. Grant funding for accessibility-focused AI development.",
  },
  {
    name: "Bevica Legater",
    src: "/logos/bevica.png",
    href: "https://www.bevicafonden.dk/",
    description:
      "Foundation supporting projects that improve quality of life for people with disabilities. Grant partner.",
  },
  {
    name: "Videnscenter om Handicap",
    src: "/logos/videnscenter-handicap.png",
    href: "https://videnomhandicap.dk/",
    description:
      "Danish knowledge center for disability research. Provided domain expertise for accessible agent design.",
  },
  {
    name: "Ivaerksaettere med Handicap",
    src: "/logos/ivaerksaettere-med-handicap.png",
    href: "https://www.ivmh.dk/",
    description:
      "Network for entrepreneurs with disabilities. Community partner and early adopter advocate.",
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
              Our Sponsors &amp; Partners
            </h1>
            <p className="mt-4 text-lg text-muted-foreground">
              The organizations that believed in us from day one — funding,
              mentoring, and opening doors.
            </p>
          </div>
        </div>

        {/* Hackathon highlight */}
        <div className="w-full px-6 py-16">
          <div className="mx-auto max-w-4xl">
            <div className="overflow-hidden rounded-xl border border-border">
              <Image
                src="/GroupPhoto.jpeg"
                alt="Hackathon winners at Microsoft Denmark"
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
                OfficeOS won the Disability Tech Hackathon hosted at Microsoft
                Denmark in Lyngby — competing against teams building
                accessibility solutions with AI. The win earned a spot in the
                Microsoft for Startups Founders Hub.
              </p>
            </div>
          </div>
        </div>

        {/* Sponsor grid */}
        <div className="w-full px-6 py-16">
          <div className="mx-auto max-w-5xl">
            <h2 className="text-center text-2xl font-bold tracking-tight mb-12">
              All Partners
            </h2>
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {sponsors.map((sponsor) => (
                <a
                  key={sponsor.name}
                  href={sponsor.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="group flex flex-col gap-4 rounded-lg border border-border p-6 transition-colors hover:bg-muted/50"
                >
                  <div className="flex h-10 items-center">
                    <Image
                      src={sponsor.src}
                      alt={sponsor.name}
                      width={120}
                      height={40}
                      className="max-h-8 w-auto object-contain"
                    />
                  </div>
                  <div>
                    <h3 className="font-semibold text-primary">
                      {sponsor.name}
                    </h3>
                    <p className="mt-1 text-sm text-muted-foreground leading-relaxed">
                      {sponsor.description}
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
