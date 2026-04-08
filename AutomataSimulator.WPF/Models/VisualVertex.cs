using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutomataSimulator.WPF.Models;

public class VisualVertex : INotifyPropertyChanged
{
    private bool _isActive;
    public string Name { get; set; } = string.Empty;
    public bool IsStart { get; set; }
    public bool IsFinal { get; set; }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => Name;
}