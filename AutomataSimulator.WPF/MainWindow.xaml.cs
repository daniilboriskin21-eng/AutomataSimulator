using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AutomataSimulator.ViewModels;
using AutomataSimulator.Core.Models.Automata;
using AutomataSimulator.Core.Models.Transitions;
using AutomataSimulator.WPF.Models;
using QuikGraph;

namespace AutomataSimulator.WPF;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;
    private Dictionary<Guid, VisualVertex> _vertexMap = new();

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentAutomaton))
            {
                Dispatcher.Invoke(() => BuildVisualGraph(_vm.CurrentAutomaton));
            }
        };

        _vm.Simulation.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == "ExecutionUpdated")
            {
                Dispatcher.Invoke(UpdateVisualHighlighting);
            }
        };
    }

    private void BuildVisualGraph(object? automaton)
    {
        if (automaton == null) return;

        var visualGraph = new BidirectionalGraph<object, IEdge<object>>();
        _vertexMap.Clear();

        var addedDirectedPairs = new HashSet<(Guid From, Guid To)>();

        if (automaton is FiniteAutomaton fa)
        {
            foreach (var s in fa.States)
            {
                var v = new VisualVertex { Id = s.Id, Name = s.Name, IsStart = s.IsStart, IsFinal = s.IsFinal };
                _vertexMap[s.Id] = v;
                visualGraph.AddVertex(v);
            }

            var groupedTransitions = fa.Transitions
                .Cast<FiniteTransition>()
                .GroupBy(t => new { t.FromStateId, t.ToStateId });

            foreach (var group in groupedTransitions)
            {
                var fromId = group.Key.FromStateId;
                var toId = group.Key.ToStateId;
                var label = string.Join(", ", group.Select(t => t.Symbol?.ToString() ?? "ε"));

                // Проверяем: это ПЕТЛЯ или ВСТРЕЧНАЯ СТРЕЛКА?
                if (fromId == toId || addedDirectedPairs.Contains((toId, fromId)))
                {
                    // Имя фиктивной вершины - это сам текст перехода!
                    var dummy = new VisualVertex { Id = Guid.NewGuid(), Name = label, IsDummy = true };
                    visualGraph.AddVertex(dummy);

                    // Соединяем A -> Плашка -> B (или A -> Плашка -> A для петель)
                    visualGraph.AddEdge(new VisualEdge(_vertexMap[fromId], dummy, label));
                    visualGraph.AddEdge(new VisualEdge(dummy, _vertexMap[toId], label));
                }
                else
                {
                    // Обычная прямая стрелка
                    visualGraph.AddEdge(new VisualEdge(_vertexMap[fromId], _vertexMap[toId], label));
                    addedDirectedPairs.Add((fromId, toId));
                }
            }
        }
        // Аналогичный код для PushdownAutomaton (скопируйте логику проверки fromId == toId)
        else if (automaton is PushdownAutomaton pda)
        {
            foreach (var s in pda.States)
            {
                var v = new VisualVertex { Id = s.Id, Name = s.Name, IsStart = s.IsStart, IsFinal = s.IsFinal };
                _vertexMap[s.Id] = v;
                visualGraph.AddVertex(v);
            }

            var groupedTransitions = pda.Transitions
                .Cast<PushdownTransition>()
                .GroupBy(t => new { t.FromStateId, t.ToStateId });

            foreach (var group in groupedTransitions)
            {
                var fromId = group.Key.FromStateId;
                var toId = group.Key.ToStateId;
                var label = string.Join("\n", group.Select(t => $"{t.InputSymbol ?? 'ε'}, {t.PopSymbol ?? 'ε'} → {t.PushSymbols ?? "ε"}"));

                if (fromId == toId || addedDirectedPairs.Contains((toId, fromId)))
                {
                    var dummy = new VisualVertex { Id = Guid.NewGuid(), Name = label, IsDummy = true };
                    visualGraph.AddVertex(dummy);
                    visualGraph.AddEdge(new VisualEdge(_vertexMap[fromId], dummy, label));
                    visualGraph.AddEdge(new VisualEdge(dummy, _vertexMap[toId], label));
                }
                else
                {
                    visualGraph.AddEdge(new VisualEdge(_vertexMap[fromId], _vertexMap[toId], label));
                    addedDirectedPairs.Add((fromId, toId));
                }
            }
        }

        GraphLayout.Graph = visualGraph;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            GraphLayout.UpdateLayout();
            GraphLayout.Relayout();
        }), DispatcherPriority.Background);

        UpdateVisualHighlighting();
    }
    private void UpdateVisualHighlighting()
    {
        if (_vm?.Simulation == null) return;

        var activeIds = _vm.Simulation.GetActiveStateIds()?.ToList() ?? new List<Guid>();

        //if (activeIds.Count == 0)
        //{
        //    foreach (var pair in _vertexMap)
        //    {
        //        pair.Value.IsActive = pair.Value.IsStart;
        //    }
        //    return;
        //}

        foreach (var pair in _vertexMap)
        {
            pair.Value.IsActive = activeIds.Contains(pair.Key);
        }
    }

    private void StepForward_Click(object sender, RoutedEventArgs e)
    {
        if (_vm?.Simulation?.StepForwardCommand.CanExecute(null) == true)
        {
            _vm.Simulation.StepForwardCommand.Execute(null);
            UpdateVisualHighlighting();
        }
    }

    // --- НОВЫЙ ОБРАБОТЧИК ---
    private void StepBackward_Click(object sender, RoutedEventArgs e)
    {
        if (_vm?.Simulation?.StepBackwardCommand.CanExecute(null) == true)
        {
            _vm.Simulation.StepBackwardCommand.Execute(null);
            UpdateVisualHighlighting();
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (_vm?.Simulation?.ResetCommand.CanExecute(null) == true)
        {
            _vm.Simulation.ResetCommand.Execute(null);
            UpdateVisualHighlighting();
        }
    }

    private void Vertex_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is VisualVertex vertex)
        {
            vertex.IsBreakpoint = !vertex.IsBreakpoint;

            // Передаем команду во ViewModel или Engine
            _vm?.Simulation.ToggleBreakpointCommand?.Execute(vertex.Id);
        }
    }
}