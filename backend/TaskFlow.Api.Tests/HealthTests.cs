using System.Net;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests;

public class HealthTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Health_Get_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
