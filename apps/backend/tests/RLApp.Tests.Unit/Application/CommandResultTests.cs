namespace RLApp.Tests.Unit.Application;

using RLApp.Application.DTOs;
using RLApp.Domain.Common;

public class CommandResultTests
{
    [Fact]
    public void Failure_WhenDomainExceptionRepresentsConcurrencyConflict_ShouldExposeConflictMetadata()
    {
        var exception = DomainException.ConcurrencyConflict("queue-1", 2, 3);

        var result = CommandResult.Failure(exception, "corr-1");

        Assert.False(result.Success);
        Assert.True(result.IsConflict);
        Assert.Equal(DomainException.ConcurrencyConflictCode, result.ErrorCode);
        Assert.Equal(exception.Message, result.Message);
    }

    [Fact]
    public void GenericFailure_WhenDomainExceptionRepresentsConcurrencyConflict_ShouldExposeConflictMetadata()
    {
        var exception = DomainException.ConcurrencyConflict("queue-2", 4, 5);

        var result = CommandResult<string>.Failure(exception, "corr-2");

        Assert.False(result.Success);
        Assert.True(result.IsConflict);
        Assert.Equal(DomainException.ConcurrencyConflictCode, result.ErrorCode);
        Assert.Equal(exception.Message, result.Message);
    }
}
