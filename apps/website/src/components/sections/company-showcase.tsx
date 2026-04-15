"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { AnimatePresence, motion } from "motion/react";
import { ArrowRight } from "lucide-react";

const companyLogos = [
  {
    id: 1,
    name: "Disability Tech Denmark",
    src: "/logos/disability-tech.png",
  },
  {
    id: 2,
    name: "Microsoft Denmark",
    src: "/logos/microsoft.png",
  },
  {
    id: 3,
    name: "DTU Skylab",
    src: "/logos/dtu-skylab.png",
  },
  {
    id: 4,
    name: "UCPH Lighthouse",
    src: "/logos/ku-lighthouse.png",
  },
  {
    id: 5,
    name: "AccessibleEU",
    src: "/logos/accessible-eu.png",
  },
  {
    id: 6,
    name: "Danske Ivaerksaettere",
    src: "/logos/danske-ivaerksaettere.png",
  },
  {
    id: 7,
    name: "TechBBQ",
    src: "/logos/techbbq.png",
  },
  {
    id: 8,
    name: "Siteimprove",
    src: "/logos/siteimprove.png",
  },
  {
    id: 9,
    name: "Elsass Fonden",
    src: "/logos/elsass-fonden.png",
  },
  {
    id: 10,
    name: "Bevica Legater",
    src: "/logos/bevica.png",
  },
  {
    id: 11,
    name: "Videnscenter om Handicap",
    src: "/logos/videnscenter-handicap.png",
  },
  {
    id: 12,
    name: "Ivaerksaettere med Handicap",
    src: "/logos/ivaerksaettere-med-handicap.png",
  },
];

export function CompanyShowcase() {
  const [isHovered, setIsHovered] = useState(false);
  const router = useRouter();

  return (
    <section
      id="company"
      className="relative flex w-full cursor-pointer flex-col items-center justify-center gap-4 px-12 py-8 md:px-20 lg:px-32"
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      onClick={() => router.push("/sponsors")}
    >
      <p className="text-xs font-medium uppercase tracking-[0.2em] text-muted-foreground">
        Judged &amp; backed by
      </p>

      <div className="relative w-full">
        {/* Marquee container */}
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

          <div
            className="flex animate-marquee items-center gap-[var(--gap)] transition-all duration-500"
            style={{
              filter: isHovered ? "blur(6px)" : "none",
              opacity: isHovered ? 0.4 : 1,
            }}
          >
            {[...companyLogos, ...companyLogos].map((logo, i) => (
              <div
                className="flex shrink-0 items-center justify-center"
                key={`${logo.id}-${i}`}
              >
                <Image
                  src={logo.src}
                  alt={logo.name}
                  width={120}
                  height={40}
                  className="max-h-7 w-auto object-contain opacity-40"
                />
              </div>
            ))}
          </div>
        </div>

        {/* Hover overlay — outside overflow-hidden */}
        <AnimatePresence>
          {isHovered && (
            <motion.div
              initial={{ opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: 8 }}
              transition={{ duration: 0.25 }}
              className="absolute inset-0 z-20 flex items-center justify-center"
            >
              <span className="flex items-center gap-2 rounded-full border border-border bg-background/90 px-5 py-2 text-sm font-medium text-primary shadow-sm backdrop-blur-sm">
                See our sponsors
                <ArrowRight className="h-3.5 w-3.5" />
              </span>
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </section>
  );
}
