import { siteConfig } from "@/lib/site";
import { Navbar } from "@/components/sections/navbar";
import { FooterSection } from "@/components/sections/footer-section";

export const metadata = {
  title: "Support — OfficeOS",
  description: "Get help with OfficeOS. Contact our support team.",
};

export default function Support() {
  return (
    <div className="relative mx-auto max-w-7xl border-x">
      <div className="absolute top-0 left-6 z-10 block h-full w-px border-border border-l" />
      <div className="absolute top-0 right-6 z-10 block h-full w-px border-border border-r" />
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
          Support
        </h1>
        <p className="mt-4 text-sm text-muted-foreground text-center">
          We&apos;re here to help.
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              Contact us
            </h2>
            <p>
              For questions, bug reports, feature requests, or anything else
              about OfficeOS, email us at{" "}
              <a
                href="mailto:support@officeos.co"
                className="text-primary underline underline-offset-4 hover:text-primary/80"
              >
                support@officeos.co
              </a>
              . We read every message and typically respond within one business
              day.
            </p>
          </section>

          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              Other resources
            </h2>
            <ul className="space-y-3 list-disc pl-5">
              <li>
                <a
                  href={siteConfig.docsUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-primary underline underline-offset-4 hover:text-primary/80"
                >
                  Documentation
                </a>{" "}
                — guides, API references, and tutorials.
              </li>
              <li>
                <a
                  href="https://changelog.officeos.co"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-primary underline underline-offset-4 hover:text-primary/80"
                >
                  Changelog
                </a>{" "}
                — latest product updates and fixes.
              </li>
              <li>
                <a
                  href="https://status.officeos.co"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-primary underline underline-offset-4 hover:text-primary/80"
                >
                  Status
                </a>{" "}
                — real-time platform availability.
              </li>
            </ul>
          </section>
        </div>
      </main>

      <FooterSection />
    </div>
  );
}
