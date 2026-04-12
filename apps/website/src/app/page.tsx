import { BentoSection } from "@/components/sections/bento-section";
import { CompanyShowcase } from "@/components/sections/company-showcase";
import { CTASection } from "@/components/sections/cta-section";
import { FAQSection } from "@/components/sections/faq-section";
import { FeatureSection } from "@/components/sections/feature-section";
import { FooterSection } from "@/components/sections/footer-section";
import { GrowthSection } from "@/components/sections/growth-section";
import { HeroSection } from "@/components/sections/hero-section";
import { Navbar } from "@/components/sections/navbar";
import { TrustSection } from "@/components/sections/trust-section";
import { CalModal } from "@/components/cal-modal";

export default function Home() {
	return (
		<div className="relative mx-auto max-w-7xl border-x">
			<div className="absolute top-0 left-6 z-10 block h-full w-px border-border border-l"></div>
			<div className="absolute top-0 right-6 z-10 block h-full w-px border-border border-r"></div>
			<Navbar />
			<main className="flex min-h-screen w-full flex-col items-center justify-center divide-y divide-border">
				<HeroSection />
				<CompanyShowcase />
				<BentoSection />
				<FeatureSection />
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
