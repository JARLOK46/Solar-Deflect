namespace TriggerBot.Core.Detection;

public sealed class DetectorController
{
    public DetectorState State { get; private set; } = DetectorState.Standby;

    public void Arm(DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        if (settings.TargetColor is null)
            throw new InvalidOperationException("A target color is required before arming the detector.");

        State = DetectorState.Armed;
    }

    public void BeginDetection()
    {
        if (State != DetectorState.Armed)
            throw new InvalidOperationException("The detector must be armed before detection can begin.");

        State = DetectorState.Running;
    }

    public void Stop()
    {
        State = State == DetectorState.Standby ? DetectorState.Standby : DetectorState.Stopping;
        State = DetectorState.Standby;
    }

    public void Fault()
    {
        State = DetectorState.Faulted;
    }
}
