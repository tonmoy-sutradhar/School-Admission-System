using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace IntegrationTests;

public sealed class HoldFlowTests : IClassFixture<AdmissionsWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HoldFlowTests(AdmissionsWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Hold_Confirm_Is_Idempotent_And_MyHolds_Works()
    {
        var studentId = "12345678901234567";
        var createClass = await _client.PostAsJsonAsync("/classes", new
        {
            name = "Class 7 - Day Shift",
            totalSeats = 5
        });
        createClass.EnsureSuccessStatusCode();
        var cls = await createClass.Content.ReadFromJsonAsync<CreatedClass>();

        using var holdRequest = new HttpRequestMessage(HttpMethod.Post, $"/classes/{cls!.Id}/holds");
        holdRequest.Headers.Add("X-Student-Id", studentId);
        var holdResponse = await _client.SendAsync(holdRequest);
        holdResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var hold = await holdResponse.Content.ReadFromJsonAsync<HoldCreated>();
        hold!.Status.Should().Be("HELD");

        using var confirm1 = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        confirm1.Headers.Add("X-Student-Id", studentId);
        var confirmed1 = await _client.SendAsync(confirm1);
        confirmed1.StatusCode.Should().Be(HttpStatusCode.OK);

        using var confirm2 = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        confirm2.Headers.Add("X-Student-Id", studentId);
        var confirmed2 = await _client.SendAsync(confirm2);
        confirmed2.StatusCode.Should().Be(HttpStatusCode.OK);

        var classDetail = await _client.GetFromJsonAsync<ClassSeat>($"/classes/{cls.Id}");
        classDetail!.ConfirmedSeats.Should().Be(1);
        classDetail.HeldSeats.Should().Be(0);

        using var me = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        me.Headers.Add("X-Student-Id", studentId);
        var myHoldsResponse = await _client.SendAsync(me);
        myHoldsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var myHolds = await myHoldsResponse.Content.ReadFromJsonAsync<List<MyHold>>();
        myHolds.Should().Contain(h => h.HoldId == hold.HoldId && h.Status == "CONFIRMED");
    }

    [Fact]
    public async Task Cancel_Hold_Frees_Seat()
    {
        var studentId = "76543210987654321";
        var createClass = await _client.PostAsJsonAsync("/classes", new
        {
            name = "Class 8 - Morning Shift",
            totalSeats = 2
        });
        var cls = await createClass.Content.ReadFromJsonAsync<CreatedClass>();

        using var holdRequest = new HttpRequestMessage(HttpMethod.Post, $"/classes/{cls!.Id}/holds");
        holdRequest.Headers.Add("X-Student-Id", studentId);
        var holdResponse = await _client.SendAsync(holdRequest);
        var hold = await holdResponse.Content.ReadFromJsonAsync<HoldCreated>();

        using var cancel = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold!.HoldId}");
        cancel.Headers.Add("X-Student-Id", studentId);
        var cancelResponse = await _client.SendAsync(cancel);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var classDetail = await _client.GetFromJsonAsync<ClassSeat>($"/classes/{cls.Id}");
        classDetail!.AvailableSeats.Should().Be(2);
        classDetail.HeldSeats.Should().Be(0);
    }

    private sealed record CreatedClass(string Id, string Name, int TotalSeats);
    private sealed record HoldCreated(string HoldId, string ClassId, string StudentId, string Status, DateTime ExpiresAt);
    private sealed record ClassSeat(string Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);
    private sealed record MyHold(string HoldId, string ClassId, string ClassName, string Status, DateTime ExpiresAt);
}
