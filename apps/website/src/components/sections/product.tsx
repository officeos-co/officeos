"use client";

import { motion } from "motion/react";
import { Placeholder } from "@/components/placeholder";
import { productConfig } from "@/lib/config";

export function Product() {
  return (
    <section className="py-24">
      <div className="mx-auto max-w-6xl px-6">
        <motion.h2
          initial={{ opacity: 0, y: 16 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5 }}
          className="mb-20 text-center text-3xl font-bold tracking-tight lg:text-4xl"
        >
          {productConfig.headline}
        </motion.h2>

        <div className="flex flex-col gap-24">
          {productConfig.sections.map((section, i) => {
            const reversed = i % 2 === 1;
            return (
              <motion.div
                key={section.title}
                initial={{ opacity: 0, y: 24 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-100px" }}
                transition={{
                  duration: 0.5,
                  delay: 0.1,
                  type: "spring",
                  stiffness: 100,
                }}
                className={`grid items-center gap-12 lg:grid-cols-2 ${
                  reversed ? "lg:direction-rtl" : ""
                }`}
              >
                <div className={`flex flex-col gap-4 ${reversed ? "lg:order-2" : ""}`}>
                  <h3 className="text-2xl font-semibold tracking-tight">
                    {section.title}
                  </h3>
                  <p className="max-w-prose text-muted-foreground">
                    {section.description}
                  </p>
                </div>
                <div className={reversed ? "lg:order-1" : ""}>
                  <Placeholder
                    label={section.visualLabel}
                    className="aspect-video w-full"
                  />
                </div>
              </motion.div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
