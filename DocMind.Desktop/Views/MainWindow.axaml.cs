using Avalonia.Controls;
using DocMind.Desktop.ViewModels;

namespace DocMind.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

     
}
    private void ListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.OpenFromHistoryCommand.CanExecute(null))
        {
            vm.OpenFromHistoryCommand.Execute(null);
        }
    }
}