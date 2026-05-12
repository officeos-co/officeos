"use client";

import { motion, AnimatePresence, useInView } from "motion/react";
import { useEffect, useRef, useState } from "react";

type Phase = "idle" | "booting" | "live";

const phaseConfig: Record<Phase, { label: string; dot: string; ping: string | null; bg: string; text: string }> = {
	idle: { label: "DEPLOY", dot: "bg-muted-foreground/30", ping: null, bg: "bg-muted", text: "text-muted-foreground" },
	booting: { label: "BOOTING", dot: "bg-blue-400", ping: "bg-blue-400/40", bg: "bg-blue-50", text: "text-blue-600" },
	live: { label: "LIVE", dot: "bg-emerald-500", ping: "bg-emerald-500/40", bg: "bg-emerald-50", text: "text-emerald-600" },
};

const bootSteps = [
	"Loading knowledge graph…",
	"Connecting tools…",
	"Agent ready",
];

export function FirstBentoAnimation() {
	const ref = useRef(null);
	const isInView = useInView(ref);
	const [phase, setPhase] = useState<Phase>("idle");
	const [bootStep, setBootStep] = useState(0);

	useEffect(() => {
		if (!isInView) {
			return;
		}

		const reset = setTimeout(() => {
			setPhase("idle");
			setBootStep(0);
		}, 0);
		const t1 = setTimeout(() => setPhase("booting"), 800);
		const t2 = setTimeout(() => setBootStep(1), 1400);
		const t3 = setTimeout(() => setBootStep(2), 2000);
		const t4 = setTimeout(() => setPhase("live"), 2400);

		return () => {
			clearTimeout(reset);
			clearTimeout(t1);
			clearTimeout(t2);
			clearTimeout(t3);
			clearTimeout(t4);
		};
	}, [isInView]);

	const visiblePhase = isInView ? phase : "idle";
	const visibleBootStep = isInView ? bootStep : 0;
	const cfg = phaseConfig[visiblePhase];

	return (
		<div
			ref={ref}
			className="flex h-full w-full items-center justify-center p-4"
		>
			<div className="pointer-events-none absolute bottom-0 left-0 z-20 h-20 w-full bg-gradient-to-t from-background to-transparent" />

			<div className="w-full max-w-xs rounded-xl border border-border/60 bg-white p-4 shadow-sm">
				{/* Header */}
				<div className="mb-3 flex items-center justify-between">
					<div className="flex items-center gap-2">
						<span className="relative flex">
							<motion.span
								key={cfg.dot}
								initial={{ scale: 0.5 }}
								animate={{ scale: 1 }}
								className={`h-2 w-2 rounded-full ${cfg.dot}`}
							/>
							{cfg.ping && (
								<span
									className={`absolute inset-0 h-2 w-2 animate-ping rounded-full ${cfg.ping}`}
								/>
							)}
						</span>
						<span className="text-xs font-semibold text-primary">
							Sales Research Agent
						</span>
					</div>
					<motion.span
						key={cfg.label}
						initial={{ scale: 0.9, opacity: 0 }}
						animate={{ scale: 1, opacity: 1 }}
						className={`rounded-full px-2 py-0.5 text-[9px] font-semibold uppercase tracking-widest ${cfg.bg} ${cfg.text}`}
					>
						{cfg.label}
					</motion.span>
				</div>

				{/* Info */}
				<div className="mb-3 space-y-1.5 text-[11px] text-muted-foreground">
					<div className="flex justify-between">
						<span>Team</span>
						<span className="text-primary">Sales</span>
					</div>
					<div className="flex justify-between">
						<span>Skills</span>
						<span className="text-primary">CRM, LinkedIn, Notion</span>
					</div>
				</div>

				{/* Boot log / status */}
				<div className="rounded-lg bg-muted/40 px-3 py-2.5">
					<AnimatePresence mode="wait">
						{visiblePhase === "idle" && (
							<motion.div
								key="idle"
								initial={{ opacity: 0 }}
								animate={{ opacity: 1 }}
								exit={{ opacity: 0 }}
								className="text-[10px] text-muted-foreground/50"
							>
								Ready to deploy
							</motion.div>
						)}
						{visiblePhase === "booting" && (
							<motion.div
								key="booting"
								initial={{ opacity: 0 }}
								animate={{ opacity: 1 }}
								exit={{ opacity: 0 }}
								className="space-y-1"
							>
								{bootSteps.slice(0, visibleBootStep + 1).map((step, i) => (
									<motion.div
										key={step}
										initial={{ opacity: 0, y: 4 }}
										animate={{ opacity: 1, y: 0 }}
										className="flex items-center gap-1.5 text-[10px]"
									>
										{i < visibleBootStep ? (
											<span className="text-emerald-500">✓</span>
										) : (
											<span className="h-1.5 w-1.5 animate-pulse rounded-full bg-blue-400" />
										)}
										<span className="text-muted-foreground">{step}</span>
									</motion.div>
								))}
							</motion.div>
						)}
						{visiblePhase === "live" && (
							<motion.div
								key="live"
								initial={{ opacity: 0 }}
								animate={{ opacity: 1 }}
								className="space-y-1"
							>
								<div className="flex items-center gap-1.5 text-[10px] text-emerald-600">
									<span>✓</span>
									<span className="font-medium">
										Live in 4.2s
									</span>
								</div>
								<div className="text-[10px] text-muted-foreground/50">
									Preparing briefing for 2pm call…
								</div>
							</motion.div>
						)}
					</AnimatePresence>
				</div>
			</div>
		</div>
	);
}
