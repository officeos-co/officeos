"use client";

import Image from "next/image";
import { useEffect, useState, useMemo } from "react";
import { motion, AnimatePresence } from "motion/react";
import {
	starters,
	floodAgents,
	logos,
	type FleetAgent,
	type BootLog,
} from "./deployment-fleet-data";

/* ── Grid constants ──────────────────────────────────────── */

const COLS = 8;
const ROWS = 6;
const CELL_W = 116;
const CELL_H = 34;
const GAP = 6;
const TOTAL_CELLS = COLS * ROWS;

function seededShuffle(arr: number[], seed: number): number[] {
	const a = [...arr];
	let s = seed;
	for (let i = a.length - 1; i > 0; i--) {
		s = (s * 16807 + 0) % 2147483647;
		const j = s % (i + 1);
		[a[i], a[j]] = [a[j], a[i]];
	}
	return a;
}

const ROW_OFFSET = (CELL_W + GAP) / 2;

function cellPos(idx: number): { x: number; y: number } {
	const col = idx % COLS;
	const row = Math.floor(idx / COLS);
	return {
		x: col * (CELL_W + GAP) + (row % 2 === 1 ? ROW_OFFSET : 0),
		y: row * (CELL_H + GAP),
	};
}

/* ── Timing ──────────────────────────────────────────────── */

const LOG_STEP = 320;
const BOOT_DURATION = starters[0].logs.length * LOG_STEP + 300;
const P1_END = BOOT_DURATION;
const SHRINK_STAGGER = 150;
const SHRINK_MS = 500;
const P2_END = P1_END + SHRINK_MS + (starters.length - 1) * SHRINK_STAGGER + 200;
const P3_DURATION = 400;
const P3_END = P2_END + P3_DURATION;
const FLOOD_STAGGER = 50;
const FLOOD_BOOT_MS = 350;
const FLOOD_COUNT = Math.min(floodAgents.length, TOTAL_CELLS - starters.length);
const P4_DURATION = FLOOD_COUNT * FLOOD_STAGGER + FLOOD_BOOT_MS + 800;
const P4_END = P3_END + P4_DURATION;
const HOLD_MS = 2200;
const CYCLE = P4_END + HOLD_MS;
const SCROLL_SPEED = 14;

function clamp01(v: number) {
	return Math.max(0, Math.min(1, v));
}

function easeOut(t: number) {
	return 1 - (1 - t) * (1 - t);
}

/* ── Boot card (large, shows streaming logs) ─────────────── */

function BootCardContent({ agent, elapsed }: { agent: FleetAgent; elapsed: number }) {
	const visible = Math.min(
		agent.logs.length,
		elapsed > 0 ? Math.floor(elapsed / LOG_STEP) + 1 : 0,
	);
	const booting = visible > 0 && visible < agent.logs.length;

	return (
		<div className="flex h-full flex-col p-3">
			<div className="mb-2 flex items-center gap-2">
				{visible >= agent.logs.length ? (
					<span className="relative flex h-2 w-2">
						<span className="absolute inset-0 animate-ping rounded-full bg-emerald-400/50" />
						<span className="relative h-2 w-2 rounded-full bg-emerald-500" />
					</span>
				) : booting ? (
					<span className="h-2 w-2 animate-pulse rounded-full bg-blue-400" />
				) : (
					<span className="h-2 w-2 rounded-full bg-muted-foreground/15" />
				)}
				<span className="text-[11px] font-medium text-primary">{agent.name}</span>
			</div>
			<div className="flex flex-col gap-1 overflow-hidden">
				{agent.logs.map((log, i) => {
					const iconSrc = log.icon ? logos[log.icon] : undefined;
					return (
						<motion.div
							key={i}
							initial={{ opacity: 0, y: 3 }}
							animate={i < visible ? { opacity: 1, y: 0 } : { opacity: 0, y: 3 }}
							transition={{ duration: 0.15 }}
							className="flex items-center gap-1.5"
						>
							{iconSrc ? (
								<Image src={iconSrc} alt="" width={10} height={10} className="h-2.5 w-2.5 shrink-0 opacity-50" />
							) : (
								<span className="h-2.5 w-2.5 shrink-0 text-center font-mono text-[8px] text-muted-foreground/30">›</span>
							)}
							<span className="truncate font-mono text-[9px] text-muted-foreground/50">{log.text}</span>
						</motion.div>
					);
				})}
				{booting && <span className="ml-4 inline-block h-2.5 w-0.5 animate-pulse bg-muted-foreground/25" />}
			</div>
		</div>
	);
}

/* ── Mini pill (cycles through activity logs) ────────────── */

function MiniPill({
	agent,
	online,
	elapsed,
	seed,
	frozen,
}: {
	agent: FleetAgent;
	online: boolean;
	elapsed: number;
	seed: number;
	frozen: boolean;
}) {
	const ACTIVITY_STEP = 4500;
	// Seeded pseudo-random offset so pills never sync — spread across full cycle
	const offset = ((seed * 7919 + 104729) % 2147483647) % (ACTIVITY_STEP * 3);
	const actIdx = online && !frozen && agent.activity.length > 0
		? Math.floor((elapsed + offset) / ACTIVITY_STEP) % agent.activity.length
		: 0;
	const act = online ? agent.activity[actIdx] : undefined;
	const iconSrc = act?.icon ? logos[act.icon] : undefined;

	return (
		<div className="flex h-full items-center overflow-hidden px-2">
			<div className="relative min-w-0 flex-1 overflow-hidden">
				<AnimatePresence mode="popLayout" initial={false}>
					{online && iconSrc ? (
						<motion.div
							key={actIdx}
							initial={{ opacity: 0, x: 6 }}
							animate={{ opacity: 1, x: 0 }}
							exit={{ opacity: 0, x: -6 }}
							transition={{ duration: 0.4, ease: [0.4, 0, 0.2, 1] }}
							className="flex items-center gap-1.5"
						>
							<Image
								src={iconSrc}
								alt=""
								width={10}
								height={10}
								className="h-2.5 w-2.5 shrink-0 opacity-50"
							/>
							<span className="truncate font-mono text-[8px] text-muted-foreground/50">
								{act!.text}
							</span>
						</motion.div>
					) : (
						<motion.span
							key="booting"
							initial={{ opacity: 0, x: 6 }}
							animate={{ opacity: 1, x: 0 }}
							exit={{ opacity: 0, x: -6 }}
							transition={{ duration: 0.4 }}
							className="block truncate text-[8px] font-medium text-muted-foreground/40"
						>
							booting…
						</motion.span>
					)}
				</AnimatePresence>
			</div>
		</div>
	);
}

/* ── Main component ──────────────────────────────────────── */

export function DeploymentSwimLane() {
	const [elapsed, setElapsed] = useState(0);

	const cellOrder = useMemo(() => {
		const indices = Array.from({ length: TOTAL_CELLS }, (_, i) => i);
		return seededShuffle(indices, 42);
	}, []);

	// Starters land at center of grid
	const starterCells = useMemo(() => [
		{ col: 2, row: 2 },
		{ col: 4, row: 2 },
		{ col: 3, row: 3 },
	], []);

	const starterTargets = useMemo(
		() => starterCells.map((c) => ({
			x: c.col * (CELL_W + GAP) + (c.row % 2 === 1 ? ROW_OFFSET : 0),
			y: c.row * (CELL_H + GAP),
		})),
		[starterCells],
	);

	const floodCellOrder = useMemo(() => {
		const starterIdxs = new Set(starterCells.map((c) => c.row * COLS + c.col));
		return cellOrder.filter((i) => !starterIdxs.has(i));
	}, [cellOrder, starterCells]);

	useEffect(() => {
		const start = performance.now();
		let raf = 0;
		const tick = (now: number) => {
			setElapsed((now - start) % CYCLE);
			raf = requestAnimationFrame(tick);
		};
		raf = requestAnimationFrame(tick);
		return () => cancelAnimationFrame(raf);
	}, []);

	const gridW = COLS * (CELL_W + GAP) - GAP;
	const gridH = ROWS * (CELL_H + GAP) - GAP;

	// Boot card sizing — center relative to visible tile area (not the grid)
	// The tile is roughly 700px wide; center the 3 cards in that space
	const largeW = 175;
	const largeH = 150;
	const bootGap = 8;
	const tileVisibleW = 700;
	const totalBootW = 3 * largeW + 2 * bootGap;
	const bootBaseX = (tileVisibleW - totalBootW) / 2;
	const bootBaseY = (gridH - largeH) / 2;

	// Scroll
	const scrollStartT = P1_END;
	const scrollElapsed = elapsed > scrollStartT ? (elapsed - scrollStartT) / 1000 : 0;
	const scrollX = -scrollElapsed * SCROLL_SPEED;

	// Per-starter shrink
	const starterStates = starters.map((agent, i) => {
		const shrinkStart = P1_END + i * SHRINK_STAGGER;
		const t = easeOut(clamp01((elapsed - shrinkStart) / SHRINK_MS));
		const isShrunk = t >= 1;

		const bootX = bootBaseX + i * (largeW + bootGap);
		const bootY = bootBaseY;
		const target = starterTargets[i];

		return {
			agent,
			x: bootX + t * (target.x - bootX),
			y: bootY + t * (target.y - bootY),
			w: largeW + t * (CELL_W - largeW),
			h: largeH + t * (CELL_H - largeH),
			isShrunk,
		};
	});

	// Flood
	const extrasVisible = elapsed >= P3_END
		? Math.min(FLOOD_COUNT, Math.floor((elapsed - P3_END) / FLOOD_STAGGER) + 1)
		: 0;

	return (
		<div className="relative flex size-full items-center justify-center overflow-hidden">
			{/* Horizontal fade gradients */}
			<div className="pointer-events-none absolute inset-y-0 left-0 z-10 w-20 bg-gradient-to-r from-background to-transparent" />
			<div className="pointer-events-none absolute inset-y-0 right-0 z-10 w-20 bg-gradient-to-l from-background to-transparent" />

			<div
				className="relative"
				style={{
					width: gridW,
					height: gridH,
					transform: `translateX(${scrollX}px)`,
				}}
			>
				{/* Starter agents */}
				{starterStates.map(({ agent, x, y, w, h, isShrunk }, i) => (
					<motion.div
						key={agent.name}
						className="absolute overflow-hidden rounded-lg border border-border/50 bg-white shadow-sm"
						animate={{ x, y, width: w, height: h }}
						transition={{ duration: 0.35, ease: [0.4, 0, 0.2, 1] }}
					>
						{!isShrunk ? (
							<BootCardContent agent={agent} elapsed={elapsed} />
						) : (
							<MiniPill agent={agent} online elapsed={elapsed} seed={i} frozen={elapsed < P4_END} />
						)}
					</motion.div>
				))}

				{/* Flood agents */}
				{Array.from({ length: extrasVisible }).map((_, floodIdx) => {
					const cellIdx = floodCellOrder[floodIdx % floodCellOrder.length];
					const pos = cellPos(cellIdx);
					const agent = floodAgents[floodIdx];
					const appearAt = P3_END + floodIdx * FLOOD_STAGGER;
					const online = elapsed - appearAt > FLOOD_BOOT_MS;

					return (
						<motion.div
							key={agent.name}
							className="absolute overflow-hidden rounded-lg border border-border/50 bg-white shadow-sm"
							style={{ width: CELL_W, height: CELL_H }}
							initial={{ opacity: 0, scale: 0.5, x: pos.x, y: pos.y }}
							animate={{ opacity: 1, scale: 1, x: pos.x, y: pos.y }}
							transition={{ type: "spring", stiffness: 500, damping: 28 }}
						>
							<MiniPill agent={agent} online elapsed={elapsed} seed={floodIdx} frozen={elapsed < P4_END} />
						</motion.div>
					);
				})}
			</div>
		</div>
	);
}
