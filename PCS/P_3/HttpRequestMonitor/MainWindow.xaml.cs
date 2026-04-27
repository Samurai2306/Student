using System.Windows;
using System.ComponentModel;
using HttpRequestMonitor.ViewModels;

namespace HttpRequestMonitor;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            _isClosing = true;
            await _viewModel.ShutdownAsync();
            Close();
            return;
        }

        _viewModel.Dispose();
        base.OnClosing(e);
    }
}