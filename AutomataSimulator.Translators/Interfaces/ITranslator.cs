using AutomataSimulator.Core.Models.Automata;

namespace AutomataSimulator.Translators.Interfaces;

public interface ITranslator<TAutomaton>
{
    TAutomaton Translate(string source);
}