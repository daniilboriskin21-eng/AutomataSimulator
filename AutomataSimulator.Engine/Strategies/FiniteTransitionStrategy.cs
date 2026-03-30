using AutomataSimulator.Core.Interfaces;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.Engine.Interfaces;
using AutomataSimulator.Engine.Models;
using System.Collections.Immutable;

namespace AutomataSimulator.Engine.Strategies;

public class FiniteTransitionStrategy : ITransitionStrategy
{
    public ExecutionState NextStep(ExecutionState current, IEnumerable<ITransition> transitions)
    {
        if (string.IsNullOrEmpty(current.RemainingInput)) return current;

        char inputSymbol = current.RemainingInput[0];
        var finiteTransitions = transitions.Cast<FiniteTransition>();

        // Ищем все переходы из текущих активных состояний по данному символу
        var nextStateIds = finiteTransitions
            .Where(t => current.ActiveStateIds.Contains(t.FromStateId) && t.Symbol == inputSymbol)
            .Select(t => t.ToStateId)
            .ToImmutableHashSet();

        if (nextStateIds.IsEmpty) return current with { RemainingInput = "" }; // Тупик

        return current with
        {
            ActiveStateIds = nextStateIds,
            RemainingInput = current.RemainingInput[1..],
            ReadPosition = current.ReadPosition + 1,
            IsEpsilonStep = false
        };
    }

    public ExecutionState ApplyEpsilonClosure(ExecutionState current, IEnumerable<ITransition> transitions)
    {
        var finiteTransitions = transitions.Cast<FiniteTransition>().ToList();
        var closure = current.ActiveStateIds.ToHashSet();
        var stack = new Stack<Guid>(closure);

        while (stack.Count > 0)
        {
            var stateId = stack.Pop();
            var reachableViaEpsilon = finiteTransitions
                .Where(t => t.FromStateId == stateId && t.Symbol == null)
                .Select(t => t.ToStateId);

            foreach (var targetId in reachableViaEpsilon)
            {
                if (closure.Add(targetId))
                    stack.Push(targetId);
            }
        }

        return current with
        {
            ActiveStateIds = closure.ToImmutableHashSet(),
            IsEpsilonStep = true
        };
    }
}