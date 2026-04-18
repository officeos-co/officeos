"use client";

import { useEffect, useState, useMemo } from "react";
import { motion } from "motion/react";
import { starters, floodAgents } from "../deployment-fleet-data";
import { ROWS, CELL_W, CELL_H, GAP, cellPos, seededShuffle } from "./grid";
import {
	PHASE, STARTER_BOOT, STARTER_TIMING, LOG_STEP, SHRINK_MS,
	FLOOD_STAGGER, FLOOD_BOOT_MS, FLOOD_DURATION, SCROLL_SPEED, CYCLE,
	clamp01, easeOut,
} from "./timing";
import { BootCardContent } from "./boot-card-content";
import { MiniPill } from "./mini-pill";

const ROW_OFFSET = (CELL_W + GAP) / 2;

/** How many flood slots can appear during the flood duration */
const MAX_FLOOD_SLOTS = Math.floor(FLOOD_DURATION / FLOOD_STAGGER) + 1;

/** Columns needed to lay out starters + all flood slots across ROWS */
const INITIAL_COLS = 8;
const COLS = Math.max(INITIAL_COLS, Math.ceil((starters.length + MAX_FLOOD_SLOTS) / ROWS));
const TOTAL_CELLS = COLS * ROWS;

export function DeploymentSwimLane() {
	const [elapsed, setElapsed] = useState(0);

	const cellOrder = useMemo(() => {
		const indices = Array.from({ length: TOTAL_CELLS }, (_, i) => i);
		return seededShuffle(indices, 42);
	}, []);

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

	const largeW = 175;
	const largeH = 150;
	const bootGap = 8;
	const tileVisibleW = 700;
	const totalBootW = 3 * largeW + 2 * bootGap;
	const bootBaseX = (tileVisibleW - totalBootW) / 2;
	const bootBaseY = (gridH - largeH) / 2;

	// Scroll starts when the first starter finishes booting
	const firstBootDone = Math.min(...STARTER_TIMING.map((t) => t.bootDone));
	const scrollElapsed = elapsed > firstBootDone ? (elapsed - firstBootDone) / 1000 : 0;
	const scrollX = -scrollElapsed * SCROLL_SPEED;

	const starterStates = starters.map((agent, i) => {
		// Each card shrinks a constant delay after *its own* boot finishes
		const t = easeOut(clamp01((elapsed - STARTER_TIMING[i].shrinkStart) / SHRINK_MS));
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

	// Number of flood slots visible so far (time-based, not agent-count-based)
	const extrasVisible = elapsed >= PHASE.flood.start
		? Math.min(MAX_FLOOD_SLOTS, Math.floor((elapsed - PHASE.flood.start) / FLOOD_STAGGER) + 1)
		: 0;

	return (
		<div className="relative flex size-full items-center justify-center overflow-hidden">
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
				{starterStates.map(({ agent, x, y, w, h, isShrunk }, i) => (
					<motion.div
						key={agent.name}
						className="absolute overflow-hidden rounded-lg border border-border/50 bg-white shadow-sm"
						animate={{ x, y, width: w, height: h }}
						transition={{ duration: 0.35, ease: [0.4, 0, 0.2, 1] }}
					>
						{!isShrunk ? (
							<BootCardContent
								agent={agent}
								elapsed={elapsed}
								delay={STARTER_BOOT[i]?.delay ?? 0}
								logStep={STARTER_BOOT[i]?.logStep ?? LOG_STEP}
							/>
						) : (
							<MiniPill agent={agent} online elapsed={elapsed} seed={i} frozen={elapsed < PHASE.flood.end} />
						)}
					</motion.div>
				))}

				{Array.from({ length: extrasVisible }).map((_, floodIdx) => {
					const cellIdx = floodCellOrder[floodIdx % floodCellOrder.length];
					const pos = cellPos(cellIdx, COLS);
					// Recycle agents — wrap around the floodAgents array
					const agent = floodAgents[floodIdx % floodAgents.length];
					const appearAt = PHASE.flood.start + floodIdx * FLOOD_STAGGER;
					const online = elapsed - appearAt > FLOOD_BOOT_MS;

					return (
						<motion.div
							key={`flood-${floodIdx}`}
							className="absolute overflow-hidden rounded-lg border border-border/50 bg-white shadow-sm"
							style={{ width: CELL_W, height: CELL_H }}
							initial={{ opacity: 0, scale: 0.5, x: pos.x, y: pos.y }}
							animate={{ opacity: 1, scale: 1, x: pos.x, y: pos.y }}
							transition={{ type: "spring", stiffness: 500, damping: 28 }}
						>
							<MiniPill agent={agent} online={online} elapsed={elapsed} seed={floodIdx} frozen={elapsed < PHASE.flood.end} />
						</motion.div>
					);
				})}
			</div>
		</div>
	);
}
