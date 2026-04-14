"use client";

import { motion, useInView } from "motion/react";
import { useRef } from "react";

const codeLines = [
  { indent: 0, text: "defineSkill", highlight: true },
  { indent: 1, text: 'name: "summarize-ticket",' },
  { indent: 1, text: "input: z.object({" },
  { indent: 2, text: "ticketId: z.string()," },
  { indent: 1, text: "})," },
  { indent: 1, text: "execute: async ({ ticketId }) => {" },
  { indent: 2, text: "const ticket = await jira.get(ticketId);" },
  { indent: 2, text: "return summarize(ticket);" },
  { indent: 1, text: "}," },
  { indent: 0, text: "});" },
];

export function FifthBentoAnimation() {
  const ref = useRef(null);
  const isInView = useInView(ref, { once: false });

  return (
    <div
      ref={ref}
      className="flex h-full w-full items-center justify-center p-4"
    >
      <div className="pointer-events-none absolute bottom-0 left-0 z-20 h-20 w-full bg-gradient-to-t from-background to-transparent" />
      <div className="w-full max-w-sm rounded-lg border border-border bg-accent/50 p-4 font-mono text-xs">
        <div className="mb-3 flex items-center gap-1.5">
          <div className="h-2.5 w-2.5 rounded-full bg-red-400/60" />
          <div className="h-2.5 w-2.5 rounded-full bg-yellow-400/60" />
          <div className="h-2.5 w-2.5 rounded-full bg-green-400/60" />
          <span className="ml-2 text-[10px] text-muted-foreground">
            skill.ts
          </span>
        </div>
        <div className="flex flex-col gap-0.5">
          {codeLines.map((line, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, x: -8 }}
              animate={isInView ? { opacity: 1, x: 0 } : { opacity: 0, x: -8 }}
              transition={{ duration: 0.2, delay: i * 0.06 }}
              className="flex"
              style={{ paddingLeft: `${line.indent * 16}px` }}
            >
              <span className="mr-3 select-none text-muted-foreground/40">
                {(i + 1).toString().padStart(2, " ")}
              </span>
              <span
                className={
                  line.highlight
                    ? "text-secondary font-semibold"
                    : "text-primary/80"
                }
              >
                {line.text}
              </span>
            </motion.div>
          ))}
        </div>
      </div>
    </div>
  );
}
