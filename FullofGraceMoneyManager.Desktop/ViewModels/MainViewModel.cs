using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullofGraceMoneyManager.Desktop.Data;
using FullofGraceMoneyManager.Desktop.Models;

namespace FullofGraceMoneyManager.Desktop.ViewModels;

public partial class NavigationMenuItem : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _iconGlyph = string.Empty;

    [ObservableProperty]
    private string? _badgeText;

    [ObservableProperty]
    private bool _isSelected;
}

public partial class MainViewModel : ObservableObject
{
    private readonly MoneyRepository _repository;
    private bool _suppressSelectionCallback;

    public event Action? StateChanged;
    public event Action<string>? ModalRequested;
    public event Action<string>? SearchFilterChanged;
    public event Action<string>? BrowserNavigationRequested;
    public event Action<string>? BrowserControlRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedNetWorth))]
    private decimal _netWorth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotalAssets))]
    private decimal _totalAssets;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTotalLiabilities))]
    private decimal _totalLiabilities;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedLiquidCash))]
    private decimal _liquidCash;

    [ObservableProperty]
    private string _activeViewKey = "Dashboard";

    [ObservableProperty]
    private NavigationMenuItem? _selectedNavigationItem;

    [ObservableProperty]
    private Account? _selectedAccount;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isBrowserBarVisible;

    [ObservableProperty]
    private string _browserUrl = "https://app.local/index.html";

    // Bottom Status Bar Properties
    [ObservableProperty]
    private string _statusContextTitle = "All Accounts (Dashboard)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedClearedBalance))]
    private decimal _clearedBalance;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedPendingTotal))]
    private decimal _pendingTotal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedEndingBalance))]
    private decimal _endingBalance;

    [ObservableProperty]
    private string _databaseStatusText = $"{DatabaseInitializer.DatabaseFileName} • Connected";

    [ObservableProperty]
    private string _databaseFullPath = string.Empty;

    public ObservableCollection<NavigationMenuItem> NavigationItems { get; } = new()
    {
        new NavigationMenuItem { Key = "Dashboard", Title = "Dashboard", IconGlyph = "📊", IsSelected = true },
        new NavigationMenuItem { Key = "AllTransactions", Title = "All Transactions", IconGlyph = "📒" },
        new NavigationMenuItem { Key = "Rules", Title = "Rules Engine", IconGlyph = "⚡" },
        new NavigationMenuItem { Key = "Budgets", Title = "Budgets", IconGlyph = "🎯" },
        new NavigationMenuItem { Key = "CashFlow", Title = "Bills & Cash Flow", IconGlyph = "📅" },
        new NavigationMenuItem { Key = "SavingsGoals", Title = "Savings Goals", IconGlyph = "🏛️" },
        new NavigationMenuItem { Key = "Investments", Title = "Investments", IconGlyph = "📈" },
        new NavigationMenuItem { Key = "DebtOptimizer", Title = "Debt & Mortgage", IconGlyph = "🏦" },
        new NavigationMenuItem { Key = "Reports", Title = "Reports", IconGlyph = "📑" },
        new NavigationMenuItem { Key = "WebPortal", Title = "Web Portal", IconGlyph = "🌐" }
    };

    public ObservableCollection<AccountGroupNode> AccountGroups { get; } = new();

    public string FormattedNetWorth => NetWorth < 0 ? $"-${Math.Abs(NetWorth):N2}" : $"${NetWorth:N2}";
    public string FormattedTotalAssets => $"${TotalAssets:N2}";
    public string FormattedTotalLiabilities => $"${TotalLiabilities:N2}";
    public string FormattedLiquidCash => $"${LiquidCash:N2}";
    public string FormattedClearedBalance => ClearedBalance < 0 ? $"-${Math.Abs(ClearedBalance):N2}" : $"${ClearedBalance:N2}";
    public string FormattedPendingTotal => PendingTotal < 0 ? $"-${Math.Abs(PendingTotal):N2}" : $"${PendingTotal:N2}";
    public string FormattedEndingBalance => EndingBalance < 0 ? $"-${Math.Abs(EndingBalance):N2}" : $"${EndingBalance:N2}";

    public MainViewModel(MoneyRepository repository)
    {
        _repository = repository;
        _selectedNavigationItem = NavigationItems[0];
        DatabaseFullPath = DatabaseInitializer.GetDatabasePath();
        DatabaseStatusText = $"{Path.GetFileName(DatabaseFullPath)} • Ready (SQLite)";

        ReloadData(notifyWebView: false);
    }

    partial void OnSelectedNavigationItemChanged(NavigationMenuItem? value)
    {
        if (_suppressSelectionCallback || value == null)
        {
            return;
        }

        _suppressSelectionCallback = true;
        foreach (var item in NavigationItems)
        {
            item.IsSelected = ReferenceEquals(item, value);
        }

        SelectedAccount = null;
        ActiveViewKey = value.Key;

        if (value.Key == "WebPortal")
        {
            IsBrowserBarVisible = true;
        }

        UpdateStatusBarSummary();
        _suppressSelectionCallback = false;

        EnsureAppWorkspaceLoaded();
        StateChanged?.Invoke();
    }

    partial void OnSelectedAccountChanged(Account? value)
    {
        if (_suppressSelectionCallback || value == null)
        {
            return;
        }

        _suppressSelectionCallback = true;
        foreach (var item in NavigationItems)
        {
            item.IsSelected = false;
        }
        SelectedNavigationItem = null;
        ActiveViewKey = "Register";

        UpdateStatusBarSummary();
        _suppressSelectionCallback = false;

        EnsureAppWorkspaceLoaded();
        StateChanged?.Invoke();
    }

    partial void OnSearchQueryChanged(string value)
    {
        SearchFilterChanged?.Invoke(value);
    }

    public void SelectAccountById(int accountId)
    {
        var allAccounts = AccountGroups.SelectMany(g => g.Accounts).ToList();
        var match = allAccounts.FirstOrDefault(a => a.Id == accountId);
        if (match != null)
        {
            SelectedAccount = match;
        }
    }

    public void NavigateByViewKey(string viewKey)
    {
        var navItem = NavigationItems.FirstOrDefault(n =>
            string.Equals(n.Key, viewKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(n.Title, viewKey, StringComparison.OrdinalIgnoreCase));

        if (navItem != null)
        {
            SelectedNavigationItem = navItem;
        }
    }

    public void ReloadData(bool notifyWebView = true)
    {
        int? previousSelectedAccountId = SelectedAccount?.Id;

        var metrics = _repository.GetDashboardMetrics();
        NetWorth = metrics.NetWorth;
        TotalAssets = metrics.TotalAssets;
        TotalLiabilities = metrics.TotalLiabilities;
        LiquidCash = metrics.LiquidCash;

        var groups = _repository.GetAccountGroups();
        AccountGroups.Clear();
        foreach (var group in groups)
        {
            AccountGroups.Add(group);
        }

        if (previousSelectedAccountId.HasValue)
        {
            _suppressSelectionCallback = true;
            SelectedAccount = AccountGroups
                .SelectMany(g => g.Accounts)
                .FirstOrDefault(a => a.Id == previousSelectedAccountId.Value);
            _suppressSelectionCallback = false;
        }

        UpdateStatusBarSummary();

        if (notifyWebView)
        {
            StateChanged?.Invoke();
        }
    }

    private void UpdateStatusBarSummary()
    {
        if (SelectedAccount != null)
        {
            StatusContextTitle = $"{SelectedAccount.Name} ({SelectedAccount.AccountNumberMask})";
            ClearedBalance = SelectedAccount.ClearedBalance;
            PendingTotal = SelectedAccount.PendingTotal;
            EndingBalance = SelectedAccount.CurrentBalance;
        }
        else
        {
            var allAccounts = AccountGroups.SelectMany(g => g.Accounts).ToList();
            string navTitle = SelectedNavigationItem?.Title ?? "All Accounts";
            StatusContextTitle = $"All Accounts • {navTitle}";
            ClearedBalance = allAccounts.Sum(a => a.ClearedBalance);
            PendingTotal = allAccounts.Sum(a => a.PendingTotal);
            EndingBalance = allAccounts.Sum(a => a.CurrentBalance);
        }
    }

    private void EnsureAppWorkspaceLoaded()
    {
        if (!BrowserUrl.StartsWith("https://app.local", StringComparison.OrdinalIgnoreCase))
        {
            BrowserUrl = "https://app.local/index.html";
            BrowserNavigationRequested?.Invoke(BrowserUrl);
        }
    }

    [RelayCommand]
    private void NewTransaction()
    {
        EnsureAppWorkspaceLoaded();
        ModalRequested?.Invoke("newTransaction");
    }

    [RelayCommand]
    private void ImportStatement()
    {
        EnsureAppWorkspaceLoaded();
        ModalRequested?.Invoke("importStatement");
    }

    [RelayCommand]
    private void Reconcile()
    {
        EnsureAppWorkspaceLoaded();
        ModalRequested?.Invoke("reconcile");
    }

    [RelayCommand]
    private void ToggleBrowserMode()
    {
        IsBrowserBarVisible = !IsBrowserBarVisible;
    }

    [RelayCommand]
    private void Refresh()
    {
        ReloadData(notifyWebView: true);
    }

    [RelayCommand]
    private void NavigateBrowserUrl()
    {
        if (string.IsNullOrWhiteSpace(BrowserUrl))
        {
            return;
        }

        string target = BrowserUrl.Trim();
        if (!target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            target = "https://" + target;
            BrowserUrl = target;
        }

        BrowserNavigationRequested?.Invoke(target);
    }

    [RelayCommand]
    private void OpenPortalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        IsBrowserBarVisible = true;
        BrowserUrl = url;
        BrowserNavigationRequested?.Invoke(url);
    }

    [RelayCommand]
    private void ReturnToAppWorkspace()
    {
        BrowserUrl = "https://app.local/index.html";
        BrowserNavigationRequested?.Invoke(BrowserUrl);
    }

    [RelayCommand]
    private void BrowserBack() => BrowserControlRequested?.Invoke("back");

    [RelayCommand]
    private void BrowserForward() => BrowserControlRequested?.Invoke("forward");

    [RelayCommand]
    private void BrowserReload() => BrowserControlRequested?.Invoke("reload");
}
