"use client";

import { motion } from "motion/react";
import { Button } from "@/components/ui/button";
import { Placeholder } from "@/components/placeholder";
import { heroConfig } from "@/lib/config";

export function Hero() {
  return (
    <section className="relative overflow-hidden py-24 lg:py-32">
      <div className="mx-auto max-w-6xl px-6">
        <div className="grid gap-12 lg:grid-cols-2 lg:items-center">
          <motion.div
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, ease: "easeOut" }}
            className="flex flex-col gap-6"
          >
            <h1 className="text-4xl font-bold leading-tight tracking-tight lg:text-5xl">
              {heroConfig.headline}
            </h1>
            <p className="max-w-prose text-lg text-muted-foreground">
              {heroConfig.subtitle}
            </p>
            <div className="flex gap-3">
              <Button size="lg">{heroConfig.primaryCta}</Button>
              <Button size="lg" variant="outline">
                {heroConfig.secondaryCta}
              </Button>
            </div>
          </motion.div>
          <motion.div
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.2, ease: "easeOut" }}
          >
            <Placeholder
              label={heroConfig.visualLabel}
              className="aspect-video w-full"
            />
          </motion.div>
        </div>
      </div>
    </section>
  );
}
