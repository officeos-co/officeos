import Link from "next/link";

export default function HomePage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 p-8 text-center">
      <h1 className="text-4xl font-bold tracking-tight">
        EnterpriseAgentOS
      </h1>
      <p className="max-w-lg text-lg text-muted-foreground">
        Kubernetes-native platform for autonomous AI agents. Single-tenant,
        self-hosted.
      </p>
      <Link
        href="/docs"
        className="rounded-lg bg-primary px-6 py-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
      >
        Read the docs
      </Link>
    </main>
  );
}
