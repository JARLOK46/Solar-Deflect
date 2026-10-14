using TriggerBot.Core.Detection;
using TriggerBot.Windows.ScreenCapture;

namespace TriggerBot.Tests;

public sealed class GdiPixelReaderTests
{
    [Fact]
    public void ReadsAValidScreenPixelOnWindows()
    {
        if (!OperatingSystem.IsWindows()) return;

        var pixel = new GdiPixelReader().Read(new ScreenPoint(0, 0));

        Assert.InRange(pixel.Red, (byte)0, byte.MaxValue);
        Assert.InRange(pixel.Green, (byte)0, byte.MaxValue);
        Assert.InRange(pixel.Blue, (byte)0, byte.MaxValue);
    }
}
