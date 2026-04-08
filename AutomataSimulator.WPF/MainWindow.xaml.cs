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

        // Следим за созданием нового автомата
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentAutomaton))
            {
                Dispatcher.Invoke(() => BuildVisualGraph(_vm.CurrentAutomaton));
            }
        };

        // Следим за шагами симуляции для обновления подсветки
        _vm.Simulation.PropertyChanged += (s, e) =>
        {
            Dispatcher.Invoke(UpdateVisualHighlighting);
        };
    }

    private void BuildVisualGraph(object? automaton)
    {
        if (automaton == null) return;

        var visualGraph = new BidirectionalGraph<object, IEdge<object>>();
        _vertexMap.Clear();

        // 1. Создаем вершины
        if (automaton is FiniteAutomaton fa)
        {
            foreach (var s in fa.States)
            {
                var v = new VisualVertex { Name = s.Name, IsStart = s.IsStart, IsFinal = s.IsFinal };
                _vertexMap[s.Id] = v;
                visualGraph.AddVertex(v);
            }

            foreach (var t in fa.Transitions.Cast<FiniteTransition>())
            {
                var label = t.Symbol?.ToString() ?? "ε";
                visualGraph.AddEdge(new VisualEdge(_vertexMap[t.FromStateId], _vertexMap[t.ToStateId], label));
            }
        }
        else if (automaton is PushdownAutomaton pda)
        {
            foreach (var s in pda.States)
            {
                var v = new VisualVertex { Name = s.Name, IsStart = s.IsStart, IsFinal = s.IsFinal };
                _vertexMap[s.Id] = v;
                visualGraph.AddVertex(v);
            }

            foreach (var t in pda.Transitions.Cast<PushdownTransition>())
            {
                var label = $"{t.InputSymbol ?? 'ε'}, {t.PopSymbol ?? 'ε'} → {t.PushSymbols ?? "ε"}";
                visualGraph.AddEdge(new VisualEdge(_vertexMap[t.FromStateId], _vertexMap[t.ToStateId], label));
            }
        }

        GraphLayout.Graph = visualGraph;

        // ПРИНУДИТЕЛЬНЫЙ RELAYOUT: расталкиваем узлы
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

        // ХИТРОСТЬ: Если движок не вернул активных состояний (симуляция не работает/не начата),
        // давай по умолчанию подсветим стартовые состояния, чтобы граф не казался "мертвым".
        if (activeIds.Count == 0)
        {
            foreach (var pair in _vertexMap)
            {
                pair.Value.IsActive = pair.Value.IsStart;
            }
            return;
        }

        // Если активные состояния есть - подсвечиваем строго их
        foreach (var pair in _vertexMap)
        {
            pair.Value.IsActive = activeIds.Contains(pair.Key);
        }
    }

    private void StepForward_Click(object sender, RoutedEventArgs e)
    {
        // Проверяем, разрешает ли ViewModel сделать шаг
        if (_vm?.Simulation?.StepForwardCommand.CanExecute(null) == true)
        {
            _vm.Simulation.StepForwardCommand.Execute(null);
            UpdateVisualHighlighting();
        }
        else
        {
            MessageBox.Show("Команда ШАГ заблокирована! Проверь логику в SimulationViewModel (возможно автомат еще не инициализирован или строка закончилась).", "Отладка");
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
}