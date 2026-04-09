import { Navigate, Route, Routes } from "react-router-dom";
import { Sidebar } from "./components/Sidebar";
import { AgentsPage } from "./pages/AgentsPage";
import { ProvidersPage } from "./pages/ProvidersPage";
import { SkillsPage } from "./pages/SkillsPage";

export function App() {
  return (
    <div className="flex h-screen w-screen overflow-hidden bg-[var(--eaos-bg)] text-[var(--eaos-text)]">
      <Sidebar />
      <main className="flex-1 overflow-y-auto">
        <Routes>
          <Route path="/" element={<Navigate to="/agents" replace />} />
          <Route path="/providers" element={<ProvidersPage />} />
          <Route path="/skills" element={<SkillsPage />} />
          <Route path="/agents" element={<AgentsPage />} />
          <Route path="*" element={<Navigate to="/agents" replace />} />
        </Routes>
      </main>
    </div>
  );
}
