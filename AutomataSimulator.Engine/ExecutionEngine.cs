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
    private HashSet<Guid> _activeStateIds = new();
    private readonly TAutomaton _automaton;
    private readonly ITransitionStrategy _strategy; // Поле теперь существует
    private readonly string _fullInput;
    private readonly List<Breakpoint> _breakpoints = new();
    private readonly Stack<ExecutionState> _history = new();

    public IEnumerable<Guid> GetActiveStateIds() => _activeStateIds;
    public ExecutionState CurrentState { get; private set; }
    public bool CanStepForward => !CurrentState.IsTerminal || HasAvailableEpsilonTransitions();
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

        // --- ЭТОТ БЛОК БЫЛ ПОТЕРЯН ---
        // Инициализируем пустой стек. Если это PDA (с магазинной памятью) 
        // и есть начальный символ стека, добавляем его.
        var initialStack = ImmutableStack<char>.Empty;
        if (_automaton is PushdownAutomaton pda && pda.InitialStackSymbol.HasValue)
        {
            initialStack = initialStack.Push(pda.InitialStackSymbol.Value);
        }
        // ------------------------------

        var initialState = new ExecutionState
        {
            ActiveStateIds = ImmutableHashSet.Create(startState.Id),
            RemainingInput = input,
            Stack = initialStack, // Теперь компилятор знает, что это такое
            ReadPosition = 0
        };

        // На старте не делаем эпсилон-замыкание, просто стоим в первой точке
        CurrentState = initialState;
        RefreshActiveIds();
    }
    private void RefreshActiveIds()
    {
        // Обновляем список, который запрашивает MainWindow для подсветки
        _activeStateIds = new HashSet<Guid>(CurrentState.ActiveStateIds);
    }

    public void StepForward()
    {
        if (!CanStepForward) return;

        // 1. Делаем "фотографию" текущего графа и кладем в стек.
        // Если ExecutionState это record, он запомнит ровно ту картину, что сейчас на экране.
        _history.Push(CurrentState);

        // 2. Внутренне применяем эпсилон-замыкание к тому, где мы сейчас стоим, 
        // чтобы автомат "увидел" все доступные пути перед чтением символа
        var closedCurrent = _strategy.ApplyEpsilonClosure(CurrentState, _automaton.Transitions.Cast<ITransition>());

        // 3. Делаем шаг по символу
        var nextState = _strategy.NextStep(closedCurrent, _automaton.Transitions.Cast<ITransition>());

        // 4. Применяем замыкание к новым узлам и сохраняем как текущее состояние
        CurrentState = _strategy.ApplyEpsilonClosure(nextState, _automaton.Transitions.Cast<ITransition>());

        // 5. Обновляем экран
        RefreshActiveIds();
    }

    public void StepBackward()
    {
        if (CanStepBackward)
        {
            // Просто достаем последнее состояние. 
            // Если это record, оно сохранилось именно таким, каким было.
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

        var initialState = new ExecutionState
        {
            ActiveStateIds = ImmutableHashSet.Create(startState.Id),
            RemainingInput = _fullInput,
            Stack = initialStack,
            ReadPosition = 0
        };

        // СТАЛО: Никакого эпсилон-замыкания. Возвращаемся в 1 точку.
        CurrentState = initialState;
        RefreshActiveIds();
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

    public void ToggleBreakpoint(Guid stateId)
    {
        var existing = _breakpoints.FirstOrDefault(b => b.StateId == stateId);
        if (existing != null) _breakpoints.Remove(existing);
        else _breakpoints.Add(new Breakpoint { StateId = stateId });
    }

    public void Run()
    {
        while (CanStepForward)
        {
            StepForward();

            // Если после шага мы оказались в состоянии с включенным брейкпоинтом - останавливаем цикл Run
            if (_breakpoints.Any(bp => bp.ShouldStop(CurrentState)))
            {
                break;
            }
        }
    }
}