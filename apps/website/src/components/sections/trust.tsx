"use client";

import { motion } from "motion/react";
import { Card, CardContent } from "@/components/ui/card";
import { trustConfig } from "@/lib/config";
import { Server, Shield, Users, FileText } from "lucide-react";

const icons = [Server, Shield, Users, FileText];

export function Trust() {
  return (
    <section className="py-24">
      <div className="mx-auto max-w-6xl px-6">
        <motion.h2
          initial={{ opacity: 0, y: 16 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5 }}
          className="mb-12 text-center text-3xl font-bold tracking-tight lg:text-4xl"
        >
          {trustConfig.headline}
        </motion.h2>
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {trustConfig.points.map((point, i) => {
            const Icon = icons[i];
            return (
              <motion.div
                key={point.title}
                initial={{ opacity: 0, y: 16 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true }}
                transition={{ duration: 0.4, delay: i * 0.1 }}
              >
                <Card className="h-full border-border/40 bg-card/50">
                  <CardContent className="flex flex-col gap-3 p-6">
                    <Icon className="h-6 w-6 text-primary" />
                    <h3 className="font-semibold">{point.title}</h3>
                    <p className="text-sm text-muted-foreground">
                      {point.description}
                    </p>
                  </CardContent>
                </Card>
              </motion.div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
