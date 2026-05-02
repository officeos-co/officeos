"use client";

import { motion, useInView } from "motion/react";
import { useEffect, useRef, useState } from "react";

interface Node {
	id: string;
	x: number;
	y: number;
	z: number;
	label: string;
	size: number;
}

interface Edge {
	from: string;
	to: string;
}

const nodes: Node[] = [
	{ id: "a", x: 0, y: 0, z: 40, label: "Company", size: 14 },
	{ id: "b", x: -120, y: -60, z: 20, label: "Contracts", size: 10 },
	{ id: "c", x: 110, y: -70, z: -10, label: "Catalog", size: 10 },
	{ id: "d", x: -80, y: 80, z: -20, label: "People", size: 10 },
	{ id: "e", x: 100, y: 70, z: 30, label: "Clients", size: 10 },
	{ id: "f", x: -160, y: 10, z: -30, label: "Legal", size: 8 },
	{ id: "g", x: 160, y: 0, z: 10, label: "Sales", size: 8 },
	{ id: "h", x: -40, y: -110, z: -15, label: "Docs", size: 8 },
	{ id: "i", x: 40, y: 110, z: -25, label: "Support", size: 8 },
	{ id: "j", x: -130, y: -110, z: 10, label: "NDA", size: 6 },
	{ id: "k", x: 150, y: -100, z: -20, label: "API", size: 6 },
	{ id: "l", x: -110, y: 120, z: 15, label: "HR", size: 6 },
];

const edges: Edge[] = [
	{ from: "a", to: "b" },
	{ from: "a", to: "c" },
	{ from: "a", to: "d" },
	{ from: "a", to: "e" },
	{ from: "b", to: "f" },
	{ from: "c", to: "g" },
	{ from: "b", to: "h" },
	{ from: "d", to: "i" },
	{ from: "f", to: "j" },
	{ from: "c", to: "k" },
	{ from: "d", to: "l" },
	{ from: "e", to: "g" },
	{ from: "h", to: "j" },
	{ from: "e", to: "i" },
];

function getNode(id: string) {
	return nodes.find((n) => n.id === id)!;
}

export function KnowledgeGraphAnimation() {
	const ref = useRef(null);
	const isInView = useInView(ref, { once: false });
	const [rotation, setRotation] = useState(0);

	useEffect(() => {
		if (!isInView) return;
		let frame: number;
		let start: number | null = null;
		const animate = (time: number) => {
			if (!start) start = time;
			setRotation(((time - start) / 80) % 360);
			frame = requestAnimationFrame(animate);
		};
		frame = requestAnimationFrame(animate);
		return () => cancelAnimationFrame(frame);
	}, [isInView]);

	const cos = Math.cos((rotation * Math.PI) / 180);
	const sin = Math.sin((rotation * Math.PI) / 180);

	const project = (node: Node) => {
		const x = node.x * cos - node.z * sin;
		const z = node.x * sin + node.z * cos;
		const scale = 1 + z / 400;
		return { x: x * scale, y: node.y * scale, z, scale, opacity: 0.4 + scale * 0.3 };
	};

	const projected = nodes.map((n) => ({ ...n, ...project(n) }));
	projected.sort((a, b) => a.z - b.z);

	return (
		<div
			ref={ref}
			className="relative flex h-full w-full items-center justify-center overflow-hidden"
		>
			<div className="pointer-events-none absolute bottom-0 left-0 z-20 h-20 w-full bg-gradient-to-t from-background to-transparent" />
			<svg
				viewBox="-220 -150 440 300"
				className="h-full w-full max-h-[300px]"
			>
				{/* Edges */}
				{edges.map((edge) => {
					const from = project(getNode(edge.from));
					const to = project(getNode(edge.to));
					return (
						<motion.line
							key={`${edge.from}-${edge.to}`}
							x1={from.x}
							y1={from.y}
							x2={to.x}
							y2={to.y}
							stroke="var(--secondary)"
							strokeWidth={0.5}
							strokeOpacity={Math.min(from.opacity, to.opacity) * 0.5}
							initial={{ pathLength: 0 }}
							animate={isInView ? { pathLength: 1 } : { pathLength: 0 }}
							transition={{ duration: 1, delay: 0.3 }}
						/>
					);
				})}

				{/* Nodes */}
				{projected.map((node, i) => (
					<motion.g
						key={node.id}
						initial={{ opacity: 0, scale: 0 }}
						animate={
							isInView
								? { opacity: node.opacity, scale: 1 }
								: { opacity: 0, scale: 0 }
						}
						transition={{ duration: 0.4, delay: i * 0.05 }}
					>
						<circle
							cx={node.x}
							cy={node.y}
							r={node.size * node.scale * 0.6}
							fill="var(--secondary)"
							opacity={node.opacity * 0.15}
						/>
						<circle
							cx={node.x}
							cy={node.y}
							r={node.size * node.scale * 0.25}
							fill="var(--secondary)"
							opacity={node.opacity}
						/>
						<text
							x={node.x}
							y={node.y + node.size * node.scale * 0.6 + 10}
							textAnchor="middle"
							className="fill-muted-foreground"
							fontSize={8 * node.scale}
							opacity={node.opacity}
						>
							{node.label}
						</text>
					</motion.g>
				))}
			</svg>
		</div>
	);
}
