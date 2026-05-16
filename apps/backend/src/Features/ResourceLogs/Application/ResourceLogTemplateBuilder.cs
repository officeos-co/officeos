namespace OffceOs.Features.ResourceLogs.Application;

internal sealed record ResourceLogTemplateItem(string Name, object? Value);

internal static class ResourceLogTemplateBuilder
{
    private static readonly Regex PlaceholderPattern = new("\\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\\}", RegexOptions.Compiled);

    public static string Render(
        string messageTemplate,
        IReadOnlyList<object?> values,
        out IReadOnlyList<ResourceLogTemplateItem> templateValues)
    {
        var index = 0;
        var captured = new List<ResourceLogTemplateItem>();
        var rendered = PlaceholderPattern.Replace(messageTemplate, match =>
        {
            var name = match.Groups["name"].Value;
            var value = index < values.Count ? values[index++] : null;
            captured.Add(new ResourceLogTemplateItem(ToCamelCase(name), SafeValue(value)));
            return FormatValue(value);
        });

        templateValues = captured;
        return rendered;
    }

    public static string? MetadataJson(
        string messageTemplate,
        IReadOnlyList<ResourceLogTemplateItem> templateValues,
        Exception? exception)
    {
        if (templateValues.Count == 0 && exception is null)
            return null;

        var metadata = new Dictionary<string, object?>
        {
            ["messageTemplate"] = messageTemplate,
        };

        foreach (var value in templateValues)
            metadata[value.Name] = value.Value;

        if (exception is not null)
        {
            metadata["exceptionType"] = exception.GetType().Name;
            metadata["exceptionMessage"] = SanitizeExceptionMessage(exception.Message);
        }

        return JsonSerializer.Serialize(metadata);
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "",
        DateTime dateTime => dateTime.ToString("O"),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };

    private static object? SafeValue(object? value) => value switch
    {
        null => null,
        string text => RedactIfSensitive(text),
        Guid or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => value,
        DateTime dateTime => dateTime.ToString("O"),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
        _ => RedactIfSensitive(Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.GetType().Name),
    };

    private static string RedactIfSensitive(string value)
    {
        if (value.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("authorization", StringComparison.OrdinalIgnoreCase))
        {
            return "[redacted]";
        }

        return value;
    }

    private static string SanitizeExceptionMessage(string message) => RedactIfSensitive(message);

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) || char.IsLower(value[0])
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
}
