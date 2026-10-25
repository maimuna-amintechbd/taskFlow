namespace TaskFlow.Api.Services;

/// <summary>
/// Strict enum parsing for user input. Enum.TryParse is too lenient for an API:
/// it also accepts numbers ("2", even undefined ones like "7"), comma lists
/// ("Todo,Done") and surrounding whitespace. Here only an exact member name is
/// accepted, ignoring case ("high" → High).
/// </summary>
public static class EnumNames
{
    // "where TEnum : struct, Enum" limits the generic type to enum types.
    public static bool TryParse<TEnum>(string? input, out TEnum value)
        where TEnum : struct, Enum
    {
        foreach (var name in Enum.GetNames<TEnum>())
        {
            if (string.Equals(name, input, StringComparison.OrdinalIgnoreCase))
            {
                // Safe: name is one of the enum's own member names.
                value = Enum.Parse<TEnum>(name);
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>The member names as a readable list, e.g. "Todo, InProgress, Done".</summary>
    public static string List<TEnum>()
        where TEnum : struct, Enum =>
        string.Join(", ", Enum.GetNames<TEnum>());
}
