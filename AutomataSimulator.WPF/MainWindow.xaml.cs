using System.Collections.Generic;
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
    private MainViewModel _vm;
    // Словарь для быстрого поиска визуального узла по имени состояния (чтобы его подсвечивать)
    private Dictionary<string, VisualVertex> _visualNodes = new();

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        // 1. Слушаем создание нового автомата
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentAutomaton))
            {
                Dispatcher.Invoke(() => BuildVisualGraph(_vm.CurrentAutomaton));
            }
        };

        // 2. Слушаем шаги симуляции (чтобы двигать подсветку)
        // Предполагаем, что у Engine или Simulation есть способ узнать текущие активные состояния.
        // Пока привяжемся к кликам по кнопке (временно, чтобы проверить работоспособность).
    }

    private void BuildVisualGraph(object? automaton)
    {
        if (automaton == null) return;

        var visualGraph = new BidirectionalGraph<object, IEdge<object>>();
        _visualNodes.Clear(); // Очищаем старые узлы

        if (automaton is FiniteAutomaton fa)
        {
            var vertexMap = fa.States.ToDictionary(
                s => s.Id,
                s => new VisualVertex { Name = s.Name, IsStart = s.IsStart, IsFinal = s.IsFinal }
            );

            foreach (var v in vertexMap.Values)
            {
                visualGraph.AddVertex(v);
                _visualNodes[v.Name] = v; // Сохраняем для анимации
            }

            foreach (var t in fa.Transitions.Cast<FiniteTransition>())
            {
                visualGraph.AddEdge(new VisualEdge(vertexMap[t.FromStateId], vertexMap[t.ToStateId], t.Symbol?.ToString() ?? "ε"));
            }
        }
        else if (automaton is PushdownAutomaton pda)
        {
            var vertexMap = pda.States.ToDictionary(
                s => s.Id,
                s => new VisualVertex { Name = s.Name, IsStart = s.IsStart, IsFinal = s.IsFinal }
            );

            foreach (var v in vertexMap.Values)
            {
                visualGraph.AddVertex(v);
                _visualNodes[v.Name] = v;
            }

            foreach (var t in pda.Transitions.Cast<PushdownTransition>())
            {
                var label = $"{t.InputSymbol ?? 'ε'}, {t.PopSymbol ?? 'ε'} → {t.PushSymbols ?? "ε"}";
                visualGraph.AddEdge(new VisualEdge(vertexMap[t.FromStateId], vertexMap[t.ToStateId], label));
            }
        }

        // Загружаем граф в интерфейс
        GraphLayout.Graph = visualGraph;

        // ИСПРАВЛЕНИЕ СЛИПАНИЯ УЗЛОВ:
        // Заставляем WPF обновить размеры элементов, а затем запускаем алгоритм расталкивания
        GraphLayout.UpdateLayout();
        GraphLayout.Relayout();

        // Подсвечиваем стартовое состояние по умолчанию
        ResetHighlighting();
    }

    // Метод для очистки всех подсветок и установки старта
    private void ResetHighlighting()
    {
        foreach (var node in _visualNodes.Values)
        {
            node.IsActive = node.IsStart;
        }
    }
}