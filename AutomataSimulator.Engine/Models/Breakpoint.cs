namespace AutomataSimulator.Engine.Models;

public class Breakpoint
{
    public Guid Id { get; } = Guid.NewGuid();
    public bool IsEnabled { get; set; } = true;

    // Привязка к конкретному состоянию автомата
    public Guid StateId { get; set; }

    // Условие (лямбда-выражение, проверяющее ExecutionState)
    // Например: state => state.Stack.Count() > 5
    public Func<ExecutionState, bool>? Condition { get; set; }

    public bool ShouldStop(ExecutionState state)
    {
        if (!IsEnabled || !state.ActiveStateIds.Contains(StateId))
            return false;

        return Condition?.Invoke(state) ?? true;
    }
}