#![recursion_limit = "256"]
#![warn(clippy::all, clippy::pedantic)]
#![allow(
    clippy::missing_errors_doc,
    clippy::missing_panics_doc,
    clippy::module_name_repetitions,
    clippy::too_many_lines,
    clippy::uninlined_format_args,
    dead_code
)]

use anyhow::Result;
use tracing_subscriber::{EnvFilter, fmt};

mod agent;
mod approval;
mod auth;
mod channels;
mod cli_input;
mod config;
mod cost;
mod cron;
mod daemon;
mod doctor;
mod gateway;
mod health;
mod heartbeat;
mod hooks;
mod i18n;
mod memory;
mod migration;
mod multimodal;
mod observability;
mod providers;
mod runtime;
mod security;
mod skills;
mod tools;
mod util;

use config::Config;

// Re-export CLI enum types so sibling modules that still reference them
// (channels, cron, skills, migration, memory/cli) continue to compile until
// those modules are deleted in subsequent commits.
#[allow(unused_imports)]
pub use zeroclaw::{ChannelCommands, CronCommands, MemoryCommands, MigrateCommands, SkillCommands};

#[tokio::main(flavor = "multi_thread")]
async fn main() -> Result<()> {
    let subscriber = fmt::Subscriber::builder()
        .with_env_filter(
            EnvFilter::try_from_default_env().unwrap_or_else(|_| EnvFilter::new("info")),
        )
        .finish();
    tracing::subscriber::set_global_default(subscriber)
        .expect("setting default subscriber failed");

    let mut config = Box::pin(Config::load_or_init()).await?;
    config.apply_env_overrides();

    let host = config.gateway.host.clone();
    let port = config.gateway.port;
    tracing::info!("Starting ZeroClaw daemon on {host}:{port}");

    Box::pin(daemon::run(config, host, port)).await
}
