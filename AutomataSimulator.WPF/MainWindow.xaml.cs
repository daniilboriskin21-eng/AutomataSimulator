using System.Linq;
using System.Windows;
using AutomataSimulator.ViewModels;
using AutomataSimulator.Core.Models.Automata;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.WPF.Models;
using QuikGraph;

namespace AutomataSimulator.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainViewModel();
        DataContext = vm;

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentAutomaton))
            {
                Dispatcher.Invoke(() => BuildVisualGraph(vm.CurrentAutomaton));
            }
        };
    }

    private void BuildVisualGraph(object? automaton)
    {
        if (automaton == null) return;

        // Используем object и IEdge<object> для 100% совместимости
        var visualGraph = new BidirectionalGraph<object, IEdge<object>>();

        if (automaton is FiniteAutomaton fa)
        {
            var vertexMap = fa.States.ToDictionary(
                s => s.Id,
                s => new VisualVertex { Name = s.Name, IsStart = s.IsStart, IsFinal = s.IsFinal }
            );

            foreach (var v in vertexMap.Values) visualGraph.AddVertex(v);

            foreach (var t in fa.Transitions.Cast<FiniteTransition>())
            {
                var edge = new VisualEdge(vertexMap[t.FromStateId], vertexMap[t.ToStateId], t.Symbol?.ToString() ?? "ε");
                visualGraph.AddEdge(edge);
            }
        }
        else if (automaton is PushdownAutomaton pda)
        {
            var vertexMap = pda.States.ToDictionary(
                s => s.Id,
                s => new VisualVertex { Name = s.Name, IsStart = s.IsStart, IsFinal = s.IsFinal }
            );

            foreach (var v in vertexMap.Values) visualGraph.AddVertex(v);

            foreach (var t in pda.Transitions.Cast<PushdownTransition>())
            {
                var label = $"{t.InputSymbol ?? 'ε'}, {t.PopSymbol ?? 'ε'} → {t.PushSymbols ?? "ε"}";
                visualGraph.AddEdge(new VisualEdge(vertexMap[t.FromStateId], vertexMap[t.ToStateId], label));
            }
        }

        GraphLayout.Graph = visualGraph;
    }
}