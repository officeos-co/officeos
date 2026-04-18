/* ── Grid constants & helpers ─────────────────────────────── */

/** Visible rows — columns expand rightward as agents are added */
export const ROWS = 6;
export const CELL_W = 116;
export const CELL_H = 34;
export const GAP = 6;

const ROW_OFFSET = (CELL_W + GAP) / 2;

/** Position for a cell given row-major index across unlimited columns */
export function cellPos(idx: number, cols: number): { x: number; y: number } {
	const col = idx % cols;
	const row = Math.floor(idx / cols);
	return {
		x: col * (CELL_W + GAP) + (row % 2 === 1 ? ROW_OFFSET : 0),
		y: row * (CELL_H + GAP),
	};
}

export function seededShuffle(arr: number[], seed: number): number[] {
	const a = [...arr];
	let s = seed;
	for (let i = a.length - 1; i > 0; i--) {
		s = (s * 16807 + 0) % 2147483647;
		const j = s % (i + 1);
		[a[i], a[j]] = [a[j], a[i]];
	}
	return a;
}
