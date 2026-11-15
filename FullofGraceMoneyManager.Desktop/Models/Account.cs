using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FullofGraceMoneyManager.Desktop.Models;

public partial class Account : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _accountNumberMask = string.Empty;

    [ObservableProperty]
    private string _accountType = "Checking"; // Checking, Savings, CreditCard, Investment, Loan, Asset

    [ObservableProperty]
    private string _accountGroup = "Banking"; // Banking, Credit Cards, Investments, Assets/Loans

    [ObservableProperty]
    private string _institution = string.Empty;

    [ObservableProperty]
    private decimal _openingBalance;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedBalance))]
    [NotifyPropertyChangedFor(nameof(IsNegativeBalance))]
    private decimal _currentBalance;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedClearedBalance))]
    private decimal _clearedBalance;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedPendingTotal))]
    private decimal _pendingTotal;

    [ObservableProperty]
    private string _currency = "USD";

    [ObservableProperty]
    private bool _isLiability;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private string? _portalUrl;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private string _createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

    public string FormattedBalance => CurrentBalance < 0
        ? $"-${Math.Abs(CurrentBalance):N2}"
        : $"${CurrentBalance:N2}";

    public string FormattedClearedBalance => ClearedBalance < 0
        ? $"-${Math.Abs(ClearedBalance):N2}"
        : $"${ClearedBalance:N2}";

    public string FormattedPendingTotal => PendingTotal < 0
        ? $"-${Math.Abs(PendingTotal):N2}"
        : $"${PendingTotal:N2}";

    public bool IsNegativeBalance => CurrentBalance < 0;
}

public partial class AccountGroupNode : ObservableObject
{
    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private string _iconGlyph = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedGroupTotal))]
    private decimal _groupTotal;

    [ObservableProperty]
    private bool _isExpanded = true;

    public ObservableCollection<Account> Accounts { get; set; } = new();

    public string FormattedGroupTotal => GroupTotal < 0
        ? $"-${Math.Abs(GroupTotal):N2}"
        : $"${GroupTotal:N2}";
}
