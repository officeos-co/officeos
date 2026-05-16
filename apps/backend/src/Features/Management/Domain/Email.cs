namespace OffceOs.Domain.Features.Management;

public readonly record struct Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.", nameof(value));

        value = value.Trim().ToLowerInvariant();

        if (!value.Contains('@') || !value.Contains('.'))
            throw new ArgumentException($"Invalid email format: {value}", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => new(value);
}
