using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace IntegrationTests;

public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Google_Endpoint_Should_Return_Ok_Structure()
    {
        var response = await _client.GetAsync("/api/v1/auth/google");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        payload.Should().NotBeNull();
    }

    [Fact]
    public async Task Users_Me_Without_Token_Should_Be_Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_Should_Be_Reachable_Or_Degraded_Without_Db()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }
}