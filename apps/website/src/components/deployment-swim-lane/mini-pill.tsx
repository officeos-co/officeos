"use client";

import Image from "next/image";
import { motion, AnimatePresence } from "motion/react";
import { logos, type FleetAgent } from "../deployment-fleet-data";

const ACTIVITY_STEP = 4500;

export function MiniPill({
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
	// Offset spread across a full activity cycle so first transitions are desynchronized
	const offset = ((seed * 7919 + 104729) % 2147483647) % (ACTIVITY_STEP * Math.max(agent.activity.length, 1));
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
