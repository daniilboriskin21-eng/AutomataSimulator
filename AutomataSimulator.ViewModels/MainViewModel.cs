using AutomataSimulator.Core.Models.Automata;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.Engine;
using AutomataSimulator.Translators.Regex;
using AutomataSimulator.Translators.Grammar;
using AutomataSimulator.ViewModels.Base;

namespace AutomataSimulator.ViewModels;

public class MainViewModel : ViewModelBase
{
    private string _sourceText = string.Empty;
    private string _testInput = string.Empty;
    private object? _currentAutomaton;

    public SimulationViewModel Simulation { get; } = new();

    public string SourceText
    {
        get => _sourceText;
        set => SetProperty(ref _sourceText, value);
    }

    public string TestInput
    {
        get => _testInput;
        set => SetProperty(ref _testInput, value);
    }

    public object? CurrentAutomaton
    {
        get => _currentAutomaton;
        private set
        {
            if (SetProperty(ref _currentAutomaton, value))
            {
                // Обновляем состояние кнопки при смене автомата
                ConvertToDfaCommand?.RaiseCanExecuteChanged();
            }
        }
    }
    public RelayCommand TranslateRegexCommand { get; }
    public RelayCommand TranslateGrammarCommand { get; }

    public RelayCommand ConvertToDfaCommand { get; }

    public MainViewModel()
    {
        TranslateRegexCommand = new RelayCommand(_ =>
        {
            if (string.IsNullOrWhiteSpace(SourceText)) return;

            // 1. Транслируем текст в модель NFA
            var translator = new ThompsonTranslator();
            var nfa = translator.Translate(SourceText);
            CurrentAutomaton = nfa;

            // 2. Создаем движок симуляции для конечного автомата
            var engine = new ExecutionEngine<FiniteAutomaton, FiniteTransition>(nfa, TestInput);

            // 3. Инициализируем SimulationViewModel этим движком и строкой (ИСПРАВЛЕНО)
            Simulation.Initialize(engine, TestInput);

            // Обновляем состояние кнопок
            TranslateRegexCommand.RaiseCanExecuteChanged();
        });

        TranslateGrammarCommand = new RelayCommand(_ =>
        {
            if (string.IsNullOrWhiteSpace(SourceText)) return;

            // 1. Транслируем грамматику в PDA
            var translator = new CfgToPdaTranslator();
            var pda = translator.Translate(SourceText);
            CurrentAutomaton = pda;

            // 2. Создаем движок симуляции для PDA
            var engine = new ExecutionEngine<PushdownAutomaton, PushdownTransition>(pda, TestInput);

            // 3. Передаем в симуляцию (ИСПРАВЛЕНО)
            Simulation.Initialize(engine, TestInput);
        });

        ConvertToDfaCommand = new RelayCommand(_ =>
        {
            if (CurrentAutomaton is FiniteAutomaton nfa && !nfa.IsDeterministic())
            {
                var dfa = AutomataSimulator.Core.Operations.NfaToDfaConverter.Convert(nfa);
                CurrentAutomaton = dfa;

                var engine = new ExecutionEngine<FiniteAutomaton, FiniteTransition>(dfa, TestInput);
                Simulation.Initialize(engine, TestInput);
            }
        }, _ => CurrentAutomaton is FiniteAutomaton fa && !fa.IsDeterministic());
        // Не забудьте вызывать ConvertToDfaCommand.RaiseCanExecuteChanged() при смене CurrentAutomaton
    }
}