namespace RLApp.Tests.Unit.Domain;

using RLApp.Domain.Common;

public class PatientTrajectoryIdFactoryTests
{
    [Fact]
    public void Create_ValidInput_ReturnsExpectedFormat()
    {
        var id = PatientTrajectoryIdFactory.Create("QUEUE-01", "PAT-001",
            new DateTime(2026, 4, 1, 9, 15, 30, DateTimeKind.Utc));

        Assert.StartsWith("TRJ-", id);
        Assert.Contains("QUEUE-01", id);
        Assert.Contains("PAT-001", id);
    }

    [Fact]
    public void Create_SameInputs_ReturnsSameId()
    {
        var ts = new DateTime(2026, 4, 1, 9, 15, 30, DateTimeKind.Utc);
        var id1 = PatientTrajectoryIdFactory.Create("QUEUE-01", "PAT-001", ts);
        var id2 = PatientTrajectoryIdFactory.Create("QUEUE-01", "PAT-001", ts);
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Create_DifferentTimestamps_ReturnDifferentIds()
    {
        var id1 = PatientTrajectoryIdFactory.Create("Q1", "P1",
            new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc));
        var id2 = PatientTrajectoryIdFactory.Create("Q1", "P1",
            new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc));
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Create_NormalizesHyphens()
    {
        var id = PatientTrajectoryIdFactory.Create("CONSULTA-EXTERNA-01", "PAT-001",
            new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc));

        Assert.StartsWith("TRJ-", id);
        // Hyphens should be normalized (removed) from queue and patient segments
        Assert.DoesNotContain("--", id);
    }
}
