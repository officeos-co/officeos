//! Converts a parsed CLI command into a GraphQL query string.

use std::collections::HashMap;
use std::hash::BuildHasher;

use super::schema_cache::{ActionInfo, SchemaCache};

/// Build a GraphQL query from parsed CLI input.
///
/// Maps CLI names back to GraphQL field names, coerces argument types
/// based on the cached schema, and selects all scalar return fields.
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

    // Build argument list
    let gql_args = build_graphql_args(args, action_info);

    // Select all return fields
    let fields = if action_info.return_fields.is_empty() {
        // If we don't know the fields, use __typename as a fallback
        "__typename".to_string()
    } else {
        action_info.return_fields.join(" ")
    };

    let args_str = if gql_args.is_empty() {
        String::new()
    } else {
        format!("({})", gql_args.join(", "))
    };

    // Handle nested return types (e.g. NotionSearchResult has a `results` field
    // that contains the actual list of NotionPage objects)
    let field_selection = build_field_selection(&fields, action_info);

    Ok(format!(
        "{{ {}{} {{ {} }} }}",
        action_info.graphql_name, args_str, field_selection
    ))
}

fn build_graphql_args<S: BuildHasher>(
    cli_args: &HashMap<String, String, S>,
    action_info: &ActionInfo,
) -> Vec<String> {
    let mut gql_args = Vec::new();
    for (cli_key, value) in cli_args {
        // Find the matching arg definition
        if let Some(arg_def) = action_info.args.iter().find(|a| a.name == *cli_key) {
            let graphql_key = snake_to_camel(cli_key);
            let gql_value = coerce_value(value, &arg_def.type_name);
            gql_args.push(format!("{graphql_key}: {gql_value}"));
        } else {
            // Unknown arg — pass as string anyway (GraphQL will error if invalid)
            let graphql_key = snake_to_camel(cli_key);
            gql_args.push(format!("{graphql_key}: \"{}\"", escape_graphql_string(value)));
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
        _ => {
            // Default: treat as string
            format!("\"{}\"", escape_graphql_string(value))
        }
    }
}

fn build_field_selection(fields: &str, _action_info: &ActionInfo) -> String {
    // If the return type has sub-object fields (like NotionSearchResult.results),
    // we need to nest the selection. For now, just use the top-level fields.
    // HotChocolate flattens lists, so this usually works.
    fields.to_string()
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

fn snake_to_camel(s: &str) -> String {
    let mut out = String::new();
    let mut capitalize_next = false;
    for ch in s.chars() {
        if ch == '_' {
            capitalize_next = true;
        } else if capitalize_next {
            out.push(ch.to_uppercase().next().unwrap_or(ch));
            capitalize_next = false;
        } else {
            out.push(ch);
        }
    }
    out
}

fn escape_graphql_string(s: &str) -> String {
    s.replace('\\', "\\\\")
        .replace('"', "\\\"")
        .replace('\n', "\\n")
}
