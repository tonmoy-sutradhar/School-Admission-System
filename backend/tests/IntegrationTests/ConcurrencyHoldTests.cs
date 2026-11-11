using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace IntegrationTests;

public sealed class ConcurrencyHoldTests : IClassFixture<AdmissionsWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConcurrencyHoldTests(AdmissionsWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Parallel_Holds_Must_Not_Overbook()
    {
        const int totalSeats = 10;
        const int parallelRequests = 50;

        var createClass = await _client.PostAsJsonAsync("/classes", new
        {
            name = "Class 9 - Concurrency Shift",
            totalSeats
        });
        createClass.EnsureSuccessStatusCode();
        var cls = await createClass.Content.ReadFromJsonAsync<CreatedClass>();
        cls.Should().NotBeNull();

        var statuses = new ConcurrentBag<(HttpStatusCode Status, string? Body)>();

        var tasks = Enumerable.Range(0, parallelRequests).Select(async i =>
        {
            var studentId = $"{i:D17}";
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{cls!.Id}/holds");
            request.Headers.Add("X-Student-Id", studentId);
            var response = await _client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            statuses.Add((response.StatusCode, body));
        });

        await Task.WhenAll(tasks);

        var created = statuses.Count(x => x.Status == HttpStatusCode.Created);
        var conflicts = statuses.Count(x => x.Status == HttpStatusCode.Conflict);

        created.Should().Be(totalSeats);
        (created + conflicts).Should().Be(parallelRequests);

        var classDetail = await _client.GetFromJsonAsync<ClassSeat>($"/classes/{cls!.Id}");
        classDetail.Should().NotBeNull();
        (classDetail!.HeldSeats + classDetail.ConfirmedSeats).Should().BeLessThanOrEqualTo(totalSeats);
        classDetail.HeldSeats.Should().Be(totalSeats);
        classDetail.AvailableSeats.Should().Be(0);
    }

    private sealed record CreatedClass(string Id, string Name, int TotalSeats);
    private sealed record ClassSeat(string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);
}
