using AutomataSimulator.Core.Interfaces;
using AutomataSimulator.Core.Models;
using AutomataSimulator.Core.Models.Automata;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.Engine.Interfaces;
using AutomataSimulator.Engine.Models;
using AutomataSimulator.Engine.Strategies;
using System.Collections.Immutable;

namespace AutomataSimulator.Engine;

public class ExecutionEngine<TAutomaton, TTransition> : IExecutionEngine
    where TAutomaton : Automaton<TTransition>
    where TTransition : Transition
{
    public int StepCount { get; set; }
    public int MaxConfigurations { get; set; }
    private readonly List<ITransition> _transitionsCache;

    private HashSet<Guid> _activeStateIds = new();
    private readonly TAutomaton _automaton;
    private readonly ITransitionStrategy _strategy; // Поле теперь существует
    private string _fullInput;
    private readonly List<Breakpoint> _breakpoints = new();
    private readonly Stack<ExecutionState> _history = new();
    public HashSet<char> Alphabet => _automaton.Alphabet;
    public ExecutionState CurrentState { get; private set; }
    public bool CanStepForward => CurrentState.ActiveConfigurations.Any() && (!CurrentState.IsTerminal || HasAvailableEpsilonTransitions());
    public bool CanStepBackward => _history.Count > 0;

    public ExecutionEngine(TAutomaton automaton, string input)
    {
        _automaton = automaton;
        _fullInput = input;

        _strategy = automaton switch
        {
            PushdownAutomaton => new PushdownTransitionStrategy(),
            _ => new FiniteTransitionStrategy()
        };

        var startState = _automaton.GetStartState()
            ?? throw new InvalidOperationException("Автомат не имеет начального состояния");

        var initialStack = ImmutableStack<char>.Empty;
        if (_automaton is PushdownAutomaton pda && pda.InitialStackSymbol.HasValue)
        {
            initialStack = initialStack.Push(pda.InitialStackSymbol.Value);
        }

        var initialConfig = new StateConfiguration(startState.Id, initialStack);

        var initialState = new ExecutionState
        {
            ActiveConfigurations = ImmutableHashSet.Create(initialConfig),
            FullInput = input,
            ReadPosition = 0
        };

        CurrentState = initialState;
        RefreshActiveIds();
        StepCount = 0;
        MaxConfigurations = CurrentState.ActiveConfigurations.Count;
        _transitionsCache = _automaton.Transitions.Cast<ITransition>().ToList();
    }
    public IEnumerable<Guid> GetActiveStateIds() => _activeStateIds;
    private void RefreshActiveIds()
    {
        _activeStateIds = CurrentState.ActiveConfigurations.Select(c => c.StateId).ToHashSet();
    }


    public void StepBackward()
    {
        if (CanStepBackward)
        {
            CurrentState = _history.Pop();
            RefreshActiveIds();
        }
    }

    public void Reset()
    {
        _history.Clear();

        var startState = _automaton.GetStartState() ?? throw new Exception("No start state");
        var initialStack = ImmutableStack<char>.Empty;
        if (_automaton is PushdownAutomaton pda && pda.InitialStackSymbol.HasValue)
            initialStack = initialStack.Push(pda.InitialStackSymbol.Value);

        var initialConfig = new StateConfiguration(startState.Id, initialStack);

        var initialState = new ExecutionState
        {
            ActiveConfigurations = ImmutableHashSet.Create(initialConfig),
            FullInput = _fullInput,
            ReadPosition = 0
        };

        CurrentState = initialState;
        RefreshActiveIds();
        StepCount = 0;
        MaxConfigurations = CurrentState.ActiveConfigurations.Count;
    }
    private bool HasAvailableEpsilonTransitions()
    {
        // Проверка: можно ли из текущих состояний уйти по эпсилон в новые состояния
        var closed = _strategy.ApplyEpsilonClosure(CurrentState, _automaton.Transitions.Cast<ITransition>());
        return !closed.ActiveConfigurations.SetEquals(CurrentState.ActiveConfigurations);
    }

    public void ToggleBreakpoint(Guid stateId)
    {
        var existing = _breakpoints.FirstOrDefault(b => b.StateId == stateId);

        if (existing != null)
        {
            _breakpoints.Remove(existing);
        }
        else
        {
            _breakpoints.Add(new Breakpoint { StateId = stateId, IsEnabled = true });
        }
    }
    private void DoStepLogic()
    {
        var closedCurrent = _strategy.ApplyEpsilonClosure(CurrentState, _transitionsCache);
        var nextState = _strategy.NextStep(closedCurrent, _transitionsCache);
        CurrentState = _strategy.ApplyEpsilonClosure(nextState, _transitionsCache);

        StepCount++;
        if (CurrentState.ActiveConfigurations.Count > MaxConfigurations)
        {
            MaxConfigurations = CurrentState.ActiveConfigurations.Count;
        }
    }
    public void StepForward()
    {
        if (!CanStepForward) return;

        _history.Push(CurrentState); // Сохраняем историю только при ручном шаге!
        DoStepLogic();
        RefreshActiveIds();
    }
    public void Run()
    {
        while (CanStepForward)
        {
            if (_breakpoints.Any(bp => bp.ShouldStop(CurrentState))) break;

            DoStepLogic(); // Вызываем логику без сохранения в историю!
        }
        RefreshActiveIds();
    }
    public void SetInput(string input)
    {
        _fullInput = input;
        Reset(); // Сбросит историю и поставит автомат в стартовое состояние с новой строкой
    }
    public bool IsAccepted
    {
        get
        {
            // 1. Строка должна быть прочитана полностью И должна остаться хоть одна живая ветка
            if (!CurrentState.IsTerminal || !CurrentState.ActiveConfigurations.Any()) return false;

            var finalStateIds = _automaton.GetFinalStates().Select(s => s.Id).ToHashSet();

            if (_automaton is PushdownAutomaton)
            {
                // Для PDA добавляем жесткое условие: Финальное состояние + ПУСТОЙ СТЕК
                return CurrentState.ActiveConfigurations.Any(c =>
                    finalStateIds.Contains(c.StateId) && c.Stack.IsEmpty);
            }

            return CurrentState.ActiveConfigurations.Any(c => finalStateIds.Contains(c.StateId));
        }
    }
    public bool IsPda => _automaton is PushdownAutomaton;
        public string GetStateName(Guid id)
    {
        var state = _automaton.States.FirstOrDefault(s => s.Id == id);
        return state?.Name ?? "Unknown";
    }
}