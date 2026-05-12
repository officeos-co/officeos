"use client";

import { ChevronRightIcon } from "@radix-ui/react-icons";
import Image from "next/image";
import Link from "next/link";
import { FlickeringGrid } from "@/components/ui/flickering-grid";
import { getSiteConfig } from "@/lib/site";
import { useMediaQuery } from "@/hooks/use-media-query";

const footerLinks = [
  {
    title: "Explore",
    links: [
      { id: 6, title: "Pricing", url: "/pricing" },
      { id: 18, title: "Support", url: "/support" },
      { id: 16, title: "Docs", url: getSiteConfig().docsUrl },
    ],
  },
  {
    title: "Company",
    links: [
      { id: 12, title: "About", url: "/about" },
      { id: 17, title: "Network", url: "/sponsors" },
      { id: 14, title: "Privacy", url: "/privacy" },
      { id: 15, title: "Terms", url: "/terms" },
    ],
  },
];

const socialLinks = {
  github: "https://github.com/officeos-co/officeos",
  linkedin: "https://www.linkedin.com/in/harro-krog-b948ab31a/",
};

export function FooterSection() {
  const tablet = useMediaQuery("(max-width: 1024px)");

  return (
    <footer id="footer" className="w-full pb-0">
      <div className="flex flex-col p-10 md:flex-row md:items-start md:justify-between">
        <div className="mx-0 flex max-w-xs flex-col items-start justify-start gap-y-5">
          <Link href="/" className="flex items-center gap-2">
            <Image
              src="/logo.svg"
              alt="OfficeOS"
              width={20}
              height={20}
              className="h-[1.125rem] w-[1.125rem]"
            />
            <p className="font-semibold text-primary text-xl">OfficeOS</p>
          </Link>
          <p className="font-medium text-muted-foreground tracking-tight">
            The intelligence layer for AI agents.
          </p>
          <div className="flex items-center gap-4 text-muted-foreground">
            <a
              href={socialLinks.github}
              target="_blank"
              rel="noopener noreferrer"
              className="hover:text-primary transition-colors"
            >
              GitHub
            </a>
            <a
              href={socialLinks.linkedin}
              target="_blank"
              rel="noopener noreferrer"
              className="hover:text-primary transition-colors"
            >
              LinkedIn
            </a>
          </div>
        </div>
        <div className="pt-5 md:ml-auto">
          <div className="flex flex-col items-start justify-start gap-y-5 md:flex-row md:items-start md:justify-end md:gap-x-16 lg:gap-x-20">
            {footerLinks.map((column, columnIndex) => (
              <ul key={columnIndex} className="flex flex-col gap-y-2">
                <li className="mb-2 font-semibold text-primary text-sm">
                  {column.title}
                </li>
                {column.links.map((link) => {
                  const isExternal = link.url.startsWith("http");
                  return (
                    <li
                      key={link.id}
                      className="group inline-flex cursor-pointer items-center justify-start gap-1 text-[15px]/snug text-muted-foreground"
                    >
                      {isExternal ? (
                        <a
                          href={link.url}
                          target="_blank"
                          rel="noopener noreferrer"
                        >
                          {link.title}
                        </a>
                      ) : (
                        <Link href={link.url}>{link.title}</Link>
                      )}
                      <div className="flex size-4 translate-x-0 transform items-center justify-center rounded border border-border opacity-0 transition-all duration-300 ease-out group-hover:translate-x-1 group-hover:opacity-100">
                        <ChevronRightIcon className="h-4 w-4" />
                      </div>
                    </li>
                  );
                })}
              </ul>
            ))}
          </div>
        </div>
      </div>
      <div className="flex items-center justify-end px-10 pb-6">
        <span className="text-xs text-muted-foreground">
          Made in Hamburg — &copy; 2026 OfficeOS
        </span>
      </div>
      <div className="relative z-0 mt-8 h-48 w-full md:h-64">
        <div className="absolute inset-0 z-10 bg-linear-to-t from-40% from-transparent to-background" />
        <div className="absolute inset-0 mx-6">
          <FlickeringGrid
            text={tablet ? "OfficeOS" : "OfficeOS"}
            fontSize={tablet ? 70 : 90}
            className="h-full w-full"
            squareSize={2}
            gridGap={tablet ? 2 : 3}
            color="var(--muted-foreground)"
            maxOpacity={0.3}
            flickerChance={0.1}
          />
        </div>
      </div>
    </footer>
  );
}
