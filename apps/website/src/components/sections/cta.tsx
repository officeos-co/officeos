"use client";

import { motion } from "motion/react";
import { Button } from "@/components/ui/button";
import { ctaConfig } from "@/lib/config";

export function Cta() {
  return (
    <section id="cta" className="py-24">
      <div className="mx-auto max-w-6xl px-6">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5 }}
          className="flex flex-col items-center gap-8 rounded-2xl border border-border/40 bg-card/50 p-12 text-center lg:p-16"
        >
          <h2 className="text-3xl font-bold tracking-tight lg:text-4xl">
            {ctaConfig.headline}
          </h2>
          <div className="flex gap-4">
            <Button size="lg">{ctaConfig.primaryCta}</Button>
            <Button size="lg" variant="outline">
              {ctaConfig.secondaryCta}
            </Button>
          </div>
          <p className="text-sm text-muted-foreground">
            {ctaConfig.reassurance}
          </p>
        </motion.div>
      </div>
    </section>
  );
}
