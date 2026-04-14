"use client";

import { useCallback, useEffect, useState } from "react";
import type { StatusResponse, ServiceStatus } from "@/lib/check-status";

const STATUS_LABELS: Record<ServiceStatus, string> = {
  operational: "Operational",
  degraded: "Degraded",
  down: "Down",
};

const OVERALL_LABELS: Record<ServiceStatus, string> = {
  operational: "All Systems Operational",
  degraded: "Partial Outage",
  down: "Major Outage",
};

function StatusBadge({ status }: { status: ServiceStatus }) {
  const colors: Record<ServiceStatus, string> = {
    operational: "bg-status-operational-bg text-status-operational",
    degraded: "bg-status-degraded-bg text-status-degraded",
    down: "bg-status-down-bg text-status-down",
  };

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium ${colors[status]}`}
    >
      <span
        className={`h-1.5 w-1.5 rounded-full ${
          status === "operational"
            ? "bg-status-operational"
            : status === "degraded"
              ? "bg-status-degraded"
              : "bg-status-down"
        }`}
      />
      {STATUS_LABELS[status]}
    </span>
  );
}

function OverallBanner({ status }: { status: ServiceStatus }) {
  const styles: Record<ServiceStatus, string> = {
    operational:
      "bg-status-operational-bg border-status-operational/20 text-status-operational",
    degraded:
      "bg-status-degraded-bg border-status-degraded/20 text-status-degraded",
    down: "bg-status-down-bg border-status-down/20 text-status-down",
  };

  return (
    <div
      className={`flex items-center gap-3 rounded-lg border px-6 py-4 ${styles[status]}`}
    >
      <span
        className={`h-3 w-3 rounded-full ${
          status === "operational"
            ? "bg-status-operational"
            : status === "degraded"
              ? "bg-status-degraded"
              : "bg-status-down"
        }`}
      />
      <span className="text-sm font-semibold">{OVERALL_LABELS[status]}</span>
    </div>
  );
}

export default function StatusPage() {
  const [data, setData] = useState<StatusResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchStatus = useCallback(async () => {
    try {
      const res = await fetch("/api/status");
      if (res.ok) {
        const json: StatusResponse = await res.json();
        setData(json);
      }
    } catch {
      // keep previous data on fetch error
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchStatus();
    const interval = setInterval(fetchStatus, 30_000);
    return () => clearInterval(interval);
  }, [fetchStatus]);

  return (
    <div className="mx-auto min-h-screen max-w-2xl px-6 py-12">
      {/* Header */}
      <header className="mb-10">
        <div className="flex items-baseline gap-3">
          <h1 className="text-lg font-semibold tracking-tight text-foreground">
            Office OS
          </h1>
          <span className="text-sm text-muted-foreground">Status</span>
        </div>
      </header>

      {/* Overall banner */}
      {loading && !data ? (
        <div className="mb-8 flex items-center gap-3 rounded-lg border border-border bg-card px-6 py-4">
          <span className="h-3 w-3 animate-pulse rounded-full bg-muted-foreground/30" />
          <span className="text-sm text-muted-foreground">
            Checking systems...
          </span>
        </div>
      ) : data ? (
        <div className="mb-8">
          <OverallBanner status={data.overall} />
        </div>
      ) : null}

      {/* Service cards */}
      <div className="space-y-3">
        {loading && !data
          ? Array.from({ length: 3 }).map((_, i) => (
              <div
                key={i}
                className="flex items-center justify-between rounded-lg border border-border bg-card px-6 py-5"
              >
                <div className="space-y-2">
                  <div className="h-4 w-24 animate-pulse rounded bg-muted" />
                  <div className="h-3 w-40 animate-pulse rounded bg-muted" />
                </div>
                <div className="h-6 w-24 animate-pulse rounded-full bg-muted" />
              </div>
            ))
          : data?.services.map((service) => (
              <div
                key={service.name}
                className="flex items-center justify-between rounded-lg border border-border bg-card px-6 py-5"
              >
                <div>
                  <p className="text-sm font-semibold text-card-foreground">
                    {service.name}
                  </p>
                  <p className="mt-0.5 text-xs text-muted-foreground">
                    {service.description}
                  </p>
                </div>
                <div className="flex items-center gap-4">
                  {service.responseTime !== null && (
                    <span className="text-xs tabular-nums text-muted-foreground">
                      {service.responseTime}ms
                    </span>
                  )}
                  <StatusBadge status={service.status} />
                </div>
              </div>
            ))}
      </div>

      {/* Last checked */}
      {data && (
        <p className="mt-6 text-center text-xs text-muted-foreground">
          Last checked{" "}
          {new Date(data.checkedAt).toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit",
          })}
          {" \u00b7 Refreshes every 30s"}
        </p>
      )}

      {/* Footer */}
      <footer className="mt-16 border-t border-border pt-6 text-center text-xs text-muted-foreground">
        <p>
          &copy; 2026 Office OS GmbH &middot;{" "}
          <a
            href="https://officeos.co"
            className="underline underline-offset-2 hover:text-foreground"
          >
            officeos.co
          </a>
        </p>
      </footer>
    </div>
  );
}
