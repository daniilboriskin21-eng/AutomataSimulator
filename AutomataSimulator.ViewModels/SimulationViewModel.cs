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
    public string? InputErrorMessage { get; private set; }
    public bool HasInputError => !string.IsNullOrEmpty(InputErrorMessage);
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
    private void ValidateInput()
    {
        if (_engine == null || string.IsNullOrEmpty(_inputString))
        {
            InputErrorMessage = null;
            return;
        }

        // Ищем символы в строке, которых нет в алфавите автомата
        var invalidChars = _inputString
            .Where(c => !_engine.Alphabet.Contains(c))
            .Distinct()
            .ToList();

        if (invalidChars.Any())
        {
            InputErrorMessage = $"⚠️ Ошибка: '{string.Join("', '", invalidChars)}' вне алфавита!";
        }
        else
        {
            InputErrorMessage = null;
        }
    }
    public bool IsActive => _engine != null;

    // Команды
    public ICommand StepForwardCommand { get; }
    public ICommand StepBackwardCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ToggleBreakpointCommand { get; }
    public ICommand RunCommand { get; }

    public SimulationViewModel()
    {
        StepForwardCommand = new RelayCommand(_ => {
            _engine?.StepForward();
            UpdateUI();
        }, _ => !HasInputError && (_engine?.CanStepForward ?? false)); // Блокируем при ошибке

        StepBackwardCommand = new RelayCommand(_ => {
            _engine?.StepBackward();
            UpdateUI();
        }, _ => _engine?.CanStepBackward ?? false); // Назад можно шагать всегда

        ResetCommand = new RelayCommand(_ => {
            _engine?.Reset();
            UpdateUI();
        });

        RunCommand = new RelayCommand(_ => {
            _engine?.Run();
            UpdateUI();
        }, _ => !HasInputError && (_engine?.CanStepForward ?? false)); // Блокируем при ошибке

        ToggleBreakpointCommand = new RelayCommand(param =>
        {
            if (param is Guid stateId)
            {
                _engine?.ToggleBreakpoint(stateId);
            }
        });
    }
    // --- ИЗМЕНЕН МЕТОД: Теперь принимает строку ---
    public void Initialize(IExecutionEngine engine, string input)
    {
        _engine = engine;
        _inputString = input;
        ValidateInput();
        UpdateUI();
    }

    // --- НОВЫЙ МЕТОД: Для смены строки без пересоздания графа ---
    public void ChangeInput(string newInput)
    {
        _inputString = newInput;
        _engine?.SetInput(newInput); // Передаем новую строку прямо в ядро
        ValidateInput();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_engine == null) return;

        StackView.Clear();

        // Берем стек из первой активной конфигурации (так как их может быть несколько)
        var firstConfig = _engine.CurrentState.ActiveConfigurations.FirstOrDefault();
        if (firstConfig != null)
        {
            foreach (var symbol in firstConfig.Stack) StackView.Add(symbol);
        }

        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(ProcessedText));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(StatusText));   // <-- ДОБАВИТЬ ЭТО
        OnPropertyChanged(nameof(StatusColor));  // <-- ДОБАВИТЬ ЭТО
        OnPropertyChanged("ExecutionUpdated");

        // ОБЯЗАТЕЛЬНО Обновляем все кнопки!
        (StepForwardCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (StepBackwardCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RunCommand as RelayCommand)?.RaiseCanExecuteChanged(); // <- Вот почему ЗАПУСК не работал!
    }
    public List<Guid> GetActiveStateIds()
    {
        return _engine?.GetActiveStateIds().ToList() ?? new List<Guid>();
    }


    public string StatusText
    {
        get
        {
            if (_engine == null) return "Ожидание симуляции...";

            // ЕСЛИ ЕСТЬ ОШИБКА АЛФАВИТА - ПОКАЗЫВАЕМ ЕЁ
            if (HasInputError) return InputErrorMessage!;

            if (!_engine.CurrentState.IsTerminal)
            {
                if (!_engine.CanStepForward)
                    return "❌ Строка не принята (Тупик)";

                return "⏳ Выполнение...";
            }

            return _engine.IsAccepted ? "✅ Строка принята!" : "❌ Строка не принята";
        }
    }

    public string StatusColor
    {
        get
        {
            if (_engine == null) return "#7F8C8D";

            // ТЕМНО-КРАСНЫЙ ЦВЕТ ДЛЯ ОШИБКИ
            if (HasInputError) return "#C0392B";

            if (!_engine.CurrentState.IsTerminal)
            {
                if (!_engine.CanStepForward) return "#E74C3C";
                return "#F39C12";
            }

            return _engine.IsAccepted ? "#2ECC71" : "#E74C3C";
        }
    }
}