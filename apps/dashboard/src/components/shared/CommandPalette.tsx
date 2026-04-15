"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
  CommandShortcut,
} from "@/components/ui/command";
import {
  LayoutDashboard,
  Bot,
  Wrench,
  Bell,
  CheckSquare,
  Settings,
  BookOpen,
  KeyRound,
  Server,
  CreditCard,
  Link2,
  User,
  Gauge,
} from "lucide-react";

export function CommandPalette() {
  const [open, setOpen] = useState(false);
  const router = useRouter();

  useEffect(() => {
    function down(e: KeyboardEvent) {
      if (e.key === "k" && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        setOpen((v) => !v);
      }
    }
    document.addEventListener("keydown", down);
    return () => document.removeEventListener("keydown", down);
  }, []);

  function nav(href: string) {
    setOpen(false);
    router.push(href);
  }

  return (
    <CommandDialog open={open} onOpenChange={setOpen}>
      <CommandInput placeholder="Search..." />
      <CommandList>
        <CommandEmpty>No results.</CommandEmpty>
        <CommandGroup heading="Navigation">
          <CommandItem onSelect={() => nav("/")}>
            <LayoutDashboard className="mr-2 h-4 w-4" />
            Overview
            <CommandShortcut>G O</CommandShortcut>
          </CommandItem>
          <CommandItem onSelect={() => nav("/agents")}>
            <Bot className="mr-2 h-4 w-4" />
            Agents
            <CommandShortcut>G A</CommandShortcut>
          </CommandItem>
          <CommandItem onSelect={() => nav("/skills")}>
            <Wrench className="mr-2 h-4 w-4" />
            Tools
            <CommandShortcut>G T</CommandShortcut>
          </CommandItem>
          <CommandItem onSelect={() => nav("/providers")}>
            <KeyRound className="mr-2 h-4 w-4" />
            Providers
          </CommandItem>
          <CommandItem onSelect={() => nav("/runners")}>
            <Server className="mr-2 h-4 w-4" />
            Runners
          </CommandItem>
          <CommandItem onSelect={() => nav("/system-events")}>
            <Bell className="mr-2 h-4 w-4" />
            Events
          </CommandItem>
          <CommandItem onSelect={() => nav("/approvals")}>
            <CheckSquare className="mr-2 h-4 w-4" />
            Approvals
          </CommandItem>
        </CommandGroup>
        <CommandSeparator />
        <CommandGroup heading="Settings">
          <CommandItem onSelect={() => nav("/settings/organization")}>
            <Settings className="mr-2 h-4 w-4" />
            Organization
          </CommandItem>
          <CommandItem onSelect={() => nav("/settings/profile")}>
            <User className="mr-2 h-4 w-4" />
            Profile
          </CommandItem>
          <CommandItem onSelect={() => nav("/settings/billing")}>
            <CreditCard className="mr-2 h-4 w-4" />
            Billing
          </CommandItem>
          <CommandItem onSelect={() => nav("/settings/channels")}>
            <Link2 className="mr-2 h-4 w-4" />
            Channels
          </CommandItem>
          <CommandItem onSelect={() => nav("/settings/api-keys")}>
            <KeyRound className="mr-2 h-4 w-4" />
            API Keys
          </CommandItem>
          <CommandItem onSelect={() => nav("/settings/limits")}>
            <Gauge className="mr-2 h-4 w-4" />
            Limits
          </CommandItem>
        </CommandGroup>
        <CommandSeparator />
        <CommandGroup heading="Help">
          <CommandItem
            onSelect={() => {
              setOpen(false);
              window.open("https://docs.officeos.co", "_blank");
            }}
          >
            <BookOpen className="mr-2 h-4 w-4" />
            Documentation
          </CommandItem>
        </CommandGroup>
      </CommandList>
    </CommandDialog>
  );
}
