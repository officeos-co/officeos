"use client";

import { motion, AnimatePresence, useInView } from "motion/react";
import { useEffect, useRef, useState } from "react";

interface ScheduledTask {
  name: string;
  schedule: string;
  nextIn: string;
}

const tasks: ScheduledTask[] = [
  { name: "Competitor scan", schedule: "Every day 7am", nextIn: "2h 14m" },
  { name: "CRM sync", schedule: "Every 2 hours", nextIn: "47m" },
  { name: "Weekly report", schedule: "Fri 5pm", nextIn: "3d 8h" },
];

export function CronJobsAnimation({
  once = false,
}: {
  startAnimationDelay?: number;
  once?: boolean;
}) {
  const ref = useRef(null);
  const isInView = useInView(ref, { once });
  const [firingIdx, setFiringIdx] = useState<number | null>(null);

  useEffect(() => {
    if (!isInView) {
      return;
    }

    // Continuously cycle through tasks "firing"
    const sequence = [
      { idx: 1, delay: 1600 },
      { idx: null, delay: 1400 },
      { idx: 0, delay: 1600 },
      { idx: null, delay: 1400 },
      { idx: 2, delay: 1600 },
      { idx: null, delay: 1400 },
    ];
    let elapsed = 0;
    const timeouts: ReturnType<typeof setTimeout>[] = [];

    const runCycle = () => {
      elapsed = 0;
      for (let i = 0; i < sequence.length; i++) {
        elapsed += sequence[i].delay;
        const s = sequence[i];
        timeouts.push(
          setTimeout(() => setFiringIdx(s.idx as number | null), elapsed),
        );
      }
    };

    // Start first cycle after a short initial delay
    const initial = setTimeout(() => {
      runCycle();
      // Repeat the full cycle continuously
      const cycleLength = sequence.reduce((sum, s) => sum + s.delay, 0);
      const interval = setInterval(runCycle, cycleLength);
      timeouts.push(interval as unknown as ReturnType<typeof setTimeout>);
    }, 2400);
    timeouts.push(initial);

    return () => {
      timeouts.forEach((t) => clearTimeout(t));
    };
  }, [isInView]);

  const activeFiringIdx = isInView ? firingIdx : null;

  return (
    <div
      ref={ref}
      className="flex h-full w-full flex-col items-center justify-center p-4"
    >
      <div className="pointer-events-none absolute bottom-0 left-0 z-20 h-20 w-full bg-gradient-to-t from-background to-transparent" />

      <div className="w-full max-w-xs flex flex-col gap-2.5">
        <AnimatePresence>
          {tasks.map((task, i) => {
            const isFiring = activeFiringIdx === i;
            return (
              <motion.div
                key={task.name}
                initial={{ opacity: 0, y: 12 }}
                animate={
                  isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 12 }
                }
                transition={{ duration: 0.3, delay: i * 0.12 }}
                className={`flex items-center gap-3 rounded-xl border px-3.5 py-3 transition-colors duration-300 ${
                  isFiring
                    ? "border-secondary/40 bg-secondary/5"
                    : "border-border/60 bg-white"
                }`}
              >
                {/* Status dot */}
                <span className="relative flex shrink-0">
                  <span
                    className={`h-2 w-2 rounded-full transition-colors duration-300 ${
                      isFiring ? "bg-secondary" : "bg-muted-foreground/20"
                    }`}
                  />
                  {isFiring && (
                    <motion.span
                      initial={{ scale: 1, opacity: 0.6 }}
                      animate={{ scale: 2.5, opacity: 0 }}
                      transition={{
                        duration: 1,
                        repeat: Infinity,
                      }}
                      className="absolute inset-0 h-2 w-2 rounded-full bg-secondary"
                    />
                  )}
                </span>

                {/* Task info */}
                <div className="flex flex-col min-w-0 flex-1">
                  <span className="text-xs font-medium text-primary">
                    {task.name}
                  </span>
                  <span className="text-[10px] text-muted-foreground/50">
                    {task.schedule}
                  </span>
                </div>

                {/* Countdown / Running */}
                <AnimatePresence mode="wait">
                  {isFiring ? (
                    <motion.span
                      key="running"
                      initial={{ opacity: 0, scale: 0.9 }}
                      animate={{ opacity: 1, scale: 1 }}
                      exit={{ opacity: 0, scale: 0.9 }}
                      className="shrink-0 rounded-full bg-secondary/10 px-2 py-0.5 text-[10px] font-medium text-secondary"
                    >
                      Running
                    </motion.span>
                  ) : (
                    <motion.span
                      key="countdown"
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      exit={{ opacity: 0 }}
                      className="shrink-0 font-mono text-[11px] text-muted-foreground/40"
                    >
                      {task.nextIn}
                    </motion.span>
                  )}
                </AnimatePresence>
              </motion.div>
            );
          })}
        </AnimatePresence>
      </div>
    </div>
  );
}
