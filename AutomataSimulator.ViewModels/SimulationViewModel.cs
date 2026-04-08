using AutomataSimulator.Engine.Interfaces;
using AutomataSimulator.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AutomataSimulator.ViewModels;

public class SimulationViewModel : ViewModelBase
{
    private IExecutionEngine? _engine;
    private string _inputString = string.Empty;
    private ObservableCollection<char> _stackView = new();

    public string InputString
    {
        get => _inputString;
        set => SetProperty(ref _inputString, value);
    }

    public ObservableCollection<char> StackView
    {
        get => _stackView;
        private set => SetProperty(ref _stackView, value);
    }

    public bool IsActive => _engine != null;

    // Команды
    public ICommand StepForwardCommand { get; }
    public ICommand StepBackwardCommand { get; }
    public ICommand ResetCommand { get; }

    public SimulationViewModel()
    {
        StepForwardCommand = new RelayCommand(_ => _engine?.StepForward(), _ => _engine?.CanStepForward ?? false);
        StepBackwardCommand = new RelayCommand(_ => _engine?.StepBackward(), _ => _engine?.CanStepBackward ?? false);
        ResetCommand = new RelayCommand(_ => _engine?.Reset());
    }

    public void Initialize(IExecutionEngine engine)
    {
        _engine = engine;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_engine == null) return;

        // Обновляем визуальное представление стека
        StackView.Clear();
        foreach (var symbol in _engine.CurrentState.Stack)
            StackView.Add(symbol);

        OnPropertyChanged(nameof(IsActive));
    }
    public List<Guid> GetActiveStateIds()
    {
        // _engine теперь "знает" про этот метод благодаря обновлению интерфейса
        return _engine?.GetActiveStateIds().ToList() ?? new List<Guid>();
    }


}