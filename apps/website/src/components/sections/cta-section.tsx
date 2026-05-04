import Image from "next/image";
import Link from "next/link";
import { getSiteConfig } from "@/lib/site";
import { ChevronRight } from "lucide-react";

export function CTASection() {
  const siteConfig = getSiteConfig();

  return (
    <section
      id="cta"
      className="relative z-20 flex w-full flex-col items-center justify-center"
    >
      <div className="w-full">
        <div className="flex h-[500px] w-full flex-col items-center justify-center overflow-hidden rounded-xl border border-border bg-muted/30 shadow-xl md:h-[480px]">
          <div className="flex flex-col items-center justify-center">
            <div className="mb-5 flex h-32 w-32 items-center justify-center rounded-3xl border border-border bg-background/75 shadow-2xl backdrop-blur-lg">
              <Image src="/logo.svg" alt="OfficeOS" width={100} height={100} />
            </div>

            <h2 className="text-center font-semibold text-2xl tracking-tight text-primary md:text-3xl">
              Ready to deploy your first agent?
            </h2>
            <p className="mt-2 text-center text-sm text-muted-foreground">
              Deploy the self-hosted stack from GitHub with your own API keys.
            </p>

            <Link
              href={siteConfig.links.github}
              target="_blank"
              rel="noopener noreferrer"
              className="btn-glow mt-5 flex h-10 items-center gap-1.5 rounded-full bg-secondary px-5 text-sm font-medium text-white tracking-wide transition-all hover:bg-secondary/80 active:scale-95"
            >
              Start Now
              <ChevronRight className="h-4 w-4" />
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
