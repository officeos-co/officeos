namespace OffceOs.Domain.Features.AgentUsage;

public static class AgentUsageActivityKinds
{
    public const string Coding = "coding";
    public const string Debugging = "debugging";
    public const string FeatureDevelopment = "feature_development";
    public const string Refactoring = "refactoring";
    public const string Testing = "testing";
    public const string Exploration = "exploration";
    public const string Planning = "planning";
    public const string Delegation = "delegation";
    public const string GitOps = "git_ops";
    public const string BuildDeploy = "build_deploy";
    public const string Brainstorming = "brainstorming";
    public const string Conversation = "conversation";
    public const string General = "general";
}

public static class AgentUsageContextPartKinds
{
    public const string SystemPrompt = "system_prompt";
    public const string UserMessage = "user_message";
    public const string AssistantMessage = "assistant_message";
    public const string ToolResult = "tool_result";
    public const string ToolCall = "tool_call";
    public const string ToolSchema = "tool_schema";
    public const string DeferredToolCatalog = "deferred_tool_catalog";
    public const string RequestOverhead = "request_overhead";
    public const string Other = "other";
}

public static class AgentUsageOutcomeKinds
{
    public const string Success = "success";
    public const string Failed = "failed";
}
