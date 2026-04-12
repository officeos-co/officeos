"use client";

import { useCallback } from "react";

const CAL_EMBED_URL = "https://cal.com/harro-krog-n9ith3/demo-officeos";

export function useCalModal() {
	const openCalModal = useCallback(() => {
		// Dispatch custom event that the CalModal component listens to
		window.dispatchEvent(new CustomEvent("open-cal-modal"));
	}, []);

	return { openCalModal, calUrl: CAL_EMBED_URL };
}
