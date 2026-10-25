using System.Net;
using System.Text.Json;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests;

public class OpenApiTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task OpenApi_Get_ReturnsDocument()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApi_GetProjectById_Documents200Response()
    {
        var client = _factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        var responses = document.RootElement
            .GetProperty("paths").GetProperty("/api/projects/{id}")
            .GetProperty("get").GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _));
    }

    [Fact]
    public async Task OpenApi_UpdateAndDeleteProject_DocumentResponses()
    {
        var client = _factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        var path = document.RootElement.GetProperty("paths").GetProperty("/api/projects/{id}");
        var putResponses = path.GetProperty("put").GetProperty("responses")
            .EnumerateObject().Select(r => r.Name).Order().ToArray();
        var deleteResponses = path.GetProperty("delete").GetProperty("responses")
            .EnumerateObject().Select(r => r.Name).Order().ToArray();
        Assert.Equal(["200", "400", "404"], putResponses);
        Assert.Equal(["204", "404"], deleteResponses);
    }

    [Theory]
    [InlineData("status", new[] { "Todo", "InProgress", "Done" })]
    [InlineData("priority", new[] { "Low", "Medium", "High" })]
    public async Task OpenApi_TaskDtoEnums_AreDocumentedAsStringNames(string property, string[] expected)
    {
        var client = _factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));

        // TaskDto.status/priority point ("$ref") at a shared enum schema; follow it.
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var schema = schemas.GetProperty("TaskDto").GetProperty("properties").GetProperty(property);
        if (schema.TryGetProperty("$ref", out var reference))
        {
            schema = schemas.GetProperty(reference.GetString()!.Split('/').Last());
        }

        var names = schema.GetProperty("enum").EnumerateArray().Select(e => e.GetString()!).ToArray();
        Assert.Equal(expected, names);
    }

    [Fact]
    public async Task SwaggerUi_Get_ReturnsPage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("html", response.Content.Headers.ContentType?.MediaType);
    }
}
