"use client";

import { siteConfig } from "@/lib/site";
import {
  ChevronDown,
  Menu,
  X,
  Cpu,
  Network,
  Puzzle,
  Cable,
  ShieldCheck,
  TrendingUp,
  HeadphonesIcon,
  Swords,
  FileCheck,
  PenTool,
} from "lucide-react";
import { AnimatePresence, motion, useScroll } from "motion/react";
import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils";

const INITIAL_WIDTH = "70rem";
const MAX_WIDTH = "800px";

const productLinks = [
  {
    href: "/product/platform",
    label: "Platform",
    description: "Kubernetes-native agent runtime",
    icon: Cpu,
  },
  {
    href: "/product/knowledge-graph",
    label: "Knowledge Graph",
    description: "Organization-wide intelligence",
    icon: Network,
  },
  {
    href: "/product/skills",
    label: "Skills & SDK",
    description: "TypeScript SDK for custom tools",
    icon: Puzzle,
  },
  {
    href: "/product/integrations",
    label: "Integrations",
    description: "Deep API & channel connections",
    icon: Cable,
  },
  {
    href: "/product/security",
    label: "Security",
    description: "Enterprise-grade compliance",
    icon: ShieldCheck,
  },
];

const solutionLinks = [
  {
    href: "/solutions/sales-intelligence",
    label: "Sales Intelligence",
    description: "Automated lead research",
    icon: TrendingUp,
  },
  {
    href: "/solutions/customer-success",
    label: "Customer Success",
    description: "AI-powered support across channels",
    icon: HeadphonesIcon,
  },
  {
    href: "/solutions/competitive-intelligence",
    label: "Competitive Intelligence",
    description: "Daily competitor monitoring",
    icon: Swords,
  },
  {
    href: "/solutions/contract-review",
    label: "Contract Review",
    description: "Automated compliance checking",
    icon: FileCheck,
  },
  {
    href: "/solutions/content-strategy",
    label: "Content Strategy",
    description: "Research-driven content planning",
    icon: PenTool,
  },
];

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

const dropdownVariants = {
  hidden: { opacity: 0, y: 8, scale: 0.96 },
  visible: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: { duration: 0.15, ease: [0.25, 0.1, 0.25, 1] as const },
  },
  exit: {
    opacity: 0,
    y: 8,
    scale: 0.96,
    transition: { duration: 0.1 },
  },
};

type DropdownKey = "product" | "solutions" | null;

function NavDropdown({ links }: { links: typeof productLinks }) {
  return (
    <motion.div
      variants={dropdownVariants}
      initial="hidden"
      animate="visible"
      exit="exit"
      className="absolute top-full left-1/2 z-50 mt-2 w-[320px] -translate-x-1/2 rounded-xl border border-border bg-background/95 p-2 shadow-lg backdrop-blur-xl"
    >
      {links.map((link) => (
        <Link
          key={link.href}
          href={link.href}
          className="flex items-start gap-3 rounded-lg px-3 py-2.5 transition-colors hover:bg-muted"
        >
          <link.icon className="mt-0.5 h-4 w-4 shrink-0 text-secondary" />
          <div>
            <p className="text-sm font-medium text-primary">{link.label}</p>
            <p className="text-xs text-muted-foreground">{link.description}</p>
          </div>
        </Link>
      ))}
    </motion.div>
  );
}

export function Navbar() {
  const pathname = usePathname();
  const { scrollY } = useScroll();
  const [hasScrolled, setHasScrolled] = useState(false);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [activeDropdown, setActiveDropdown] = useState<DropdownKey>(null);
  const [mobileExpanded, setMobileExpanded] = useState<DropdownKey>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const unsubscribe = scrollY.on("change", (latest) => {
      setHasScrolled(latest > 10);
    });
    return unsubscribe;
  }, [scrollY]);

  const handleMouseEnter = (key: DropdownKey) => {
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    setActiveDropdown(key);
  };

  const handleMouseLeave = () => {
    timeoutRef.current = setTimeout(() => {
      setActiveDropdown(null);
    }, 150);
  };

  const toggleDrawer = () => setIsDrawerOpen((prev) => !prev);
  const handleOverlayClick = () => setIsDrawerOpen(false);

  const toggleMobileSection = (key: DropdownKey) => {
    setMobileExpanded((prev) => (prev === key ? null : key));
  };

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
          <div className="flex h-[56px] items-center justify-between p-4">
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
                width={20}
                height={20}
                className="h-[1.125rem] w-[1.125rem]"
              />
              <p className="font-semibold text-lg text-primary">OfficeOS</p>
            </Link>

            {/* Desktop nav */}
            <div className="hidden items-center gap-1 md:flex">
              {/* Product dropdown */}
              <div
                className="relative"
                onMouseEnter={() => handleMouseEnter("product")}
                onMouseLeave={handleMouseLeave}
              >
                <button className="flex cursor-pointer items-center gap-1 rounded-lg px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:text-primary">
                  Product
                  <ChevronDown
                    className={cn(
                      "h-3.5 w-3.5 transition-transform duration-200",
                      activeDropdown === "product" && "rotate-180",
                    )}
                  />
                </button>
                <AnimatePresence>
                  {activeDropdown === "product" && (
                    <NavDropdown links={productLinks} />
                  )}
                </AnimatePresence>
              </div>

              {/* Solutions dropdown */}
              <div
                className="relative"
                onMouseEnter={() => handleMouseEnter("solutions")}
                onMouseLeave={handleMouseLeave}
              >
                <button className="flex cursor-pointer items-center gap-1 rounded-lg px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:text-primary">
                  Solutions
                  <ChevronDown
                    className={cn(
                      "h-3.5 w-3.5 transition-transform duration-200",
                      activeDropdown === "solutions" && "rotate-180",
                    )}
                  />
                </button>
                <AnimatePresence>
                  {activeDropdown === "solutions" && (
                    <NavDropdown links={solutionLinks} />
                  )}
                </AnimatePresence>
              </div>

              <Link
                className="btn-glow ml-2 flex h-8 w-fit items-center justify-center rounded-full bg-secondary px-4 font-normal text-sm text-white tracking-wide"
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

                <div className="flex flex-col gap-1">
                  {/* Product section */}
                  <button
                    onClick={() => toggleMobileSection("product")}
                    className="flex cursor-pointer items-center justify-between rounded-lg px-3 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-muted"
                  >
                    Product
                    <ChevronDown
                      className={cn(
                        "h-4 w-4 text-muted-foreground transition-transform duration-200",
                        mobileExpanded === "product" && "rotate-180",
                      )}
                    />
                  </button>
                  <AnimatePresence>
                    {mobileExpanded === "product" && (
                      <motion.div
                        initial={{ height: 0, opacity: 0 }}
                        animate={{ height: "auto", opacity: 1 }}
                        exit={{ height: 0, opacity: 0 }}
                        transition={{ duration: 0.2 }}
                        className="overflow-hidden"
                      >
                        <div className="flex flex-col gap-0.5 pb-2 pl-2">
                          {productLinks.map((link) => (
                            <Link
                              key={link.href}
                              href={link.href}
                              onClick={() => setIsDrawerOpen(false)}
                              className="flex items-center gap-3 rounded-lg px-3 py-2 transition-colors hover:bg-muted"
                            >
                              <link.icon className="h-4 w-4 shrink-0 text-secondary" />
                              <div>
                                <p className="text-sm font-medium text-primary">
                                  {link.label}
                                </p>
                                <p className="text-xs text-muted-foreground">
                                  {link.description}
                                </p>
                              </div>
                            </Link>
                          ))}
                        </div>
                      </motion.div>
                    )}
                  </AnimatePresence>

                  {/* Solutions section */}
                  <button
                    onClick={() => toggleMobileSection("solutions")}
                    className="flex cursor-pointer items-center justify-between rounded-lg px-3 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-muted"
                  >
                    Solutions
                    <ChevronDown
                      className={cn(
                        "h-4 w-4 text-muted-foreground transition-transform duration-200",
                        mobileExpanded === "solutions" && "rotate-180",
                      )}
                    />
                  </button>
                  <AnimatePresence>
                    {mobileExpanded === "solutions" && (
                      <motion.div
                        initial={{ height: 0, opacity: 0 }}
                        animate={{ height: "auto", opacity: 1 }}
                        exit={{ height: 0, opacity: 0 }}
                        transition={{ duration: 0.2 }}
                        className="overflow-hidden"
                      >
                        <div className="flex flex-col gap-0.5 pb-2 pl-2">
                          {solutionLinks.map((link) => (
                            <Link
                              key={link.href}
                              href={link.href}
                              onClick={() => setIsDrawerOpen(false)}
                              className="flex items-center gap-3 rounded-lg px-3 py-2 transition-colors hover:bg-muted"
                            >
                              <link.icon className="h-4 w-4 shrink-0 text-secondary" />
                              <div>
                                <p className="text-sm font-medium text-primary">
                                  {link.label}
                                </p>
                                <p className="text-xs text-muted-foreground">
                                  {link.description}
                                </p>
                              </div>
                            </Link>
                          ))}
                        </div>
                      </motion.div>
                    )}
                  </AnimatePresence>
                </div>

                <Link
                  href={siteConfig.dashboardUrl}
                  onClick={() => setIsDrawerOpen(false)}
                  className="btn-glow flex h-8 w-full items-center justify-center rounded-full bg-secondary px-4 font-normal text-sm tracking-wide transition-all ease-out hover:bg-secondary/80 active:scale-95"
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
