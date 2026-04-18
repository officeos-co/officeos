"use client";

import Image from "next/image";
import { motion } from "motion/react";
import { logos, type FleetAgent } from "../deployment-fleet-data";
import { LOG_STEP } from "./timing";

export function BootCardContent({
	agent,
	elapsed,
	delay = 0,
	logStep = LOG_STEP,
}: {
	agent: FleetAgent;
	elapsed: number;
	/** ms before this card starts streaming logs */
	delay?: number;
	/** ms between each log line (varies per card) */
	logStep?: number;
}) {
	const adjusted = elapsed - delay;
	const visible = Math.min(
		agent.logs.length,
		adjusted > 0 ? Math.floor(adjusted / logStep) + 1 : 0,
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
