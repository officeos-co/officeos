"use client";

import Image from "next/image";
import { useState, useEffect, useCallback, useRef } from "react";
import { motion, AnimatePresence } from "motion/react";
import {
  logos,
  agents,
  sidebarChannels,
  type AgentPhase,
  type LogEntry,
  type PhaseStatus,
} from "./agent-showcase-data";

/* ── Status helpers ───────────────────────────────────────── */

const statusBadge: Record<PhaseStatus, { label: string; bg: string; text: string }> = {
  idle: { label: "IDLE", bg: "bg-muted", text: "text-muted-foreground/50" },
  wake: { label: "INCOMING", bg: "bg-amber-50", text: "text-amber-600" },
  boot: { label: "BOOTING", bg: "bg-blue-50", text: "text-blue-600" },
  working: { label: "WORKING", bg: "bg-emerald-50", text: "text-emerald-600" },
};

function dotColor(status: PhaseStatus) {
  if (status === "working") return "bg-emerald-500";
  if (status === "boot") return "bg-blue-400";
  if (status === "wake") return "bg-amber-400";
  return "bg-muted-foreground/30";
}

function dotPing(status: PhaseStatus) {
  if (status === "working") return "bg-emerald-500/40";
  if (status === "boot") return "bg-blue-400/40";
  if (status === "wake") return "bg-amber-400/40";
  return null;
}

/* ── Small components ─────────────────────────────────────── */

function LiveLog({ entries, visible }: { entries: LogEntry[]; visible: number }) {
  return (
    <div className="space-y-1.5 font-mono text-xs">
      {entries.slice(0, visible).map((e, i) => (
        <motion.div
          key={`${e.time}-${i}`}
          initial={{ opacity: 0, y: 4 }}
          animate={{ opacity: 1, y: 0 }}
          className="flex items-start gap-2"
        >
          <span className="shrink-0 text-muted-foreground/50">{e.time}</span>
          {e.icon && logos[e.icon] && (
            <Image src={logos[e.icon]} alt={e.icon} width={12} height={12} className="mt-0.5 shrink-0 opacity-50" />
          )}
          <span className="text-muted-foreground">{e.text}</span>
        </motion.div>
      ))}
      <span className="inline-block h-3 w-1.5 animate-pulse bg-muted-foreground/40" />
    </div>
  );
}

function ToolPill({ name }: { name: string }) {
  const logo = logos[name];
  return (
    <span className="flex items-center gap-2 rounded-lg border border-border/60 bg-muted/30 px-3 py-1.5 text-xs font-medium text-primary">
      {logo && <Image src={logo} alt={name} width={14} height={14} className="shrink-0" />}
      {name}
    </span>
  );
}

const CheckSvg = () => (
  <svg width="8" height="8" viewBox="0 0 8 8" fill="none">
    <path d="M1.5 4L3.2 5.7L6.5 2.3" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round" />
  </svg>
);

/* ── Phase detail renderers ───────────────────────────────── */

function WakePhaseDetail({ phase }: { phase: AgentPhase }) {
  return (
    <>
      <div className="mb-4 rounded-xl bg-amber-50/50 p-3.5">
        <span className="mb-2 block text-[10px] font-semibold uppercase tracking-widest text-amber-600/60">Activating</span>
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-amber-400" />
          Connecting tools and loading context…
        </div>
      </div>
      {phase.channels && phase.channels.length > 0 && (
        <div className="rounded-xl bg-muted/40 p-3.5">
          <span className="mb-2 block text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60">Trigger</span>
          <div className="flex items-start gap-2.5">
            {logos[phase.channels[0].channel] && (
              <Image src={logos[phase.channels[0].channel]} alt={phase.channels[0].channel} width={14} height={14} className="mt-0.5 shrink-0" />
            )}
            <div>
              <p className="text-xs text-primary">{phase.channels[0].text}</p>
              <span className="text-[10px] text-muted-foreground/40">via {phase.channels[0].channel} · {phase.channels[0].time}</span>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

function BootPhaseDetail({ phase, logVisible }: { phase: AgentPhase; logVisible: number }) {
  return (
    <div className="rounded-xl bg-muted/40 p-3.5">
      <span className="mb-2 block text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60">Initializing</span>
      {phase.log && <LiveLog entries={phase.log} visible={logVisible} />}
    </div>
  );
}

function WorkingPhaseDetail({ phase, logVisible }: { phase: AgentPhase; logVisible: number }) {
  return (
    <>
      {phase.tasks && (
        <div className="mb-4 rounded-xl bg-muted/40 p-3.5">
          <span className="mb-2 block text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60">Tasks</span>
          <div className="space-y-1.5">
            {phase.tasks.map((t) => (
              <div key={t.name} className="flex items-center gap-2 text-xs">
                <span className={`flex h-3.5 w-3.5 shrink-0 items-center justify-center rounded border ${t.done ? "border-emerald-300 bg-emerald-50 text-emerald-500" : "border-border bg-white"}`}>
                  {t.done && <CheckSvg />}
                </span>
                <span className={t.done ? "text-muted-foreground/50 line-through" : "text-primary"}>{t.name}</span>
              </div>
            ))}
          </div>
        </div>
      )}
      {phase.log && (
        <div className="rounded-xl bg-muted/40 p-3.5">
          <span className="mb-2 block text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60">Live Log</span>
          <LiveLog entries={phase.log} visible={logVisible} />
        </div>
      )}
    </>
  );
}

/* ── Main showcase ────────────────────────────────────────── */

export function AgentShowcase() {
  const [selected, setSelected] = useState(0);
  const [phaseIdx, setPhaseIdx] = useState(0);
  const [logVisible, setLogVisible] = useState(0);
  const phaseTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const logTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const cycleTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const agent = agents[selected];
  const phase = agent.phases[phaseIdx] ?? agent.phases[agent.phases.length - 1];

  // Sidebar statuses: selected agent shows real phase, past agents show "working",
  // future agents show their initial phase (e.g. "boot" for Pipeline Agent)
  const sidebarStatuses: PhaseStatus[] = agents.map((_, i) => {
    if (i === selected) return phase.status;
    if (i < selected) return "working";
    return agents[i].phases[0].status;
  });

  const clearTimers = useCallback(() => {
    if (phaseTimerRef.current) clearTimeout(phaseTimerRef.current);
    if (logTimerRef.current) clearInterval(logTimerRef.current);
    if (cycleTimerRef.current) clearTimeout(cycleTimerRef.current);
  }, []);

  useEffect(() => {
    clearTimers();
    setLogVisible(0);

    const p = agents[selected].phases[phaseIdx];
    if (!p) return;

    const logEntries = p.log ?? [];
    if (logEntries.length > 0) {
      let i = 0;
      logTimerRef.current = setInterval(() => {
        i++;
        setLogVisible(i + 1);
        if (i >= logEntries.length - 1 && logTimerRef.current) {
          clearInterval(logTimerRef.current);
        }
      }, 500);
    }

    const hasNextPhase = phaseIdx < agents[selected].phases.length - 1;
    if (hasNextPhase) {
      phaseTimerRef.current = setTimeout(() => {
        setPhaseIdx((prev) => prev + 1);
      }, p.duration);
    } else {
      cycleTimerRef.current = setTimeout(() => {
        setSelected((prev) => (prev + 1) % agents.length);
        setPhaseIdx(0);
      }, p.duration);
    }

    return clearTimers;
  }, [selected, phaseIdx, clearTimers]);

  const selectAgent = useCallback((i: number) => {
    setSelected(i);
    setPhaseIdx(0);
  }, []);

  const phaseChannels = phase.channels ?? [];
  const visibleChannels =
    phase.status === "working" || phase.status === "boot"
      ? phaseChannels.slice(0, Math.max(1, Math.floor(logVisible / 2)))
      : phaseChannels;

  const badge = statusBadge[phase.status];
  const metric = phase.metric ?? agent.metric;

  return (
    <div className="relative mx-auto w-full max-w-6xl">
      <div className="overflow-hidden rounded-xl border border-border/80 bg-white shadow-2xl shadow-black/8">
        {/* Titlebar */}
        <div className="flex items-center justify-between border-b border-border/60 bg-muted/60 px-4 py-2.5">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-1.5">
              <span className="h-2.5 w-2.5 rounded-full bg-[#ff5f57]" />
              <span className="h-2.5 w-2.5 rounded-full bg-[#febc2e]" />
              <span className="h-2.5 w-2.5 rounded-full bg-[#28c840]" />
            </div>
            <nav className="flex items-center gap-0.5">
              <span className="rounded-md bg-white px-3 py-1 text-xs font-medium text-primary shadow-sm">Overview</span>
              <span className="cursor-default rounded-md px-3 py-1 text-xs text-muted-foreground/70 transition-colors hover:text-muted-foreground">Agents</span>
              <span className="cursor-default rounded-md px-3 py-1 text-xs text-muted-foreground/70 transition-colors hover:text-muted-foreground">Logs</span>
            </nav>
          </div>
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-1.5">
              <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-500" />
              <span className="text-[11px] text-muted-foreground/60">Last sync 2m ago</span>
            </div>
            <span className="flex items-center gap-1.5 text-[11px] text-muted-foreground/50">
              <span className="h-1.5 w-1.5 rounded-full bg-secondary/50" />
              Auto-cycling
            </span>
          </div>
        </div>

        {/* Body */}
        <div className="flex" style={{ aspectRatio: "16 / 9" }}>
          {/* Sidebar */}
          <div className="hidden w-52 shrink-0 border-r border-border/60 bg-muted/60 md:flex md:flex-col">
            <div className="flex-1 p-3">
              <div className="mb-3 flex items-center justify-between px-1">
                <span className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground/70">Agents</span>
                <span className="rounded-full bg-white/60 px-2 py-0.5 text-[11px] font-medium text-muted-foreground">
                  {agents.length}
                </span>
              </div>
              <div className="space-y-0.5">
                {agents.map((a, i) => {
                  const sts = sidebarStatuses[i];
                  const ping = dotPing(sts);
                  return (
                    <button
                      key={a.id}
                      type="button"
                      onClick={() => selectAgent(i)}
                      className={`flex w-full items-start gap-2 rounded-lg px-2.5 py-2 text-left transition-colors ${
                        selected === i ? "bg-white/70 shadow-sm" : "hover:bg-white/40"
                      }`}
                    >
                      <span className="relative mt-1.5 flex shrink-0">
                        <motion.span
                          key={`${a.id}-dot-${sts}`}
                          initial={{ scale: 0.5 }}
                          animate={{ scale: 1 }}
                          className={`h-2 w-2 rounded-full ${dotColor(sts)}`}
                        />
                        {ping && <span className={`absolute inset-0 h-2 w-2 animate-ping rounded-full ${ping}`} />}
                      </span>
                      <div className="min-w-0 flex-1">
                        <div className="truncate text-xs font-medium text-primary">{a.name}</div>
                        <div className={`truncate text-[10px] ${sts === "idle" || sts === "boot" ? "text-muted-foreground/40" : "text-muted-foreground/60"}`}>
                          {i === selected
                            ? (phase.statusText.length > 30 ? phase.statusText.slice(0, 30) + "…" : phase.statusText)
                            : (a.phases[0].statusText.length > 30 ? a.phases[0].statusText.slice(0, 30) + "…" : a.phases[0].statusText)}
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>

            <div className="border-t border-border/60 p-3">
              <div className="mb-2.5 px-1">
                <span className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground/70">Channels</span>
              </div>
              <div className="space-y-0.5">
                {sidebarChannels.map((ch) => (
                  <div key={ch.name} className="flex items-center gap-2 rounded-lg px-2.5 py-1.5">
                    <Image src={ch.logo} alt={ch.name} width={13} height={13} className="shrink-0 opacity-60" />
                    <span className="text-[11px] text-muted-foreground/70">{ch.name}</span>
                    <span className="ml-auto h-1.5 w-1.5 rounded-full bg-emerald-500/60" />
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Main content */}
          <div className="relative flex-1 overflow-hidden bg-muted">
            <div className="absolute inset-0 grid grid-cols-1 gap-4 p-4 md:grid-cols-12 md:gap-5 md:p-5">
              {/* Detail card */}
              <div className="overflow-hidden md:col-span-7">
                <AnimatePresence mode="wait">
                  <motion.div
                    key={`${agent.id}-${phaseIdx}`}
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -8 }}
                    transition={{ duration: 0.25 }}
                    className="rounded-2xl border border-border/60 bg-white p-5 shadow-sm"
                  >
                    <div className="mb-4 flex items-center gap-2.5">
                      <motion.span
                        key={badge.label}
                        initial={{ scale: 0.9, opacity: 0 }}
                        animate={{ scale: 1, opacity: 1 }}
                        className={`rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest ${badge.bg} ${badge.text}`}
                      >
                        {badge.label}
                      </motion.span>
                      {phase.status === "boot" && <span className="text-xs text-blue-500/60">Just deployed</span>}
                      {phase.status === "wake" && <span className="text-xs text-amber-500/60">just now</span>}
                      {phase.status === "working" && <span className="text-xs text-muted-foreground/50">2m ago</span>}
                    </div>

                    <h3 className="mb-1 text-base font-semibold text-primary">{agent.name}</h3>
                    <p className="mb-4 text-sm text-muted-foreground">{phase.statusText}</p>

                    {phase.status === "wake" && <WakePhaseDetail phase={phase} />}
                    {phase.status === "boot" && <BootPhaseDetail phase={phase} logVisible={logVisible} />}
                    {phase.status === "working" && <WorkingPhaseDetail phase={phase} logVisible={logVisible} />}
                  </motion.div>
                </AnimatePresence>
              </div>

              {/* Right column */}
              <div className="flex flex-col gap-4 overflow-hidden md:col-span-5">
                <AnimatePresence mode="wait">
                  <motion.div
                    key={`tools-${agent.id}`}
                    initial={{ opacity: 0, x: 12 }}
                    animate={{ opacity: 1, x: 0 }}
                    exit={{ opacity: 0, x: -12 }}
                    transition={{ duration: 0.2, delay: 0.05 }}
                    className="rounded-2xl border border-border/60 bg-white p-5 shadow-sm"
                  >
                    <span className="mb-3 block text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60">Tools</span>
                    <div className="flex flex-wrap gap-2">
                      {agent.tools.map((tool) => <ToolPill key={tool} name={tool} />)}
                    </div>
                    <div className="mt-4 text-[10px] text-muted-foreground/50">{agent.role}</div>
                  </motion.div>
                </AnimatePresence>

                <AnimatePresence mode="wait">
                  <motion.div
                    key={`channels-${agent.id}`}
                    initial={{ opacity: 0, x: 12 }}
                    animate={{ opacity: 1, x: 0 }}
                    exit={{ opacity: 0, x: -12 }}
                    transition={{ duration: 0.2, delay: 0.08 }}
                    className="rounded-2xl border border-border/60 bg-white p-5 shadow-sm"
                  >
                    <span className="mb-3 block text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60">Channel Activity</span>
                    <div className="space-y-2.5">
                      <AnimatePresence>
                        {visibleChannels.map((ch, i) => (
                          <motion.div
                            key={`${ch.channel}-${ch.time}-${ch.direction}`}
                            initial={{ opacity: 0, y: 6 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ duration: 0.3, delay: i * 0.05 }}
                            className="flex items-start gap-2.5"
                          >
                            {logos[ch.channel] && (
                              <Image src={logos[ch.channel]} alt={ch.channel} width={14} height={14} className="mt-0.5 shrink-0" />
                            )}
                            <div className="min-w-0 flex-1">
                              <p className="text-xs text-primary">{ch.text}</p>
                              <span className="text-[10px] text-muted-foreground/40">
                                {ch.direction === "received" ? "received" : "sent"} via {ch.channel} · {ch.time}
                              </span>
                            </div>
                            <span className={`mt-1 shrink-0 rounded-full px-1.5 py-0.5 text-[9px] font-medium ${
                              ch.direction === "received" ? "bg-blue-50 text-blue-500" : "bg-emerald-50 text-emerald-500"
                            }`}>
                              {ch.direction === "received" ? "IN" : "OUT"}
                            </span>
                          </motion.div>
                        ))}
                      </AnimatePresence>
                      {visibleChannels.length === 0 && (
                        <span className="text-xs text-muted-foreground/30">No activity yet</span>
                      )}
                    </div>
                  </motion.div>
                </AnimatePresence>

                <AnimatePresence mode="wait">
                  <motion.div
                    key={`metric-${agent.id}`}
                    initial={{ opacity: 0, x: 12 }}
                    animate={{ opacity: 1, x: 0 }}
                    exit={{ opacity: 0, x: -12 }}
                    transition={{ duration: 0.2, delay: 0.12 }}
                    className="rounded-2xl border border-border/60 bg-white p-5 shadow-sm"
                  >
                    <span className="mb-2 block text-[10px] font-semibold uppercase tracking-widest text-muted-foreground/60">{metric.label}</span>
                    <div className="flex items-baseline gap-2">
                      <AnimatePresence mode="wait">
                        <motion.span
                          key={`${metric.value}-${metric.sub}`}
                          initial={{ opacity: 0, y: 4 }}
                          animate={{ opacity: 1, y: 0 }}
                          exit={{ opacity: 0, y: -4 }}
                          transition={{ duration: 0.2 }}
                          className="text-3xl font-semibold tracking-tight text-primary"
                        >
                          {metric.value}
                        </motion.span>
                      </AnimatePresence>
                      <span className="text-xs text-muted-foreground/60">{metric.sub}</span>
                    </div>
                  </motion.div>
                </AnimatePresence>
              </div>
            </div>
          </div>
        </div>

        {/* Bottom bar */}
        <div className="flex items-center justify-between border-t border-border/60 bg-muted/60 px-4 py-2">
          <div className="flex items-center gap-4 text-[11px] text-muted-foreground/50">
            <span className="flex items-center gap-1.5">
              <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" />
              {agents.length} agents
            </span>
            <span className="hidden text-muted-foreground/30 sm:inline">·</span>
            <span className="hidden sm:inline">189 tasks completed today</span>
          </div>
          <div className="flex items-center gap-2 rounded-lg border border-border/50 bg-white/80 px-3 py-1.5">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none" className="text-muted-foreground/40">
              <path d="M8 2L9.5 6H13.5L10.5 8.5L11.5 13L8 10L4.5 13L5.5 8.5L2.5 6H6.5L8 2Z" stroke="currentColor" strokeWidth="1.2" strokeLinejoin="round" />
            </svg>
            <span className="text-[11px] text-muted-foreground/40">Ask your agents anything…</span>
          </div>
        </div>
      </div>
    </div>
  );
}
