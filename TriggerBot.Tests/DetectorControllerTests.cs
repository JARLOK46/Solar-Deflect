using TriggerBot.Core.Detection;

namespace TriggerBot.Tests;

public sealed class DetectorControllerTests
{
    [Fact]
    public void CannotArmWithoutTargetColor()
    {
        var controller = new DetectorController();

        Assert.Throws<InvalidOperationException>(() => controller.Arm(DetectionSettings.Default));
        Assert.Equal(DetectorState.Standby, controller.State);
    }

    [Fact]
    public void MovesFromArmedToRunningAndBackToStandby()
    {
        var controller = new DetectorController();
        var settings = DetectionSettings.Default with { TargetColor = new RgbColor(179, 0, 0) };

        controller.Arm(settings);
        Assert.Equal(DetectorState.Armed, controller.State);

        controller.BeginDetection();
        Assert.Equal(DetectorState.Running, controller.State);

        controller.Stop();
        Assert.Equal(DetectorState.Standby, controller.State);
    }

    [Fact]
    public void FaultIsExplicitAndCanBeObserved()
    {
        var controller = new DetectorController();

        controller.Fault();

        Assert.Equal(DetectorState.Faulted, controller.State);
    }
}
