using System.Text.Json;
using OffceOs.Domain.Features.Providers;

namespace OffceOs.Tests.Shared;

public static class LlmProviderDispatcherTestData
{
    public static JsonElement RequestBody(string model) => JsonSerializer.SerializeToElement(new
    {
        model,
        messages = new[] { new { role = "user", content = "hello" } },
        stream = true,
    });

    public static string ModelFor(ProviderDefinition provider) =>
        provider.Models.FirstOrDefault()?.Id ?? $"{provider.Slug}/test-model";
}
