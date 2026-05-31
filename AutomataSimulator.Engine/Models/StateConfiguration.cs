using System.Collections.Immutable;

namespace AutomataSimulator.Engine.Models;

public record StateConfiguration(Guid StateId, IImmutableStack<char> Stack)
{
    // Переопределяем Equals для корректной работы HashSet
    public virtual bool Equals(StateConfiguration? other)
    {
        if (other == null) return false;
        if (StateId != other.StateId) return false;
        return Stack.SequenceEqual(other.Stack);
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(StateId);

        // Быстро хэшируем элементы стека без создания строк
        foreach (var item in Stack)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}