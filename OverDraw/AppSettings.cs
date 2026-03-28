using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace OverDraw;

public class AppSettings
{
    public string PenColorHex { get; set; } = "#FFFF0000";
    public double PenThickness { get; set; } = 3.0;
    public double FadeDurationSeconds { get; set; } = 2.0;
    public string ModifierKey { get; set; } = "Ctrl"; // Ctrl, Shift, Alt
    public List<StampData?> Stamps { get; set; } = new(new StampData?[10]);
    public List<string> RecentColors { get; set; } = new();

    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "overdraw-settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public WpfColor GetPenColor()
    {
        try { return (WpfColor)WpfColorConverter.ConvertFromString(PenColorHex); }
        catch { return Colors.Red; }
    }

    public void SetPenColor(WpfColor c) => PenColorHex = c.ToString();

    public int GetModifierVirtualKey() => ModifierKey switch
    {
        "Shift" => NativeMethods.VK_SHIFT,
        "Alt" => NativeMethods.VK_MENU,
        _ => NativeMethods.VK_CONTROL
    };

    public void EnsureStampSlots()
    {
        while (Stamps.Count < 10) Stamps.Add(null);
    }

    public void Save()
    {
        try
        {
            EnsureStampSlots();
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* portable — silently ignore if read-only */ }
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                settings.EnsureStampSlots();
                return settings;
            }
        }
        catch { }
        var def = new AppSettings();
        def.EnsureStampSlots();
        return def;
    }
}
