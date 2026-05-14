using System.Collections.ObjectModel;
using AutomataSimulator.Core.Enums;
using AutomataSimulator.Core.Models;
using AutomataSimulator.Core.Models.Automata;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.ViewModels.Base;

namespace AutomataSimulator.ViewModels;

public class ManualConstructorViewModel : ViewModelBase
{
    private AutomatonType _selectedType = AutomatonType.DFA;
    private string _statesText = "q0, q1";
    private string _alphabetText = "a, b";
    private string _stackAlphabetText = "z0, X";
    private string _startStateText = "q0";
    private string _finalStatesText = "q1";
    private string _transitionsText = "q0, a -> q1\nq1, b -> q1";
    private string _errorMessage = string.Empty;

    public AutomatonType SelectedType
    {
        get => _selectedType;
        set { SetProperty(ref _selectedType, value); OnPropertyChanged(nameof(IsPda)); }
    }

    public bool IsPda => SelectedType == AutomatonType.PDA;

    public ObservableCollection<AutomatonType> AutomatonTypes { get; } = new() { AutomatonType.DFA, AutomatonType.NFA, AutomatonType.PDA };

    public string StatesText { get => _statesText; set => SetProperty(ref _statesText, value); }
    public string AlphabetText { get => _alphabetText; set => SetProperty(ref _alphabetText, value); }
    public string StackAlphabetText { get => _stackAlphabetText; set => SetProperty(ref _stackAlphabetText, value); }
    public string StartStateText { get => _startStateText; set => SetProperty(ref _startStateText, value); }
    public string FinalStatesText { get => _finalStatesText; set => SetProperty(ref _finalStatesText, value); }
    public string TransitionsText { get => _transitionsText; set => SetProperty(ref _transitionsText, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public object? ResultAutomaton { get; private set; }

    public bool TryBuild()
    {
        try
        {
            ErrorMessage = "";
            var statesMap = new Dictionary<string, State>();

            // 1. Парсим состояния
            var stateNames = StatesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!stateNames.Any()) throw new Exception("Множество состояний Q не может быть пустым.");

            var finals = FinalStatesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
            var start = StartStateText.Trim();

            foreach (var name in stateNames)
            {
                statesMap[name] = new State { Name = name, IsStart = name == start, IsFinal = finals.Contains(name) };
            }

            if (!statesMap.Values.Any(s => s.IsStart)) throw new Exception($"Начальное состояние '{start}' не найдено в Q.");

            // 2. Парсим алфавиты
            var alphabet = AlphabetText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.First()).ToHashSet();

            // 3. Собираем автомат
            if (SelectedType == AutomatonType.DFA || SelectedType == AutomatonType.NFA)
            {
                var fa = new FiniteAutomaton(SelectedType == AutomatonType.DFA)
                {
                    Name = "Manual FA",
                    Origin = CreationOrigin.Manual,
                    Alphabet = alphabet,
                    States = statesMap.Values.ToList()
                };

                foreach (var line in TransitionsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    // Формат: q0, a -> q1
                    var parts = line.Split(new[] { ",", "->" }, StringSplitOptions.TrimEntries);
                    if (parts.Length < 3) continue;

                    var from = parts[0];
                    var symbol = (parts[1] == "ε" || parts[1] == "eps" || string.IsNullOrEmpty(parts[1])) ? (char?)null : parts[1].First();
                    var to = parts[2];

                    if (!statesMap.ContainsKey(from) || !statesMap.ContainsKey(to)) throw new Exception($"Неизвестное состояние в переходе: {line}");

                    fa.Transitions.Add(new FiniteTransition { FromStateId = statesMap[from].Id, ToStateId = statesMap[to].Id, Symbol = symbol });
                }
                ResultAutomaton = fa;
            }
            else // PDA
            {
                var pda = new PushdownAutomaton
                {
                    Name = "Manual PDA",
                    Origin = CreationOrigin.Manual,
                    Alphabet = alphabet,
                    States = statesMap.Values.ToList()
                };

                var stackAlph = StackAlphabetText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.First()).ToHashSet();
                pda.StackAlphabet = stackAlph;
                pda.InitialStackSymbol = stackAlph.FirstOrDefault();

                foreach (var line in TransitionsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    // Формат: q0, a, z0 -> q1, Az0
                    var parts = line.Split(new[] { ",", "->" }, StringSplitOptions.TrimEntries);
                    if (parts.Length < 5) throw new Exception($"Неверный формат перехода PDA: {line}");

                    var from = parts[0];
                    var input = (parts[1] == "ε" || parts[1] == "eps") ? (char?)null : parts[1].First();
                    var pop = (parts[2] == "ε" || parts[2] == "eps") ? (char?)null : parts[2].First();
                    var to = parts[3];
                    var push = (parts[4] == "ε" || parts[4] == "eps") ? "" : parts[4];

                    if (!statesMap.ContainsKey(from) || !statesMap.ContainsKey(to)) throw new Exception($"Неизвестное состояние: {line}");

                    pda.Transitions.Add(new PushdownTransition
                    {
                        FromStateId = statesMap[from].Id,
                        ToStateId = statesMap[to].Id,
                        InputSymbol = input,
                        PopSymbol = pop,
                        PushSymbols = push
                    });
                }
                ResultAutomaton = pda;
            }
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }
}