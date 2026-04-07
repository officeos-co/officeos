use super::{SharedProcess, Tunnel, TunnelProcess, kill_shared, new_shared_process};
use anyhow::{Result, bail};
use tokio::io::AsyncBufReadExt;
use tokio::process::Command;

/// OpenVPN Tunnel — uses the `openvpn` CLI to establish a VPN connection.
///
/// Requires the `openvpn` binary installed and accessible. On most systems,
/// OpenVPN requires root/administrator privileges to create tun/tap devices.
///
/// The tunnel exposes the gateway via the VPN network using a configured
/// `advertise_address` (e.g., `"10.8.0.2:42617"`).
pub struct OpenVpnTunnel {
    config_file: String,
    auth_file: Option<String>,
    advertise_address: Option<String>,
    connect_timeout_secs: u64,
    extra_args: Vec<String>,
    proc: SharedProcess,
}

impl OpenVpnTunnel {
    /// Create a new OpenVPN tunnel instance.
    ///
    /// * `config_file` — path to the `.ovpn` configuration file.
    /// * `auth_file` — optional path to a credentials file for `--auth-user-pass`.
    /// * `advertise_address` — optional public address to advertise once connected.
    /// * `connect_timeout_secs` — seconds to wait for the initialization sequence.
    /// * `extra_args` — additional CLI arguments forwarded to the `openvpn` binary.
    pub fn new(
        config_file: String,
        auth_file: Option<String>,
        advertise_address: Option<String>,
        connect_timeout_secs: u64,
        extra_args: Vec<String>,
    ) -> Self {
        Self {
            config_file,
            auth_file,
            advertise_address,
            connect_timeout_secs,
            extra_args,
            proc: new_shared_process(),
        }
    }

    /// Build the openvpn command arguments.
    fn build_args(&self) -> Vec<String> {
        let mut args = vec!["--config".to_string(), self.config_file.clone()];

        if let Some(ref auth) = self.auth_file {
            args.push("--auth-user-pass".to_string());
            args.push(auth.clone());
        }

        args.extend(self.extra_args.iter().cloned());
        args
    }
}

#[async_trait::async_trait]
impl Tunnel for OpenVpnTunnel {
    fn name(&self) -> &str {
        "openvpn"
    }

    /// Spawn the `openvpn` process and wait for the "Initialization Sequence
    /// Completed" marker on stderr. Returns the public URL on success.
    async fn start(&self, local_host: &str, local_port: u16) -> Result<String> {
        // Validate config file exists before spawning
        if !std::path::Path::new(&self.config_file).exists() {
            bail!("OpenVPN config file not found: {}", self.config_file);
        }

        let args = self.build_args();

        let mut child = Command::new("openvpn")
            .args(&args)
            .stdout(std::process::Stdio::null())
            .stderr(std::process::Stdio::piped())
            .kill_on_drop(true)
            .spawn()?;

        // Wait for "Initialization Sequence Completed" in stderr
        let stderr = child
            .stderr
            .take()
            .ok_or_else(|| anyhow::anyhow!("Failed to capture openvpn stderr"))?;

        let mut reader = tokio::io::BufReader::new(stderr).lines();
        let deadline = tokio::time::Instant::now()
            + tokio::time::Duration::from_secs(self.connect_timeout_secs);

        let mut connected = false;
        while tokio::time::Instant::now() < deadline {
            let line =
                tokio::time::timeout(tokio::time::Duration::from_secs(3), reader.next_line()).await;

            match line {
                Ok(Ok(Some(l))) => {
                    tracing::debug!("openvpn: {l}");
                    if l.contains("Initialization Sequence Completed") {
                        connected = true;
                        break;
                    }
                }
                Ok(Ok(None)) => {
                    bail!("OpenVPN process exited before connection was established");
                }
                Ok(Err(e)) => {
                    bail!("Error reading openvpn output: {e}");
                }
                Err(_) => {
                    // Timeout on individual line read, continue waiting
                }
            }
        }

        if !connected {
            child.kill().await.ok();
            bail!(
                "OpenVPN connection timed out after {}s waiting for initialization",
                self.connect_timeout_secs
            );
        }

        let public_url = self
            .advertise_address
            .clone()
            .unwrap_or_else(|| format!("http://{local_host}:{local_port}"));

        // Drain stderr in background to prevent OS pipe buffer from filling and
        // blocking the openvpn process.
        tokio::spawn(async move {
            while let Ok(Some(line)) = reader.next_line().await {
                tracing::trace!("openvpn: {line}");
            }
        });

        let mut guard = self.proc.lock().await;
        *guard = Some(TunnelProcess {
            child,
            public_url: public_url.clone(),
        });

        Ok(public_url)
    }

    /// Kill the openvpn child process and release its resources.
    async fn stop(&self) -> Result<()> {
        kill_shared(&self.proc).await
    }

    /// Return `true` if the openvpn child process is still running.
    async fn health_check(&self) -> bool {
        let guard = self.proc.lock().await;
        guard.as_ref().is_some_and(|tp| tp.child.id().is_some())
    }

    /// Return the public URL if the tunnel has been started.
    fn public_url(&self) -> Option<String> {
        self.proc
            .try_lock()
            .ok()
            .and_then(|g| g.as_ref().map(|tp| tp.public_url.clone()))
    }
}


#[cfg(test)]
#[path = "openvpn.test.rs"]
mod tests;
