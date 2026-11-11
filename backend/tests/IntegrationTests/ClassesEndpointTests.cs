using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace IntegrationTests;

public sealed class ClassesEndpointTests : IClassFixture<AdmissionsWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClassesEndpointTests(AdmissionsWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_And_List_Classes_Should_Return_Seat_Fields()
    {
        var create = await _client.PostAsJsonAsync("/classes", new
        {
            name = "Class 6 - Morning Shift",
            totalSeats = 40
        });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<CreatedClass>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Class 6 - Morning Shift");
        created.TotalSeats.Should().Be(40);
        created.Id.Should().NotBeNullOrWhiteSpace();

        var list = await _client.GetAsync("/classes");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var classes = await list.Content.ReadFromJsonAsync<List<ClassSeat>>();
        classes.Should().NotBeNull();
        classes!.Should().Contain(c =>
            c.Id == created.Id &&
            c.TotalSeats == 40 &&
            c.HeldSeats == 0 &&
            c.ConfirmedSeats == 0 &&
            c.AvailableSeats == 40);

        var one = await _client.GetAsync($"/classes/{created.Id}");
        one.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await one.Content.ReadFromJsonAsync<ClassSeat>();
        detail!.AvailableSeats.Should().Be(40);
    }

    private sealed record CreatedClass(string Id, string Name, int TotalSeats);

    private sealed record ClassSeat(
        string Id,
        string Name,
        int TotalSeats,
        int HeldSeats,
        int ConfirmedSeats,
        int AvailableSeats);
}
