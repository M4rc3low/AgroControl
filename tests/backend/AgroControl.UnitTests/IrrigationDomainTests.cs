using AgroControl.Domain.Modules.Irrigation;

namespace AgroControl.UnitTests;

public sealed class IrrigationDomainTests
{
    [Fact]
    public void Zone_requires_ordered_moisture_thresholds()
    {
        Assert.Throws<ArgumentException>(() => IrrigationZone.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Zona Norte", 10m, IrrigationMethod.CenterPivot,
            40m, 35m, 70m, null, DateTime.UtcNow));
    }

    [Fact]
    public void Application_calculates_estimated_volume_deterministically()
    {
        var application = IrrigationApplication.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2.5m, 12m,
            IrrigationApplicationSource.Manual, DateTime.UtcNow, null, null, DateTime.UtcNow);

        Assert.Equal(300m, application.EstimatedVolumeCubicMeters);
    }

    [Fact]
    public void Correction_can_compensate_previous_application_without_editing_history()
    {
        var application = IrrigationApplication.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2.5m, -2m,
            IrrigationApplicationSource.Correction, DateTime.UtcNow, null, "Ajuste de medição", DateTime.UtcNow);

        Assert.Equal(-50m, application.EstimatedVolumeCubicMeters);
    }

    [Theory]
    [InlineData(20d, WaterCondition.Critical, IrrigationRecommendation.Irrigate)]
    [InlineData(45d, WaterCondition.Dry, IrrigationRecommendation.Irrigate)]
    [InlineData(60d, WaterCondition.Target, IrrigationRecommendation.Monitor)]
    [InlineData(90d, WaterCondition.Wet, IrrigationRecommendation.AvoidIrrigation)]
    public void Decision_policy_classifies_soil_moisture(double reading, WaterCondition expectedCondition, IrrigationRecommendation expectedRecommendation)
    {
        var condition = IrrigationDecisionPolicy.Classify(reading, 35m, 50m, 75m);
        Assert.Equal(expectedCondition, condition);
        Assert.Equal(expectedRecommendation, IrrigationDecisionPolicy.Recommend(condition));
    }
}
