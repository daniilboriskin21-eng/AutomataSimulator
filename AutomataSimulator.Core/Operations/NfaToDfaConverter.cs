using AutomataSimulator.Core.Enums;
using AutomataSimulator.Core.Models;
using AutomataSimulator.Core.Models.Automata;
using AutomataSimulator.Core.Models.Transitions;

namespace AutomataSimulator.Core.Operations;

public static class NfaToDfaConverter
{
    public static FiniteAutomaton Convert(FiniteAutomaton nfa)
    {
        if (nfa.IsDeterministic()) return nfa; // Уже DFA

        var dfa = new FiniteAutomaton(isDeterministic: true)
        {
            Name = nfa.Name + " (DFA)",
            Origin = CreationOrigin.Manual,
            Alphabet = nfa.Alphabet.Where(c => c != null).ToHashSet()
        };

        // Собираем все символы переходов (кроме эпсилон)
        var alphabet = nfa.Transitions
            .Select(t => t.Symbol)
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .Distinct()
            .ToList();

        dfa.Alphabet = alphabet.ToHashSet();

        // Маппинг: набор ID состояний NFA -> одно состояние DFA
        var dfaStates = new Dictionary<string, State>();
        var unprocessedStates = new Queue<HashSet<Guid>>();

        // 1. Начальное состояние DFA = эпсилон-замыкание начального состояния NFA
        var startNfaState = nfa.GetStartState() ?? throw new Exception("No start state in NFA");
        var dfaStartStateSet = GetEpsilonClosure(nfa, new HashSet<Guid> { startNfaState.Id });

        var dfaStartState = CreateDfaState(nfa, dfaStartStateSet);
        dfaStartState.IsStart = true;

        dfa.States.Add(dfaStartState);
        dfaStates[GetStateKey(dfaStartStateSet)] = dfaStartState;
        unprocessedStates.Enqueue(dfaStartStateSet);

        // 2. Основной цикл алгоритма построения подмножеств
        while (unprocessedStates.Count > 0)
        {
            var currentSet = unprocessedStates.Dequeue();
            var currentDfaState = dfaStates[GetStateKey(currentSet)];

            foreach (var symbol in alphabet)
            {
                // Переход по символу
                var nextSet = GetNextStates(nfa, currentSet, symbol);
                // Эпсилон-замыкание полученного множества
                var closureSet = GetEpsilonClosure(nfa, nextSet);

                if (closureSet.Count == 0) continue; // Тупик (в DFA можно добавить "мусорное" состояние, но для симуляции можно просто опустить переход)

                var key = GetStateKey(closureSet);
                if (!dfaStates.TryGetValue(key, out var nextDfaState))
                {
                    nextDfaState = CreateDfaState(nfa, closureSet);
                    dfa.States.Add(nextDfaState);
                    dfaStates[key] = nextDfaState;
                    unprocessedStates.Enqueue(closureSet);
                }

                dfa.Transitions.Add(new FiniteTransition
                {
                    FromStateId = currentDfaState.Id,
                    ToStateId = nextDfaState.Id,
                    Symbol = symbol
                });
            }
        }

        return dfa;
    }

    private static HashSet<Guid> GetEpsilonClosure(FiniteAutomaton nfa, HashSet<Guid> states)
    {
        var closure = new HashSet<Guid>(states);
        var stack = new Stack<Guid>(states);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            var reachable = nfa.Transitions
                .Where(t => t.FromStateId == current && t.Symbol == null)
                .Select(t => t.ToStateId);

            foreach (var r in reachable)
            {
                if (closure.Add(r)) stack.Push(r);
            }
        }
        return closure;
    }

    private static HashSet<Guid> GetNextStates(FiniteAutomaton nfa, HashSet<Guid> states, char symbol)
    {
        return nfa.Transitions
            .Where(t => states.Contains(t.FromStateId) && t.Symbol == symbol)
            .Select(t => t.ToStateId)
            .ToHashSet();
    }

    private static State CreateDfaState(FiniteAutomaton nfa, HashSet<Guid> nfaStateIds)
    {
        bool isFinal = nfa.States.Any(s => nfaStateIds.Contains(s.Id) && s.IsFinal);
        return new State
        {
            Name = "{" + string.Join(",", nfa.States.Where(s => nfaStateIds.Contains(s.Id)).Select(s => s.Name)) + "}",
            IsFinal = isFinal
        };
    }

    private static string GetStateKey(HashSet<Guid> states) =>
        string.Join("|", states.OrderBy(g => g));
}