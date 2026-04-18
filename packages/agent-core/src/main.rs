// Rust guideline compliant 2026-02-21

use mimalloc::MiMalloc;

/// Use mimalloc as the global allocator per M-MIMALLOC-APPS.
#[global_allocator]
static GLOBAL: MiMalloc = MiMalloc;

#[tokio::main]
async fn main() {
    tracing_subscriber::fmt::init();
    let (agent_id, backend_url) = zeroclaw_agent::env::load_env().expect("missing env");
    tracing::info!(
        name: "env.load.success",
        agent_id = %agent_id,
        "environment loaded: {{agent_id}}",
    );
    let cfg = zeroclaw_agent::bootstrap::bootstrap(agent_id, backend_url)
        .await
        .expect("bootstrap failed");
    tracing::info!(
        name: "bootstrap.complete",
        agent_id = %cfg.agent_id,
        agent_name = %cfg.display_name,
        "bootstrap complete: {{agent_id}} {{agent_name}}",
    );
    let cfg = std::sync::Arc::new(cfg);
    zeroclaw_agent::personality::seed(&cfg.memory_dir, &cfg.system_prompt)
        .await
        .expect("personality seed failed");
    tracing::info!(name: "personality.seed.success", "personality seeded");
    let agent = std::sync::Arc::new(zeroclaw_agent::agent::Agent::new(std::sync::Arc::clone(
        &cfg,
    )));

    // Send BOOTSTRAP.md as the first message and sync to backend logs.
    let log_client = zeroclaw_agent::log_client::LogClient::new(
        cfg.backend_url.clone(),
        cfg.backend_token.clone(),
    );
    if let Err(e) = zeroclaw_agent::auto_bootstrap::run(&cfg, &agent, &log_client).await {
        tracing::warn!(
            name: "auto_bootstrap.failed",
            error_message = %e,
            "auto-bootstrap failed (non-fatal): {{error_message}}",
        );
    }

    zeroclaw_agent::gateway::serve(cfg, agent)
        .await
        .expect("gateway failed");
}
