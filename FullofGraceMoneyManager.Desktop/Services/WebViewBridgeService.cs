using System.IO;
using System.Text.Json;
using FullofGraceMoneyManager.Desktop.Data;
using FullofGraceMoneyManager.Desktop.Models;
using FullofGraceMoneyManager.Desktop.ViewModels;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace FullofGraceMoneyManager.Desktop.Services;

public class WebViewBridgeService
{
    public const string VirtualHostName = "app.local";
    public const string DefaultAppUrl = "https://app.local/index.html";

    private readonly WebView2 _webView;
    private readonly MainViewModel _viewModel;
    private readonly MoneyRepository _repository;
    private readonly StatementParserService _parserService = new();
    private readonly RulesAndDeduplicationService _rulesAndDedupService = new();
    private bool _isReady;
    private string _debtStrategyMode = "Avalanche";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public WebViewBridgeService(WebView2 webView, MainViewModel viewModel, MoneyRepository repository)
    {
        _webView = webView;
        _viewModel = viewModel;
        _repository = repository;

        _viewModel.StateChanged += PushStateToWebView;
        _viewModel.ModalRequested += PushOpenModalToWebView;
        _viewModel.SearchFilterChanged += PushSearchFilterToWebView;
        _viewModel.BrowserNavigationRequested += NavigateToUrl;
        _viewModel.BrowserControlRequested += HandleBrowserControl;
    }

    public async Task InitializeAsync()
    {
        string userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FullofGraceMoneyManager",
            "WebView2Data");

        Directory.CreateDirectory(userDataFolder);

        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await _webView.EnsureCoreWebView2Async(env);

        string wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!Directory.Exists(wwwrootPath))
        {
            Directory.CreateDirectory(wwwrootPath);
        }

        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            VirtualHostName,
            wwwrootPath,
            CoreWebView2HostResourceAccessKind.Allow);

        _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        _webView.CoreWebView2.SourceChanged += (_, _) =>
        {
            string currentUri = _webView.CoreWebView2.Source;
            if (!string.IsNullOrWhiteSpace(currentUri))
            {
                _viewModel.BrowserUrl = currentUri;
            }
        };

        _webView.CoreWebView2.Navigate(DefaultAppUrl);
    }

    public void PushStateToWebView()
    {
        if (!_isReady || _webView.CoreWebView2 == null)
        {
            return;
        }

        int? selectedAccountId = _viewModel.SelectedAccount?.Id;
        var accounts = _repository.GetAccountsWithBalances();
        var categories = _repository.GetCategories();
        var payees = _repository.GetPayees();
        var rules = _repository.GetRules();
        var transactions = _repository.GetTransactions(selectedAccountId, _viewModel.SearchQuery);
        var metrics = _repository.GetDashboardMetrics();
        var budgetSummary = _repository.GetMonthlyBudgetSummary("2026-11");
        var portfolioSummary = _repository.GetInvestmentPortfolioSummary();
        var recurringSummary = _repository.GetRecurringCashFlowSummary();
        var reportSummary = _repository.GetExecutiveReportSummary();
        var debtSummary = _repository.GetDebtPayoffSummary(_debtStrategyMode);
        var savingsGoalsSummary = _repository.GetSavingsGoalsSummary();

        var payload = new
        {
            type = "stateSync",
            activeView = _viewModel.ActiveViewKey,
            selectedAccountId,
            selectedAccount = _viewModel.SelectedAccount,
            searchQuery = _viewModel.SearchQuery,
            metrics,
            budgetSummary,
            portfolioSummary,
            recurringSummary,
            reportSummary,
            debtSummary,
            savingsGoalsSummary,
            accounts,
            categories,
            payees,
            rules,
            transactions
        };

        string json = JsonSerializer.Serialize(payload, JsonOptions);
        _webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    public void PushOpenModalToWebView(string modalName)
    {
        if (!_isReady || _webView.CoreWebView2 == null)
        {
            return;
        }

        string json = JsonSerializer.Serialize(new
        {
            type = "openModal",
            modal = modalName,
            selectedAccountId = _viewModel.SelectedAccount?.Id ?? 1
        }, JsonOptions);

        _webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    public void PushSearchFilterToWebView(string query)
    {
        if (!_isReady || _webView.CoreWebView2 == null)
        {
            return;
        }

        PushStateToWebView();
    }

    public void PushStatementPreview(StatementImportPreviewResult preview)
    {
        if (!_isReady || _webView.CoreWebView2 == null)
        {
            return;
        }

        string json = JsonSerializer.Serialize(new
        {
            type = "statementPreview",
            preview
        }, JsonOptions);

        _webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    public void PushToast(string message)
    {
        if (!_isReady || _webView.CoreWebView2 == null)
        {
            return;
        }

        string json = JsonSerializer.Serialize(new
        {
            type = "toast",
            message
        }, JsonOptions);

        _webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    private void NavigateToUrl(string url)
    {
        if (_webView.CoreWebView2 == null)
        {
            return;
        }

        _webView.CoreWebView2.Navigate(url);
    }

    private void HandleBrowserControl(string action)
    {
        if (_webView.CoreWebView2 == null)
        {
            return;
        }

        switch (action)
        {
            case "back":
                if (_webView.CoreWebView2.CanGoBack)
                {
                    _webView.CoreWebView2.GoBack();
                }
                break;
            case "forward":
                if (_webView.CoreWebView2.CanGoForward)
                {
                    _webView.CoreWebView2.GoForward();
                }
                break;
            case "reload":
                _webView.CoreWebView2.Reload();
                break;
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp))
            {
                return;
            }

            string? messageType = typeProp.GetString();
            switch (messageType)
            {
                case "ready":
                    _isReady = true;
                    PushStateToWebView();
                    break;

                case "selectAccount":
                    if (root.TryGetProperty("accountId", out var accProp) && accProp.TryGetInt32(out int accId))
                    {
                        _viewModel.SelectAccountById(accId);
                    }
                    break;

                case "navigate":
                    if (root.TryGetProperty("view", out var viewProp))
                    {
                        string? viewKey = viewProp.GetString();
                        if (!string.IsNullOrWhiteSpace(viewKey))
                        {
                            _viewModel.NavigateByViewKey(viewKey);
                        }
                    }
                    break;

                case "cycleStatus":
                    if (root.TryGetProperty("transactionId", out var txIdProp) && txIdProp.TryGetInt32(out int txId))
                    {
                        string newStatus = _repository.CycleTransactionStatus(txId);
                        _viewModel.ReloadData(notifyWebView: true);
                        PushToast($"Transaction #{txId} marked as {newStatus}");
                    }
                    break;

                case "createTransaction":
                    HandleCreateTransaction(root);
                    break;

                case "updateTransaction":
                    HandleUpdateTransaction(root);
                    break;

                case "deleteTransaction":
                    if (root.TryGetProperty("transactionId", out var delTxEl) && delTxEl.TryGetInt32(out int delTxId))
                    {
                        _repository.DeleteTransaction(delTxId);
                        _viewModel.ReloadData(notifyWebView: true);
                        PushToast($"Deleted transaction #{delTxId} from SQLite.");
                    }
                    break;

                case "completeReconciliation":
                    HandleCompleteReconciliation(root);
                    break;

                case "reconcileAccount":
                    int? targetAccountId = _viewModel.SelectedAccount?.Id;
                    if (root.TryGetProperty("accountId", out var recAccProp) &&
                        recAccProp.ValueKind == JsonValueKind.Number &&
                        recAccProp.TryGetInt32(out int parsedRecId))
                    {
                        targetAccountId = parsedRecId;
                    }

                    int reconciledCount = _repository.ReconcileClearedTransactions(targetAccountId);
                    _viewModel.ReloadData(notifyWebView: true);
                    PushToast($"Reconciled {reconciledCount} cleared transaction(s).");
                    break;

                case "parseStatement":
                    HandleParseStatement(root);
                    break;

                case "browseStatementFile":
                    HandleBrowseStatementFile(root);
                    break;

                case "commitStatementImport":
                    HandleCommitStatementImport(root);
                    break;

                case "createRule":
                    HandleCreateRule(root);
                    break;

                case "toggleRule":
                    if (root.TryGetProperty("ruleId", out var toggleIdEl) && toggleIdEl.TryGetInt32(out int toggleRuleId))
                    {
                        bool enabled = _repository.ToggleRuleEnabled(toggleRuleId);
                        _viewModel.ReloadData(notifyWebView: true);
                        PushToast($"Rule #{toggleRuleId} {(enabled ? "enabled" : "disabled")}");
                    }
                    break;

                case "deleteRule":
                    if (root.TryGetProperty("ruleId", out var delIdEl) && delIdEl.TryGetInt32(out int deleteRuleId))
                    {
                        _repository.DeleteRule(deleteRuleId);
                        _viewModel.ReloadData(notifyWebView: true);
                        PushToast($"Deleted rule #{deleteRuleId}");
                    }
                    break;

                case "applyRulesToRegister":
                    int? scopeAccId = _viewModel.SelectedAccount?.Id;
                    int updated = _repository.ApplyRulesToExistingTransactions(scopeAccId);
                    _viewModel.ReloadData(notifyWebView: true);
                    PushToast($"Rules Engine normalized & categorized {updated} register transaction(s).");
                    break;

                case "updateBudgetEnvelope":
                    HandleUpdateBudgetEnvelope(root);
                    break;

                case "autoFundBudget":
                    string autoMonth = root.TryGetProperty("monthKey", out var amEl) && amEl.ValueKind == JsonValueKind.String
                        ? (amEl.GetString() ?? "2026-11")
                        : "2026-11";
                    int fundedCount = _repository.AutoFundUnderfundedEnvelopes(autoMonth);
                    _viewModel.ReloadData(notifyWebView: true);
                    PushToast($"Auto-funded {fundedCount} underfunded envelope(s) to their monthly targets.");
                    break;

                case "moveEnvelopeFunds":
                    HandleMoveEnvelopeFunds(root);
                    break;

                case "updateHolding":
                    HandleUpdateHolding(root);
                    break;

                case "createHolding":
                    HandleCreateHolding(root);
                    break;

                case "deleteHolding":
                    if (root.TryGetProperty("holdingId", out var delHoldEl) && delHoldEl.TryGetInt32(out int delHoldId))
                    {
                        _repository.DeleteInvestmentHolding(delHoldId);
                        _viewModel.ReloadData(notifyWebView: true);
                        PushToast($"Deleted investment position #{delHoldId} and synced account balance.");
                    }
                    break;

                case "simulateMarketPulse":
                    _repository.SimulateMarketPulse();
                    _viewModel.ReloadData(notifyWebView: true);
                    PushToast("Simulated market quote update across all investment holdings.");
                    break;

                case "postRecurringSchedule":
                    if (root.TryGetProperty("scheduleId", out var postSchedEl) && postSchedEl.TryGetInt32(out int postSchedId))
                    {
                        var postedTx = _repository.PostRecurringScheduleToRegister(postSchedId);
                        _viewModel.ReloadData(notifyWebView: true);
                        if (postedTx != null)
                        {
                            PushToast($"Posted '{postedTx.PayeeName}' ({postedTx.Amount:C2}) to register & advanced due date.");
                        }
                    }
                    break;

                case "skipRecurringSchedule":
                    if (root.TryGetProperty("scheduleId", out var skipSchedEl) && skipSchedEl.TryGetInt32(out int skipSchedId))
                    {
                        string nextDue = _repository.SkipRecurringScheduleCycle(skipSchedId);
                        _viewModel.ReloadData(notifyWebView: true);
                        PushToast($"Advanced schedule #{skipSchedId} to next cycle ({nextDue}).");
                    }
                    break;

                case "createRecurringSchedule":
                    HandleCreateRecurringSchedule(root);
                    break;

                case "deleteRecurringSchedule":
                    if (root.TryGetProperty("scheduleId", out var delSchedEl) && delSchedEl.TryGetInt32(out int delSchedId))
                    {
                        _repository.DeleteRecurringSchedule(delSchedId);
                        _viewModel.ReloadData(notifyWebView: true);
                        PushToast($"Deleted recurring schedule #{delSchedId}.");
                    }
                    break;

                case "exportLedgerCsv":
                    HandleExportFile(
                        title: "Export Checkbook Ledger to CSV",
                        defaultFileName: "FullOfGrace_Ledger_Export_2026-11.csv",
                        filter: "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*",
                        contentGenerator: () => _repository.ExportTransactionsCsv());
                    break;

                case "exportStewardshipTaxCsv":
                    HandleExportFile(
                        title: "Export Charitable Stewardship & Tax Receipts to CSV",
                        defaultFileName: "FullOfGrace_Stewardship_Tax_Receipts_2026.csv",
                        filter: "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*",
                        contentGenerator: () => _repository.ExportStewardshipTaxReceiptsCsv());
                    break;

                case "exportWorkspaceJsonBackup":
                    HandleExportFile(
                        title: "Save Full Workspace JSON Snapshot Backup",
                        defaultFileName: "FullOfGrace_WorkspaceBackup_2026-11.json",
                        filter: "JSON Backup Snapshot (*.json)|*.json|All Files (*.*)|*.*",
                        contentGenerator: () => _repository.ExportFullWorkspaceJsonBackup());
                    break;

                case "setDebtStrategy":
                    if (root.TryGetProperty("strategyMode", out var stratEl) && stratEl.ValueKind == JsonValueKind.String)
                    {
                        string mode = stratEl.GetString() ?? "Avalanche";
                        _debtStrategyMode = string.Equals(mode, "Snowball", StringComparison.OrdinalIgnoreCase)
                            ? "Snowball"
                            : "Avalanche";
                        PushStateToWebView();
                        PushToast($"Switched debt payoff cascade to {_debtStrategyMode} strategy.");
                    }
                    break;

                case "updateDebtLiability":
                    HandleUpdateDebtLiability(root);
                    break;

                case "createDebtLiability":
                    HandleCreateDebtLiability(root);
                    break;

                case "deleteDebtLiability":
                    if (root.TryGetProperty("liabilityId", out var delDebtEl) && delDebtEl.TryGetInt32(out int delDebtId))
                    {
                        _repository.DeleteDebtLiability(delDebtId);
                        _viewModel.ReloadData(notifyWebView: true);
                        PushToast($"Deleted debt profile #{delDebtId}.");
                    }
                    break;

                case "updateSavingsGoal":
                    HandleUpdateSavingsGoal(root);
                    break;

                case "contributeSavingsGoal":
                    HandleContributeSavingsGoal(root);
                    break;

                case "createSavingsGoal":
                    HandleCreateSavingsGoal(root);
                    break;

                case "deleteSavingsGoal":
                    if (root.TryGetProperty("goalId", out var delGoalEl) && delGoalEl.TryGetInt32(out int delGoalId))
                    {
                        _repository.DeleteSavingsGoal(delGoalId);
                        _viewModel.ReloadData(notifyWebView: true);
                        PushToast($"Deleted savings goal #{delGoalId}.");
                    }
                    break;

                case "openExternalPortal":
                    if (root.TryGetProperty("url", out var urlProp))
                    {
                        string? portalUrl = urlProp.GetString();
                        if (!string.IsNullOrWhiteSpace(portalUrl))
                        {
                            _viewModel.OpenPortalUrlCommand.Execute(portalUrl);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            PushToast($"Bridge warning: {ex.Message}");
        }
    }

    private void HandleUpdateSavingsGoal(JsonElement root)
    {
        if (!root.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out int id))
        {
            return;
        }

        decimal target = root.TryGetProperty("targetAmount", out var tEl) && tEl.TryGetDecimal(out decimal parsedTarget)
            ? parsedTarget
            : 0m;
        decimal saved = root.TryGetProperty("currentSavedAmount", out var sEl) && sEl.TryGetDecimal(out decimal parsedSaved)
            ? parsedSaved
            : 0m;
        decimal monthly = root.TryGetProperty("monthlyContributionTarget", out var mEl) && mEl.TryGetDecimal(out decimal parsedMonthly)
            ? parsedMonthly
            : 0m;
        string targetDate = root.TryGetProperty("targetDate", out var dEl) && dEl.ValueKind == JsonValueKind.String
            ? (dEl.GetString() ?? "2027-06-01")
            : "2027-06-01";

        _repository.UpdateSavingsGoal(id, target, saved, monthly, targetDate);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Updated savings goal #{id} (Saved: {saved:C2} / Target: {target:C2}).");
    }

    private void HandleContributeSavingsGoal(JsonElement root)
    {
        if (!root.TryGetProperty("goalId", out var idEl) || !idEl.TryGetInt32(out int goalId))
        {
            return;
        }

        decimal amount = root.TryGetProperty("amount", out var amtEl) && amtEl.TryGetDecimal(out decimal parsedAmt)
            ? parsedAmt
            : 250m;
        bool recordTransfer = !root.TryGetProperty("recordLedgerTransfer", out var recEl) || recEl.ValueKind != JsonValueKind.False;

        var updated = _repository.RecordSavingsGoalContribution(goalId, amount, recordTransfer);
        _viewModel.ReloadData(notifyWebView: true);
        if (updated != null)
        {
            PushToast($"Allocated +{amount:C2} to '{updated.Name}' ({updated.ProgressPercent:N1}% funded).");
        }
    }

    private void HandleCreateSavingsGoal(JsonElement root)
    {
        var goal = JsonSerializer.Deserialize<SavingsGoal>(root.GetRawText(), JsonOptions);
        if (goal == null || string.IsNullOrWhiteSpace(goal.Name))
        {
            return;
        }

        var created = _repository.CreateSavingsGoal(goal);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Created sinking fund '{created.Name}' ({created.CurrentSavedAmount:C2} / {created.TargetAmount:C2}).");
    }

    private void HandleUpdateDebtLiability(JsonElement root)
    {
        if (!root.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out int id))
        {
            return;
        }

        decimal balance = root.TryGetProperty("currentBalance", out var balEl) && balEl.TryGetDecimal(out decimal parsedBal)
            ? parsedBal
            : 0m;
        decimal apr = root.TryGetProperty("interestRateApr", out var aprEl) && aprEl.TryGetDecimal(out decimal parsedApr)
            ? parsedApr
            : 0m;
        decimal minPayment = root.TryGetProperty("minimumMonthlyPayment", out var minEl) && minEl.TryGetDecimal(out decimal parsedMin)
            ? parsedMin
            : 0m;
        decimal extraPrincipal = root.TryGetProperty("extraPrincipalMonthly", out var extEl) && extEl.TryGetDecimal(out decimal parsedExt)
            ? parsedExt
            : 0m;

        _repository.UpdateDebtLiability(id, balance, apr, minPayment, extraPrincipal);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Updated debt liability #{id} (+{extraPrincipal:C2}/mo extra principal).");
    }

    private void HandleCreateDebtLiability(JsonElement root)
    {
        var profile = JsonSerializer.Deserialize<DebtLiabilityProfile>(root.GetRawText(), JsonOptions);
        if (profile == null || string.IsNullOrWhiteSpace(profile.Name))
        {
            return;
        }

        var created = _repository.CreateDebtLiability(profile);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Added debt profile '{created.Name}' ({created.CurrentBalance:C2} @ {created.InterestRateApr:N2}% APR).");
    }

    private void HandleExportFile(string title, string defaultFileName, string filter, Func<string> contentGenerator)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = defaultFileName,
            Filter = filter
        };

        if (dialog.ShowDialog() == true)
        {
            string content = contentGenerator();
            File.WriteAllText(dialog.FileName, content);
            PushToast($"Exported '{Path.GetFileName(dialog.FileName)}' ({content.Length:N0} bytes).");
        }
    }

    private void HandleCreateRecurringSchedule(JsonElement root)
    {
        var schedule = JsonSerializer.Deserialize<RecurringSchedule>(root.GetRawText(), JsonOptions);
        if (schedule == null || string.IsNullOrWhiteSpace(schedule.PayeeName))
        {
            return;
        }

        var created = _repository.CreateRecurringSchedule(schedule);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Created {created.Frequency} schedule for {created.PayeeName} ({created.Amount:C2} due {created.NextDueDate}).");
    }

    private void HandleUpdateHolding(JsonElement root)
    {
        if (!root.TryGetProperty("holdingId", out var idEl) || !idEl.TryGetInt32(out int holdingId))
        {
            return;
        }

        decimal shares = root.TryGetProperty("shares", out var shEl) && shEl.TryGetDecimal(out decimal parsedShares)
            ? parsedShares
            : 0m;

        decimal avgCost = root.TryGetProperty("averageCostPerShare", out var acEl) && acEl.TryGetDecimal(out decimal parsedAvgCost)
            ? parsedAvgCost
            : 0m;

        decimal price = root.TryGetProperty("currentPrice", out var prEl) && prEl.TryGetDecimal(out decimal parsedPrice)
            ? parsedPrice
            : 0m;

        _repository.UpdateHoldingPosition(holdingId, shares, avgCost, price);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Updated holding #{holdingId} ({shares:N3} shares @ {price:C2}).");
    }

    private void HandleCreateHolding(JsonElement root)
    {
        var holding = JsonSerializer.Deserialize<InvestmentHolding>(root.GetRawText(), JsonOptions);
        if (holding == null || string.IsNullOrWhiteSpace(holding.TickerSymbol))
        {
            return;
        }

        if (holding.AccountId <= 0)
        {
            holding.AccountId = 4;
        }

        var created = _repository.CreateInvestmentHolding(holding);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Added {created.TickerSymbol} ({created.Shares:N2} shares, {created.MarketValue:C2}) to {created.AccountName}.");
    }

    private void HandleUpdateBudgetEnvelope(JsonElement root)
    {
        if (!root.TryGetProperty("categoryId", out var catEl) || !catEl.TryGetInt32(out int categoryId))
        {
            return;
        }

        string monthKey = root.TryGetProperty("monthKey", out var mkEl) && mkEl.ValueKind == JsonValueKind.String
            ? (mkEl.GetString() ?? "2026-11")
            : "2026-11";

        decimal assigned = root.TryGetProperty("assignedAmount", out var asgEl) && asgEl.TryGetDecimal(out decimal parsedAssigned)
            ? parsedAssigned
            : 0m;

        decimal? monthlyTarget = null;
        if (root.TryGetProperty("monthlyTarget", out var tgtEl) && tgtEl.TryGetDecimal(out decimal parsedTarget))
        {
            monthlyTarget = parsedTarget;
        }

        _repository.UpdateEnvelopeAssignment(monthKey, categoryId, assigned, monthlyTarget);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Updated envelope allocation ({assigned:C2}).");
    }

    private void HandleMoveEnvelopeFunds(JsonElement root)
    {
        if (!root.TryGetProperty("fromCategoryId", out var fromEl) || !fromEl.TryGetInt32(out int fromCatId) ||
            !root.TryGetProperty("toCategoryId", out var toEl) || !toEl.TryGetInt32(out int toCatId) ||
            !root.TryGetProperty("amount", out var amtEl) || !amtEl.TryGetDecimal(out decimal amount))
        {
            return;
        }

        string monthKey = root.TryGetProperty("monthKey", out var mkEl) && mkEl.ValueKind == JsonValueKind.String
            ? (mkEl.GetString() ?? "2026-11")
            : "2026-11";

        _repository.MoveEnvelopeFunds(monthKey, fromCatId, toCatId, amount);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Moved {amount:C2} between budget envelopes.");
    }

    private void HandleParseStatement(JsonElement root)
    {
        int accountId = root.TryGetProperty("accountId", out var accEl) && accEl.TryGetInt32(out int aId)
            ? aId
            : (_viewModel.SelectedAccount?.Id ?? 1);

        string fileName = "statement.ofx";
        string content = string.Empty;

        if (root.TryGetProperty("sampleFormat", out var sampleEl) && sampleEl.ValueKind == JsonValueKind.String)
        {
            string fmt = sampleEl.GetString() ?? "OFX";
            (fileName, content) = StatementParserService.GetSampleStatementPayload(fmt);
        }
        else
        {
            if (root.TryGetProperty("fileName", out var fnEl) && fnEl.ValueKind == JsonValueKind.String)
            {
                fileName = fnEl.GetString() ?? "statement.ofx";
            }
            if (root.TryGetProperty("content", out var cntEl) && cntEl.ValueKind == JsonValueKind.String)
            {
                content = cntEl.GetString() ?? string.Empty;
            }
        }

        var (detectedFormat, parsedItems) = _parserService.ParseStatementContent(fileName, content);
        var rules = _repository.GetRules();
        var categories = _repository.GetCategories();
        var existingTransactions = _repository.GetTransactions(accountId);

        var preview = _rulesAndDedupService.ProcessImportBatch(
            fileName,
            detectedFormat,
            accountId,
            parsedItems,
            rules,
            categories,
            existingTransactions);

        PushStatementPreview(preview);
    }

    private void HandleBrowseStatementFile(JsonElement root)
    {
        int accountId = root.TryGetProperty("accountId", out var accEl) && accEl.TryGetInt32(out int aId)
            ? aId
            : (_viewModel.SelectedAccount?.Id ?? 1);

        var dialog = new OpenFileDialog
        {
            Title = "Select Bank or Credit Card Statement (OFX, QFX, QIF, CSV)",
            Filter = "Financial Statements (*.ofx;*.qfx;*.qif;*.csv)|*.ofx;*.qfx;*.qif;*.csv|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            string fileName = Path.GetFileName(dialog.FileName);
            string content = File.ReadAllText(dialog.FileName);

            var (detectedFormat, parsedItems) = _parserService.ParseStatementContent(fileName, content);
            var rules = _repository.GetRules();
            var categories = _repository.GetCategories();
            var existingTransactions = _repository.GetTransactions(accountId);

            var preview = _rulesAndDedupService.ProcessImportBatch(
                fileName,
                detectedFormat,
                accountId,
                parsedItems,
                rules,
                categories,
                existingTransactions);

            PushStatementPreview(preview);
        }
    }

    private void HandleCommitStatementImport(JsonElement root)
    {
        int accountId = root.TryGetProperty("accountId", out var accEl) && accEl.TryGetInt32(out int aId)
            ? aId
            : (_viewModel.SelectedAccount?.Id ?? 1);

        if (!root.TryGetProperty("items", out var itemsEl) || itemsEl.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var items = JsonSerializer.Deserialize<List<ImportedStatementItem>>(itemsEl.GetRawText(), JsonOptions)
                    ?? new List<ImportedStatementItem>();

        int committedCount = _repository.CommitImportedTransactions(accountId, items);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Committed {committedCount} imported statement transaction(s) to SQLite.");
    }

    private void HandleCreateRule(JsonElement root)
    {
        var rule = JsonSerializer.Deserialize<CategorizationRule>(root.GetRawText(), JsonOptions);
        if (rule == null || string.IsNullOrWhiteSpace(rule.MatchPattern))
        {
            return;
        }

        var created = _repository.CreateRule(rule);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Created rule '{created.Name}' (Priority {created.Priority})");
    }

    private void HandleCreateTransaction(JsonElement root)
    {
        int accountId = root.TryGetProperty("accountId", out var accEl) && accEl.TryGetInt32(out int aId)
            ? aId
            : (_viewModel.SelectedAccount?.Id ?? 1);

        string date = root.TryGetProperty("transactionDate", out var dateEl) && dateEl.ValueKind == JsonValueKind.String
            ? (dateEl.GetString() ?? DateTime.Today.ToString("yyyy-MM-dd"))
            : DateTime.Today.ToString("yyyy-MM-dd");

        string payeeName = root.TryGetProperty("payeeName", out var payeeEl) && payeeEl.ValueKind == JsonValueKind.String
            ? (payeeEl.GetString() ?? "Manual Entry")
            : "Manual Entry";

        int? categoryId = null;
        if (root.TryGetProperty("categoryId", out var catEl) &&
            catEl.ValueKind == JsonValueKind.Number &&
            catEl.TryGetInt32(out int cId) && cId > 0)
        {
            categoryId = cId;
        }

        string? memo = root.TryGetProperty("memo", out var memoEl) && memoEl.ValueKind == JsonValueKind.String
            ? memoEl.GetString()
            : null;

        string? tags = root.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.String
            ? tagsEl.GetString()
            : null;

        string? receiptRef = root.TryGetProperty("receiptRef", out var receiptEl) && receiptEl.ValueKind == JsonValueKind.String
            ? receiptEl.GetString()
            : null;

        string? refNum = root.TryGetProperty("referenceNumber", out var refEl) && refEl.ValueKind == JsonValueKind.String
            ? refEl.GetString()
            : null;

        decimal amount = root.TryGetProperty("amount", out var amtEl) && amtEl.TryGetDecimal(out decimal parsedAmt)
            ? parsedAmt
            : 0m;

        string status = root.TryGetProperty("status", out var statusEl) && statusEl.ValueKind == JsonValueKind.String
            ? (statusEl.GetString() ?? TransactionStatus.Uncleared)
            : TransactionStatus.Uncleared;

        List<Split>? splits = null;
        if (root.TryGetProperty("splits", out var splitsEl) && splitsEl.ValueKind == JsonValueKind.Array)
        {
            splits = new List<Split>();
            foreach (var item in splitsEl.EnumerateArray())
            {
                int? sCatId = item.TryGetProperty("categoryId", out var scEl) && scEl.ValueKind == JsonValueKind.Number && scEl.TryGetInt32(out int sci)
                    ? sci
                    : null;
                string? sMemo = item.TryGetProperty("memo", out var smEl) && smEl.ValueKind == JsonValueKind.String
                    ? smEl.GetString()
                    : null;
                decimal sAmt = item.TryGetProperty("amount", out var saEl) && saEl.TryGetDecimal(out decimal sai)
                    ? sai
                    : 0m;

                if (sAmt != 0m)
                {
                    splits.Add(new Split
                    {
                        CategoryId = sCatId,
                        Memo = sMemo,
                        Amount = sAmt
                    });
                }
            }
        }

        _repository.CreateTransaction(
            accountId: accountId,
            transactionDate: date,
            payeeName: payeeName,
            categoryId: categoryId,
            memo: memo,
            signedAmount: amount,
            status: status,
            referenceNumber: refNum,
            splits: splits,
            tags: tags,
            receiptRef: receiptRef);

        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Saved transaction for {payeeName} ({amount:C2})");
    }

    private void HandleUpdateTransaction(JsonElement root)
    {
        if (!root.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out int txId))
        {
            return;
        }

        string date = root.TryGetProperty("transactionDate", out var dateEl) && dateEl.ValueKind == JsonValueKind.String
            ? (dateEl.GetString() ?? DateTime.Today.ToString("yyyy-MM-dd"))
            : DateTime.Today.ToString("yyyy-MM-dd");

        string? refNum = root.TryGetProperty("referenceNumber", out var refEl) && refEl.ValueKind == JsonValueKind.String
            ? refEl.GetString()
            : null;

        string payeeName = root.TryGetProperty("payeeName", out var payeeEl) && payeeEl.ValueKind == JsonValueKind.String
            ? (payeeEl.GetString() ?? "Manual Entry")
            : "Manual Entry";

        int? categoryId = null;
        if (root.TryGetProperty("categoryId", out var catEl) &&
            catEl.ValueKind == JsonValueKind.Number &&
            catEl.TryGetInt32(out int cId) && cId > 0)
        {
            categoryId = cId;
        }

        string? memo = root.TryGetProperty("memo", out var memoEl) && memoEl.ValueKind == JsonValueKind.String
            ? memoEl.GetString()
            : null;

        string? tags = root.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.String
            ? tagsEl.GetString()
            : null;

        string? receiptRef = root.TryGetProperty("receiptRef", out var receiptEl) && receiptEl.ValueKind == JsonValueKind.String
            ? receiptEl.GetString()
            : null;

        decimal amount = root.TryGetProperty("amount", out var amtEl) && amtEl.TryGetDecimal(out decimal parsedAmt)
            ? parsedAmt
            : 0m;

        string status = root.TryGetProperty("status", out var statusEl) && statusEl.ValueKind == JsonValueKind.String
            ? (statusEl.GetString() ?? TransactionStatus.Uncleared)
            : TransactionStatus.Uncleared;

        _repository.UpdateTransactionInline(
            transactionId: txId,
            transactionDate: date,
            referenceNumber: refNum,
            payeeName: payeeName,
            categoryId: categoryId,
            memo: memo,
            signedAmount: amount,
            status: status,
            tags: tags,
            receiptRef: receiptRef);

        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Updated transaction #{txId} ({payeeName})");
    }

    private void HandleCompleteReconciliation(JsonElement root)
    {
        var req = JsonSerializer.Deserialize<ReconciliationSessionCommitRequest>(root.GetRawText(), JsonOptions);
        if (req == null)
        {
            return;
        }

        if (req.AccountId <= 0)
        {
            req.AccountId = _viewModel.SelectedAccount?.Id ?? 1;
        }

        int count = _repository.CompleteReconciliationSession(req);
        _viewModel.ReloadData(notifyWebView: true);
        PushToast($"Statement reconciled! Locked {count} transaction(s) at {req.StatementEndingBalance:C2}.");
    }
}
