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

    // --- НОВЫЕ СВОЙСТВА ДЛЯ ПРОГРЕССА ---
    public string ProcessedText => _engine != null ? _inputString.Substring(0, _engine.CurrentState.ReadPosition) : "";
    public string RemainingText => _engine != null ? _inputString.Substring(_engine.CurrentState.ReadPosition) : _inputString;

    public double ProgressPercentage => (_engine == null || _inputString.Length == 0)
        ? 0
        : (_engine.CurrentState.ReadPosition / (double)_inputString.Length) * 100;

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
        StepForwardCommand = new RelayCommand(_ => {
            _engine?.StepForward();
            UpdateUI();
        }, _ => _engine?.CanStepForward ?? false);

        StepBackwardCommand = new RelayCommand(_ => {
            _engine?.StepBackward();
            UpdateUI(); // Это вызовет событие "ExecutionUpdated"
        }, _ => _engine?.CanStepBackward ?? false);

        ResetCommand = new RelayCommand(_ => {
            _engine?.Reset();
            UpdateUI(); // Это вернет прогресс-бар на 0 и сбросит граф
        });

        ToggleBreakpointCommand = new RelayCommand(param => {
            if (param is Guid stateId) _engine?.ToggleBreakpoint(stateId);
        });

        RunCommand = new RelayCommand(_ => {
            _engine?.Run();
            UpdateUI();
        }, _ => _engine?.CanStepForward ?? false);

    }

    // --- ИЗМЕНЕН МЕТОД: Теперь принимает строку ---
    public void Initialize(IExecutionEngine engine, string input)
    {
        _engine = engine;
        _inputString = input;
        UpdateUI();
    }

    // --- НОВЫЙ МЕТОД: Для смены строки без пересоздания графа ---
    public void ChangeInput(string newInput)
    {
        _inputString = newInput;
        _engine?.Reset();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_engine == null) return;

        StackView.Clear();
        foreach (var symbol in _engine.CurrentState.Stack)
            StackView.Add(symbol);

        // Уведомляем интерфейс обо всех изменениях
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(ProcessedText));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged("ExecutionUpdated");

        // Принудительно обновляем кнопки
        (StepForwardCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (StepBackwardCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    public List<Guid> GetActiveStateIds()
    {
        return _engine?.GetActiveStateIds().ToList() ?? new List<Guid>();
    }

    public ICommand ToggleBreakpointCommand { get; }
    public ICommand RunCommand { get; }

    // В конструкторе:
}