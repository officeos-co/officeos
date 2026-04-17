#[tokio::main]
async fn main() {
    tracing_subscriber::fmt::init();
    let (agent_id, backend_url) = zeroclaw_agent::env::load_env().expect("missing env");
    tracing::info!(agent_id = %agent_id, "loading environment");
    let cfg = zeroclaw_agent::bootstrap::bootstrap(agent_id, backend_url)
        .await
        .expect("bootstrap failed");
    tracing::info!(agent_id = %cfg.agent_id, name = %cfg.display_name, "bootstrap complete");
    let cfg = std::sync::Arc::new(cfg);
    zeroclaw_agent::personality::seed(&cfg.memory_dir, &cfg.system_prompt)
        .await
        .expect("personality seed failed");
    tracing::info!("personality seeded");
    let agent = std::sync::Arc::new(zeroclaw_agent::agent::Agent::new(cfg.clone()));

    // Send BOOTSTRAP.md as the first message and sync to backend logs.
    let log_client = zeroclaw_agent::log_client::LogClient::new(
        cfg.backend_url.clone(),
        cfg.backend_token.clone(),
    );
    if let Err(e) = zeroclaw_agent::auto_bootstrap::run(&cfg, &agent, &log_client).await {
        tracing::warn!(error = %e, "auto-bootstrap failed (non-fatal)");
    }

    zeroclaw_agent::gateway::serve(cfg, agent)
        .await
        .expect("gateway failed");
}
