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
        if (!IsEnabled) return false;

        // Проверяем, есть ли хотя бы в одной ветви вычислений (конфигурации) состояние с нашим StateId
        bool isHit = state.ActiveConfigurations.Any(c => c.StateId == StateId);

        if (!isHit) return false;

        // Если есть условный брейкпоинт (лямбда), проверяем его, иначе просто останавливаемся
        return Condition?.Invoke(state) ?? true;
    }
}