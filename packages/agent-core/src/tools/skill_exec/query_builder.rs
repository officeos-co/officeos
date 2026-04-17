//! Converts a parsed CLI command into a GraphQL query string.

use std::collections::HashMap;
use std::hash::BuildHasher;

use super::schema_cache::{ActionInfo, SchemaCache};

/// Build a GraphQL query from parsed CLI input.
pub fn build_query<S: BuildHasher>(
    skill: &str,
    action: &str,
    args: &HashMap<String, String, S>,
    schema: &SchemaCache,
) -> Result<String, String> {
    let action_info = schema
        .get_action(skill, action)
        .ok_or_else(|| format!("Unknown action: {skill} {action}. Use \"{skill} --help\"."))?;

    // Validate required args
    for arg_def in &action_info.args {
        if arg_def.required && !args.contains_key(&arg_def.name) {
            return Err(format!(
                "Missing required argument: --{}\n\nUsage: {} {} {}",
                arg_def.name,
                skill,
                action,
                usage_string(action_info),
            ));
        }
    }

    let gql_args = build_graphql_args(args, action_info);

    let fields = if action_info.return_fields.is_empty() {
        "__typename".to_string()
    } else {
        action_info.return_fields.join(" ")
    };

    let args_str = if gql_args.is_empty() {
        String::new()
    } else {
        format!("({})", gql_args.join(", "))
    };

    Ok(format!(
        "{{ {}{} {{ {} }} }}",
        action_info.graphql_name, args_str, fields
    ))
}

fn build_graphql_args<S: BuildHasher>(
    cli_args: &HashMap<String, String, S>,
    action_info: &ActionInfo,
) -> Vec<String> {
    let mut gql_args = Vec::new();
    for (cli_key, value) in cli_args {
        if let Some(arg_def) = action_info.args.iter().find(|a| a.name == *cli_key) {
            let gql_value = coerce_value(value, &arg_def.type_name);
            gql_args.push(format!("{}: {gql_value}", cli_key));
        } else {
            gql_args.push(format!("{}: \"{}\"", cli_key, escape_graphql_string(value)));
        }
    }
    gql_args
}

fn coerce_value(value: &str, type_name: &str) -> String {
    match type_name {
        "INT" | "Int" => {
            if value.parse::<i64>().is_ok() {
                value.to_string()
            } else {
                format!("\"{}\"", escape_graphql_string(value))
            }
        }
        "FLOAT" | "Float" => {
            if value.parse::<f64>().is_ok() {
                value.to_string()
            } else {
                format!("\"{}\"", escape_graphql_string(value))
            }
        }
        "BOOL" | "Boolean" => match value.to_lowercase().as_str() {
            "true" | "1" | "yes" => "true".to_string(),
            "false" | "0" | "no" => "false".to_string(),
            _ => format!("\"{}\"", escape_graphql_string(value)),
        },
        _ => format!("\"{}\"", escape_graphql_string(value)),
    }
}

fn usage_string(action: &ActionInfo) -> String {
    action
        .args
        .iter()
        .map(|a| {
            if a.required {
                format!("--{} <{}>", a.name, a.type_name)
            } else {
                format!("[--{} <{}>]", a.name, a.type_name)
            }
        })
        .collect::<Vec<_>>()
        .join(" ")
}

fn escape_graphql_string(s: &str) -> String {
    s.replace('\\', "\\\\")
        .replace('"', "\\\"")
        .replace('\n', "\\n")
}
