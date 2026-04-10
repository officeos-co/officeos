//! Deterministic CLI-style parser for skill commands.
//!
//! Grammar:
//!   command     = [skill] [action] [flags] [--help]
//!   skill       = identifier
//!   action      = identifier
//!   flags       = (--key value)*
//!   --help      = introspection at current level
//!
//! Examples:
//!   ""                              → Help(TopLevel)
//!   "--help"                        → Help(TopLevel)
//!   "notion"                        → Help(Skill("notion"))
//!   "notion --help"                 → Help(Skill("notion"))
//!   "notion search --help"          → Help(Action("notion", "search"))
//!   "notion search --query test"    → Query { skill, action, args }

use std::collections::HashMap;

#[derive(Debug, Clone, PartialEq)]
pub enum HelpLevel {
    TopLevel,
    Skill(String),
    Action(String, String),
}

#[derive(Debug, Clone, PartialEq)]
pub enum ParsedCommand {
    Help(HelpLevel),
    Query {
        skill: String,
        action: String,
        args: HashMap<String, String>,
    },
}

pub fn parse(input: &str) -> ParsedCommand {
    let tokens = tokenize(input);

    if tokens.is_empty() {
        return ParsedCommand::Help(HelpLevel::TopLevel);
    }

    // Check for top-level --help
    if tokens.len() == 1 && tokens[0] == "--help" {
        return ParsedCommand::Help(HelpLevel::TopLevel);
    }

    let skill = tokens[0].to_lowercase();

    // "notion" or "notion --help"
    if tokens.len() == 1 {
        return ParsedCommand::Help(HelpLevel::Skill(skill));
    }
    if tokens.len() == 2 && tokens[1] == "--help" {
        return ParsedCommand::Help(HelpLevel::Skill(skill));
    }

    let action = tokens[1].to_lowercase();

    // Remaining tokens are flags or --help
    let flag_tokens = &tokens[2..];

    // Check for --help among flags
    if flag_tokens.iter().any(|t| t == "--help") {
        return ParsedCommand::Help(HelpLevel::Action(skill, action));
    }

    // Parse --key value pairs
    let args = parse_flags(flag_tokens);

    ParsedCommand::Query {
        skill,
        action,
        args,
    }
}

fn parse_flags(tokens: &[String]) -> HashMap<String, String> {
    let mut args = HashMap::new();
    let mut i = 0;
    while i < tokens.len() {
        let token = &tokens[i];
        if let Some(key) = token.strip_prefix("--") {
            if i + 1 < tokens.len() && !tokens[i + 1].starts_with("--") {
                args.insert(key.to_string(), tokens[i + 1].clone());
                i += 2;
            } else {
                // Flag without value — treat as boolean true
                args.insert(key.to_string(), "true".to_string());
                i += 1;
            }
        } else {
            // Positional arg — skip (shouldn't happen in well-formed input)
            i += 1;
        }
    }
    args
}

/// Tokenize respecting quoted strings.
fn tokenize(input: &str) -> Vec<String> {
    let mut tokens = Vec::new();
    let mut current = String::new();
    let mut in_quote = false;
    let mut quote_char = '"';

    for ch in input.chars() {
        if in_quote {
            if ch == quote_char {
                in_quote = false;
            } else {
                current.push(ch);
            }
        } else if ch == '"' || ch == '\'' {
            in_quote = true;
            quote_char = ch;
        } else if ch.is_whitespace() {
            if !current.is_empty() {
                tokens.push(std::mem::take(&mut current));
            }
        } else {
            current.push(ch);
        }
    }
    if !current.is_empty() {
        tokens.push(current);
    }
    tokens
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn empty_is_top_help() {
        assert_eq!(parse(""), ParsedCommand::Help(HelpLevel::TopLevel));
    }

    #[test]
    fn help_flag_is_top_help() {
        assert_eq!(parse("--help"), ParsedCommand::Help(HelpLevel::TopLevel));
    }

    #[test]
    fn skill_only_is_skill_help() {
        assert_eq!(
            parse("notion"),
            ParsedCommand::Help(HelpLevel::Skill("notion".into()))
        );
    }

    #[test]
    fn skill_help_flag() {
        assert_eq!(
            parse("notion --help"),
            ParsedCommand::Help(HelpLevel::Skill("notion".into()))
        );
    }

    #[test]
    fn action_help() {
        assert_eq!(
            parse("notion search --help"),
            ParsedCommand::Help(HelpLevel::Action("notion".into(), "search".into()))
        );
    }

    #[test]
    fn query_with_args() {
        let cmd = parse("notion search --query \"meeting notes\" --page_size 5");
        match cmd {
            ParsedCommand::Query {
                skill,
                action,
                args,
            } => {
                assert_eq!(skill, "notion");
                assert_eq!(action, "search");
                assert_eq!(args.get("query").unwrap(), "meeting notes");
                assert_eq!(args.get("page_size").unwrap(), "5");
            }
            _ => panic!("expected Query"),
        }
    }

    #[test]
    fn query_no_args() {
        let cmd = parse("github repos");
        match cmd {
            ParsedCommand::Query {
                skill,
                action,
                args,
            } => {
                assert_eq!(skill, "github");
                assert_eq!(action, "repos");
                assert!(args.is_empty());
            }
            _ => panic!("expected Query"),
        }
    }
}
