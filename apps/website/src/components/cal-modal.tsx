"use client";

import { useCallback, useEffect, useRef, useState } from "react";

declare global {
  interface Window {
    Cal?: (...args: unknown[]) => void;
  }
}

const CAL_LINK = "harro-krog-n9ith3/demo-officeos";

function initCal(): void {
  if (window.Cal) return;

  // Set up the command queue BEFORE the script loads (standard Cal embed pattern)
  const Cal: ((...args: unknown[]) => void) & {
    loaded?: boolean;
    q?: unknown[][];
    ns?: Record<string, unknown>;
  } = function (...args: unknown[]) {
    if (args[0] === "init") {
      const api = function (...a: unknown[]) { api.q.push(a); };
      api.q = [] as unknown[][];
      const ns = args[1] as string | undefined;
      if (typeof ns === "string") {
        Cal.ns![ns] = Cal.ns![ns] || api;
        Cal.q!.push(args);
      } else {
        Cal.q!.push(args);
      }
      return;
    }
    Cal.q!.push(args);
  };
  Cal.q = [];
  Cal.ns = {};
  window.Cal = Cal;

  const script = document.createElement("script");
  script.src = "https://app.cal.com/embed/embed.js";
  script.async = true;
  document.head.appendChild(script);
}

export function CalModal() {
  const [isOpen, setIsOpen] = useState(false);
  const embedRef = useRef<HTMLDivElement>(null);
  const initialised = useRef(false);

  const open = useCallback(() => setIsOpen(true), []);
  const close = useCallback(() => setIsOpen(false), []);

  useEffect(() => {
    window.addEventListener("open-cal-modal", open);
    return () => window.removeEventListener("open-cal-modal", open);
  }, [open]);

  useEffect(() => {
    document.body.style.overflow = isOpen ? "hidden" : "";
    return () => {
      document.body.style.overflow = "";
    };
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen || initialised.current) return;
    initialised.current = true;

    initCal();
    const Cal = window.Cal!;
    Cal("init", { origin: "https://cal.com" });
    Cal("inline", {
      elementOrSelector: "#cal-embed",
      calLink: CAL_LINK,
      config: { layout: "month_view" },
    });
    Cal("ui", {
      theme: "light",
      hideEventTypeDetails: false,
      layout: "month_view",
      styles: {
        body: { background: "transparent" },
      },
    });
  }, [isOpen]);

  return (
    <div className={`fixed inset-0 z-[100] flex items-center justify-center ${isOpen ? "" : "hidden"}`}>
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={close}
      />
      {/* Cal embed container */}
      <div
        id="cal-embed"
        ref={embedRef}
        className="relative z-10 h-[600px] w-full max-w-4xl mx-4 overflow-hidden [&_iframe]:overflow-hidden"
        style={{ scrollbarWidth: "none" }}
      />
    </div>
  );
}
