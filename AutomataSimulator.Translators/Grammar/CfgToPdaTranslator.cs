using System;
using System.Collections.Generic;
using System.Linq;
using AutomataSimulator.Core.Enums;
using AutomataSimulator.Core.Models;
using AutomataSimulator.Core.Models.Automata;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.Translators.Interfaces;

namespace AutomataSimulator.Translators.Grammar;

public class CfgToPdaTranslator : ITranslator<PushdownAutomaton>
{
    public PushdownAutomaton Translate(string grammarSource)
    {
        var pda = new PushdownAutomaton
        {
            Origin = CreationOrigin.Grammar,
            OriginSource = grammarSource,
            Name = "PDA from Grammar"
        };

        // 1. Создаем основное состояние
        var q = new State { Name = "q_loop", IsStart = true, IsFinal = true };
        pda.States.Add(q);

        var lines = grammarSource.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var terminals = new HashSet<char>();
        var nonTerminals = new HashSet<char>();

        // 2. Парсим правила: S -> aSb | ε
        foreach (var line in lines)
        {
            var parts = line.Split("->", StringSplitOptions.TrimEntries);
            if (parts.Length < 2) continue;

            char nt = parts[0][0];
            nonTerminals.Add(nt);

            // Если это первая строка, устанавливаем стартовый символ стека
            if (pda.InitialStackSymbol == null) pda.InitialStackSymbol = nt;

            var productions = parts[1].Split('|', StringSplitOptions.TrimEntries);
            foreach (var prod in productions)
            {
                // Правило (q, ε, nt) -> (q, prod)
                pda.Transitions.Add(new PushdownTransition
                {
                    FromStateId = q.Id,
                    ToStateId = q.Id,
                    InputSymbol = null,
                    PopSymbol = nt,
                    PushSymbols = prod == "ε" ? "" : prod
                });

                foreach (char c in prod)
                    if (!char.IsUpper(c) && c != 'ε') terminals.Add(c);
            }
        }

        // 3. Добавляем переходы сопоставления для терминалов: (q, a, a) -> (q, ε)
        foreach (char t in terminals)
        {
            pda.Transitions.Add(new PushdownTransition
            {
                FromStateId = q.Id,
                ToStateId = q.Id,
                InputSymbol = t,
                PopSymbol = t,
                PushSymbols = ""
            });
        }

        pda.Alphabet = terminals;
        pda.StackAlphabet = new HashSet<char>(terminals.Concat(nonTerminals));

        return pda;
    }
}