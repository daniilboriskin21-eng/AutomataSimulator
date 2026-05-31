using Microsoft.Win32;
using System.IO;
using System;
using AutomataSimulator.Core.Models.Automata;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.Core.Services;
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
        set
        {
            if (SetProperty(ref _testInput, value))
            {
                // Как только пользователь ввел новый символ, сразу отдаем его симулятору
                Simulation.ChangeInput(value);
            }
        }
    }
    public object? CurrentAutomaton
    {
        get => _currentAutomaton;
        private set
        {
            if (SetProperty(ref _currentAutomaton, value))
            {
                // Принудительно обновляем состояние кнопок при смене автомата
                ConvertToDfaCommand?.RaiseCanExecuteChanged();
                SaveProjectCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    // Объявление команд
    public RelayCommand TranslateRegexCommand { get; }
    public RelayCommand TranslateGrammarCommand { get; }
    public RelayCommand ConvertToDfaCommand { get; }
    public RelayCommand SaveProjectCommand { get; }
    public RelayCommand LoadProjectCommand { get; }

    public MainViewModel()
    {
        // 1. Создание NFA из Regex
        TranslateRegexCommand = new RelayCommand(_ =>
        {
            if (string.IsNullOrWhiteSpace(SourceText)) return;

            var translator = new ThompsonTranslator();
            var nfa = translator.Translate(SourceText);
            CurrentAutomaton = nfa;

            var engine = new ExecutionEngine<FiniteAutomaton, FiniteTransition>(nfa, TestInput);
            Simulation.Initialize(engine, TestInput);
        });

        // 2. Создание PDA из Грамматики
        TranslateGrammarCommand = new RelayCommand(_ =>
        {
            if (string.IsNullOrWhiteSpace(SourceText)) return;

            var translator = new CfgToPdaTranslator();
            var pda = translator.Translate(SourceText);
            CurrentAutomaton = pda;

            var engine = new ExecutionEngine<PushdownAutomaton, PushdownTransition>(pda, TestInput);
            Simulation.Initialize(engine, TestInput);
        });

        // 3. Конвертация NFA в DFA
        ConvertToDfaCommand = new RelayCommand(_ =>
        {
            if (CurrentAutomaton is FiniteAutomaton nfa && !nfa.IsDeterministic())
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var dfa = AutomataSimulator.Core.Operations.NfaToDfaConverter.Convert(nfa);
                sw.Stop();

                System.Windows.MessageBox.Show(
                    $"Состояний NFA: {nfa.States.Count}\n" +
                    $"Состояний DFA: {dfa.States.Count}\n" +
                    $"Время алгоритма подмножеств: {sw.ElapsedMilliseconds} мс",
                    "Эксперимент 3 (NFA -> DFA)");

                CurrentAutomaton = dfa;
                var engine = new ExecutionEngine<FiniteAutomaton, FiniteTransition>(dfa, TestInput);
                Simulation.Initialize(engine, TestInput);
            }
        }, _ => CurrentAutomaton is FiniteAutomaton fa && !fa.IsDeterministic());

        // 4. Сохранение проекта
        SaveProjectCommand = new RelayCommand(_ =>
        {
            if (CurrentAutomaton == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "Automata Project (*.json)|*.json",
                DefaultExt = ".json",
                FileName = "NewAutomaton.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = ProjectSerializer.Serialize(CurrentAutomaton);
                File.WriteAllText(dialog.FileName, json);
            }
        }, _ => CurrentAutomaton != null); // Кнопка активна только если автомат существует

        // 5. Загрузка проекта
        LoadProjectCommand = new RelayCommand(_ =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Automata Project (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var automaton = ProjectSerializer.Deserialize(json);

                    CurrentAutomaton = automaton; // Запускает сеттер, который делает кнопку "Сохранить" активной

                    if (automaton is FiniteAutomaton fa)
                    {
                        var engine = new ExecutionEngine<FiniteAutomaton, FiniteTransition>(fa, TestInput);
                        Simulation.Initialize(engine, TestInput);
                    }
                    else if (automaton is PushdownAutomaton pda)
                    {
                        var engine = new ExecutionEngine<PushdownAutomaton, PushdownTransition>(pda, TestInput);
                        Simulation.Initialize(engine, TestInput);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
                }
            }
        });
    }

    public void LoadManualAutomaton(object automaton)
    {
        CurrentAutomaton = automaton;

        if (automaton is FiniteAutomaton fa)
        {
            var engine = new AutomataSimulator.Engine.ExecutionEngine<FiniteAutomaton, FiniteTransition>(fa, TestInput);
            Simulation.Initialize(engine, TestInput);
        }
        else if (automaton is PushdownAutomaton pda)
        {
            var engine = new AutomataSimulator.Engine.ExecutionEngine<PushdownAutomaton, PushdownTransition>(pda, TestInput);
            Simulation.Initialize(engine, TestInput);
        }
    }
}