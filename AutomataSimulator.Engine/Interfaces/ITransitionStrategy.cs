using AutomataSimulator.Core.Interfaces;
using AutomataSimulator.Core.Models;
using AutomataSimulator.Engine.Models;

namespace AutomataSimulator.Engine.Interfaces;

public interface ITransitionStrategy
{
    // Вычисляет следующее состояние на основе текущего и входного символа
    ExecutionState NextStep(ExecutionState current, IEnumerable<ITransition> transitions);

    // Вычисляет достижимые состояния по эпсилон-переходам
    ExecutionState ApplyEpsilonClosure(ExecutionState current, IEnumerable<ITransition> transitions);
}