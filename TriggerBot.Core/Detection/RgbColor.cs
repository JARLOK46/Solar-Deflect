namespace TriggerBot.Core.Detection;

public readonly record struct RgbColor
{
    public RgbColor(byte red, byte green, byte blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }

    public byte Red { get; }
    public byte Green { get; }
    public byte Blue { get; }
}
