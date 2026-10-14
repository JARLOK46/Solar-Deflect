using System.ComponentModel;
using System.Runtime.CompilerServices;
using TriggerBot.Core.Detection;

namespace TriggerBot.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly DetectorController _detector = new();
    private string _activeSection = "Dashboard";
    private string _status = "Ready to configure";
    private DetectionSettings _settings = DetectionSettings.Default with
    {
        TargetColor = new RgbColor(179, 0, 0)
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ActiveSection
    {
        get => _activeSection;
        set => SetField(ref _activeSection, value);
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public bool IsArmed => _detector.State is DetectorState.Armed or DetectorState.Running;
    public DetectorState DetectorState => _detector.State;
    public RgbColor? TargetColor => _settings.TargetColor;
    public int Tolerance => _settings.Tolerance;
    public TimeSpan ClickDelay => _settings.ClickDelay;
    public TimeSpan Cooldown => _settings.Cooldown;
    public bool PositionLockEnabled => _settings.PositionLockEnabled;
    public DetectionSettings Settings => _settings;

    public string StatusLabel => _detector.State switch
    {
        DetectorState.Armed or DetectorState.Running => "Active",
        DetectorState.Faulted => "Fault",
        _ => "Standby"
    };

    public void ToggleArmed()
    {
        if (IsArmed)
        {
            _detector.Stop();
            Status = "TriggerBot stopped";
        }
        else
        {
            try
            {
                _detector.Arm(_settings);
                Status = "TriggerBot armed · hold E to detect";
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
            {
                Status = exception.Message;
            }
        }

        OnPropertyChanged(nameof(IsArmed));
        OnPropertyChanged(nameof(DetectorState));
        OnPropertyChanged(nameof(StatusLabel));
    }

    public void SetStatus(string status) => Status = status;

    public void MarkFault(Exception exception)
    {
        _detector.Fault();
        Status = $"Detector fault: {exception.Message}";
        OnPropertyChanged(nameof(IsArmed));
        OnPropertyChanged(nameof(DetectorState));
        OnPropertyChanged(nameof(StatusLabel));
    }

    public void SetTargetColor(RgbColor color)
    {
        _settings = _settings with { TargetColor = color };
        Status = $"Target color set to RGB {color.Red}, {color.Green}, {color.Blue}";
        OnPropertyChanged(nameof(TargetColor));
    }

    public void UpdateSettings(DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        if (IsArmed)
            throw new InvalidOperationException("Stop the detector before changing settings.");

        _settings = settings;
        Status = "Settings updated";
        OnPropertyChanged(nameof(Tolerance));
        OnPropertyChanged(nameof(ClickDelay));
        OnPropertyChanged(nameof(Cooldown));
        OnPropertyChanged(nameof(PositionLockEnabled));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
