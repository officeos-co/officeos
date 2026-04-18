// Rust guideline compliant 2026-02-21

//! Agent runtime for `EnterpriseAgentOS` pods.
//!
//! The authoritative contract lives in `API.md` at the crate root.

pub mod agent;
pub mod auto_bootstrap;
pub mod bootstrap;
pub mod config;
pub mod env;
pub mod error;
pub mod gateway;
pub mod llm;
pub mod log_client;
pub mod personality;
pub mod tools;
