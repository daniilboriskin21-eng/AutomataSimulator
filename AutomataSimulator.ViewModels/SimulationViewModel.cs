using AutomataSimulator.Engine.Interfaces;
using AutomataSimulator.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AutomataSimulator.ViewModels;
public class SimulationViewModel : ViewModelBase
{
    private IExecutionEngine? _engine;
    private string _inputString = string.Empty;
    private ObservableCollection<ComputationBranch> _branchesView = new();
    public ObservableCollection<ComputationBranch> BranchesView
    {
        get => _branchesView;
        private set => SetProperty(ref _branchesView, value);
    }
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _engine?.Run();
            sw.Stop();

            UpdateUI();

            // Выводим результаты эксперимента на экран!
            System.Windows.MessageBox.Show(
                $"Время работы: {sw.ElapsedMilliseconds} мс\n" +
                $"Кол-во шагов: {_engine?.StepCount}\n" +
                $"Макс. конфигураций (параллельных ветвей): {_engine?.MaxConfigurations}",
                "Метрики симуляции");
        }, _ => !HasInputError && (_engine?.CanStepForward ?? false));

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

        // Очищаем старый список веток
        BranchesView.Clear();

        int branchNum = 1;
        foreach (var config in _engine.CurrentState.ActiveConfigurations)
        {
            string stateName = _engine.GetStateName(config.StateId);

            // Переводим стек в строку. ImmutableStack при перечислении отдает элементы от вершины ко дну.
            string stackStr = config.Stack.IsEmpty ? "[Пусто]" : string.Join(" ", config.Stack);

            BranchesView.Add(new ComputationBranch
            {
                Title = $"Ветка {branchNum}: Узел {stateName}",
                StackContent = $"Стек: {stackStr}",
                ShowStack = _engine.IsPda // Показываем текст стека только для PDA
            });

            branchNum++;
        }

        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(ProcessedText));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColor));
        OnPropertyChanged("ExecutionUpdated");

        (StepForwardCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (StepBackwardCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RunCommand as RelayCommand)?.RaiseCanExecuteChanged();
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