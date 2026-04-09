//! The [`Sandbox`] trait — pluggable OS-level isolation around tool execution.
//!
//! This file defines the [`Sandbox`] trait at line 22. It is a small trait
//! with `pre_execute` / `post_execute` hooks that run around every
//! `Tool::execute` call so OS-level isolation can be applied without the tool
//! itself having to know which backend is in use.
//!
//! Implementors include [`NoopSandbox`] (the default fallback),
//! `LandlockSandbox` on Linux when the `sandbox-landlock` feature is enabled,
//! and `SeatbeltSandbox` on macOS (experimental). The agent holds an
//! `Arc<dyn Sandbox>` so the same instance is shared across all tool
//! executions and tokio tasks for the lifetime of the process.
//!
//! Sandbox selection happens at boot via `create_sandbox(config)` in
//! [`crate::security::detect`], which inspects the host OS and the active
//! cargo features to pick the strongest backend available. Adding a new
//! backend means implementing this trait in a new submodule and registering
//! it in `detect::create_sandbox`.
//!
//! ## Key types
//! - [`Sandbox`] — pre/post execution isolation hooks
//! - [`NoopSandbox`] — no-op fallback used when nothing better is available
//!
//! ## Related
//! - `src/security/detect.rs` — `create_sandbox` factory
//! - `src/security/landlock.rs` — Linux Landlock backend
//! - `src/security/seatbelt.rs` — macOS Seatbelt backend
//! - `src/tools/traits.rs` — `Tool::execute`, the call site wrapped by the sandbox

use async_trait::async_trait;
use std::process::Command;

/// Sandbox backend for OS-level process isolation.
///
/// Implement this trait to add a new sandboxing strategy. The runtime queries
/// [`is_available`](Sandbox::is_available) at startup to select the best
/// backend for the current platform, then calls
/// [`wrap_command`](Sandbox::wrap_command) before every shell execution.
///
/// Implementations must be `Send + Sync` because the sandbox may be shared
/// across concurrent tool executions on the Tokio runtime.
#[async_trait]
pub trait Sandbox: Send + Sync {
    /// Wrap a command with sandbox protection.
    ///
    /// Mutates `cmd` in place to apply isolation constraints (e.g., prepending
    /// a wrapper binary, setting environment variables, adding seccomp filters).
    ///
    /// # Errors
    ///
    /// Returns `std::io::Error` if the sandbox configuration cannot be applied
    /// (e.g., missing wrapper binary, invalid policy file).
    fn wrap_command(&self, cmd: &mut Command) -> std::io::Result<()>;

    /// Check if this sandbox backend is available on the current platform.
    ///
    /// Returns `true` when all required kernel features, binaries, and
    /// permissions are present. The runtime calls this at startup to select
    /// the most capable available backend.
    fn is_available(&self) -> bool;

    /// Return the human-readable name of this sandbox backend.
    ///
    /// Used in logs and diagnostics to identify which isolation strategy is
    /// active (e.g., `"firejail"`, `"bubblewrap"`, `"none"`).
    fn name(&self) -> &str;

    /// Return a brief description of the isolation guarantees this sandbox provides.
    ///
    /// Displayed in status output and health checks so operators can verify
    /// the active security posture.
    fn description(&self) -> &str;
}

/// No-op sandbox that provides no additional OS-level isolation.
///
/// Always reports itself as available. Use this as the fallback when no
/// platform-specific sandbox backend is detected, or in development
/// environments where isolation is not required. Security in this mode
/// relies entirely on application-layer controls.
#[derive(Debug, Clone, Default)]
pub struct NoopSandbox;

impl Sandbox for NoopSandbox {
    fn wrap_command(&self, _cmd: &mut Command) -> std::io::Result<()> {
        // Pass through unchanged
        Ok(())
    }

    fn is_available(&self) -> bool {
        true
    }

    fn name(&self) -> &str {
        "none"
    }

    fn description(&self) -> &str {
        "No sandboxing (application-layer security only)"
    }
}

#[cfg(test)]
#[path = "traits.test.rs"]
mod tests;
