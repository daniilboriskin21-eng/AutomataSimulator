using QuikGraph;

namespace AutomataSimulator.WPF.Models;

public class VisualEdge : Edge<object>
{
    public string Label { get; set; }

    public VisualEdge(object source, object target, string label)
        : base(source, target)
    {
        Label = label;
    }

    // Добавляем эту строку:
    public override string ToString() => Label;
}