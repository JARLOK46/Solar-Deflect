namespace TriggerBot.Core.Detection;

public interface IDetectionRuntime
{
    bool IsActivationKeyPressed { get; }
    ScreenPoint GetCursorPosition();
    RgbColor ReadPixel(ScreenPoint point);
    void LeftClick();
}
