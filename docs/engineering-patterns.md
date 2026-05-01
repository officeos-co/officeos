# Engineering Patterns

## Dependency-Hiding Factories

Use a small factory/builder service when a caller only passes dependencies through to construct another object.

```csharp
// Avoid: orchestration service leaks construction details.
await WidgetRegistry.CreateAsync(repo, cache, runtime, requestId, ct);

// Prefer: orchestration service asks for intent.
await widgetRegistryFactory.CreateAsync(requestId, ct);
```

This keeps orchestration code focused on workflow, makes constructor dependencies reflect real responsibilities, and gives the factory one place to own object construction, optional integrations, fallback behavior, and cleanup rules.

Good fit:

- constructing per-request or per-turn registries
- building tool collections from several infrastructure dependencies
- hiding repeated binding/setup mechanics behind one intent-level method

Avoid it when the factory would only wrap a single constructor with no policy or dependency reduction.
