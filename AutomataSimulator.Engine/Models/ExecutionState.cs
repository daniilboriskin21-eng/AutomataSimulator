using System.Collections.Immutable;

namespace AutomataSimulator.Engine.Models;

public record ExecutionState
{
    public IImmutableSet<StateConfiguration> ActiveConfigurations { get; init; } = ImmutableHashSet<StateConfiguration>.Empty;
    public string FullInput { get; init; } = string.Empty;
    public int ReadPosition { get; init; }
    public bool IsEpsilonStep { get; init; }
    public bool IsTerminal => ReadPosition >= FullInput.Length;
}