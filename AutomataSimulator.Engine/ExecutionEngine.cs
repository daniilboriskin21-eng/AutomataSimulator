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
    private readonly TAutomaton _automaton;
    private readonly ITransitionStrategy _strategy; // Поле теперь существует
    private readonly string _fullInput;
    private readonly List<Breakpoint> _breakpoints = new();
    private readonly Stack<ExecutionState> _history = new();

    public ExecutionState CurrentState { get; private set; }
    public bool CanStepForward => !CurrentState.IsTerminal || HasAvailableEpsilonTransitions();
    public bool CanStepBackward => _history.Count > 0;

    public ExecutionEngine(TAutomaton automaton, string input)
    {
        _automaton = automaton;
        _fullInput = input;

        // Инициализация стратегии в зависимости от типа автомата
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

        // Начальное состояние + сразу применяем эпсилон-замыкание для стартового узла
        var initialState = new ExecutionState
        {
            ActiveStateIds = ImmutableHashSet.Create(startState.Id),
            RemainingInput = input,
            Stack = initialStack,
            ReadPosition = 0
        };

        CurrentState = _strategy.ApplyEpsilonClosure(initialState, _automaton.Transitions.Cast<ITransition>());
    }

    public void StepForward()
    {
        if (!CanStepForward) return;

        _history.Push(CurrentState);

        // 1. Пытаемся сделать шаг по символу
        var nextState = _strategy.NextStep(CurrentState, _automaton.Transitions.Cast<ITransition>());

        // 2. Применяем эпсилон-замыкание для новых состояний
        CurrentState = _strategy.ApplyEpsilonClosure(nextState, _automaton.Transitions.Cast<ITransition>());

        CheckBreakpoints();
    }

    public void StepBackward()
    {
        if (CanStepBackward)
        {
            CurrentState = _history.Pop();
        }
    }

    public void Reset()
    {
        _history.Clear();
        // (Логика сброса к начальному состоянию из конструктора)
    }

    private void CheckBreakpoints()
    {
        foreach (var bp in _breakpoints)
        {
            if (bp.ShouldStop(CurrentState)) break;
        }
    }

    private bool HasAvailableEpsilonTransitions()
    {
        // Проверка: можно ли из текущих состояний уйти по эпсилон в новые состояния
        var closed = _strategy.ApplyEpsilonClosure(CurrentState, _automaton.Transitions.Cast<ITransition>());
        return !closed.ActiveStateIds.SetEquals(CurrentState.ActiveStateIds);
    }
}