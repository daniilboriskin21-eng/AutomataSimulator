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
        // Если строка пуста, мы все еще можем делать шаги, если это разрешено правилами PDA (переходы по пустому вводу)
        char? inputSymbol = !string.IsNullOrEmpty(current.RemainingInput) ? current.RemainingInput[0] : null;
        var pdaTransitions = transitions.Cast<PushdownTransition>();

        // Для PDA мы берем верхний символ стека
        char? topOfStack = current.Stack.IsEmpty ? null : current.Stack.Peek();

        // Ищем подходящий переход:
        // 1. Совпадает текущее состояние
        // 2. Совпадает входной символ (или переход не требует символа)
        // 3. Совпадает символ на вершине стека (или переход не требует извлечения)
        var validTransition = pdaTransitions.FirstOrDefault(t =>
            current.ActiveStateIds.Contains(t.FromStateId) &&
            t.InputSymbol == inputSymbol &&
            t.PopSymbol == topOfStack);

        if (validTransition == null) return current; // Тупик или нужно проверить эпсилон

        // Обновляем стек
        var newStack = current.Stack;
        if (validTransition.PopSymbol.HasValue)
            newStack = newStack.Pop();

        if (!string.IsNullOrEmpty(validTransition.PushSymbols))
        {
            // Символы кладутся в стек в обратном порядке, чтобы первый символ строки оказался сверху
            foreach (var c in validTransition.PushSymbols.Reverse())
            {
                newStack = newStack.Push(c);
            }
        }

        return current with
        {
            ActiveStateIds = ImmutableHashSet.Create(validTransition.ToStateId),
            RemainingInput = inputSymbol.HasValue ? current.RemainingInput[1..] : current.RemainingInput,
            ReadPosition = inputSymbol.HasValue ? current.ReadPosition + 1 : current.ReadPosition,
            Stack = newStack,
            IsEpsilonStep = !inputSymbol.HasValue
        };
    }

    public ExecutionState ApplyEpsilonClosure(ExecutionState current, IEnumerable<ITransition> transitions)
    {
        // В классическом PDA эпсилон-замыкание сложнее, так как оно зависит от состояния стека.
        // Для упрощения симуляции в пошаговом режиме часто эпсилон-переходы считаются отдельным шагом "Step".
        // Но здесь мы можем реализовать поиск автоматических переходов, не требующих входного символа.
        return current;
    }
}