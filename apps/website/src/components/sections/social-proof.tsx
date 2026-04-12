"use client";

import { motion } from "motion/react";
import { socialProofConfig } from "@/lib/config";

export function SocialProof() {
  return (
    <section className="border-y border-border/40 py-12">
      <div className="mx-auto max-w-6xl px-6">
        <motion.div
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5 }}
          className="flex flex-col items-center gap-8"
        >
          <p className="text-sm text-muted-foreground">
            {socialProofConfig.headline}
          </p>
          <div className="flex flex-wrap items-center justify-center gap-6">
            {socialProofConfig.logos.map((name) => (
              <div
                key={name}
                className="flex h-10 w-28 items-center justify-center rounded-md bg-muted/50 border border-dashed border-muted-foreground/20"
              >
                <span className="text-xs text-muted-foreground">{name}</span>
              </div>
            ))}
          </div>
        </motion.div>
      </div>
    </section>
  );
}
