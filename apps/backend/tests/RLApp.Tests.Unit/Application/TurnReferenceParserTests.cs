namespace RLApp.Tests.Unit.Application;

using RLApp.Application.Services;

public class TurnReferenceParserTests
{
    [Fact]
    public void Build_ConcatenatesQueueAndPatientId()
    {
        var result = TurnReferenceParser.Build("QUEUE-01", "PAT-001");
        Assert.Equal("QUEUE-01-PAT-001", result);
    }

    // ── TryExtractPatientId ───────────────────────────────────

    [Fact]
    public void TryExtractPatientId_ValidTurnId_ExtractsPatientId()
    {
        var ok = TurnReferenceParser.TryExtractPatientId("QUEUE-01-PAT-001", "QUEUE-01", out var patientId);
        Assert.True(ok);
        Assert.Equal("PAT-001", patientId);
    }

    [Fact]
    public void TryExtractPatientId_MismatchedQueue_ReturnsFalse()
    {
        var ok = TurnReferenceParser.TryExtractPatientId("QUEUE-02-PAT-001", "QUEUE-01", out var patientId);
        Assert.False(ok);
        Assert.Equal(string.Empty, patientId);
    }

    [Theory]
    [InlineData(null, "QUEUE-01")]
    [InlineData("", "QUEUE-01")]
    [InlineData("   ", "QUEUE-01")]
    [InlineData("QUEUE-01-PAT-001", null)]
    [InlineData("QUEUE-01-PAT-001", "")]
    [InlineData("QUEUE-01-PAT-001", "   ")]
    public void TryExtractPatientId_NullOrWhitespace_ReturnsFalse(string? turnId, string? queueId)
    {
        var ok = TurnReferenceParser.TryExtractPatientId(turnId!, queueId!, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryExtractPatientId_TurnIdEqualsPrefix_ReturnsFalse()
    {
        var ok = TurnReferenceParser.TryExtractPatientId("QUEUE-01-", "QUEUE-01", out _);
        Assert.False(ok);
    }

    // ── TryExtractQueueId ─────────────────────────────────────

    [Fact]
    public void TryExtractQueueId_ValidTurnId_ExtractsQueueId()
    {
        var ok = TurnReferenceParser.TryExtractQueueId("QUEUE-01-PAT-001", "PAT-001", out var queueId);
        Assert.True(ok);
        Assert.Equal("QUEUE-01", queueId);
    }

    [Fact]
    public void TryExtractQueueId_MismatchedPatient_ReturnsFalse()
    {
        var ok = TurnReferenceParser.TryExtractQueueId("QUEUE-01-PAT-002", "PAT-001", out var queueId);
        Assert.False(ok);
        Assert.Equal(string.Empty, queueId);
    }

    [Theory]
    [InlineData(null, "PAT-001")]
    [InlineData("", "PAT-001")]
    [InlineData("QUEUE-01-PAT-001", null)]
    [InlineData("QUEUE-01-PAT-001", "")]
    public void TryExtractQueueId_NullOrWhitespace_ReturnsFalse(string? turnId, string? patientId)
    {
        var ok = TurnReferenceParser.TryExtractQueueId(turnId!, patientId!, out _);
        Assert.False(ok);
    }
}
