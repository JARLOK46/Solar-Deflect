namespace TriggerBot.Core.Detection;

public interface IPixelReader
{
    RgbColor Read(ScreenPoint point);
}
