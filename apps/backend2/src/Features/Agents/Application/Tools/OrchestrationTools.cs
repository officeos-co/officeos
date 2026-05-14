namespace OffceOs.Application.Features.Agents;

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
    private readonly AgentTaskStore _agentTaskStore;
    private readonly Guid _agentId;
    public TaskCreateTool(AgentTaskStore store, Guid agentId) { _agentTaskStore = store; _agentId = agentId; }
    public string Name => "task_create";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("task_create", "Create a task in the current agent task list.",
        new { type = "object", properties = new { subject = new { type = "string" }, description = new { type = "string" }, active_form = new { type = "string" } }, required = new[] { "subject", "description" } });
    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var task = _agentTaskStore.Create(_agentId, args.GetProperty("subject").GetString() ?? "", args.GetProperty("description").GetString() ?? "", args.TryGetProperty("active_form", out var af) ? af.GetString() : null);
        return Task.FromResult<AgentResult<ToolResult>>(new ToolResult(true, FormatTask(task)));
    }
    internal static string FormatTask(AgentTaskItem t) => $"#{t.Id} [{t.Status}] {t.Subject}" + (string.IsNullOrWhiteSpace(t.Owner) ? "" : $" ({t.Owner})") + (t.BlockedBy.Count > 0 ? $" [blocked by {string.Join(", ", t.BlockedBy.Select(id => "#" + id))}]" : "");
}

internal sealed class TaskListTool : IAgentTool
{
    private readonly AgentTaskStore _agentTaskStore;
    private readonly Guid _agentId;
    public TaskListTool(AgentTaskStore store, Guid agentId) { _agentTaskStore = store; _agentId = agentId; }
    public string Name => "task_list";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe => true;
    public ToolSchema Schema => new("task_list", "List current agent tasks.", new { type = "object", properties = new { } });
    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var tasks = _agentTaskStore.List(_agentId);
        var output = tasks.Count == 0 ? "No tasks found." : string.Join("\n", tasks.Select(TaskCreateTool.FormatTask));
        return Task.FromResult<AgentResult<ToolResult>>(new ToolResult(true, output));
    }
}

internal sealed class TaskGetTool : IAgentTool
{
    private readonly AgentTaskStore _agentTaskStore;
    private readonly Guid _agentId;
    public TaskGetTool(AgentTaskStore store, Guid agentId) { _agentTaskStore = store; _agentId = agentId; }
    public string Name => "task_get";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe => true;
    public ToolSchema Schema => new("task_get", "Get full task details by ID.", new { type = "object", properties = new { task_id = new { type = "string" } }, required = new[] { "task_id" } });
    public Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var task = _agentTaskStore.Get(_agentId, args.GetProperty("task_id").GetString() ?? "");
        return Task.FromResult<AgentResult<ToolResult>>(task is null
            ? new ToolResult(false, "", "Task not found.")
            : new ToolResult(true, JsonSerializer.Serialize(task, new JsonSerializerOptions { WriteIndented = true })));
    }
}

internal sealed class TaskUpdateTool : IAgentTool
{
    private readonly AgentTaskStore _agentTaskStore;
    private readonly Guid _agentId;
    public TaskUpdateTool(AgentTaskStore store, Guid agentId) { _agentTaskStore = store; _agentId = agentId; }
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
        var task = _agentTaskStore.Update(_agentId, id, t =>
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

internal sealed class RoutineCreateTool : IAgentTool
{
    private readonly IAgentRoutineService _agentRoutineService;
    private readonly Guid _agentId;
    private readonly Guid? _ownerId;
    private readonly Guid? _workspaceId;

    public RoutineCreateTool(IAgentRoutineService agentRoutineService, Guid agentId, Guid? ownerId, Guid? workspaceId)
    {
        _agentRoutineService = agentRoutineService;
        _agentId = agentId;
        _ownerId = ownerId;
        _workspaceId = workspaceId;
    }

    public string Name => "routine_create";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("routine_create", "Create a routine for this agent with schedule, API, or GitHub triggers.",
        new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string" },
                prompt = new { type = "string" },
                schedule_triggers = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string" },
                            expression = new { type = "string", description = "Five-field cron expression in UTC" }
                        },
                        required = new[] { "name", "expression" }
                    }
                },
                api_triggers = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new { name = new { type = "string" } },
                        required = new[] { "name" }
                    }
                },
                github_triggers = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string" },
                            owner = new { type = "string" },
                            repo = new { type = "string" },
                            events = new { type = "array", items = new { type = "string" } },
                            secret = new { type = "string" }
                        },
                        required = new[] { "name", "owner", "repo", "events", "secret" }
                    }
                }
            },
            required = new[] { "name", "prompt" }
        });

    public Task<ToolValidationResult> ValidateAsync(JsonElement args, CancellationToken ct = default)
    {
        var triggerCount = CountArray(args, "schedule_triggers") + CountArray(args, "api_triggers") + CountArray(args, "github_triggers");
        if (triggerCount == 0)
            return Task.FromResult(ToolValidationResult.Invalid("At least one schedule, API, or GitHub trigger is required."));

        if (args.TryGetProperty("schedule_triggers", out var schedules) && schedules.ValueKind == JsonValueKind.Array)
        {
            foreach (var trigger in schedules.EnumerateArray())
            {
                var expression = GetString(trigger, "expression");
                if (string.IsNullOrWhiteSpace(expression))
                    return Task.FromResult(ToolValidationResult.Invalid("Schedule routine triggers require an expression."));

                try { Cronos.CronExpression.Parse(expression); }
                catch (Exception ex) { return Task.FromResult(ToolValidationResult.Invalid($"Invalid cron expression: {ex.Message}")); }
            }
        }

        if (args.TryGetProperty("github_triggers", out var githubTriggers) && githubTriggers.ValueKind == JsonValueKind.Array)
        {
            foreach (var trigger in githubTriggers.EnumerateArray())
            {
                if (!trigger.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array || !events.EnumerateArray().Any())
                    return Task.FromResult(ToolValidationResult.Invalid("GitHub routine triggers require at least one event."));
            }
        }

        return Task.FromResult(ToolValidationResult.Valid);
    }

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        if (!_ownerId.HasValue || !_workspaceId.HasValue)
            return new ToolResult(false, "", "Routine creation requires owner and workspace context.");

        var result = await _agentRoutineService.CreateAsync(
            new CreateAgentRoutineRequest(
                _agentId,
                args.GetProperty("name").GetString() ?? "",
                args.GetProperty("prompt").GetString() ?? "",
                ReadScheduleTriggers(args),
                ReadApiTriggers(args),
                ReadGitHubTriggers(args)),
            _ownerId.Value,
            _workspaceId.Value,
            ct);

        var output = JsonSerializer.Serialize(new
        {
            routine_id = result.Routine.Id,
            name = result.Routine.Name,
            triggers = result.Routine.Triggers.Select(trigger => new
            {
                trigger_id = trigger.Id,
                kind = trigger.Kind,
                name = trigger.Name,
                next_run = trigger.NextRunAt,
            }),
            generated_secrets = result.GeneratedSecrets.Select(secret => new
            {
                trigger_id = secret.TriggerId,
                kind = secret.Kind,
                name = secret.Name,
                secret = secret.Secret,
            }),
        }, new JsonSerializerOptions { WriteIndented = true });

        return new ToolResult(true, output);
    }

    private static int CountArray(JsonElement args, string property)
        => args.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;

    private static IReadOnlyList<CreateScheduleRoutineTriggerRequest> ReadScheduleTriggers(JsonElement args)
        => args.TryGetProperty("schedule_triggers", out var triggers) && triggers.ValueKind == JsonValueKind.Array
            ? triggers.EnumerateArray()
                .Select(trigger => new CreateScheduleRoutineTriggerRequest(
                    GetString(trigger, "name"),
                    GetString(trigger, "expression")))
                .ToList()
            : [];

    private static IReadOnlyList<CreateApiRoutineTriggerRequest> ReadApiTriggers(JsonElement args)
        => args.TryGetProperty("api_triggers", out var triggers) && triggers.ValueKind == JsonValueKind.Array
            ? triggers.EnumerateArray()
                .Select(trigger => new CreateApiRoutineTriggerRequest(GetString(trigger, "name")))
                .ToList()
            : [];

    private static IReadOnlyList<CreateGitHubRoutineTriggerRequest> ReadGitHubTriggers(JsonElement args)
        => args.TryGetProperty("github_triggers", out var triggers) && triggers.ValueKind == JsonValueKind.Array
            ? triggers.EnumerateArray()
                .Select(trigger => new CreateGitHubRoutineTriggerRequest(
                    GetString(trigger, "name"),
                    GetString(trigger, "owner"),
                    GetString(trigger, "repo"),
                    trigger.TryGetProperty("events", out var events) && events.ValueKind == JsonValueKind.Array
                        ? events.EnumerateArray().Select(item => item.GetString() ?? "").ToList()
                        : [],
                    GetString(trigger, "secret")))
                .ToList()
            : [];

    private static string GetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}

internal sealed class RoutineListTool : IAgentTool
{
    private readonly IAgentRoutineRepository _agentRoutineRepository;
    private readonly Guid _agentId;
    public RoutineListTool(IAgentRoutineRepository agentRoutineRepository, Guid agentId) { _agentRoutineRepository = agentRoutineRepository; _agentId = agentId; }
    public string Name => "routine_list";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public bool IsReadOnly => true;
    public ToolSchema Schema => new("routine_list", "List routines for this agent.", new { type = "object", properties = new { } });
    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var routines = await _agentRoutineRepository.ListAsync(new AgentRoutineFilter { AgentId = _agentId }, ct);
        return new ToolResult(true, routines.Count == 0
            ? "No routines found."
            : string.Join("\n", routines.Select(routine => $"{routine.Id} [{(routine.Enabled ? "enabled" : "disabled")}] {routine.Name} triggers={routine.Triggers.Count}")));
    }
}

internal sealed class RoutineDeleteTool : IAgentTool
{
    private readonly IAgentRoutineRepository _agentRoutineRepository;
    private readonly Guid _agentId;
    public RoutineDeleteTool(IAgentRoutineRepository agentRoutineRepository, Guid agentId) { _agentRoutineRepository = agentRoutineRepository; _agentId = agentId; }
    public string Name => "routine_delete";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("routine_delete", "Delete a routine by ID.", new { type = "object", properties = new { routine_id = new { type = "string" } }, required = new[] { "routine_id" } });
    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        if (!Guid.TryParse(args.GetProperty("routine_id").GetString(), out var id)) return new ToolResult(false, "", "Invalid routine_id.");
        var routine = await _agentRoutineRepository.GetByAsync(new AgentRoutineFilter { Id = id }, ct);
        if (routine is null || routine.AgentId != _agentId) return new ToolResult(false, "", "Routine not found.");
        var deleted = await _agentRoutineRepository.DeleteAsync(id, ct);
        return new ToolResult(deleted, deleted ? $"Deleted routine {id}." : "", deleted ? null : "Routine not found.");
    }
}

internal sealed class AgentSpawnTool : IAgentTool
{
    private readonly IAgentRunRepository _agentRunRepository;
    private readonly Guid _agentId;

    public AgentSpawnTool(IAgentRunRepository runs, Guid agentId)
    {
        _agentRunRepository = runs;
        _agentId = agentId;
    }

    public string Name => "agent_spawn";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("agent_spawn",
        "Create a child agent run for independent subagent or fork work. Background mode records the run and returns its ID.",
        new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string" },
                description = new { type = "string" },
                prompt = new { type = "string" },
                mode = new { type = "string", @enum = new[] { "subagent", "fork" } },
                run_in_background = new { type = "boolean" },
                read_only = new { type = "boolean" }
            },
            required = new[] { "name", "prompt", "mode" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var mode = args.TryGetProperty("mode", out var modeProp) ? modeProp.GetString() : "subagent";
        if (mode is not ("subagent" or "fork"))
            return new ToolResult(false, "", "mode must be subagent or fork.");

        var run = await _agentRunRepository.CreateAsync(new AgentRunRecord
        {
            AgentId = _agentId,
            ParentRunId = AgentRunContext.RunId,
            Kind = mode,
            Status = "queued",
            Name = args.TryGetProperty("name", out var name) ? name.GetString() ?? mode : mode,
            Description = args.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Prompt = args.GetProperty("prompt").GetString() ?? "",
        }, ct);

        var payload = JsonSerializer.Serialize(new
        {
            run_id = run.Id,
            status = run.Status,
            mode = run.Kind,
            note = "Run record created. Execution worker dispatch will pick this up when backend subagent scheduling is enabled."
        }, new JsonSerializerOptions { WriteIndented = true });
        return new ToolResult(true, payload);
    }
}

internal sealed class InternalChannelSendTool : IAgentTool
{
    private readonly IChannelService _channelService;
    private readonly Guid _agentId;

    public InternalChannelSendTool(IChannelService channelService, Guid agentId)
    {
        _channelService = channelService;
        _agentId = agentId;
    }

    public string Name => "internal_channel_send";
    public AgentToolKind Kind => AgentToolKind.Planning;
    public ToolSchema Schema => new("internal_channel_send",
        "Send a message from this agent to agents connected through an internal agent channel.",
        new
        {
            type = "object",
            properties = new
            {
                channel_connection_id = new { type = "string", description = "Internal channel connection ID" },
                message = new { type = "string" }
            },
            required = new[] { "channel_connection_id", "message" }
        });

    public async Task<AgentResult<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var rawChannelConnectionId = args.GetProperty("channel_connection_id").GetString();
        if (!Guid.TryParse(rawChannelConnectionId, out var channelConnectionId))
            return new ToolResult(false, "", "channel_connection_id must be a valid GUID.");

        var message = args.GetProperty("message").GetString();
        if (string.IsNullOrWhiteSpace(message))
            return new ToolResult(false, "", "message is required.");

        try
        {
            var receiverIds = await _channelService.SendInternalMessageAsync(_agentId, channelConnectionId, message, ct);
            return new ToolResult(true, JsonSerializer.Serialize(new
            {
                delivered_to_agent_ids = receiverIds,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (InvalidOperationException ex)
        {
            return new ToolResult(false, "", ex.Message);
        }
    }
}
