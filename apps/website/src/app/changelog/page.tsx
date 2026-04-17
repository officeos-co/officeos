import { Navbar } from "@/components/sections/navbar";
import { FooterSection } from "@/components/sections/footer-section";
import { getChangelogEntries } from "@/lib/changelog";
import { ChangelogTimeline } from "@/components/changelog-timeline";

export const metadata = {
  title: "Changelog — OfficeOS",
  description: "Latest updates and releases from OfficeOS.",
};

export default function ChangelogPage() {
  const entries = getChangelogEntries();

  return (
    <div className="relative mx-auto max-w-7xl border-x">
      <div className="absolute top-0 left-6 z-10 block h-full w-px border-border border-l" />
      <div className="absolute top-0 right-6 z-10 block h-full w-px border-border border-r" />
      <Navbar />

      <main className="flex min-h-screen w-full flex-col items-center">
        <div className="mx-auto max-w-4xl w-full px-6 pt-20 pb-28 md:pt-28">
          <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl lg:text-6xl">
            Changelog
          </h1>
          <p className="mt-4 text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed text-center">
            New updates and improvements to OfficeOS.
          </p>

          <div className="mt-20">
            <ChangelogTimeline entries={entries} />
          </div>
        </div>
      </main>

      <FooterSection />
    </div>
  );
}
