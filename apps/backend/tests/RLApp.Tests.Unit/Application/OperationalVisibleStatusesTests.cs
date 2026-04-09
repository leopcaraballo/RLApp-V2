namespace RLApp.Tests.Unit.Application;

using RLApp.Ports.Outbound;

public class OperationalVisibleStatusesTests
{
    [Theory]
    [InlineData(OperationalVisibleStatuses.Waiting, true)]
    [InlineData(OperationalVisibleStatuses.WaitingForConsultation, true)]
    [InlineData(OperationalVisibleStatuses.AtCashier, false)]
    [InlineData(OperationalVisibleStatuses.PaymentPending, false)]
    [InlineData(OperationalVisibleStatuses.InConsultation, false)]
    public void CountsAsWaiting_UsesOnlyWaitingBuckets(string status, bool expected)
    {
        Assert.Equal(expected, OperationalVisibleStatuses.CountsAsWaiting(status));
    }

    [Fact]
    public void ResolveNextStatus_PreservesMoreAdvancedStatus()
    {
        var resolved = OperationalVisibleStatuses.ResolveNextStatus(
            OperationalVisibleStatuses.InConsultation,
            OperationalVisibleStatuses.Called);

        Assert.Equal(OperationalVisibleStatuses.InConsultation, resolved);
    }

    [Fact]
    public void ResolveNextStatus_AllowsTerminalTransition()
    {
        var resolved = OperationalVisibleStatuses.ResolveNextStatus(
            OperationalVisibleStatuses.WaitingForConsultation,
            OperationalVisibleStatuses.Completed);

        Assert.Equal(OperationalVisibleStatuses.Completed, resolved);
    }
}
