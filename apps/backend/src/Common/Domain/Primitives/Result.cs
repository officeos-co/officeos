namespace OffceOs.Common.Domain.Primitives;

/// <summary>
/// Discriminated result type that forces callers to handle success and failure.
/// Used at infrastructure boundaries to replace exceptions with values.
/// Named AgentResult to avoid conflict with other Result types.
/// </summary>
public readonly struct AgentResult<T>
{
    private readonly T? _value;
    private readonly AgentError? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed AgentResult. Check IsSuccess first.");

    public AgentError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful AgentResult. Check IsFailure first.");

    private AgentResult(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private AgentResult(AgentError error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public static AgentResult<T> Ok(T value) => new(value);
    public static AgentResult<T> Fail(AgentError error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> success, Func<AgentError, TOut> failure)
        => IsSuccess ? success(_value!) : failure(_error!);

    public AgentResult<TNext> Bind<TNext>(Func<T, AgentResult<TNext>> next)
        => IsSuccess ? next(_value!) : AgentResult<TNext>.Fail(_error!);

    public static implicit operator AgentResult<T>(T value) => Ok(value);
    public static implicit operator AgentResult<T>(AgentError error) => Fail(error);
}
