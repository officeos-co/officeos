import { NavLink } from "react-router-dom";

const navItems = [
  { to: "/providers", label: "Providers" },
  { to: "/skills", label: "Skills" },
  { to: "/agents", label: "Agents" },
];

export function Sidebar() {
  return (
    <aside className="flex h-full w-[260px] shrink-0 flex-col border-r border-[var(--eaos-border)] bg-[var(--eaos-sidebar)] px-3 py-4">
      <div className="mb-6 flex items-center gap-2 px-2">
        <div className="h-7 w-7 rounded-md bg-white text-black grid place-items-center text-xs font-bold">
          E
        </div>
        <span className="text-sm font-semibold">EnterpriseAgentOS</span>
      </div>

      <nav className="flex flex-col gap-1">
        <div className="px-2 pb-1 text-[11px] uppercase tracking-wider text-[var(--eaos-text-muted)]">
          Workspace
        </div>
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              [
                "rounded-md px-3 py-2 text-sm transition-colors",
                isActive
                  ? "bg-black text-white"
                  : "text-[var(--eaos-text-muted)] hover:bg-[var(--eaos-panel)] hover:text-white",
              ].join(" ")
            }
          >
            {item.label}
          </NavLink>
        ))}
      </nav>

      <div className="mt-auto flex items-center gap-2 rounded-md border border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-3 py-2">
        <div className="h-6 w-6 rounded-full bg-gradient-to-br from-fuchsia-500 to-indigo-500" />
        <div className="text-xs">
          <div className="font-medium">Local dev</div>
          <div className="text-[var(--eaos-text-muted)]">single-tenant</div>
        </div>
      </div>
    </aside>
  );
}
