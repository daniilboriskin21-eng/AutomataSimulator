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

        var nextConfigs = new HashSet<StateConfiguration>();

        foreach (var config in current.ActiveConfigurations)
        {
            var reachable = finiteTransitions
                .Where(t => t.FromStateId == config.StateId && t.Symbol == inputSymbol)
                .Select(t => new StateConfiguration(t.ToStateId, ImmutableStack<char>.Empty));

            foreach (var r in reachable) nextConfigs.Add(r);
        }

        if (nextConfigs.Count == 0) return current with { ActiveConfigurations = ImmutableHashSet<StateConfiguration>.Empty };

        return current with
        {
            ActiveConfigurations = nextConfigs.ToImmutableHashSet(),
            RemainingInput = current.RemainingInput[1..],
            ReadPosition = current.ReadPosition + 1,
            IsEpsilonStep = false
        };
    }

    public ExecutionState ApplyEpsilonClosure(ExecutionState current, IEnumerable<ITransition> transitions)
    {
        var finiteTransitions = transitions.Cast<FiniteTransition>().ToList();
        var closure = new HashSet<StateConfiguration>(current.ActiveConfigurations);
        var stack = new Stack<StateConfiguration>(current.ActiveConfigurations);

        while (stack.Count > 0)
        {
            var config = stack.Pop();
            var reachable = finiteTransitions
                .Where(t => t.FromStateId == config.StateId && t.Symbol == null)
                .Select(t => new StateConfiguration(t.ToStateId, ImmutableStack<char>.Empty));

            foreach (var r in reachable)
            {
                if (closure.Add(r)) stack.Push(r);
            }
        }

        return current with { ActiveConfigurations = closure.ToImmutableHashSet(), IsEpsilonStep = true };
    }
}