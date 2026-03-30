using AutomataSimulator.Engine.Models;

namespace AutomataSimulator.Engine.Interfaces;

public interface IExecutionEngine
{
    ExecutionState CurrentState { get; }
    bool CanStepForward { get; }
    bool CanStepBackward { get; }

    void StepForward();
    void StepBackward();
    void Reset();
}