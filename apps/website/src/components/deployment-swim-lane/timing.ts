/* ── Timing ───────────────────────────────────────────────── */

import { starters } from "../deployment-fleet-data";

/** Base ms between each boot log line */
export const LOG_STEP = 320;

/** Per-starter random delay (ms) before logs start, and per-card log speed */
export const STARTER_BOOT: { delay: number; logStep: number }[] = [
  { delay: 0, logStep: 280 },
  { delay: 400, logStep: 350 },
  { delay: 180, logStep: 310 },
];

export const SHRINK_MS = 500;

/** Constant delay after a starter finishes its logs before it shrinks to a pill */
const SHRINK_AFTER_BOOT = 200;

/** Per-starter: when boot logs finish, and when shrink starts */
export const STARTER_TIMING = starters.map((s, i) => {
  const { delay, logStep } = STARTER_BOOT[i] ?? { delay: 0, logStep: LOG_STEP };
  const bootDone = delay + s.logs.length * logStep;
  const shrinkStart = bootDone + SHRINK_AFTER_BOOT;
  return { bootDone, shrinkStart };
});

/** ms between each flood agent appearing */
export const FLOOD_STAGGER = 50;
/** ms a flood agent takes to go from "booting" to online */
export const FLOOD_BOOT_MS = 350;

/** How long the flood phase lasts (agents keep spawning for this long) */
export const FLOOD_DURATION = 4000;

/** Hold time after flood ends before cycle resets */
export const HOLD_MS = 2000;

export const SCROLL_SPEED = 14;

/*
 * Phase timeline — derived once, each phase starts where the previous ended.
 * Shrink end = when the last starter finishes shrinking
 */
const shrinkEnd =
  Math.max(...STARTER_TIMING.map((t) => t.shrinkStart)) + SHRINK_MS + 200;
const floodStart = shrinkEnd + 400;
const floodEnd = floodStart + FLOOD_DURATION;

/** End of each phase — used to gate animation stages */
export const PHASE = {
  shrink: shrinkEnd,
  flood: { start: floodStart, end: floodEnd },
} as const;

/** Total cycle duration */
export const CYCLE = floodEnd + HOLD_MS;

/* ── Easing helpers ──────────────────────────────────────── */

export function clamp01(v: number) {
  return Math.max(0, Math.min(1, v));
}

export function easeOut(t: number) {
  return 1 - (1 - t) * (1 - t);
}
