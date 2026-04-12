"use client";

import { motion } from "motion/react";
import { integrationConfig } from "@/lib/config";

export function Integrations() {
  return (
    <section className="border-y border-border/40 py-24">
      <div className="mx-auto max-w-6xl px-6">
        <motion.h2
          initial={{ opacity: 0, y: 16 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5 }}
          className="mb-12 text-center text-3xl font-bold tracking-tight lg:text-4xl"
        >
          {integrationConfig.headline}
        </motion.h2>
        <motion.div
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5, delay: 0.15 }}
          className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-6"
        >
          {integrationConfig.tools.map((tool) => (
            <div
              key={tool}
              className="flex h-20 items-center justify-center rounded-xl bg-muted/50 border border-dashed border-muted-foreground/20"
            >
              <span className="text-sm text-muted-foreground">{tool}</span>
            </div>
          ))}
        </motion.div>
      </div>
    </section>
  );
}
