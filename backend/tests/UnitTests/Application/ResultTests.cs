using BuildingBlocks.Application.Common.Models;
using FluentAssertions;

namespace UnitTests.Application;

public class ResultTests
{
    [Fact]
    public void Success_Should_Expose_Value()
    {
        var result = Result.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_Should_Expose_Error()
    {
        var result = Result.Failure<int>(Error.NotFound("missing"));
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound");
        var act = () => _ = result.Value;
        act.Should().Throw<InvalidOperationException>();
    }
}