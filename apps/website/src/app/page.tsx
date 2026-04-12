import { Navbar } from "@/components/sections/navbar";
import { Hero } from "@/components/sections/hero";
import { SocialProof } from "@/components/sections/social-proof";
import { Product } from "@/components/sections/product";
import { Capabilities } from "@/components/sections/capabilities";
import { Integrations } from "@/components/sections/integrations";
import { Trust } from "@/components/sections/trust";
import { Cta } from "@/components/sections/cta";
import { Footer } from "@/components/sections/footer";

export default function Home() {
  return (
    <>
      <Navbar />
      <main className="flex-1">
        <Hero />
        <SocialProof />
        <Product />
        <Capabilities />
        <Integrations />
        <Trust />
        <Cta />
      </main>
      <Footer />
    </>
  );
}
