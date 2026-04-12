"use client";

import { useCallback, useEffect, useState } from "react";
import { X } from "lucide-react";

const CAL_EMBED_URL = "https://cal.com/harro-krog-n9ith3/demo-officeos?embed=true&theme=light";

export function CalModal() {
	const [isOpen, setIsOpen] = useState(false);

	const open = useCallback(() => setIsOpen(true), []);
	const close = useCallback(() => setIsOpen(false), []);

	useEffect(() => {
		window.addEventListener("open-cal-modal", open);
		return () => window.removeEventListener("open-cal-modal", open);
	}, [open]);

	useEffect(() => {
		if (isOpen) {
			document.body.style.overflow = "hidden";
		} else {
			document.body.style.overflow = "";
		}
		return () => {
			document.body.style.overflow = "";
		};
	}, [isOpen]);

	if (!isOpen) return null;

	return (
		<div className="fixed inset-0 z-[100] flex items-center justify-center">
			{/* Backdrop */}
			<div
				className="absolute inset-0 bg-black/60 backdrop-blur-sm"
				onClick={close}
			/>

			{/* Modal */}
			<div className="relative z-10 w-full max-w-xl rounded-2xl bg-background shadow-2xl overflow-hidden mx-4">
				<button
					type="button"
					onClick={close}
					className="absolute top-3 right-3 z-20 flex h-8 w-8 items-center justify-center rounded-full bg-muted hover:bg-muted-foreground/20 transition-colors"
				>
					<X className="h-4 w-4" />
				</button>

				<iframe
					src={CAL_EMBED_URL}
					className="h-[650px] w-full border-0"
					title="Book a Demo — Office OS"
				/>
			</div>
		</div>
	);
}
