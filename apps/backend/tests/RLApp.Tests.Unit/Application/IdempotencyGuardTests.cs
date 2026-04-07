namespace RLApp.Tests.Unit.Application;

using RLApp.Application.Services;

public class IdempotencyGuardTests
{
    [Fact]
    public void TryAcquire_FirstCall_ReturnsTrue()
    {
        var guard = new IdempotencyGuard();
        Assert.True(guard.TryAcquire("key-1", "corr-1"));
    }

    [Fact]
    public void TryAcquire_DuplicateCall_ReturnsFalse()
    {
        var guard = new IdempotencyGuard();
        guard.TryAcquire("key-1", "corr-1");
        Assert.False(guard.TryAcquire("key-1", "corr-1"));
    }

    [Fact]
    public void TryAcquire_AfterRelease_ReturnsTrue()
    {
        var guard = new IdempotencyGuard();
        guard.TryAcquire("key-1", "corr-1");
        guard.Release("key-1", "corr-1");
        Assert.True(guard.TryAcquire("key-1", "corr-1"));
    }

    [Fact]
    public void TryAcquire_NullKey_AlwaysReturnsTrue()
    {
        var guard = new IdempotencyGuard();
        Assert.True(guard.TryAcquire(null, "corr-1"));
        Assert.True(guard.TryAcquire(null, "corr-1"));
    }

    [Fact]
    public void TryAcquire_EmptyKey_AlwaysReturnsTrue()
    {
        var guard = new IdempotencyGuard();
        Assert.True(guard.TryAcquire("", "corr-1"));
        Assert.True(guard.TryAcquire("  ", "corr-1"));
    }

    [Fact]
    public void TryAcquire_SameKeyDifferentCorrelation_BothSucceed()
    {
        var guard = new IdempotencyGuard();
        Assert.True(guard.TryAcquire("key-1", "corr-1"));
        Assert.True(guard.TryAcquire("key-1", "corr-2"));
    }
}
