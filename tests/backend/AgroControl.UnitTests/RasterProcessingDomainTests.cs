using AgroControl.Domain.Modules.PrecisionAgriculture;

namespace AgroControl.UnitTests;

public sealed class RasterProcessingDomainTests
{
    [Fact]
    public void Processing_state_follows_expected_lifecycle()
    {
        var state = RasterProcessingState.CreatePending();
        Assert.Equal(RasterProcessingStatus.Pending, state.Status);

        state.Start();
        Assert.Equal(RasterProcessingStatus.Processing, state.Status);

        state.Succeed();
        Assert.Equal(RasterProcessingStatus.Succeeded, state.Status);
    }

    [Fact]
    public void Processing_cannot_succeed_before_it_starts()
    {
        var state = RasterProcessingState.CreatePending();
        Assert.Throws<InvalidOperationException>(() => state.Succeed());
    }

    [Fact]
    public void Completed_processing_cannot_be_failed_after_success()
    {
        var state = RasterProcessingState.CreatePending();
        state.Start();
        state.Succeed();
        Assert.Throws<InvalidOperationException>(() => state.Fail());
    }
}
