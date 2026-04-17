//! GraphQL introspection cache — fetches the schema from the backend
//! and generates CLI-style --help text from it.

use std::collections::HashMap;
use std::fmt::Write;
use std::time::{Duration, Instant};

use serde::Deserialize;

use super::parser::HelpLevel;

const CACHE_TTL_SECS: u64 = 300;

#[derive(Debug, Clone, Deserialize)]
struct IntrospectionResponse {
    data: IntrospectionData,
}

#[derive(Debug, Clone, Deserialize)]
struct IntrospectionData {
    #[serde(rename = "__schema")]
    schema: SchemaData,
}

#[derive(Debug, Clone, Deserialize)]
struct SchemaData {
    #[serde(rename = "queryType")]
    query_type: QueryType,
}

#[derive(Debug, Clone, Deserialize)]
struct QueryType {
    fields: Vec<FieldInfo>,
}

#[derive(Debug, Clone, Deserialize)]
pub struct FieldInfo {
    pub name: String,
    pub description: Option<String>,
    #[serde(default)]
    pub args: Vec<ArgInfo>,
    #[serde(rename = "type")]
    pub field_type: TypeRef,
}

#[derive(Debug, Clone, Deserialize)]
pub struct ArgInfo {
    pub name: String,
    pub description: Option<String>,
    #[serde(rename = "type")]
    pub arg_type: TypeRef,
    #[serde(rename = "defaultValue")]
    pub default_value: Option<String>,
}

#[derive(Debug, Clone, Deserialize)]
pub struct TypeRef {
    pub name: Option<String>,
    pub kind: Option<String>,
    #[serde(rename = "ofType")]
    pub of_type: Option<Box<TypeRef>>,
    #[serde(default)]
    pub fields: Option<Vec<ReturnFieldInfo>>,
}

#[derive(Debug, Clone, Deserialize)]
pub struct ReturnFieldInfo {
    pub name: String,
    #[serde(rename = "type")]
    pub field_type: TypeRef,
}

#[derive(Debug, Clone)]
pub struct SkillInfo {
    pub name: String,
    pub actions: Vec<ActionInfo>,
}

#[derive(Debug, Clone)]
pub struct ActionInfo {
    pub cli_name: String,
    pub graphql_name: String,
    pub description: String,
    pub args: Vec<ActionArg>,
    pub return_fields: Vec<String>,
}

#[derive(Debug, Clone)]
pub struct ActionArg {
    pub name: String,
    pub type_name: String,
    pub required: bool,
    pub description: String,
    pub default_value: Option<String>,
}

pub struct SchemaCache {
    graphql_url: String,
    bearer_token: Option<String>,
    skills: Option<HashMap<String, SkillInfo>>,
    last_fetch: Option<Instant>,
}

impl SchemaCache {
    pub fn new(graphql_url: String, bearer_token: Option<String>) -> Self {
        Self {
            graphql_url,
            bearer_token,
            skills: None,
            last_fetch: None,
        }
    }

    pub async fn ensure_loaded(&mut self) -> anyhow::Result<()> {
        if let Some(last) = self.last_fetch {
            if last.elapsed() < Duration::from_secs(CACHE_TTL_SECS) {
                return Ok(());
            }
        }
        self.fetch_schema().await
    }

    pub fn help_text(&self, level: &HelpLevel) -> String {
        let skills = match &self.skills {
            Some(s) => s,
            None => return "Schema not loaded. Run a query first to fetch the schema.".to_string(),
        };

        match level {
            HelpLevel::TopLevel => {
                let mut out = String::from("Available skills:\n");
                let mut names: Vec<_> = skills.keys().collect();
                names.sort();
                for name in names {
                    let skill = &skills[name];
                    let action_names: Vec<_> =
                        skill.actions.iter().map(|a| a.cli_name.as_str()).collect();
                    let _ = writeln!(
                        out,
                        "  {:<14} actions: {}",
                        name,
                        action_names.join(", ")
                    );
                }
                out.push_str("\nUsage: <skill> <action> [--flags]\n");
                out.push_str("Use \"<skill> --help\" for details.\n");
                out
            }
            HelpLevel::Skill(skill_name) => {
                let Some(skill) = skills.get(skill_name) else {
                    return format!("Unknown skill: {skill_name}. Use --help to list skills.");
                };
                let mut out = format!("{}:\n\n", skill.name);
                for action in &skill.actions {
                    let spaced = action.cli_name.replace('_', " ");
                    if spaced != action.cli_name {
                        let _ = write!(
                            out,
                            "  {} {} (or: {} {})",
                            skill.name, spaced, skill.name, action.cli_name
                        );
                    } else {
                        let _ = write!(out, "  {} {}", skill.name, action.cli_name);
                    }
                    if !action.description.is_empty() {
                        let _ = write!(out, "  — {}", action.description);
                    }
                    out.push('\n');
                    for arg in &action.args {
                        let req = if arg.required { " (required)" } else { "" };
                        let def = arg
                            .default_value
                            .as_ref()
                            .map(|d| format!(", default: {d}"))
                            .unwrap_or_default();
                        let _ = writeln!(
                            out,
                            "    --{:<20} {}{}{}",
                            format!("{} {}", arg.name, arg.type_name),
                            arg.description,
                            req,
                            def,
                        );
                    }
                    out.push('\n');
                }
                out
            }
            HelpLevel::Action(skill_name, action_name) => {
                let Some(skill) = skills.get(skill_name) else {
                    return format!("Unknown skill: {skill_name}");
                };
                let Some(action) = skill.actions.iter().find(|a| a.cli_name == *action_name)
                else {
                    return format!(
                        "Unknown action: {action_name}. Use \"{skill_name} --help\" to list actions."
                    );
                };
                let mut out = format!(
                    "{} {} — {}\n\nArguments:\n",
                    skill.name, action.cli_name, action.description
                );
                for arg in &action.args {
                    let req = if arg.required { " (required)" } else { "" };
                    let def = arg
                        .default_value
                        .as_ref()
                        .map(|d| format!(", default: {d}"))
                        .unwrap_or_default();
                    let _ = writeln!(
                        out,
                        "  --{:<20} {}{}{}",
                        format!("{} {}", arg.name, arg.type_name),
                        arg.description,
                        req,
                        def,
                    );
                }
                if !action.return_fields.is_empty() {
                    let _ = writeln!(out, "\nReturns: {}", action.return_fields.join(", "));
                }
                out
            }
        }
    }

    pub fn get_action(&self, skill: &str, action: &str) -> Option<&ActionInfo> {
        self.skills
            .as_ref()?
            .get(skill)?
            .actions
            .iter()
            .find(|a| a.cli_name == action)
    }

    async fn fetch_schema(&mut self) -> anyhow::Result<()> {
        let query = r#"{
            __schema {
                queryType {
                    fields {
                        name
                        description
                        args {
                            name
                            description
                            defaultValue
                            type { name kind ofType { name kind ofType { name kind } } }
                        }
                        type {
                            name
                            kind
                            ofType {
                                name
                                kind
                                fields { name type { name kind ofType { name kind } } }
                            }
                            fields { name type { name kind ofType { name kind } } }
                        }
                    }
                }
            }
        }"#;

        let client = reqwest::Client::builder()
            .timeout(Duration::from_secs(10))
            .build()?;

        let mut req = client
            .post(&self.graphql_url)
            .header("content-type", "application/json")
            .json(&serde_json::json!({ "query": query }));
        if let Some(token) = &self.bearer_token {
            req = req.bearer_auth(token);
        }

        let resp = req.send().await?;
        if !resp.status().is_success() {
            anyhow::bail!("GraphQL introspection failed: HTTP {}", resp.status());
        }

        let body: IntrospectionResponse = resp.json().await?;
        let skills = parse_schema_to_skills(&body.data.schema.query_type.fields);
        self.skills = Some(skills);
        self.last_fetch = Some(Instant::now());

        tracing::info!(
            skill_count = self.skills.as_ref().map(|s| s.len()).unwrap_or(0),
            "GraphQL skill schema cached"
        );

        Ok(())
    }
}

/// Parse GraphQL query fields into skill groupings.
///
/// Convention: field names use `skill_action` format (underscore-separated).
/// E.g. `github_list_issues` -> skill="github", action="list_issues".
fn parse_schema_to_skills(fields: &[FieldInfo]) -> HashMap<String, SkillInfo> {
    let mut skills: HashMap<String, SkillInfo> = HashMap::new();

    for field in fields {
        // Split on first underscore: skill_action
        let Some((skill_name, action_name)) = field.name.split_once('_') else {
            continue;
        };

        let args: Vec<ActionArg> = field
            .args
            .iter()
            .filter(|a| a.name != "ct")
            .map(|a| {
                let (type_name, required) = resolve_type(&a.arg_type);
                ActionArg {
                    name: a.name.clone(),
                    type_name,
                    required,
                    description: a.description.clone().unwrap_or_default(),
                    default_value: a.default_value.clone(),
                }
            })
            .collect();

        let return_fields = extract_return_fields(&field.field_type);

        let action = ActionInfo {
            cli_name: action_name.to_string(),
            graphql_name: field.name.clone(),
            description: field.description.clone().unwrap_or_default(),
            args,
            return_fields,
        };

        skills
            .entry(skill_name.to_string())
            .or_insert_with(|| SkillInfo {
                name: skill_name.to_string(),
                actions: Vec::new(),
            })
            .actions
            .push(action);
    }

    skills
}

fn resolve_type(t: &TypeRef) -> (String, bool) {
    match t.kind.as_deref() {
        Some("NON_NULL") => {
            let inner = t
                .of_type
                .as_ref()
                .map(|o| type_name(o))
                .unwrap_or("Any".to_string());
            (inner, true)
        }
        _ => (type_name(t), false),
    }
}

fn type_name(t: &TypeRef) -> String {
    if let Some(name) = &t.name {
        return match name.as_str() {
            "Int" => "INT".to_string(),
            "Float" => "FLOAT".to_string(),
            "String" => "STRING".to_string(),
            "Boolean" => "BOOL".to_string(),
            _ => name.clone(),
        };
    }
    if let Some(of) = &t.of_type {
        let inner = type_name(of);
        return match t.kind.as_deref() {
            Some("LIST") => format!("[{inner}]"),
            _ => inner,
        };
    }
    "Any".to_string()
}

fn extract_return_fields(t: &TypeRef) -> Vec<String> {
    let concrete = unwrap_type(t);
    concrete
        .fields
        .as_ref()
        .map(|fields| fields.iter().map(|f| f.name.clone()).collect())
        .unwrap_or_default()
}

fn unwrap_type(t: &TypeRef) -> &TypeRef {
    match t.kind.as_deref() {
        Some("NON_NULL" | "LIST") => t.of_type.as_ref().map(|o| unwrap_type(o)).unwrap_or(t),
        _ => t,
    }
}
