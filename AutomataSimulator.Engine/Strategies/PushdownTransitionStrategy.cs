using AutomataSimulator.Core.Interfaces;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.Engine.Interfaces;
using AutomataSimulator.Engine.Models;
using System.Collections.Immutable;

namespace AutomataSimulator.Engine.Strategies;

public class PushdownTransitionStrategy : ITransitionStrategy
{
    public ExecutionState NextStep(ExecutionState current, IEnumerable<ITransition> transitions)
    {
        if (current.IsTerminal) return current;

        char input = current.FullInput[current.ReadPosition];
        var pdaTransitions = transitions.Cast<PushdownTransition>().ToList();

        var nextConfigs = new HashSet<StateConfiguration>();

        foreach (var config in current.ActiveConfigurations)
        {
            char? top = config.Stack.IsEmpty ? null : config.Stack.Peek();
            var valid = pdaTransitions.Where(t => t.FromStateId == config.StateId && t.InputSymbol == input && t.PopSymbol == top);

            foreach (var t in valid)
            {
                var nextStack = config.Stack;
                if (t.PopSymbol.HasValue) nextStack = nextStack.Pop();
                if (!string.IsNullOrEmpty(t.PushSymbols))
                    foreach (var c in t.PushSymbols.Reverse()) nextStack = nextStack.Push(c);

                nextConfigs.Add(new StateConfiguration(t.ToStateId, nextStack));
            }
        }

        if (nextConfigs.Count == 0) return current with { ActiveConfigurations = ImmutableHashSet<StateConfiguration>.Empty };

        return current with
        {
            ActiveConfigurations = nextConfigs.ToImmutableHashSet(),
            ReadPosition = current.ReadPosition + 1,
            IsEpsilonStep = false
        };
    }
    public ExecutionState ApplyEpsilonClosure(ExecutionState current, IEnumerable<ITransition> transitions)
    {
        var pdaTransitions = transitions.Cast<PushdownTransition>().ToList();
        var queue = new Queue<StateConfiguration>(current.ActiveConfigurations);
        var closure = new HashSet<StateConfiguration>(current.ActiveConfigurations);

        while (queue.Count > 0)
        {
            var config = queue.Dequeue();
            char? top = config.Stack.IsEmpty ? null : config.Stack.Peek();

            var epsTransitions = pdaTransitions.Where(t => t.FromStateId == config.StateId && t.InputSymbol == null && t.PopSymbol == top);

            foreach (var t in epsTransitions)
            {
                var nextStack = config.Stack;
                if (t.PopSymbol.HasValue) nextStack = nextStack.Pop();
                if (!string.IsNullOrEmpty(t.PushSymbols))
                    foreach (var c in t.PushSymbols.Reverse()) nextStack = nextStack.Push(c);

                var newConfig = new StateConfiguration(t.ToStateId, nextStack);
                if (closure.Add(newConfig)) queue.Enqueue(newConfig);
            }
        }

        return current with { ActiveConfigurations = closure.ToImmutableHashSet(), IsEpsilonStep = true };
    }
}