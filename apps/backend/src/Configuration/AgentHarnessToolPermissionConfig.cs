namespace OffceOs.Configuration;

public sealed class AgentHarnessToolPermissionConfig
{
    public HashSet<string> EagerToolNames { get; init; } = new(StringComparer.Ordinal)
    {
        "tool_search",
        "shell",
        "file_read",
        "file_write",
        "file_edit",
        "content_search",
        "glob_search",
        "memory_store",
        "memory_recall",
        "memory_forget",
        "ask_user_question",
        "task_create",
        "task_list",
        "task_get",
        "task_update",
        "routine_create",
        "routine_list",
        "routine_delete",
        "internal_channel_send",
        "http_request",
        "web_fetch",
    };

    public HashSet<string> SelfManagementToolNames { get; init; } = new(StringComparer.Ordinal)
    {
        "routine_create",
        "routine_list",
        "routine_delete",
    };

    public HashSet<string> DeferredToolNamePrefixes { get; init; } = new(StringComparer.Ordinal)
    {
        "browser__",
    };

    public bool DeferIntegrationTools { get; init; } = true;

    public bool DeferUnknownBuiltinTools { get; init; } = true;
}
