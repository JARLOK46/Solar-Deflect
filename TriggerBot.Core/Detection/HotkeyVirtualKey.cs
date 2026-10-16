namespace TriggerBot.Core.Detection;

public static class HotkeyVirtualKey
{
    private static readonly IReadOnlyDictionary<string, uint> Keys = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
    {
        ["XButton1"] = 0x05,
        ["XButton2"] = 0x06,
        ["Space"] = 0x20,
        ["Enter"] = 0x0D,
        ["Tab"] = 0x09,
        ["R"] = 0x52,
        ["E"] = 0x45,
        ["F5"] = 0x74,
        ["F6"] = 0x75,
        ["F7"] = 0x76,
        ["F8"] = 0x77,
        ["F9"] = 0x78,
        ["F10"] = 0x79,
        ["F11"] = 0x7A,
        ["F12"] = 0x7B
    };

    public static bool TryParse(string? value, out uint virtualKey)
    {
        virtualKey = 0;
        return value is not null && Keys.TryGetValue(value.Trim(), out virtualKey);
    }

    public static string SupportedValues => string.Join(", ", Keys.Keys);
}
