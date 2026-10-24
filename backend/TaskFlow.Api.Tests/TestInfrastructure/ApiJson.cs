using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskFlow.Api.Tests.TestInfrastructure;

/// <summary>JSON settings matching the API: camelCase names and enums as strings.</summary>
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
