using AutomataSimulator.Engine.Models;

namespace AutomataSimulator.Engine.Interfaces;

public interface IExecutionEngine
{
    public int StepCount { get; set; }
    public int MaxConfigurations { get; set; }

    ExecutionState CurrentState { get; }
    bool CanStepForward { get; }
    bool CanStepBackward { get; }

    void StepForward();
    void StepBackward();
    void Reset();

    /// <summary>
    /// Возвращает список ID всех состояний, в которых сейчас находится автомат.
    /// </summary>
    IEnumerable<Guid> GetActiveStateIds();
    void ToggleBreakpoint(Guid stateId);
    void Run();
    void SetInput(string input);
    bool IsAccepted { get; }
    HashSet<char> Alphabet { get; }
    bool IsPda { get; }
    string GetStateName(Guid id);
}