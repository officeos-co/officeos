//! Observability subsystem index — events, observers, and pluggable backends.
//!
//! This module re-exports the [`Observer`] trait, the [`ObserverEvent`] enum,
//! [`MultiObserver`], and the concrete backends ([`NoopObserver`],
//! [`LogObserver`], [`VerboseObserver`], [`PrometheusObserver`],
//! [`OtelObserver`], `DoraObserver`, `RuntimeTraceObserver`). It also exposes
//! the [`create_observer`] factory used by the agent at boot.
//!
//! The agent holds an `Arc<dyn Observer>` and calls
//! `observer.record_event(&event)` at every notable point in the lifecycle:
//! turn start/end, tool call start/end, provider call start/end, cache
//! hits/misses, memory operations, and channel ingress/egress. Backends are
//! responsible for their own buffering, batching, and export — the agent just
//! fires the event and moves on.
//!
//! [`MultiObserver`] composes multiple backends so events fan out to all
//! enabled observers simultaneously (e.g. Log + Prometheus + Otel). Two
//! backends are feature-gated: `observability-prometheus` (default,
//! Prometheus counters/gauges served at `/metrics`) and `observability-otel`
//! (OTLP trace + metrics export). `RuntimeTraceObserver` writes structured
//! event traces to disk for offline analysis via `zeroclaw doctor traces`,
//! and `DoraObserver` aggregates events into DORA metrics (deployment
//! frequency, lead time, MTTR, change failure rate).
//!
//! ## Key types
//! - [`Observer`] — trait every backend implements
//! - [`ObserverEvent`] — enum of all lifecycle events
//! - [`MultiObserver`] — fan-out composer
//! - [`create_observer`] — factory used at agent boot
//!
//! ## Related
//! - `src/observability/traits.rs` — trait + event enum definitions
//! - `src/agent/mod.rs` — main call sites for `record_event`
//! - `src/observability/dora.rs` — DORA metric aggregation

pub mod dora;
pub mod log;
pub mod multi;
pub mod noop;
#[cfg(feature = "observability-otel")]
pub mod otel;
#[cfg(feature = "observability-prometheus")]
pub mod prometheus;
pub mod runtime_trace;
pub mod traits;
pub mod verbose;

#[allow(unused_imports)]
pub use self::log::LogObserver;
#[allow(unused_imports)]
pub use self::multi::MultiObserver;
pub use noop::NoopObserver;
#[cfg(feature = "observability-otel")]
pub use otel::OtelObserver;
#[cfg(feature = "observability-prometheus")]
pub use prometheus::PrometheusObserver;
pub use traits::{Observer, ObserverEvent};
#[allow(unused_imports)]
pub use verbose::VerboseObserver;

use crate::config::ObservabilityConfig;

/// Factory: create the right observer from config
pub fn create_observer(config: &ObservabilityConfig) -> Box<dyn Observer> {
    match config.backend.as_str() {
        "log" => Box::new(LogObserver::new()),
        "verbose" => Box::new(VerboseObserver::new()),
        "prometheus" => {
            #[cfg(feature = "observability-prometheus")]
            {
                Box::new(PrometheusObserver::new())
            }
            #[cfg(not(feature = "observability-prometheus"))]
            {
                tracing::warn!(
                    "Prometheus backend requested but this build was compiled without `observability-prometheus`; falling back to noop."
                );
                Box::new(NoopObserver)
            }
        }
        "otel" | "opentelemetry" | "otlp" => {
            #[cfg(feature = "observability-otel")]
            match OtelObserver::new(
                config.otel_endpoint.as_deref(),
                config.otel_service_name.as_deref(),
            ) {
                Ok(obs) => {
                    tracing::info!(
                        endpoint = config
                            .otel_endpoint
                            .as_deref()
                            .unwrap_or("http://localhost:4318"),
                        "OpenTelemetry observer initialized"
                    );
                    Box::new(obs)
                }
                Err(e) => {
                    tracing::error!("Failed to create OTel observer: {e}. Falling back to noop.");
                    Box::new(NoopObserver)
                }
            }
            #[cfg(not(feature = "observability-otel"))]
            {
                tracing::warn!(
                    "OpenTelemetry backend requested but this build was compiled without `observability-otel`; falling back to noop."
                );
                Box::new(NoopObserver)
            }
        }
        "none" | "noop" => Box::new(NoopObserver),
        _ => {
            tracing::warn!(
                "Unknown observability backend '{}', falling back to noop",
                config.backend
            );
            Box::new(NoopObserver)
        }
    }
}

#[cfg(test)]
#[path = "tests.rs"]
mod tests;
