namespace OffceOs.Domain.Features.Agents;

/// <summary>
/// A validated personality file name from the known set, with composition ordering.
/// </summary>
public readonly record struct PersonalityFileName : IComparable<PersonalityFileName>
{
    private static readonly string[] OrderedNames =
        ["AGENTS.md", "SOUL.md", "TOOLS.md", "IDENTITY.md", "USER.md", "MEMORY.md", "BOOTSTRAP.md"];

    public string Value { get; }

    /// <summary>Composition order index (0-based). Lower = earlier in system prompt.</summary>
    public int CompositionOrder { get; }

    public PersonalityFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Personality file name cannot be empty.", nameof(value));

        value = value.Trim();
        var index = Array.IndexOf(OrderedNames, value);
        if (index < 0)
            throw new ArgumentException($"Unknown personality file: {value}. Must be one of: {string.Join(", ", OrderedNames)}", nameof(value));

        Value = value;
        CompositionOrder = index;
    }

    public static IReadOnlyList<string> KnownFileNames => OrderedNames;

    public int CompareTo(PersonalityFileName other) => CompositionOrder.CompareTo(other.CompositionOrder);

    public override string ToString() => Value;

    public static implicit operator string(PersonalityFileName name) => name.Value;
    public static explicit operator PersonalityFileName(string value) => new(value);
}
