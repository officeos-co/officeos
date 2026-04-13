//! REST API handlers for heartbeat task management.
//!
//! These endpoints let the dashboard manage heartbeat tasks via the pod gateway,
//! following the same pattern as the cron API routes.

use super::AppState;
use super::api::{HeartbeatTaskAddBody, HeartbeatTaskPatchBody, require_auth};
use axum::{
    extract::{Path, State},
    http::{HeaderMap, StatusCode},
    response::{IntoResponse, Json},
};
use crate::heartbeat::engine::{
    HeartbeatEngine, HeartbeatState, HeartbeatTask, TaskPriority, TaskStatus,
    format_duration_secs, parse_duration_str,
};

/// GET /api/heartbeat/tasks — list all heartbeat tasks with state
pub async fn handle_heartbeat_tasks_list(
    State(state): State<AppState>,
    headers: HeaderMap,
) -> impl IntoResponse {
    if let Err(e) = require_auth(&state, &headers) {
        return e.into_response();
    }

    let config = state.config.lock().clone();
    let tasks = match HeartbeatEngine::read_tasks_from_file(&config.workspace_dir) {
        Ok(t) => t,
        Err(e) => {
            return (
                StatusCode::INTERNAL_SERVER_ERROR,
                Json(serde_json::json!({"error": format!("Failed to read tasks: {e}")})),
            )
                .into_response();
        }
    };

    let task_state = HeartbeatState::load(&config.workspace_dir);

    let tasks_json: Vec<serde_json::Value> = tasks
        .iter()
        .map(|t| {
            let key = t.name.as_deref().unwrap_or(&t.text);
            let state_entry = task_state.tasks.get(key);
            serde_json::json!({
                "name": t.name,
                "text": t.text,
                "prompt": t.prompt,
                "priority": format!("{}", t.priority),
                "status": format!("{}", t.status),
                "interval": t.interval_secs.map(format_duration_secs),
                "interval_secs": t.interval_secs,
                "last_run_at": state_entry.and_then(|s| s.last_run_at).map(|t| t.to_rfc3339()),
                "run_count": state_entry.map(|s| s.run_count).unwrap_or(0),
            })
        })
        .collect();

    Json(serde_json::json!({"tasks": tasks_json})).into_response()
}

/// POST /api/heartbeat/tasks — add a new heartbeat task
pub async fn handle_heartbeat_tasks_add(
    State(state): State<AppState>,
    headers: HeaderMap,
    Json(body): Json<HeartbeatTaskAddBody>,
) -> impl IntoResponse {
    if let Err(e) = require_auth(&state, &headers) {
        return e.into_response();
    }

    let config = state.config.lock().clone();
    let mut tasks = match HeartbeatEngine::read_tasks_from_file(&config.workspace_dir) {
        Ok(t) => t,
        Err(e) => {
            return (
                StatusCode::INTERNAL_SERVER_ERROR,
                Json(serde_json::json!({"error": format!("Failed to read tasks: {e}")})),
            )
                .into_response();
        }
    };

    // Check for duplicate name
    if tasks.iter().any(|t| t.name.as_deref() == Some(&body.name)) {
        return (
            StatusCode::CONFLICT,
            Json(serde_json::json!({"error": format!("Task '{}' already exists", body.name)})),
        )
            .into_response();
    }

    let priority = match body.priority.as_deref() {
        Some("high") => TaskPriority::High,
        Some("low") => TaskPriority::Low,
        _ => TaskPriority::Medium,
    };

    let interval_secs = body.interval.as_deref().map(parse_duration_str);

    tasks.push(HeartbeatTask {
        text: body.prompt.clone(),
        priority,
        status: TaskStatus::Active,
        name: Some(body.name.clone()),
        interval_secs,
        prompt: Some(body.prompt),
    });

    if let Err(e) = HeartbeatEngine::write_tasks_to_file(&config.workspace_dir, &tasks) {
        return (
            StatusCode::INTERNAL_SERVER_ERROR,
            Json(serde_json::json!({"error": format!("Failed to write tasks: {e}")})),
        )
            .into_response();
    }

    Json(serde_json::json!({"status": "ok", "name": body.name})).into_response()
}

/// PATCH /api/heartbeat/tasks/{name} — update an existing task
pub async fn handle_heartbeat_tasks_patch(
    State(state): State<AppState>,
    headers: HeaderMap,
    Path(name): Path<String>,
    Json(body): Json<HeartbeatTaskPatchBody>,
) -> impl IntoResponse {
    if let Err(e) = require_auth(&state, &headers) {
        return e.into_response();
    }

    let config = state.config.lock().clone();
    let mut tasks = match HeartbeatEngine::read_tasks_from_file(&config.workspace_dir) {
        Ok(t) => t,
        Err(e) => {
            return (
                StatusCode::INTERNAL_SERVER_ERROR,
                Json(serde_json::json!({"error": format!("Failed to read tasks: {e}")})),
            )
                .into_response();
        }
    };

    let task = tasks.iter_mut().find(|t| {
        t.name.as_deref() == Some(&name) || t.text == name
    });

    let task = match task {
        Some(t) => t,
        None => {
            return (
                StatusCode::NOT_FOUND,
                Json(serde_json::json!({"error": format!("Task '{name}' not found")})),
            )
                .into_response();
        }
    };

    if let Some(ref prompt) = body.prompt {
        task.prompt = Some(prompt.clone());
        task.text = prompt.clone();
    }

    if let Some(ref interval) = body.interval {
        task.interval_secs = Some(parse_duration_str(interval));
    }

    if let Some(ref priority) = body.priority {
        task.priority = match priority.as_str() {
            "high" => TaskPriority::High,
            "low" => TaskPriority::Low,
            _ => TaskPriority::Medium,
        };
    }

    if let Some(ref status) = body.status {
        task.status = match status.as_str() {
            "paused" | "pause" => TaskStatus::Paused,
            "completed" | "complete" | "done" => TaskStatus::Completed,
            _ => TaskStatus::Active,
        };
    }

    if let Err(e) = HeartbeatEngine::write_tasks_to_file(&config.workspace_dir, &tasks) {
        return (
            StatusCode::INTERNAL_SERVER_ERROR,
            Json(serde_json::json!({"error": format!("Failed to write tasks: {e}")})),
        )
            .into_response();
    }

    Json(serde_json::json!({"status": "ok"})).into_response()
}

/// DELETE /api/heartbeat/tasks/{name} — remove a task by name
pub async fn handle_heartbeat_tasks_delete(
    State(state): State<AppState>,
    headers: HeaderMap,
    Path(name): Path<String>,
) -> impl IntoResponse {
    if let Err(e) = require_auth(&state, &headers) {
        return e.into_response();
    }

    let config = state.config.lock().clone();
    let mut tasks = match HeartbeatEngine::read_tasks_from_file(&config.workspace_dir) {
        Ok(t) => t,
        Err(e) => {
            return (
                StatusCode::INTERNAL_SERVER_ERROR,
                Json(serde_json::json!({"error": format!("Failed to read tasks: {e}")})),
            )
                .into_response();
        }
    };

    let original_len = tasks.len();
    tasks.retain(|t| t.name.as_deref() != Some(&name) && t.text != name);

    if tasks.len() == original_len {
        return (
            StatusCode::NOT_FOUND,
            Json(serde_json::json!({"error": format!("Task '{name}' not found")})),
        )
            .into_response();
    }

    if let Err(e) = HeartbeatEngine::write_tasks_to_file(&config.workspace_dir, &tasks) {
        return (
            StatusCode::INTERNAL_SERVER_ERROR,
            Json(serde_json::json!({"error": format!("Failed to write tasks: {e}")})),
        )
            .into_response();
    }

    Json(serde_json::json!({"status": "ok"})).into_response()
}

/// GET /api/heartbeat/status — current metrics and next tick info
pub async fn handle_heartbeat_status(
    State(state): State<AppState>,
    headers: HeaderMap,
) -> impl IntoResponse {
    if let Err(e) = require_auth(&state, &headers) {
        return e.into_response();
    }

    let config = state.config.lock().clone();
    let health = crate::health::snapshot();

    let heartbeat_health = health.components.get("heartbeat").cloned();

    let tasks = HeartbeatEngine::read_tasks_from_file(&config.workspace_dir)
        .unwrap_or_default();
    let active_count = tasks.iter().filter(|t| t.status == TaskStatus::Active).count();
    let paused_count = tasks.iter().filter(|t| t.status == TaskStatus::Paused).count();

    Json(serde_json::json!({
        "enabled": config.heartbeat.enabled,
        "interval_minutes": config.heartbeat.interval_minutes,
        "two_phase": config.heartbeat.two_phase,
        "adaptive": config.heartbeat.adaptive,
        "total_tasks": tasks.len(),
        "active_tasks": active_count,
        "paused_tasks": paused_count,
        "health": heartbeat_health,
    }))
    .into_response()
}
