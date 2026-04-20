# Agent Error Handling — Result Pattern Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace ad-hoc try/catch error handling with a compile-time-enforced Result pattern so every agent-context error flows through a single logging path into AgentLogRecord.

**Architecture:** Domain primitives (`Result<T>`, `AgentError`, `AgentErrorCategory`) live in Domain layer. Infrastructure boundaries (pod, LLM, skill-runtime) return `Result<T>` instead of throwing. AgentTurnService matches on results and logs errors through TurnLogger. A new `ErrorCategory` column on AgentLogRecord enables dashboard filtering.

**Tech Stack:** C# 12, .NET 9, EF Core (Postgres), Hot Chocolate GraphQL

---

### Task 1: Domain Primitives — `AgentErrorCategory`, `AgentError`, `Result<T>`

**Files:**
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Primitives/AgentErrorCategory.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Primitives/AgentError.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Primitives/Result.cs`
- Test: `apps/backend/EnterpriseAgentOs.Api.Tests/ResultTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// EnterpriseAgentOs.Api.Tests/ResultTests.cs
using EnterpriseAgentOs.Domain.Primitives;

namespace EnterpriseAgentOs.Api.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_result_is_success()
    {
        Result<int> result = Result<int>.Ok(42);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Fail_result_is_failure()
    {
        var error = new AgentError(AgentErrorCategory.LlmCall, "timeout");
        Result<int> result = Result<int>.Fail(error);
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Equal(AgentErrorCategory.LlmCall, result.Error.Category);
        Assert.Equal("timeout", result.Error.Message);
    }

    [Fact]
    public void Match_routes_to_success_branch()
    {
        Result<int> result = Result<int>.Ok(10);
        var output = result.Match(
            success: v => $"got {v}",
            failure: e => $"error: {e.Message}");
        Assert.Equal("got 10", output);
    }

    [Fact]
    public void Match_routes_to_failure_branch()
    {
        var error = new AgentError(AgentErrorCategory.PodConnection, "unreachable");
        Result<int> result = Result<int>.Fail(error);
        var output = result.Match(
            success: v => $"got {v}",
            failure: e => $"error: {e.Message}");
        Assert.Equal("error: unreachable", output);
    }

    [Fact]
    public void Implicit_conversion_from_value()
    {
        Result<string> result = "hello";
        Assert.True(result.IsSuccess);
        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Implicit_conversion_from_error()
    {
        var error = new AgentError(AgentErrorCategory.Configuration, "missing key");
        Result<string> result = error;
        Assert.True(result.IsFailure);
        Assert.Equal("missing key", result.Error.Message);
    }

    [Fact]
    public void Bind_chains_success()
    {
        Result<int> result = Result<int>.Ok(5);
        var chained = result.Bind(v => Result<string>.Ok($"value={v}"));
        Assert.True(chained.IsSuccess);
        Assert.Equal("value=5", chained.Value);
    }

    [Fact]
    public void Bind_short_circuits_on_failure()
    {
        var error = new AgentError(AgentErrorCategory.ToolExecution, "failed");
        Result<int> result = Result<int>.Fail(error);
        var chained = result.Bind(v => Result<string>.Ok($"value={v}"));
        Assert.True(chained.IsFailure);
        Assert.Equal("failed", chained.Error.Message);
    }

    [Fact]
    public void Accessing_value_on_failure_throws()
    {
        var error = new AgentError(AgentErrorCategory.Memory, "not found");
        Result<int> result = Result<int>.Fail(error);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Accessing_error_on_success_throws()
    {
        Result<int> result = Result<int>.Ok(1);
        Assert.Throws<InvalidOperationException>(() => _ = result.Error);
    }

    [Fact]
    public void AgentError_detail_is_optional()
    {
        var error = new AgentError(AgentErrorCategory.SkillExecution, "bad params");
        Assert.Null(error.Detail);

        var detailed = new AgentError(AgentErrorCategory.SkillExecution, "bad params", "stack trace here");
        Assert.Equal("stack trace here", detailed.Detail);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd apps/backend && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj --filter ResultTests -v q`
Expected: FAIL — namespace `EnterpriseAgentOs.Domain.Primitives` does not exist

- [ ] **Step 3: Implement the domain primitives**

```csharp
// src/EnterpriseAgentOs.Domain/Primitives/AgentErrorCategory.cs
namespace EnterpriseAgentOs.Domain.Primitives;

public enum AgentErrorCategory
{
    PodConnection,
    LlmCall,
    ToolExecution,
    SkillExecution,
    TurnOrchestration,
    Memory,
    Configuration,
}
```

```csharp
// src/EnterpriseAgentOs.Domain/Primitives/AgentError.cs
namespace EnterpriseAgentOs.Domain.Primitives;

/// <summary>
/// Structured agent error with category for dashboard filtering.
/// </summary>
public sealed record AgentError(
    AgentErrorCategory Category,
    string Message,
    string? Detail = null);
```

```csharp
// src/EnterpriseAgentOs.Domain/Primitives/Result.cs
namespace EnterpriseAgentOs.Domain.Primitives;

/// <summary>
/// Discriminated result type that forces callers to handle success and failure.
/// Used at infrastructure boundaries to replace exceptions with values.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly AgentError? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result. Check IsSuccess first.");

    public AgentError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result. Check IsFailure first.");

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(AgentError error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(AgentError error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> success, Func<AgentError, TOut> failure)
        => IsSuccess ? success(_value!) : failure(_error!);

    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> next)
        => IsSuccess ? next(_value!) : Result<TNext>.Fail(_error!);

    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(AgentError error) => Fail(error);
}
```

- [ ] **Step 4: Add global using to Domain**

Add to `src/EnterpriseAgentOs.Domain/GlobalUsings.cs`:
```csharp
global using EnterpriseAgentOs.Domain.Primitives;
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `cd apps/backend && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj --filter ResultTests -v q`
Expected: 11 passed, 0 failed

- [ ] **Step 6: Commit**

```bash
git add apps/backend/src/EnterpriseAgentOs.Domain/Primitives/ apps/backend/src/EnterpriseAgentOs.Domain/GlobalUsings.cs apps/backend/EnterpriseAgentOs.Api.Tests/ResultTests.cs
git commit -m "feat: add Result<T>, AgentError, AgentErrorCategory domain primitives"
```

---

### Task 2: Add `ErrorCategory` Column to `AgentLogRecord` + Migration

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Domain/Models/AgentLogRecord.cs`
- Modify: `apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/EaosDbContext.cs:209-218`
- Create: EF migration (auto-generated)

- [ ] **Step 1: Add property to AgentLogRecord**

Add after line 72 in `AgentLogRecord.cs` (before the closing brace):
```csharp
    /// <summary>
    /// Error category for Error-type entries. Null for non-error entries.
    /// Enables dashboard filtering by failure type (e.g. PodConnection, LlmCall).
    /// </summary>
    [MaxLength(32)]
    public string? ErrorCategory { get; set; }
```

- [ ] **Step 2: Configure column in EaosDbContext**

Add inside the `modelBuilder.Entity<AgentLogRecord>` block at line 209:
```csharp
e.Property(l => l.ErrorCategory).HasMaxLength(32);
```

- [ ] **Step 3: Create and verify migration**

Run:
```bash
cd apps/backend
dotnet ef migrations add AddErrorCategoryToAgentLog --project src/EnterpriseAgentOs.Infrastructure --startup-project src/EnterpriseAgentOs.Api
```
Expected: Migration file created in `src/EnterpriseAgentOs.Infrastructure/Persistence/Migrations/`

- [ ] **Step 4: Build to verify**

Run: `cd apps/backend && dotnet build EnterpriseAgentOs.sln --nologo -v q`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add apps/backend/src/EnterpriseAgentOs.Domain/Models/AgentLogRecord.cs apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/EaosDbContext.cs apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/Migrations/
git commit -m "feat: add ErrorCategory column to AgentLogRecord"
```

---

### Task 3: Update TurnLogger — `Error(AgentError)` Overload

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/TurnLogger.cs`
- Modify: `apps/backend/EnterpriseAgentOs.Api.Tests/AgentLoggingTests.cs`

- [ ] **Step 1: Write the failing test**

Add to `AgentLoggingTests.cs`:
```csharp
[Fact]
public void Error_with_AgentError_logs_category()
{
    var logs = new List<AgentLogRecord>();
    var logger = new TurnLogger(Guid.NewGuid(), "corr-1", logs.Add);

    var error = new AgentError(AgentErrorCategory.LlmCall, "Provider timeout", "HttpRequestException: ...");
    logger.Error(error);

    Assert.Single(logs);
    Assert.Equal(AgentLogType.Error, logs[0].Type);
    Assert.Equal("LlmCall: Provider timeout", logs[0].Content);
    Assert.Equal("LlmCall", logs[0].ErrorCategory);
}

[Fact]
public void Error_with_AgentError_includes_detail_when_present()
{
    var logs = new List<AgentLogRecord>();
    var logger = new TurnLogger(Guid.NewGuid(), "corr-1", logs.Add);

    var error = new AgentError(AgentErrorCategory.PodConnection, "Unreachable", "WebSocketException: connection refused");
    logger.Error(error);

    Assert.Contains("WebSocketException", logs[0].Content);
}

[Fact]
public void Error_string_overload_still_works()
{
    var logs = new List<AgentLogRecord>();
    var logger = new TurnLogger(Guid.NewGuid(), "corr-1", logs.Add);

    logger.Error("something broke");

    Assert.Single(logs);
    Assert.Equal(AgentLogType.Error, logs[0].Type);
    Assert.Equal("something broke", logs[0].Content);
    Assert.Null(logs[0].ErrorCategory);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd apps/backend && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj --filter "Error_with_AgentError" -v q`
Expected: FAIL — no `Error(AgentError)` overload

- [ ] **Step 3: Add the overload to TurnLogger**

Add after the existing `Error(string message)` method (line 68 in TurnLogger.cs):
```csharp
    public void Error(AgentError agentError)
    {
        var content = agentError.Detail is not null
            ? $"{agentError.Category}: {agentError.Message}\n{agentError.Detail}"
            : $"{agentError.Category}: {agentError.Message}";

        _emit(new AgentLogRecord
        {
            AgentId = _agentId,
            Type = AgentLogType.Error,
            Content = content,
            ErrorCategory = agentError.Category.ToString(),
            CorrelationId = _correlationId,
            Time = DateTime.UtcNow,
        });
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd apps/backend && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj --filter "AgentLoggingTests" -v q`
Expected: All pass

- [ ] **Step 5: Commit**

```bash
git add apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/TurnLogger.cs apps/backend/EnterpriseAgentOs.Api.Tests/AgentLoggingTests.cs
git commit -m "feat: add Error(AgentError) overload to TurnLogger with category"
```

---

### Task 4: Convert PodConnection to Return `Result<T>`

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/PodConnection.cs`
- Modify: `apps/backend/EnterpriseAgentOs.Api.Tests/AgentTurnTests.cs` (if pod tests exist)

- [ ] **Step 1: Change `ConnectAsync` return type**

In `PodConnection.cs`, change `ConnectAsync` (line 16) from:
```csharp
public async Task ConnectAsync(string podName, string ns, Guid agentId, CancellationToken ct)
```
to:
```csharp
public async Task<Result<bool>> ConnectAsync(string podName, string ns, Guid agentId, CancellationToken ct)
```

Wrap the body in try/catch, return `Result<bool>`:
```csharp
public async Task<Result<bool>> ConnectAsync(string podName, string ns, Guid agentId, CancellationToken ct)
{
    try
    {
        var uri = new Uri($"ws://{podName}.default.svc.cluster.local:42617/ws?agent={agentId}");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        await _ws.ConnectAsync(uri, cts.Token);
        return true;
    }
    catch (Exception ex)
    {
        return new AgentError(AgentErrorCategory.PodConnection, $"Failed to connect to pod: {ex.Message}", ex.ToString());
    }
}
```

- [ ] **Step 2: Change `ExecuteAsync` return type**

Change `ExecuteAsync` (line 28) from:
```csharp
public async Task<(string Output, int ExitCode)> ExecuteAsync(string command, CancellationToken ct)
```
to:
```csharp
public async Task<Result<(string Output, int ExitCode)>> ExecuteAsync(string command, CancellationToken ct)
```

Wrap body in try/catch:
```csharp
public async Task<Result<(string Output, int ExitCode)>> ExecuteAsync(string command, CancellationToken ct)
{
    try
    {
        await SendRawAsync(command + "\n", ct);
        return await ReadUntilPromptAsync(ct);
    }
    catch (Exception ex)
    {
        return new AgentError(AgentErrorCategory.PodConnection, $"Command execution failed: {ex.Message}", ex.ToString());
    }
}
```

- [ ] **Step 3: Build to verify**

Run: `cd apps/backend && dotnet build EnterpriseAgentOs.sln --nologo -v q`
Expected: Build errors in AgentTurnService (callers not updated yet) — this is expected. Note the exact errors for Task 6.

- [ ] **Step 4: Commit**

```bash
git add apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/PodConnection.cs
git commit -m "feat: PodConnection returns Result<T> instead of throwing"
```

---

### Task 5: Convert LlmProviderDispatcher to Return `Result<T>`

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Infrastructure/Adapters/LlmProviders/LlmProviderDispatcher.cs`

- [ ] **Step 1: Change `DispatchAsync` return type**

Change `DispatchAsync` (line 43) from:
```csharp
public async Task<HttpResponseMessage> DispatchAsync(string provider, string apiKey, string model, JsonElement requestBody, CancellationToken ct)
```
to:
```csharp
public async Task<Result<HttpResponseMessage>> DispatchAsync(string provider, string apiKey, string model, JsonElement requestBody, CancellationToken ct)
```

Wrap the body: keep the existing logic but catch exceptions and return `Result`:
```csharp
public async Task<Result<HttpResponseMessage>> DispatchAsync(string provider, string apiKey, string model, JsonElement requestBody, CancellationToken ct)
{
    try
    {
        var (baseProvider, _) = ParseProviderModel(provider);

        if (!IsSupported(baseProvider))
            return new AgentError(AgentErrorCategory.Configuration, $"Unsupported provider: {provider}");

        var response = baseProvider switch
        {
            "anthropic" => await DispatchAnthropicAsync(apiKey, model, requestBody, ct),
            _ => await DispatchOpenAiCompatAsync(
                GetBaseUrl(baseProvider), apiKey, model, requestBody, ct),
        };

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return new AgentError(AgentErrorCategory.LlmCall,
                $"LLM returned {(int)response.StatusCode}", body);
        }

        return response;
    }
    catch (TaskCanceledException ex)
    {
        return new AgentError(AgentErrorCategory.LlmCall, "LLM call timed out", ex.Message);
    }
    catch (HttpRequestException ex)
    {
        return new AgentError(AgentErrorCategory.LlmCall, $"LLM call failed: {ex.Message}", ex.ToString());
    }
    catch (Exception ex)
    {
        return new AgentError(AgentErrorCategory.LlmCall, $"Unexpected LLM error: {ex.Message}", ex.ToString());
    }
}
```

Note: You will need to verify the exact internal method names (`ParseProviderModel`, `GetBaseUrl`) by reading the file. The pattern above matches the current structure — adapt to exact names.

- [ ] **Step 2: Build to verify**

Run: `cd apps/backend && dotnet build EnterpriseAgentOs.sln --nologo -v q`
Expected: Build errors in AgentTurnService (caller not updated) — expected, fixed in Task 6.

- [ ] **Step 3: Commit**

```bash
git add apps/backend/src/EnterpriseAgentOs.Infrastructure/Adapters/LlmProviders/LlmProviderDispatcher.cs
git commit -m "feat: LlmProviderDispatcher returns Result<T> instead of throwing"
```

---

### Task 6: Convert AgentTurnService to Use Result Pattern

This is the critical task — replace all try/catch blocks with Result matching.

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/AgentTurnService.cs`

- [ ] **Step 1: Replace pod connection try/catch (lines 83-94)**

Replace:
```csharp
using var pod = new PodConnection();
try
{
    var podStart = Stopwatch.GetTimestamp();
    await pod.ConnectAsync(agent.PodName, "default", agentId, ct);
    log.PodConnected((int)Stopwatch.GetElapsedTime(podStart).TotalMilliseconds);
}
catch (Exception ex)
{
    log.Error($"Failed to connect to pod: {ex.Message}");
    return;
}
```

With:
```csharp
using var pod = new PodConnection();
var podStart = Stopwatch.GetTimestamp();
var podResult = await pod.ConnectAsync(agent.PodName, "default", agentId, ct);
if (podResult.IsFailure)
{
    log.Error(podResult.Error);
    return;
}
log.PodConnected((int)Stopwatch.GetElapsedTime(podStart).TotalMilliseconds);
```

- [ ] **Step 2: Replace LLM call try/catch (lines 178-188)**

Replace:
```csharp
HttpResponseMessage llmResponse;
var llmStart = Stopwatch.GetTimestamp();
try
{
    llmResponse = await _llmProviderDispatcher.DispatchAsync(provider, apiKey, agent.Model ?? "auto", requestBody, ct);
}
catch (Exception ex)
{
    log.Error($"LLM call failed: {ex.Message}");
    return;
}
```

With:
```csharp
var llmStart = Stopwatch.GetTimestamp();
var llmResult = await _llmProviderDispatcher.DispatchAsync(provider, apiKey, agent.Model ?? "auto", requestBody, ct);
if (llmResult.IsFailure)
{
    log.Error(llmResult.Error);
    return;
}
var llmResponse = llmResult.Value;
```

- [ ] **Step 3: Replace JSON parse try/catch (lines 226-233)**

Replace:
```csharp
JsonElement args;
try
{
    args = JsonSerializer.Deserialize<JsonElement>(tc.Arguments);
}
catch
{
    args = JsonSerializer.SerializeToElement(new { });
}
```

With:
```csharp
JsonElement args;
try
{
    args = JsonSerializer.Deserialize<JsonElement>(tc.Arguments);
}
catch
{
    // Malformed tool arguments from LLM — not a categorized agent error,
    // just a fallback to empty object so the tool can handle gracefully.
    args = JsonSerializer.SerializeToElement(new { });
}
```

(This one stays as try/catch — it's an LLM output parse, not an infrastructure boundary. The fallback is intentional.)

- [ ] **Step 4: Build and run all tests**

Run: `cd apps/backend && dotnet build EnterpriseAgentOs.sln --nologo -v q && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj -v q`
Expected: 0 errors, all tests pass

- [ ] **Step 5: Commit**

```bash
git add apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/AgentTurnService.cs
git commit -m "feat: AgentTurnService uses Result pattern instead of try/catch"
```

---

### Task 7: Convert Tool Execution to Return `Result<ToolResult>`

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/Tools/IAgentTool.cs`
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/Tools/BashTools.cs`
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/Tools/HttpTools.cs`
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/Tools/MemoryTools.cs`
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/Tools/SkillExecTool.cs`
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/Tools/SkillReadTool.cs`

- [ ] **Step 1: Update IAgentTool interface**

Change `ExecuteAsync` return type in `IAgentTool.cs`:
```csharp
Task<Result<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct = default);
```

- [ ] **Step 2: Update each tool to return `Result<ToolResult>`**

For each tool, wrap the body in try/catch and return `Result<ToolResult>`:

Pattern for tools that already return `ToolResult`:
```csharp
public async Task<Result<ToolResult>> ExecuteAsync(JsonElement args, CancellationToken ct)
{
    try
    {
        // ... existing logic ...
        return new ToolResult(true, output);
    }
    catch (Exception ex)
    {
        return new AgentError(AgentErrorCategory.ToolExecution, $"{Name} failed: {ex.Message}", ex.ToString());
    }
}
```

For `SkillExecTool`, use `AgentErrorCategory.SkillExecution` instead.
For memory tools, use `AgentErrorCategory.Memory` instead.

Each tool file needs updating — apply the same pattern to all `ExecuteAsync` methods in:
- `BashTools.cs`: ShellTool, FileReadTool, FileWriteTool, FileEditTool, ContentSearchTool, GlobSearchTool
- `HttpTools.cs`: HttpRequestTool, WebFetchTool
- `MemoryTools.cs`: MemoryStoreTool, MemoryRecallTool, MemoryForgetTool
- `SkillExecTool.cs`: SkillExecTool
- `SkillReadTool.cs`: SkillReadTool

- [ ] **Step 3: Update ToolRegistry.DispatchAsync**

The registry dispatches tool calls — update its return type and callers. Find `ToolRegistry` (likely in the same Tools folder) and change `DispatchAsync` to return `Result<ToolResult>`.

- [ ] **Step 4: Update AgentTurnService tool dispatch (lines 240-266)**

Replace:
```csharp
var result = await registry.DispatchAsync(tc.Name, args, ct);
var toolDurationMs = (int)Stopwatch.GetElapsedTime(toolStart).TotalMilliseconds;
var output = result.Success ? result.Output : $"[error] {result.Error}\n{result.Output}";
```

With:
```csharp
var toolDispatchResult = await registry.DispatchAsync(tc.Name, args, ct);
var toolDurationMs = (int)Stopwatch.GetElapsedTime(toolStart).TotalMilliseconds;

if (toolDispatchResult.IsFailure)
{
    log.Error(toolDispatchResult.Error);
    log.ToolCallResult(tc.Name, false, toolDispatchResult.Error.Message, toolDurationMs);
    history.Push(new ChatMessage
    {
        Role = "tool",
        Content = $"[error] {toolDispatchResult.Error.Message}",
        ToolCallId = tc.Id,
    });
    continue;
}

var result = toolDispatchResult.Value;
var output = result.Success ? result.Output : $"[error] {result.Error}\n{result.Output}";
```

- [ ] **Step 5: Build and run all tests**

Run: `cd apps/backend && dotnet build EnterpriseAgentOs.sln --nologo -v q && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj -v q`
Expected: 0 errors, all tests pass

- [ ] **Step 6: Commit**

```bash
git add apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/Tools/ apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/AgentTurnService.cs
git commit -m "feat: all agent tools return Result<ToolResult> instead of throwing"
```

---

### Task 8: Expose `ErrorCategory` in GraphQL + Update Dashboard Query

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Domain/DTOs/AgentLogs/` (find the DTO that `ToDto()` maps to)
- Modify: `apps/dashboard/` (the GraphQL query for agent logs)

- [ ] **Step 1: Find and update the DTO**

Find the DTO that `AgentLogRecord.ToDto()` maps to. Add `ErrorCategory` string field to it.

- [ ] **Step 2: Update `ToDto()` extension method**

Map `ErrorCategory` from the record to the DTO.

- [ ] **Step 3: Update the dashboard GraphQL query**

In the dashboard, find the `AgentLogs` query (visible in the error log: `agentLogs(agentId: $agentId, limit: $limit)`) and add `errorCategory` to the selected fields.

- [ ] **Step 4: Build both projects**

Run:
```bash
cd apps/backend && dotnet build EnterpriseAgentOs.sln --nologo -v q
cd ../../apps/dashboard && npm run build
```
Expected: Both succeed

- [ ] **Step 5: Commit**

```bash
git add apps/backend/ apps/dashboard/
git commit -m "feat: expose ErrorCategory in GraphQL and dashboard query"
```

---

### Task 9: Final Verification — Full Test Suite + Build

- [ ] **Step 1: Run full test suite**

Run: `cd apps/backend && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj -v q`
Expected: All tests pass

- [ ] **Step 2: Run full build**

Run: `cd apps/backend && dotnet build EnterpriseAgentOs.sln --nologo -v q`
Expected: 0 errors

- [ ] **Step 3: Update CLAUDE.md**

Add to the anti-patterns section in `apps/backend/CLAUDE.md`:
```markdown
- **Throwing exceptions from agent-context code.** Infrastructure boundaries (pod, LLM, skill-runtime, tools) return `Result<T>`. AgentTurnService matches on results. Only the outermost safety net in `AgentLogService.SendMessageAsync` uses try/catch.
- **Writing AgentLogRecord errors without a category.** Use `TurnLogger.Error(AgentError)` which sets `ErrorCategory` automatically. The string overload is for legacy compatibility only.
```

- [ ] **Step 4: Commit**

```bash
git add apps/backend/CLAUDE.md
git commit -m "docs: update CLAUDE.md with Result pattern conventions"
```
