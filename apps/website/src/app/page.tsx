import { BentoSection } from "@/components/sections/bento-section";
import { CompanyShowcase } from "@/components/sections/company-showcase";
import { CTASection } from "@/components/sections/cta-section";
import { FAQSection } from "@/components/sections/faq-section";
import { FooterSection } from "@/components/sections/footer-section";
import { GrowthSection } from "@/components/sections/growth-section";
import { QuoteSection } from "@/components/sections/quote-section";
import { HeroSection } from "@/components/sections/hero-section";
import { Navbar } from "@/components/sections/navbar";
import { TrustSection } from "@/components/sections/trust-section";
import { siteConfig } from "@/lib/site";
import { CalModal } from "@/components/cal-modal";

const organizationJsonLd = {
  "@context": "https://schema.org",
  "@type": "Organization",
  name: "OfficeOS",
  legalName: "OfficeOS",
  url: siteConfig.url,
  description:
    "AI agent platform that deploys autonomous agents across your company with enterprise knowledge, custom skills, and full infrastructure control.",
  foundingDate: "2025",
  address: {
    "@type": "PostalAddress",
    addressLocality: "Hamburg",
    addressCountry: "DE",
  },
  sameAs: ["https://github.com/officeos"],
};

const webSiteJsonLd = {
  "@context": "https://schema.org",
  "@type": "WebSite",
  name: "OfficeOS",
  url: siteConfig.url,
};

const softwareJsonLd = {
  "@context": "https://schema.org",
  "@type": "SoftwareApplication",
  name: "OfficeOS",
  applicationCategory: "BusinessApplication",
  operatingSystem: "Kubernetes",
  description:
    "Deploy autonomous AI agents across your company. Enterprise knowledge graph, custom skills SDK, self-hosted Kubernetes runtime, and full credential isolation.",
  url: siteConfig.url,
  offers: {
    "@type": "Offer",
    price: "0",
    priceCurrency: "USD",
    description: "Free tier includes 3 agents. Bring your own API keys.",
  },
  featureList: [
    "Enterprise Knowledge Graph",
    "Custom Skills SDK (TypeScript)",
    "Channel Integration (Slack, Email, WhatsApp, Teams)",
    "Cron Jobs & Scheduling",
    "Central Credential Management",
    "One-Click Deployment",
    "Rust-based Runtime",
    "Self-Hosted Kubernetes",
    "Sandboxed Execution",
    "Full Audit Trail",
  ],
};

export default function Home() {
  return (
    <div className="relative mx-auto max-w-7xl border-x">
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(organizationJsonLd),
        }}
      />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(webSiteJsonLd),
        }}
      />
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(softwareJsonLd),
        }}
      />
      <div className="absolute top-0 left-6 z-10 block h-full w-px border-border border-l"></div>
      <div className="absolute top-0 right-6 z-10 block h-full w-px border-border border-r"></div>
      <Navbar />
      <main className="flex min-h-screen w-full flex-col items-center justify-center divide-y divide-border">
        <HeroSection />
        <CompanyShowcase />
        <BentoSection />
        <QuoteSection />
        <GrowthSection />
        <TrustSection />
        <FAQSection />
        <CTASection />
        <FooterSection />
      </main>
      <CalModal />
    </div>
  );
}
