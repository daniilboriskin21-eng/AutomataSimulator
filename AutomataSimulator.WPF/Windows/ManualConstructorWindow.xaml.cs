using System.Windows;
using AutomataSimulator.ViewModels;

namespace AutomataSimulator.WPF.Windows;

public partial class ManualConstructorWindow : Window
{
    public ManualConstructorViewModel ViewModel { get; }

    public ManualConstructorWindow()
    {
        InitializeComponent();
        ViewModel = new ManualConstructorViewModel();
        DataContext = ViewModel;
    }

    private void Build_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TryBuild())
        {
            DialogResult = true; // Закрываем с успехом
        }
    }
}