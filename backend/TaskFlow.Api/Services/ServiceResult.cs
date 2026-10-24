namespace TaskFlow.Api.Services;

/// <summary>The three outcomes a service method can report to a controller.</summary>
public enum ResultKind
{
    Ok,
    NotFound,
    Invalid,
}

/// <summary>
/// What a service returns instead of throwing for expected outcomes.
/// The controller turns it into an HTTP response with a switch.
/// </summary>
public class ServiceResult<T>
{
    private ServiceResult(ResultKind kind, T? value, Dictionary<string, string[]> errors)
    {
        Kind = kind;
        Value = value;
        Errors = errors;
    }

    public ResultKind Kind { get; }

    /// <summary>Set only when <see cref="Kind"/> is <see cref="ResultKind.Ok"/>.</summary>
    public T? Value { get; }

    /// <summary>Field errors keyed by camelCase property name; empty unless Invalid.</summary>
    public Dictionary<string, string[]> Errors { get; }

    public static ServiceResult<T> Ok(T value) => new(ResultKind.Ok, value, []);

    public static ServiceResult<T> NotFound() => new(ResultKind.NotFound, default, []);

    public static ServiceResult<T> Invalid(Dictionary<string, string[]> errors) =>
        new(ResultKind.Invalid, default, errors);
}

/// <summary>A result with no value, for operations such as delete.</summary>
public class ServiceResult
{
    private ServiceResult(ResultKind kind, Dictionary<string, string[]> errors)
    {
        Kind = kind;
        Errors = errors;
    }

    public ResultKind Kind { get; }

    public Dictionary<string, string[]> Errors { get; }

    public static ServiceResult Ok() => new(ResultKind.Ok, []);

    public static ServiceResult NotFound() => new(ResultKind.NotFound, []);

    public static ServiceResult Invalid(Dictionary<string, string[]> errors) =>
        new(ResultKind.Invalid, errors);
}
