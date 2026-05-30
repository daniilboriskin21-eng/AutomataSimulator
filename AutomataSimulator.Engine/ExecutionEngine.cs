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
    private string _fullInput;
    private readonly List<Breakpoint> _breakpoints = new();
    private readonly Stack<ExecutionState> _history = new();
    public HashSet<char> Alphabet => _automaton.Alphabet;
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

        // Инициализируем пустой стек. Если это PDA (с магазинной памятью) 
        // и есть начальный символ стека, добавляем его.
        var initialStack = ImmutableStack<char>.Empty;
        if (_automaton is PushdownAutomaton pda && pda.InitialStackSymbol.HasValue)
        {
            initialStack = initialStack.Push(pda.InitialStackSymbol.Value);
        }

        var initialConfig = new StateConfiguration(startState.Id, initialStack);

        var initialState = new ExecutionState
        {
            ActiveConfigurations = ImmutableHashSet.Create(initialConfig),
            RemainingInput = input, // В методе Reset тут будет _fullInput
            ReadPosition = 0
        };

        // На старте не делаем эпсилон-замыкание, просто стоим в первой точке
        CurrentState = initialState;
        RefreshActiveIds();
    }
    public IEnumerable<Guid> GetActiveStateIds() => _activeStateIds;
    private void RefreshActiveIds()
    {
        // Собираем все уникальные ID состояний из всех активных конфигураций
        _activeStateIds = CurrentState.ActiveConfigurations.Select(c => c.StateId).ToHashSet();
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

        var initialConfig = new StateConfiguration(startState.Id, initialStack);

        var initialState = new ExecutionState
        {
            ActiveConfigurations = ImmutableHashSet.Create(initialConfig),
            RemainingInput = _fullInput, // В методе Reset тут будет _fullInput
            ReadPosition = 0
        };

        // СТАЛО: Никакого эпсилон-замыкания. Возвращаемся в 1 точку.
        CurrentState = initialState;
        RefreshActiveIds();
    }
    private bool HasAvailableEpsilonTransitions()
    {
        // Проверка: можно ли из текущих состояний уйти по эпсилон в новые состояния
        var closed = _strategy.ApplyEpsilonClosure(CurrentState, _automaton.Transitions.Cast<ITransition>());
        return !closed.ActiveConfigurations.SetEquals(CurrentState.ActiveConfigurations);
    }

    public void ToggleBreakpoint(Guid stateId)
    {
        // Ищем, есть ли уже брейкпоинт на этом состоянии
        var existing = _breakpoints.FirstOrDefault(b => b.StateId == stateId);

        if (existing != null)
        {
            // Если есть - удаляем (снимаем точку останова)
            _breakpoints.Remove(existing);
        }
        else
        {
            // Если нет - добавляем
            _breakpoints.Add(new Breakpoint { StateId = stateId, IsEnabled = true });
        }
    }

    public void Run()
    {
        while (CanStepForward)
        {
            StepForward();

            // После каждого шага проверяем, не попали ли мы на состояние с брейкпоинтом
            if (_breakpoints.Any(bp => bp.ShouldStop(CurrentState)))
            {
                // Если попали - прерываем цикл выполнения
                break;
            }
        }
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
            // 1. Строка должна быть полностью прочитана (IsTerminal == true)
            if (!CurrentState.IsTerminal) return false;

            // 2. Получаем ID всех финальных состояний автомата
            var finalStateIds = _automaton.GetFinalStates().Select(s => s.Id).ToHashSet();

            // 3. Если хотя бы одна активная конфигурация находится в финальном состоянии - Успех!
            return CurrentState.ActiveConfigurations.Any(c => finalStateIds.Contains(c.StateId));
        }
    }
}