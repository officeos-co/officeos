use super::*;
use crate::skills::SkillTool;

fn sample_http_tool() -> SkillTool {
    let mut args = HashMap::new();
    args.insert("city".to_string(), "City name to look up".to_string());

    SkillTool {
        name: "get_weather".to_string(),
        description: "Fetch weather for a city".to_string(),
        kind: "http".to_string(),
        command: "https://api.example.com/weather?city={{city}}".to_string(),
        args,
    }
}

#[test]
fn skill_http_tool_name_is_prefixed() {
    let tool = SkillHttpTool::new("weather_skill", &sample_http_tool());
    assert_eq!(tool.name(), "weather_skill.get_weather");
}

#[test]
fn skill_http_tool_description() {
    let tool = SkillHttpTool::new("weather_skill", &sample_http_tool());
    assert_eq!(tool.description(), "Fetch weather for a city");
}

#[test]
fn skill_http_tool_parameters_schema() {
    let tool = SkillHttpTool::new("weather_skill", &sample_http_tool());
    let schema = tool.parameters_schema();

    assert_eq!(schema["type"], "object");
    assert!(schema["properties"]["city"].is_object());
    assert_eq!(schema["properties"]["city"]["type"], "string");
}

#[test]
fn skill_http_tool_substitute_args() {
    let tool = SkillHttpTool::new("weather_skill", &sample_http_tool());
    let result = tool.substitute_args(&serde_json::json!({"city": "London"}));
    assert_eq!(result, "https://api.example.com/weather?city=London");
}

#[test]
fn skill_http_tool_spec_roundtrip() {
    let tool = SkillHttpTool::new("weather_skill", &sample_http_tool());
    let spec = tool.spec();
    assert_eq!(spec.name, "weather_skill.get_weather");
    assert_eq!(spec.description, "Fetch weather for a city");
    assert_eq!(spec.parameters["type"], "object");
}

#[test]
fn skill_http_tool_empty_args() {
    let st = SkillTool {
        name: "ping".to_string(),
        description: "Ping endpoint".to_string(),
        kind: "http".to_string(),
        command: "https://api.example.com/ping".to_string(),
        args: HashMap::new(),
    };
    let tool = SkillHttpTool::new("s", &st);
    let schema = tool.parameters_schema();
    assert!(schema["properties"].as_object().unwrap().is_empty());
}
