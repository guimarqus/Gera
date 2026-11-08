using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Gera.IntegrationTests.Api;

public class HealthCheckTests
{
    [Fact]
    public async Task GetHealthCheck_DeveRetornar200EStatusHealthy()
    {
        using var factory = new TestApi();
        using var client = factory.CreateClient();
        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var status = jsonDoc.RootElement.GetProperty("status").GetString();

        status.Should().Be("Healthy");
    }
}

