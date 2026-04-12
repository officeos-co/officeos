//! Remote memory backend — delegates to the backend's Postgres-backed API.
//!
//! Used when `ZEROCLAW_AGENT_ID` is set (managed mode). All memory operations
//! are HTTP calls to the backend at `/api/agents/me/memory/*`, authenticated
//! with the agent UUID as bearer token.

use super::traits::{Memory, MemoryCategory, MemoryEntry, ProceduralMessage};
use async_trait::async_trait;
use reqwest::Client;
use serde::{Deserialize, Serialize};
use std::fmt::Write;

pub struct RemoteMemory {
    client: Client,
    base_url: String,
    agent_id: String,
}

impl RemoteMemory {
    pub fn new(base_url: &str, agent_id: &str) -> Self {
        Self {
            client: Client::new(),
            base_url: base_url.trim_end_matches('/').to_string(),
            agent_id: agent_id.to_string(),
        }
    }

    fn url(&self, path: &str) -> String {
        format!("{}/api/agents/me/{}", self.base_url, path.trim_start_matches('/'))
    }

    fn auth_header(&self) -> String {
        format!("Bearer {}", self.agent_id)
    }
}

/// Wire format for memory entries returned by the backend.
#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct RemoteMemoryEntry {
    id: String,
    key: String,
    content: String,
    category: String,
    namespace: String,
    session_id: Option<String>,
    importance: Option<f64>,
    superseded_by: Option<String>,
    created_at: String,
}

impl From<RemoteMemoryEntry> for MemoryEntry {
    fn from(r: RemoteMemoryEntry) -> Self {
        let category = match r.category.as_str() {
            "core" => MemoryCategory::Core,
            "daily" => MemoryCategory::Daily,
            "conversation" => MemoryCategory::Conversation,
            other => MemoryCategory::Custom(other.to_string()),
        };
        MemoryEntry {
            id: r.id,
            key: r.key,
            content: r.content,
            category,
            timestamp: r.created_at,
            session_id: r.session_id,
            score: None,
            namespace: r.namespace,
            importance: r.importance,
            superseded_by: r.superseded_by,
        }
    }
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
struct StoreRequest<'a> {
    key: &'a str,
    content: &'a str,
    #[serde(skip_serializing_if = "Option::is_none")]
    category: Option<&'a str>,
    #[serde(skip_serializing_if = "Option::is_none")]
    namespace: Option<&'a str>,
    #[serde(skip_serializing_if = "Option::is_none")]
    session_id: Option<&'a str>,
    #[serde(skip_serializing_if = "Option::is_none")]
    importance: Option<f64>,
}

#[async_trait]
impl Memory for RemoteMemory {
    fn name(&self) -> &str {
        "remote"
    }

    async fn store(
        &self,
        key: &str,
        content: &str,
        category: MemoryCategory,
        session_id: Option<&str>,
    ) -> anyhow::Result<()> {
        let cat = category.to_string();
        let body = StoreRequest {
            key,
            content,
            category: Some(&cat),
            namespace: None,
            session_id,
            importance: None,
        };

        let resp = self
            .client
            .post(self.url("memory"))
            .header("Authorization", self.auth_header())
            .json(&body)
            .send()
            .await?;

        if !resp.status().is_success() {
            let status = resp.status();
            let text = resp.text().await.unwrap_or_default();
            anyhow::bail!("remote memory store failed ({status}): {text}");
        }
        Ok(())
    }

    async fn recall(
        &self,
        query: &str,
        limit: usize,
        session_id: Option<&str>,
        since: Option<&str>,
        until: Option<&str>,
    ) -> anyhow::Result<Vec<MemoryEntry>> {
        let mut url = format!("memory/search?query={}&limit={}", urlencoding::encode(query), limit);
        if let Some(sid) = session_id {
            let _ = write!(url, "&sessionId={}", urlencoding::encode(sid));
        }
        if let Some(s) = since {
            let _ = write!(url, "&since={}", urlencoding::encode(s));
        }
        if let Some(u) = until {
            let _ = write!(url, "&until={}", urlencoding::encode(u));
        }

        let resp = self
            .client
            .get(self.url(&url))
            .header("Authorization", self.auth_header())
            .send()
            .await?;

        if !resp.status().is_success() {
            let status = resp.status();
            let text = resp.text().await.unwrap_or_default();
            anyhow::bail!("remote memory recall failed ({status}): {text}");
        }

        let entries: Vec<RemoteMemoryEntry> = resp.json().await?;
        Ok(entries.into_iter().map(Into::into).collect())
    }

    async fn get(&self, key: &str) -> anyhow::Result<Option<MemoryEntry>> {
        let resp = self
            .client
            .get(self.url(&format!("memory/{}", urlencoding::encode(key))))
            .header("Authorization", self.auth_header())
            .send()
            .await?;

        if resp.status() == reqwest::StatusCode::NOT_FOUND {
            return Ok(None);
        }
        if !resp.status().is_success() {
            let status = resp.status();
            let text = resp.text().await.unwrap_or_default();
            anyhow::bail!("remote memory get failed ({status}): {text}");
        }

        let entry: RemoteMemoryEntry = resp.json().await?;
        Ok(Some(entry.into()))
    }

    async fn list(
        &self,
        category: Option<&MemoryCategory>,
        session_id: Option<&str>,
    ) -> anyhow::Result<Vec<MemoryEntry>> {
        let mut params = Vec::new();
        if let Some(cat) = category {
            params.push(format!("category={}", urlencoding::encode(&cat.to_string())));
        }
        if let Some(sid) = session_id {
            params.push(format!("sessionId={}", urlencoding::encode(sid)));
        }

        let path = if params.is_empty() {
            "memory".to_string()
        } else {
            format!("memory?{}", params.join("&"))
        };

        let resp = self
            .client
            .get(self.url(&path))
            .header("Authorization", self.auth_header())
            .send()
            .await?;

        if !resp.status().is_success() {
            let status = resp.status();
            let text = resp.text().await.unwrap_or_default();
            anyhow::bail!("remote memory list failed ({status}): {text}");
        }

        let entries: Vec<RemoteMemoryEntry> = resp.json().await?;
        Ok(entries.into_iter().map(Into::into).collect())
    }

    async fn forget(&self, key: &str) -> anyhow::Result<bool> {
        let resp = self
            .client
            .delete(self.url(&format!("memory/{}", urlencoding::encode(key))))
            .header("Authorization", self.auth_header())
            .send()
            .await?;

        if resp.status() == reqwest::StatusCode::NOT_FOUND {
            return Ok(false);
        }
        if !resp.status().is_success() {
            let status = resp.status();
            let text = resp.text().await.unwrap_or_default();
            anyhow::bail!("remote memory forget failed ({status}): {text}");
        }
        Ok(true)
    }

    async fn count(&self) -> anyhow::Result<usize> {
        // List all and count — the backend doesn't have a dedicated count endpoint
        let entries = self.list(None, None).await?;
        Ok(entries.len())
    }

    async fn health_check(&self) -> bool {
        // Try listing with limit=1 to check connectivity
        self.client
            .get(self.url("memory?limit=1"))
            .header("Authorization", self.auth_header())
            .send()
            .await
            .map(|r| r.status().is_success())
            .unwrap_or(false)
    }

    async fn store_with_metadata(
        &self,
        key: &str,
        content: &str,
        category: MemoryCategory,
        session_id: Option<&str>,
        namespace: Option<&str>,
        importance: Option<f64>,
    ) -> anyhow::Result<()> {
        let cat = category.to_string();
        let body = StoreRequest {
            key,
            content,
            category: Some(&cat),
            namespace,
            session_id,
            importance,
        };

        let resp = self
            .client
            .post(self.url("memory"))
            .header("Authorization", self.auth_header())
            .json(&body)
            .send()
            .await?;

        if !resp.status().is_success() {
            let status = resp.status();
            let text = resp.text().await.unwrap_or_default();
            anyhow::bail!("remote memory store_with_metadata failed ({status}): {text}");
        }
        Ok(())
    }

    async fn store_procedural(
        &self,
        _messages: &[ProceduralMessage],
        _session_id: Option<&str>,
    ) -> anyhow::Result<()> {
        // Procedural memory extraction is not yet supported by the remote backend
        Ok(())
    }
}
