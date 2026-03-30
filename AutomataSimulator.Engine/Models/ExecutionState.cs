using System.Collections.Immutable;
using AutomataSimulator.Core.Models;

namespace AutomataSimulator.Engine.Models;

/// <summary>
/// Снимок (Snapshot) текущего состояния симуляции.
/// </summary>
public record ExecutionState
{
    // Текущие активные состояния (для NFA может быть несколько)
    public IImmutableSet<Guid> ActiveStateIds { get; init; } = ImmutableHashSet<Guid>.Empty;

    // Оставшаяся часть входной строки
    public string RemainingInput { get; init; } = string.Empty;

    // Текущее содержимое стека (для PDA)
    public IImmutableStack<char> Stack { get; init; } = ImmutableStack<char>.Empty;

    // Сколько символов уже прочитано
    public int ReadPosition { get; init; }

    // Был ли этот шаг переходом по эпсилон
    public bool IsEpsilonStep { get; init; }

    public bool IsTerminal => string.IsNullOrEmpty(RemainingInput);
}