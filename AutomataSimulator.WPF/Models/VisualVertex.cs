namespace AutomataSimulator.WPF.Models;

public class VisualVertex
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsStart { get; set; }
    public bool IsFinal { get; set; }

    public override string ToString() => Name;
}