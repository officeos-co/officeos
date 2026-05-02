"use client";

import { motion, useInView } from "motion/react";
import { useRef } from "react";
import { Lock } from "lucide-react";

const credentials = [
  { label: "OpenAI API Key", key: "sk-proj-••••••••3xK" },
  { label: "Anthropic API Key", key: "sk-ant-••••••••9mR" },
  { label: "Google OAuth Token", key: "ya29.••••••••7jQ" },
  { label: "Telegram Bot Token", key: "bot••••••••4wP" },
];

export function CredentialsAnimation() {
  const ref = useRef(null);
  const isInView = useInView(ref, { once: false });

  return (
    <div
      ref={ref}
      className="flex h-full w-full flex-col items-center justify-center gap-2 p-4"
    >
      <div className="pointer-events-none absolute bottom-0 left-0 z-20 h-20 w-full bg-gradient-to-t from-background to-transparent" />
      <div className="w-full max-w-xs flex flex-col gap-2">
        {credentials.map((cred, i) => (
          <motion.div
            key={cred.label}
            initial={{ opacity: 0, y: 12 }}
            animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 12 }}
            transition={{ duration: 0.3, delay: i * 0.1 }}
            className="flex items-center gap-3 rounded-lg border border-border bg-accent/50 px-3 py-2.5"
          >
            <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-secondary/10">
              <Lock className="h-3.5 w-3.5 text-secondary" />
            </div>
            <div className="flex flex-col min-w-0">
              <span className="text-xs font-medium text-primary">
                {cred.label}
              </span>
              <span className="text-[10px] font-mono text-muted-foreground truncate">
                {cred.key}
              </span>
            </div>
            <motion.div
              initial={{ scale: 0 }}
              animate={isInView ? { scale: 1 } : { scale: 0 }}
              transition={{ duration: 0.2, delay: 0.4 + i * 0.1 }}
              className="ml-auto flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-green-500/20"
            >
              <div className="h-1.5 w-1.5 rounded-full bg-green-500" />
            </motion.div>
          </motion.div>
        ))}
      </div>
    </div>
  );
}
