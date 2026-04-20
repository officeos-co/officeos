# Agent Error Handling — Result Pattern Design

## Problem

Error handling is ad-hoc. Each service catches exceptions independently, sometimes logs to AgentLogRecord, sometimes only to Serilog. Developers can forget to catch, miscategorize, or silently swallow errors. No compile-time enforcement.

## Decision

Adopt the Result pattern. Services at infrastructure boundaries return `Result<T>` instead of throwing. AgentTurnService (the orchestrator) matches on results and logs errors through a single path. The compiler forces every caller to handle both success and failure.

## Domain Primitives (Domain layer, zero dependencies)

### `Result<T>` (struct)

```csharp
public readonly struct Result<T>
{
    public T Value { get; }
    public AgentError Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public static Result<T> Ok(T value);
    public static Result<T> Fail(AgentError error);

    public TOut Match<TOut>(Func<T, TOut> success, Func<AgentError, TOut> failure);
    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> next);

    public static implicit operator Result<T>(T value);
    public static implicit operator Result<T>(AgentError error);
}
```

### `AgentError` (sealed record)

```csharp
public sealed record AgentError(
    AgentErrorCategory Category,
    string Message,
    string? Detail = null
);
```

### `AgentErrorCategory` (enum)

```csharp
public enum AgentErrorCategory
{
    PodConnection,
    LlmCall,
    ToolExecution,
    SkillExecution,
    TurnOrchestration,
    Memory,
    Configuration
}
```

## Infrastructure Boundaries — Only Place Try/Catch Lives

External calls (HTTP, WebSocket, DB for tools) wrap in try/catch and return `Result<T>`:

- `PodConnection.ExecuteCommandAsync` → `Result<string>`
- `LlmProviderDispatcher.SendAsync` → `Result<LlmResponse>`
- `SkillRuntimeClient.ExecuteAsync` → `Result<SkillResult>`
- Tool execution methods → `Result<ToolResult>`
- Memory read/write → `Result<T>`

## AgentTurnService — Single Error Logging Point

The orchestrator matches on each Result. On failure, calls `TurnLogger.Error(AgentError)` which:
1. Maps `AgentError` → `AgentLogRecord` with category
2. Persists to DB
3. Broadcasts via subscription

No try/catch in the turn loop except the outermost safety net in `SendMessageAsync`.

## Database Change

Add `ErrorCategory` column (nullable string) to `agent_logs` table. Populated only when `Type == Error`. Enables dashboard filtering by failure type.

## What Changes

| Component | Before | After |
|-----------|--------|-------|
| PodConnection | Throws | Returns `Result<T>` |
| LlmProviderDispatcher | Throws | Returns `Result<T>` |
| Tool execution | `ToolResult` bool | Returns `Result<ToolResult>` |
| SkillRuntimeClient | Throws | Returns `Result<T>` |
| MemoryTools | Throws | Returns `Result<T>` |
| AgentTurnService | Try/catch everywhere | Matches on Results, single error logging |
| AgentLogRecord | Flat `Error` type | `Error` + `ErrorCategory` column |
| TurnLogger | No AgentError overload | `Error(AgentError)` method |

## What Stays

- AgentLogService still writes records
- TurnLogger still exists (gains overload)
- Fire-and-forget Task.Run safety net stays as last resort
- Serilog stays as secondary diagnostic channel
