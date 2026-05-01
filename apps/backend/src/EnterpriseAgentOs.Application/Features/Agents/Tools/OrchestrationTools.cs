using System.Collections.Concurrent;
using Cronos;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentTaskStore
{
    private readonly ConcurrentDictionary<Guid, List<AgentTaskItem>> _tasks = new();
    private readonly ConcurrentDictionary<Guid, int> _nextIds = new();

    public AgentTaskItem Create(Guid agentId, string subject, string description, string? activeForm)
    {
        var id = _nextIds.AddOrUpdate(agentId, 1, (_, value) => value + 1).ToString();
        var task = new AgentTaskItem(id, subject, description, activeForm, "pending", null, [], []);
        lock (Get(agentId))
            Get(agentId).Add(task);
        return task;
    }

    public IReadOnlyList<AgentTaskItem> List(Guid agentId)
    {
        lock (Get(agentId))
            return Get(agentId).Where(t => t.Status != "deleted").ToList();
    }

    public AgentTaskItem? Get(Guid agentId, string id)
    {
        lock (Get(agentId))
            return Get(agentId).FirstOrDefault(t => t.Id == id && t.Status != "deleted");
    }

    public AgentTaskItem? Update(Guid agentId, string id, Action<AgentTaskItem> update)
    {
        lock (Get(agentId))
        {
            var task = Get(agentId).FirstOrDefault(t => t.Id == id);
            if (task is null) return null;
            update(task);
            return task;
        }
    }

    private List<AgentTaskItem> Get(Guid agentId) => _tasks.GetOrAdd(agentId, _ => []);
}

internal sealed record AgentTaskItem(
    string Id,
    string Subject,
    string Description,
    string? ActiveForm,
    string Status,
    string? Owner,
    List<string> Blocks,
    List<string> BlockedBy)
{
    public string Subject { get; set; } = Subject;
    public string Description { get; set; } = Description;
    public string? ActiveForm { get; set; } = ActiveForm;
    public string Status { get; set; } = Status;
    public string? Owner { get; set; } = Owner;
}

internal sealed class AskUserQuestionTool : IAgentTool
{
    public string Name => "ask_user_question";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("ask_user_question",
        "Ask the user a concise multiple-choice question when a preference or decision is required.",
        new
        {
            type = "object",
            properties = new
            {
                question = new { type = "string", description = "Single question to ask" },
                options = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            label = new { type = "string" },
                            description = new { type = "string" }
                        },
                        required = new[] { "label", "description" }
                    }
                }
            },
            required = new[] { "question", "options" }
        });

    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var question = args.GetProperty("question").GetString() ?? "";
        var options = args.TryGetProperty("options", out var o) && o.ValueKind == JsonValueKind.Array
            ? string.Join("\n", o.EnumerateArray().Select((item, idx) =>
                $"{idx + 1}. {item.GetProperty("label").GetString()}: {item.GetProperty("description").GetString()}"))
            : "";

        return Task.FromResult<AgentResult<ToolResult>>(new ToolResult(true,
            $"Question for user:\n{question}\n\nOptions:\n{options}\n\nStop and wait for the user's answer before proceeding."));
    }
}

internal sealed class TaskCreateTool : IAgentTool
{
    private readonly AgentTaskStore _store;
    private readonly Guid _agentId;
    public TaskCreateTool(AgentTaskStore store, Guid agentId) { _store = store; _agentId = agentId; }
    public string Name => "task_create";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("task_create", "Create a task in the current agent task list.",
        new { type = "object", properties = new { subject = new { type = "string" }, description = new { type = "string" }, active_form = new { type = "string" } }, required = new[] { "subject", "description" } });
    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var task = _store.Create(_agentId, args.GetProperty("subject").GetString() ?? "", args.GetProperty("description").GetString() ?? "", args.TryGetProperty("active_form", out var af) ? af.GetString() : null);
        return Task.FromResult<AgentResult<ToolResult>>(new ToolResult(true, FormatTask(task)));
    }
    internal static string FormatTask(AgentTaskItem t) => $"#{t.Id} [{t.Status}] {t.Subject}" + (string.IsNullOrWhiteSpace(t.Owner) ? "" : $" ({t.Owner})") + (t.BlockedBy.Count > 0 ? $" [blocked by {string.Join(", ", t.BlockedBy.Select(id => "#" + id))}]" : "");
}

internal sealed class TaskListTool : IAgentTool
{
    private readonly AgentTaskStore _store;
    private readonly Guid _agentId;
    public TaskListTool(AgentTaskStore store, Guid agentId) { _store = store; _agentId = agentId; }
    public string Name => "task_list";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe => true;
    public ToolSchema Schema => new("task_list", "List current agent tasks.", new { type = "object", properties = new { } });
    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var tasks = _store.List(_agentId);
        var output = tasks.Count == 0 ? "No tasks found." : string.Join("\n", tasks.Select(TaskCreateTool.FormatTask));
        return Task.FromResult<AgentResult<ToolResult>>(new ToolResult(true, output));
    }
}

internal sealed class TaskGetTool : IAgentTool
{
    private readonly AgentTaskStore _store;
    private readonly Guid _agentId;
    public TaskGetTool(AgentTaskStore store, Guid agentId) { _store = store; _agentId = agentId; }
    public string Name => "task_get";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe => true;
    public ToolSchema Schema => new("task_get", "Get full task details by ID.", new { type = "object", properties = new { task_id = new { type = "string" } }, required = new[] { "task_id" } });
    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var task = _store.Get(_agentId, args.GetProperty("task_id").GetString() ?? "");
        return Task.FromResult<AgentResult<ToolResult>>(task is null
            ? new ToolResult(false, "", "Task not found.")
            : new ToolResult(true, JsonSerializer.Serialize(task, new JsonSerializerOptions { WriteIndented = true })));
    }
}

internal sealed class TaskUpdateTool : IAgentTool
{
    private readonly AgentTaskStore _store;
    private readonly Guid _agentId;
    public TaskUpdateTool(AgentTaskStore store, Guid agentId) { _store = store; _agentId = agentId; }
    public string Name => "task_update";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("task_update", "Update a task status, owner, details, or dependencies.",
        new
        {
            type = "object",
            properties = new
            {
                task_id = new { type = "string" },
                status = new { type = "string", @enum = new[] { "pending", "in_progress", "completed", "deleted" } },
                subject = new { type = "string" },
                description = new { type = "string" },
                active_form = new { type = "string" },
                owner = new { type = "string" },
                add_blocks = new { type = "array", items = new { type = "string" } },
                add_blocked_by = new { type = "array", items = new { type = "string" } }
            },
            required = new[] { "task_id" }
        });
    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var id = args.GetProperty("task_id").GetString() ?? "";
        var task = _store.Update(_agentId, id, t =>
        {
            if (args.TryGetProperty("status", out var s)) t.Status = s.GetString() ?? t.Status;
            if (args.TryGetProperty("subject", out var sub)) t.Subject = sub.GetString() ?? t.Subject;
            if (args.TryGetProperty("description", out var d)) t.Description = d.GetString() ?? t.Description;
            if (args.TryGetProperty("active_form", out var af)) t.ActiveForm = af.GetString();
            if (args.TryGetProperty("owner", out var owner)) t.Owner = owner.GetString();
            AddIds(args, "add_blocks", t.Blocks);
            AddIds(args, "add_blocked_by", t.BlockedBy);
        });
        return Task.FromResult<AgentResult<ToolResult>>(task is null
            ? new ToolResult(false, "", "Task not found.")
            : new ToolResult(true, TaskCreateTool.FormatTask(task)));
    }
    private static void AddIds(JsonElement args, string property, List<string> target)
    {
        if (!args.TryGetProperty(property, out var ids) || ids.ValueKind != JsonValueKind.Array) return;
        foreach (var item in ids.EnumerateArray())
        {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value) && !target.Contains(value)) target.Add(value);
        }
    }
}

internal sealed class CronCreateTool : IAgentTool
{
    private readonly IAgentCronJobRepository _repo;
    private readonly Guid _agentId;
    public CronCreateTool(IAgentCronJobRepository repo, Guid agentId) { _repo = repo; _agentId = agentId; }
    public string Name => "cron_create";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("cron_create", "Schedule a prompt to run later using a five-field cron expression in UTC.",
        new { type = "object", properties = new { name = new { type = "string" }, expression = new { type = "string" }, prompt = new { type = "string" } }, required = new[] { "name", "expression", "prompt" } });
    public Task<ToolValidationResult> ValidateAsync(JsonElement args, CancellationToken ct = default)
    {
        try { Cronos.CronExpression.Parse(args.GetProperty("expression").GetString() ?? ""); return Task.FromResult(ToolValidationResult.Valid); }
        catch (Exception ex) { return Task.FromResult(ToolValidationResult.Invalid($"Invalid cron expression: {ex.Message}")); }
    }
    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var job = await _repo.CreateAsync(_agentId, args.GetProperty("name").GetString() ?? "", args.GetProperty("expression").GetString() ?? "", args.GetProperty("prompt").GetString() ?? "", ct);
        return new ToolResult(true, $"Created cron job {job.Id} '{job.Name}' next_run={job.NextRunAt:O}");
    }
}

internal sealed class CronListTool : IAgentTool
{
    private readonly IAgentCronJobRepository _repo;
    private readonly Guid _agentId;
    public CronListTool(IAgentCronJobRepository repo, Guid agentId) { _repo = repo; _agentId = agentId; }
    public string Name => "cron_list";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public bool IsReadOnly => true;
    public ToolSchema Schema => new("cron_list", "List scheduled cron jobs for this agent.", new { type = "object", properties = new { } });
    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var jobs = await _repo.ListAsync(_agentId, ct);
        return new ToolResult(true, jobs.Count == 0 ? "No cron jobs found." : string.Join("\n", jobs.Select(j => $"{j.Id} [{(j.Enabled ? "enabled" : "disabled")}] {j.Name} {j.Expression} next={j.NextRunAt:O}")));
    }
}

internal sealed class CronDeleteTool : IAgentTool
{
    private readonly IAgentCronJobRepository _repo;
    private readonly Guid _agentId;
    public CronDeleteTool(IAgentCronJobRepository repo, Guid agentId) { _repo = repo; _agentId = agentId; }
    public string Name => "cron_delete";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("cron_delete", "Delete a scheduled cron job by ID.", new { type = "object", properties = new { job_id = new { type = "string" } }, required = new[] { "job_id" } });
    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        if (!Guid.TryParse(args.GetProperty("job_id").GetString(), out var id)) return new ToolResult(false, "", "Invalid job_id.");
        var job = await _repo.GetAsync(id, ct);
        if (job is null || job.AgentId != _agentId) return new ToolResult(false, "", "Cron job not found.");
        var deleted = await _repo.DeleteAsync(id, ct);
        return new ToolResult(deleted, deleted ? $"Deleted cron job {id}." : "", deleted ? null : "Cron job not found.");
    }
}
