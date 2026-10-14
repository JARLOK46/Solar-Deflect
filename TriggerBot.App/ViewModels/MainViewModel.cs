using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TriggerBot.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _activeSection = "Dashboard";
    private string _status = "Ready to configure";
    private bool _isArmed;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ActiveSection
    {
        get => _activeSection;
        set => SetField(ref _activeSection, value);
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public bool IsArmed
    {
        get => _isArmed;
        set => SetField(ref _isArmed, value);
    }

    public string StatusLabel => IsArmed ? "Active" : "Standby";

    public void ToggleArmed()
    {
        IsArmed = !IsArmed;
        Status = IsArmed ? "TriggerBot is active" : "TriggerBot stopped";
        OnPropertyChanged(nameof(StatusLabel));
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
