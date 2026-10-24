using System.Net;
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
    public async Task SwaggerUi_Get_ReturnsPage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("html", response.Content.Headers.ContentType?.MediaType);
    }
}
