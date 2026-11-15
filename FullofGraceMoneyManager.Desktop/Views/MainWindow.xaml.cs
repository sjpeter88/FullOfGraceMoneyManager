using System.Windows;
using System.Windows.Input;
using FullofGraceMoneyManager.Desktop.Data;
using FullofGraceMoneyManager.Desktop.Models;
using FullofGraceMoneyManager.Desktop.Services;
using FullofGraceMoneyManager.Desktop.ViewModels;

namespace FullofGraceMoneyManager.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly WebViewBridgeService _bridgeService;

    public MainWindow()
    {
        InitializeComponent();

        var repository = new MoneyRepository();
        _viewModel = new MainViewModel(repository);
        DataContext = _viewModel;

        _bridgeService = new WebViewBridgeService(WorkspaceWebView, _viewModel, repository);
        Loaded += MainWindow_OnLoaded;
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        await _bridgeService.InitializeAsync();
    }

    private void AccountsTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is Account selectedAccount)
        {
            _viewModel.SelectedAccount = selectedAccount;
        }
    }

    private void BrowserUrlTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _viewModel.NavigateBrowserUrlCommand.Execute(null);
            e.Handled = true;
        }
    }
}
