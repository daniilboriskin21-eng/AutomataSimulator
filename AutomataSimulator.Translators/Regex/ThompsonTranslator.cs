using System.Collections.Generic;
using System.Linq;
using AutomataSimulator.Core.Enums;
using AutomataSimulator.Core.Models;
using AutomataSimulator.Core.Models.Automata;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.Translators.Interfaces;

namespace AutomataSimulator.Translators.Regex;

public class ThompsonTranslator : ITranslator<FiniteAutomaton>
{
    private class Fragment
    {
        public State Start { get; init; } = null!;
        public State End { get; init; } = null!;
    }

    public FiniteAutomaton Translate(string regex)
    {
        var postfix = ToPostfix(AddConcatenationMarkers(regex));
        var stack = new Stack<Fragment>();
        var nfa = new FiniteAutomaton(isDeterministic: false)
        {
            Origin = CreationOrigin.Regex,
            OriginSource = regex
        };

        foreach (char c in postfix)
        {
            switch (c)
            {
                case '*': // Итерация Клини
                    stack.Push(ProcessStar(nfa, stack.Pop()));
                    break;
                case '|': // Объединение
                    var rightOr = stack.Pop();
                    var leftOr = stack.Pop();
                    stack.Push(ProcessUnion(nfa, leftOr, rightOr));
                    break;
                case '.': // Конкатенация
                    var rightCat = stack.Pop();
                    var leftCat = stack.Pop();
                    stack.Push(ProcessConcat(nfa, leftCat, rightCat));
                    break;
                default: // Символ
                    stack.Push(ProcessLiteral(nfa, c));
                    break;
            }
        }

        var finalFragment = stack.Pop();
        finalFragment.Start.IsStart = true;
        finalFragment.End.IsFinal = true;

        return nfa;
    }

    private Fragment ProcessLiteral(FiniteAutomaton nfa, char symbol)
    {
        var s = new State { Name = $"q{nfa.States.Count}" };
        var e = new State { Name = $"q{nfa.States.Count + 1}" };
        nfa.States.Add(s);
        nfa.States.Add(e);
        nfa.Transitions.Add(new FiniteTransition { FromStateId = s.Id, ToStateId = e.Id, Symbol = symbol });
        return new Fragment { Start = s, End = e };
    }

    private Fragment ProcessConcat(FiniteAutomaton nfa, Fragment left, Fragment right)
    {
        // Соединяем конец левого фрагмента с началом правого через эпсилон
        nfa.Transitions.Add(new FiniteTransition { FromStateId = left.End.Id, ToStateId = right.Start.Id, Symbol = null });
        return new Fragment { Start = left.Start, End = right.End };
    }

    private Fragment ProcessUnion(FiniteAutomaton nfa, Fragment left, Fragment right)
    {
        var s = new State { Name = $"q{nfa.States.Count}" };
        var e = new State { Name = $"q{nfa.States.Count + 1}" };
        nfa.States.AddRange(new[] { s, e });

        nfa.Transitions.Add(new FiniteTransition { FromStateId = s.Id, ToStateId = left.Start.Id, Symbol = null });
        nfa.Transitions.Add(new FiniteTransition { FromStateId = s.Id, ToStateId = right.Start.Id, Symbol = null });
        nfa.Transitions.Add(new FiniteTransition { FromStateId = left.End.Id, ToStateId = e.Id, Symbol = null });
        nfa.Transitions.Add(new FiniteTransition { FromStateId = right.End.Id, ToStateId = e.Id, Symbol = null });

        return new Fragment { Start = s, End = e };
    }

    private Fragment ProcessStar(FiniteAutomaton nfa, Fragment inner)
    {
        var s = new State { Name = $"q{nfa.States.Count}" };
        var e = new State { Name = $"q{nfa.States.Count + 1}" };
        nfa.States.AddRange(new[] { s, e });

        nfa.Transitions.Add(new FiniteTransition { FromStateId = s.Id, ToStateId = inner.Start.Id, Symbol = null });
        nfa.Transitions.Add(new FiniteTransition { FromStateId = s.Id, ToStateId = e.Id, Symbol = null });
        nfa.Transitions.Add(new FiniteTransition { FromStateId = inner.End.Id, ToStateId = inner.Start.Id, Symbol = null });
        nfa.Transitions.Add(new FiniteTransition { FromStateId = inner.End.Id, ToStateId = e.Id, Symbol = null });

        return new Fragment { Start = s, End = e };
    }

    // Вспомогательные методы для парсинга (упрощенная реализация)
    private string AddConcatenationMarkers(string regex) { /* Логика добавления точек между символами */ return regex; }
    private string ToPostfix(string regex) { /* Алгоритм Shunting-yard */ return regex; }
}