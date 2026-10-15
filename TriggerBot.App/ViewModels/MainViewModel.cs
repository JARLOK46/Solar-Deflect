using System.ComponentModel;
using System.Runtime.CompilerServices;
using TriggerBot.Configuration;
using TriggerBot.Core.Detection;

namespace TriggerBot.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly DetectorController _detector = new();
    private readonly DetectionConfigurationStore _configurationStore = new();
    private string _activeSection = "Dashboard";
    private string _status = "Listo para configurar";
    private DetectionSettings _settings;

    public MainViewModel()
    {
        _settings = _configurationStore.LoadOrDefault();
    }

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
    public string TargetColorLabel => TargetColor is { } color
        ? $"RGB {color.Red}, {color.Green}, {color.Blue}"
        : "Sin color seleccionado";
    public string TargetColorStatus => TargetColor is null ? "Selecciona un color con F6" : "Color guardado";
    public int Tolerance => _settings.Tolerance;
    public TimeSpan ClickInterval => _settings.ClickInterval;
    public TimeSpan ClickDelay => _settings.ClickDelay;
    public TimeSpan Cooldown => _settings.Cooldown;
    public TimeSpan SpamClickInterval => _settings.SpamClickInterval;
    public bool SpamEnabled => _settings.SpamEnabled;
    public bool PositionLockEnabled => _settings.PositionLockEnabled;
    public DetectionSettings Settings => _settings;

    public string StatusLabel => _detector.State switch
    {
        DetectorState.Armed or DetectorState.Running => "Activo",
        DetectorState.Faulted => "Error",
        _ => "En espera"
    };

    public void ToggleArmed()
    {
        if (IsArmed)
        {
            _detector.Stop();
            Status = "Detector detenido";
        }
        else
        {
            try
            {
                _detector.Arm(_settings);
                Status = "Detector activado · mantén E para detectar";
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
        Status = $"Error del detector: {exception.Message}";
        OnPropertyChanged(nameof(IsArmed));
        OnPropertyChanged(nameof(DetectorState));
        OnPropertyChanged(nameof(StatusLabel));
    }

    public void SetTargetColor(RgbColor color)
    {
        EnsureStopped();
        _settings = _settings with { TargetColor = color };
        PersistSettings();
        Status = $"Color objetivo: RGB {color.Red}, {color.Green}, {color.Blue}";
        OnPropertyChanged(nameof(TargetColor));
        OnPropertyChanged(nameof(TargetColorLabel));
        OnPropertyChanged(nameof(TargetColorStatus));
    }

    public void SetLockedPosition(ScreenPoint point)
    {
        EnsureStopped();
        _settings = _settings with { LockedPosition = point };
        PersistSettings();
        Status = $"Posición guardada en ({point.X}, {point.Y})";
        OnPropertyChanged(nameof(Settings));
    }

    public void UpdateSettings(DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        EnsureStopped();

        _settings = settings;
        PersistSettings();
        Status = "Ajustes actualizados";
        OnPropertyChanged(nameof(Tolerance));
        OnPropertyChanged(nameof(ClickInterval));
        OnPropertyChanged(nameof(ClickDelay));
        OnPropertyChanged(nameof(Cooldown));
        OnPropertyChanged(nameof(SpamClickInterval));
        OnPropertyChanged(nameof(SpamEnabled));
        OnPropertyChanged(nameof(PositionLockEnabled));
    }

    private void PersistSettings() => _configurationStore.Save(_settings);

    private void EnsureStopped()
    {
        if (IsArmed)
            throw new InvalidOperationException("Detén el detector antes de cambiar los ajustes.");
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
