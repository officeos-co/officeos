"use client";

import { motion } from "motion/react";
import { Placeholder } from "@/components/placeholder";
import { Card, CardContent } from "@/components/ui/card";
import { capabilityConfig } from "@/lib/config";
import { Check, X } from "lucide-react";

export function Capabilities() {
  const { knowledgeGraph, footprint, skills } = capabilityConfig;

  return (
    <section className="py-24">
      <div className="mx-auto max-w-6xl px-6 flex flex-col gap-32">
        {/* A: Knowledge Graph */}
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5 }}
          className="flex flex-col gap-8"
        >
          <div className="max-w-2xl">
            <h2 className="text-3xl font-bold tracking-tight lg:text-4xl">
              {knowledgeGraph.title}
            </h2>
            <p className="mt-4 text-muted-foreground max-w-prose">
              {knowledgeGraph.description}
            </p>
          </div>
          <Placeholder
            label={knowledgeGraph.visualLabel}
            className="aspect-[21/9] w-full"
          />
        </motion.div>

        {/* B: Micro Footprint — Bento Grid */}
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5 }}
          className="flex flex-col gap-8"
        >
          <h2 className="text-3xl font-bold tracking-tight lg:text-4xl">
            {footprint.title}
          </h2>
          <div className="grid gap-4 sm:grid-cols-3">
            {footprint.metrics.map((metric, i) => (
              <motion.div
                key={metric.label}
                initial={{ opacity: 0, y: 16 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.4, delay: i * 0.1 }}
              >
                <Card className="border-border/40 bg-card/50">
                  <CardContent className="flex flex-col items-center justify-center p-8 text-center">
                    <span className="text-4xl font-bold text-primary font-mono">
                      {metric.value}
                    </span>
                    <span className="mt-2 text-sm text-muted-foreground">
                      {metric.label}
                    </span>
                  </CardContent>
                </Card>
              </motion.div>
            ))}
          </div>
        </motion.div>

        {/* C: Skills Comparison */}
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5 }}
          className="flex flex-col gap-8"
        >
          <h2 className="text-3xl font-bold tracking-tight lg:text-4xl max-w-2xl">
            {skills.title}
          </h2>
          <div className="grid gap-6 sm:grid-cols-2">
            {/* Generic MCP */}
            <Card className="border-border/40 bg-card/50">
              <CardContent className="p-6">
                <h3 className="mb-4 text-lg font-semibold text-muted-foreground">
                  {skills.comparison.generic.label}
                </h3>
                <ul className="space-y-3">
                  {skills.comparison.generic.points.map((point) => (
                    <li
                      key={point}
                      className="flex items-start gap-2 text-sm text-muted-foreground"
                    >
                      <X className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground/50" />
                      {point}
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>

            {/* Custom Skill */}
            <Card className="border-primary/30 bg-card/50">
              <CardContent className="p-6">
                <h3 className="mb-4 text-lg font-semibold text-primary">
                  {skills.comparison.custom.label}
                </h3>
                <ul className="space-y-3">
                  {skills.comparison.custom.points.map((point) => (
                    <li
                      key={point}
                      className="flex items-start gap-2 text-sm text-foreground"
                    >
                      <Check className="mt-0.5 h-4 w-4 shrink-0 text-primary" />
                      {point}
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
