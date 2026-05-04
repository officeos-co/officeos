"use client";

import { getSiteConfig } from "@/lib/site";
import { Menu, X } from "lucide-react";
import { AnimatePresence, motion, useScroll } from "motion/react";
import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { cn } from "@/lib/utils";

const INITIAL_WIDTH = "70rem";
const MAX_WIDTH = "800px";

const overlayVariants = {
  hidden: { opacity: 0 },
  visible: { opacity: 1 },
  exit: { opacity: 0 },
};

const drawerVariants = {
  hidden: { opacity: 0, y: 100 },
  visible: {
    opacity: 1,
    y: 0,
    rotate: 0,
    transition: {
      type: "spring" as const,
      damping: 15,
      stiffness: 200,
      staggerChildren: 0.03,
    },
  },
  exit: {
    opacity: 0,
    y: 100,
    transition: { duration: 0.1 },
  },
};

function formatStars(count: number): string {
  if (count >= 1000) {
    const k = count / 1000;
    return k % 1 === 0 ? `${k}K` : `${k.toFixed(1)}K`;
  }
  return count.toString();
}

function GitHubStars({ compact }: { compact: boolean }) {
  const [stars, setStars] = useState<number | null>(null);

  useEffect(() => {
    fetch("https://api.github.com/repos/officeos-co/officeos")
      .then((res) => res.json())
      .then((data) => {
        if (typeof data.stargazers_count === "number") {
          setStars(data.stargazers_count);
        }
      })
      .catch(() => {});
  }, []);

  return (
    <a
      href="https://github.com/officeos-co/officeos"
      target="_blank"
      rel="noopener noreferrer"
      className={cn(
        "flex h-8 items-center gap-2 rounded-full px-2 text-sm text-muted-foreground transition-all hover:text-primary",
        compact
          ? "border-transparent px-2"
          : "border border-border hover:bg-muted",
      )}
    >
      <Image
        src="/github.svg"
        alt="GitHub"
        width={18}
        height={18}
        className="h-5 w-5"
      />
      {stars !== null && (
        <>
          <span className="text-xs font-medium">{formatStars(stars)}</span>
        </>
      )}
    </a>
  );
}

export function Navbar() {
  const siteConfig = getSiteConfig();
  const pathname = usePathname();
  const { scrollY } = useScroll();
  const [hasScrolled, setHasScrolled] = useState(false);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  useEffect(() => {
    const unsubscribe = scrollY.on("change", (latest) => {
      setHasScrolled(latest > 10);
    });
    return unsubscribe;
  }, [scrollY]);

  const toggleDrawer = () => setIsDrawerOpen((prev) => !prev);
  const handleOverlayClick = () => setIsDrawerOpen(false);

  return (
    <header
      className={cn(
        "sticky z-50 mx-4 flex justify-center transition-all duration-300 md:mx-0",
        hasScrolled ? "top-6" : "top-4 mx-0",
      )}
    >
      <motion.div
        initial={{ width: INITIAL_WIDTH }}
        animate={{ width: hasScrolled ? MAX_WIDTH : INITIAL_WIDTH }}
        transition={{ duration: 0.3, ease: [0.25, 0.1, 0.25, 1] }}
      >
        <div
          className={cn(
            "mx-auto max-w-7xl rounded-2xl transition-all duration-300 xl:px-0",
            hasScrolled
              ? "border border-border bg-background/75 px-2 backdrop-blur-lg"
              : "px-7 shadow-none",
          )}
        >
          <div className="flex h-[56px] items-center p-4">
            <Link
              href="/"
              onClick={(e) => {
                if (pathname === "/") {
                  e.preventDefault();
                  window.scrollTo({ top: 0, behavior: "smooth" });
                }
              }}
              className="flex items-center gap-1"
            >
              <Image
                src="/logo.svg"
                alt="OfficeOS"
                width={24}
                height={24}
                className="h-7 w-7"
              />
              <p className="font-semibold text-xl text-primary">OfficeOS</p>
            </Link>

            <div className="flex-1" />

            {/* Desktop actions — right */}
            <div className="hidden items-center gap-2 md:flex">
              <GitHubStars compact={hasScrolled} />

              <Link
                className="btn-glow flex h-10 w-fit items-center justify-center rounded-full bg-secondary px-5 font-medium text-sm text-white tracking-wide"
                href={siteConfig.dashboardUrl}
              >
                Start Free
              </Link>
            </div>

            {/* Mobile menu button */}
            <button
              className="flex size-8 cursor-pointer items-center justify-center rounded-md border border-border md:hidden"
              onClick={toggleDrawer}
            >
              {isDrawerOpen ? (
                <X className="size-5" />
              ) : (
                <Menu className="size-5" />
              )}
            </button>
          </div>
        </div>
      </motion.div>

      {/* Mobile drawer */}
      <AnimatePresence>
        {isDrawerOpen && (
          <>
            <motion.div
              className="fixed inset-0 bg-overlay backdrop-blur-sm"
              initial="hidden"
              animate="visible"
              exit="exit"
              variants={overlayVariants}
              transition={{ duration: 0.2 }}
              onClick={handleOverlayClick}
            />

            <motion.div
              className="fixed inset-x-0 bottom-3 mx-auto w-[95%] max-h-[80vh] overflow-y-auto rounded-xl border border-border bg-background p-4 shadow-lg"
              initial="hidden"
              animate="visible"
              exit="exit"
              variants={drawerVariants}
            >
              <div className="flex flex-col gap-4">
                <div className="flex items-center justify-between">
                  <Link href="/" className="flex items-center gap-3">
                    <p className="font-semibold text-lg text-primary">
                      OfficeOS
                    </p>
                  </Link>
                  <button
                    onClick={toggleDrawer}
                    className="cursor-pointer rounded-md border border-border p-1"
                  >
                    <X className="size-5" />
                  </button>
                </div>

                <Link
                  href={siteConfig.dashboardUrl}
                  onClick={() => setIsDrawerOpen(false)}
                  className="btn-glow flex h-10 w-full items-center justify-center rounded-full bg-secondary px-5 font-medium text-sm tracking-wide transition-all ease-out hover:bg-secondary/80 active:scale-95"
                >
                  Start Free
                </Link>
              </div>
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </header>
  );
}
