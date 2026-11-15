using System.Globalization;
using System.Text;
using System.Text.Json;
using FullofGraceMoneyManager.Desktop.Models;
using FullofGraceMoneyManager.Desktop.Services;
using Microsoft.Data.Sqlite;

namespace FullofGraceMoneyManager.Desktop.Data;

public class DashboardMetrics
{
    public decimal NetWorth { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal LiquidCash { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal SavingsRatePercent => MonthlyIncome > 0
        ? Math.Round((MonthlyIncome - MonthlyExpenses) / MonthlyIncome * 100m, 1)
        : 0m;
    public List<CategorySpendingItem> CategoryBreakdown { get; set; } = new();
    public List<CashFlowPoint> MonthlyCashFlow { get; set; } = new();
}

public class CategorySpendingItem
{
    public string CategoryName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#38BDF8";
    public decimal Spent { get; set; }
    public decimal Budget { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class CashFlowPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net => Income - Expense;
}

public class MoneyRepository
{
    private readonly string _connectionString;

    public MoneyRepository()
    {
        _connectionString = DatabaseInitializer.GetConnectionString();
    }

    public List<Account> GetAccountsWithBalances()
    {
        var accounts = new List<Account>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = """
            SELECT
                a.Id,
                a.Name,
                a.AccountNumberMask,
                a.AccountType,
                a.AccountGroup,
                a.Institution,
                a.OpeningBalance,
                a.Currency,
                a.IsLiability,
                a.IsActive,
                a.PortalUrl,
                a.Notes,
                a.CreatedAt,
                COALESCE(SUM(CASE WHEN t.Status IN ('Cleared', 'Reconciled') THEN t.Amount ELSE 0 END), 0) AS ClearedDelta,
                COALESCE(SUM(CASE WHEN t.Status = 'Uncleared' THEN t.Amount ELSE 0 END), 0) AS PendingDelta
            FROM Accounts a
            LEFT JOIN Transactions t ON t.AccountId = a.Id
            WHERE a.IsActive = 1
            GROUP BY a.Id
            ORDER BY a.Id;
            """;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            decimal opening = reader.GetDecimal(6);
            decimal clearedDelta = reader.GetDecimal(13);
            decimal pendingDelta = reader.GetDecimal(14);
            decimal clearedBalance = opening + clearedDelta;
            decimal currentBalance = clearedBalance + pendingDelta;

            accounts.Add(new Account
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                AccountNumberMask = reader.GetString(2),
                AccountType = reader.GetString(3),
                AccountGroup = reader.GetString(4),
                Institution = reader.GetString(5),
                OpeningBalance = opening,
                ClearedBalance = clearedBalance,
                PendingTotal = pendingDelta,
                CurrentBalance = currentBalance,
                Currency = reader.GetString(7),
                IsLiability = reader.GetInt32(8) != 0,
                IsActive = reader.GetInt32(9) != 0,
                PortalUrl = reader.IsDBNull(10) ? null : reader.GetString(10),
                Notes = reader.IsDBNull(11) ? null : reader.GetString(11),
                CreatedAt = reader.GetString(12)
            });
        }

        return accounts;
    }

    public List<AccountGroupNode> GetAccountGroups()
    {
        var allAccounts = GetAccountsWithBalances();
        string[] groupOrder = ["Banking", "Credit Cards", "Investments", "Assets/Loans"];

        var result = new List<AccountGroupNode>();
        foreach (string groupName in groupOrder)
        {
            var groupAccounts = allAccounts
                .Where(a => string.Equals(a.AccountGroup, groupName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            string glyph = groupName switch
            {
                "Banking" => "🏦",
                "Credit Cards" => "💳",
                "Investments" => "📈",
                "Assets/Loans" => "🏠",
                _ => "📁"
            };

            var node = new AccountGroupNode
            {
                GroupName = groupName,
                IconGlyph = glyph,
                GroupTotal = groupAccounts.Sum(a => a.CurrentBalance),
                IsExpanded = true
            };

            foreach (var acct in groupAccounts)
            {
                node.Accounts.Add(acct);
            }

            result.Add(node);
        }

        return result;
    }

    public Account CreateAccount(
        string name,
        string accountType,
        string institution,
        string accountNumberMask,
        decimal openingBalance,
        string? portalUrl = null,
        string? notes = null)
    {
        string cleanType = string.IsNullOrWhiteSpace(accountType) ? "Checking" : accountType.Trim();
        string group = cleanType switch
        {
            "CreditCard" or "Credit Card" => "Credit Cards",
            "Investment" or "Brokerage" or "401k" or "IRA" => "Investments",
            "Loan" or "Mortgage" or "Asset" => "Assets/Loans",
            _ => "Banking"
        };
        bool isLiability = group is "Credit Cards" or "Assets/Loans" && openingBalance <= 0m;

        string cleanMask = string.IsNullOrWhiteSpace(accountNumberMask)
            ? ""
            : (accountNumberMask.Trim().StartsWith("••") ? accountNumberMask.Trim() : "••" + accountNumberMask.Trim().TrimStart('•', '*'));

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Accounts (
                Name, AccountNumberMask, AccountType, AccountGroup,
                Institution, OpeningBalance, Currency, IsLiability,
                IsActive, PortalUrl, Notes
            )
            VALUES (
                @name, @mask, @type, @group,
                @inst, @openBal, 'USD', @isLiab,
                1, @portal, @notes
            );
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(name) ? "New Account" : name.Trim());
        cmd.Parameters.AddWithValue("@mask", cleanMask);
        cmd.Parameters.AddWithValue("@type", cleanType);
        cmd.Parameters.AddWithValue("@group", group);
        cmd.Parameters.AddWithValue("@inst", string.IsNullOrWhiteSpace(institution) ? "Personal" : institution.Trim());
        cmd.Parameters.AddWithValue("@openBal", openingBalance);
        cmd.Parameters.AddWithValue("@isLiab", isLiability ? 1 : 0);
        cmd.Parameters.AddWithValue("@portal", string.IsNullOrWhiteSpace(portalUrl) ? DBNull.Value : portalUrl.Trim());
        cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes.Trim());

        int newId = Convert.ToInt32(cmd.ExecuteScalar());
        return GetAccountsWithBalances().First(a => a.Id == newId);
    }

    public Account? UpdateAccount(
        int accountId,
        string name,
        string accountType,
        string institution,
        string accountNumberMask,
        decimal openingBalance,
        string? portalUrl = null,
        string? notes = null)
    {
        string cleanType = string.IsNullOrWhiteSpace(accountType) ? "Checking" : accountType.Trim();
        string group = cleanType switch
        {
            "CreditCard" or "Credit Card" => "Credit Cards",
            "Investment" or "Brokerage" or "401k" or "IRA" => "Investments",
            "Loan" or "Mortgage" or "Asset" => "Assets/Loans",
            _ => "Banking"
        };
        bool isLiability = group is "Credit Cards" or "Assets/Loans" && openingBalance <= 0m;

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Accounts
            SET Name = @name,
                AccountNumberMask = @mask,
                AccountType = @type,
                AccountGroup = @group,
                Institution = @inst,
                OpeningBalance = @openBal,
                IsLiability = @isLiab,
                PortalUrl = @portal,
                Notes = @notes
            WHERE Id = @id;
            """;
        cmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(name) ? "Account" : name.Trim());
        cmd.Parameters.AddWithValue("@mask", accountNumberMask?.Trim() ?? "");
        cmd.Parameters.AddWithValue("@type", cleanType);
        cmd.Parameters.AddWithValue("@group", group);
        cmd.Parameters.AddWithValue("@inst", institution?.Trim() ?? "");
        cmd.Parameters.AddWithValue("@openBal", openingBalance);
        cmd.Parameters.AddWithValue("@isLiab", isLiability ? 1 : 0);
        cmd.Parameters.AddWithValue("@portal", string.IsNullOrWhiteSpace(portalUrl) ? DBNull.Value : portalUrl.Trim());
        cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes.Trim());
        cmd.Parameters.AddWithValue("@id", accountId);
        cmd.ExecuteNonQuery();

        return GetAccountsWithBalances().FirstOrDefault(a => a.Id == accountId);
    }

    public void DeleteAccount(int accountId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Accounts WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", accountId);
        cmd.ExecuteNonQuery();
    }

    public Category CreateCategory(string name, int? parentId, string categoryType, decimal monthlyBudget, string? colorHex = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Categories (Name, ParentId, CategoryType, ColorHex, Icon, MonthlyBudget)
            VALUES (@name, @parentId, @type, @color, 'Tag', @budget);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(name) ? "Custom Category" : name.Trim());
        cmd.Parameters.AddWithValue("@parentId", parentId.HasValue && parentId.Value > 0 ? parentId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@type", string.IsNullOrWhiteSpace(categoryType) ? "Expense" : categoryType.Trim());
        cmd.Parameters.AddWithValue("@color", string.IsNullOrWhiteSpace(colorHex) ? "#38BDF8" : colorHex.Trim());
        cmd.Parameters.AddWithValue("@budget", Math.Max(0m, monthlyBudget));
        int id = Convert.ToInt32(cmd.ExecuteScalar());
        return GetCategories().First(c => c.Id == id);
    }

    private int EnsureValidAccountId(SqliteConnection conn, int requestedAccountId, string fallbackType = "Checking", string fallbackName = "Primary Checking")
    {
        if (requestedAccountId > 0)
        {
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(1) FROM Accounts WHERE Id = @id;";
            checkCmd.Parameters.AddWithValue("@id", requestedAccountId);
            if (Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0L) > 0)
            {
                return requestedAccountId;
            }
        }

        using (var firstCmd = conn.CreateCommand())
        {
            firstCmd.CommandText = "SELECT Id FROM Accounts ORDER BY Id LIMIT 1;";
            var existing = firstCmd.ExecuteScalar();
            if (existing != null && existing != DBNull.Value)
            {
                return Convert.ToInt32(existing);
            }
        }

        var created = CreateAccount(fallbackName, fallbackType, "Personal", "", 0m);
        return created.Id;
    }

    public List<Category> GetCategories()
    {
        var list = new List<Category>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = """
            SELECT
                c.Id,
                c.Name,
                c.ParentId,
                p.Name AS ParentName,
                c.CategoryType,
                c.ColorHex,
                c.Icon,
                c.MonthlyBudget
            FROM Categories c
            LEFT JOIN Categories p ON p.Id = c.ParentId
            ORDER BY COALESCE(p.Name, c.Name), c.ParentId IS NOT NULL, c.Name;
            """;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ParentId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                ParentName = reader.IsDBNull(3) ? null : reader.GetString(3),
                CategoryType = reader.GetString(4),
                ColorHex = reader.GetString(5),
                Icon = reader.GetString(6),
                MonthlyBudget = reader.GetDecimal(7)
            });
        }

        return list;
    }

    public List<Payee> GetPayees()
    {
        var list = new List<Payee>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = """
            SELECT
                p.Id,
                p.Name,
                p.DefaultCategoryId,
                c.Name AS DefaultCategoryName,
                p.NormalizedPattern,
                p.WebsiteUrl,
                p.Notes
            FROM Payees p
            LEFT JOIN Categories c ON c.Id = p.DefaultCategoryId
            ORDER BY p.Name;
            """;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Payee
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                DefaultCategoryId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                DefaultCategoryName = reader.IsDBNull(3) ? null : reader.GetString(3),
                NormalizedPattern = reader.IsDBNull(4) ? null : reader.GetString(4),
                WebsiteUrl = reader.IsDBNull(5) ? null : reader.GetString(5),
                Notes = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return list;
    }

    public List<CategorizationRule> GetRules()
    {
        var categoriesById = GetCategories().ToDictionary(c => c.Id);
        var rules = new List<CategorizationRule>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = """
            SELECT
                Id, Name, Priority, IsEnabled, MatchField, MatchOperator, MatchPattern,
                MinAmount, MaxAmount, TargetAccountId, RenamePayeeTo, AssignCategoryId, SetStatus, AppendMemo
            FROM CategorizationRules
            ORDER BY Priority ASC, Id ASC;
            """;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int? assignCatId = reader.IsDBNull(11) ? null : reader.GetInt32(11);
            string assignCatName = assignCatId.HasValue && categoriesById.TryGetValue(assignCatId.Value, out var cat)
                ? cat.FullPath
                : "No Category Change";

            rules.Add(new CategorizationRule
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Priority = reader.GetInt32(2),
                IsEnabled = reader.GetInt32(3) != 0,
                MatchField = reader.GetString(4),
                MatchOperator = reader.GetString(5),
                MatchPattern = reader.GetString(6),
                MinAmount = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                MaxAmount = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                TargetAccountId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                RenamePayeeTo = reader.IsDBNull(10) ? null : reader.GetString(10),
                AssignCategoryId = assignCatId,
                AssignCategoryName = assignCatName,
                SetStatus = reader.IsDBNull(12) ? null : reader.GetString(12),
                AppendMemo = reader.IsDBNull(13) ? null : reader.GetString(13)
            });
        }

        return rules;
    }

    public CategorizationRule CreateRule(CategorizationRule rule)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO CategorizationRules (
                Name, Priority, IsEnabled, MatchField, MatchOperator, MatchPattern,
                MinAmount, MaxAmount, TargetAccountId, RenamePayeeTo, AssignCategoryId, SetStatus, AppendMemo
            )
            VALUES (
                @name, @priority, @enabled, @field, @op, @pattern,
                @minAmt, @maxAmt, @targetAcc, @renameTo, @catId, @status, @memo
            );
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("@name", string.IsNullOrWhiteSpace(rule.Name) ? $"Rule: {rule.MatchPattern}" : rule.Name.Trim());
        cmd.Parameters.AddWithValue("@priority", rule.Priority <= 0 ? 50 : rule.Priority);
        cmd.Parameters.AddWithValue("@enabled", rule.IsEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("@field", string.IsNullOrWhiteSpace(rule.MatchField) ? "Payee" : rule.MatchField);
        cmd.Parameters.AddWithValue("@op", string.IsNullOrWhiteSpace(rule.MatchOperator) ? "Contains" : rule.MatchOperator);
        cmd.Parameters.AddWithValue("@pattern", rule.MatchPattern.Trim());
        cmd.Parameters.AddWithValue("@minAmt", rule.MinAmount.HasValue ? rule.MinAmount.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@maxAmt", rule.MaxAmount.HasValue ? rule.MaxAmount.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@targetAcc", rule.TargetAccountId.HasValue ? rule.TargetAccountId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@renameTo", string.IsNullOrWhiteSpace(rule.RenamePayeeTo) ? DBNull.Value : rule.RenamePayeeTo.Trim());
        cmd.Parameters.AddWithValue("@catId", rule.AssignCategoryId.HasValue ? rule.AssignCategoryId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(rule.SetStatus) ? DBNull.Value : rule.SetStatus.Trim());
        cmd.Parameters.AddWithValue("@memo", string.IsNullOrWhiteSpace(rule.AppendMemo) ? DBNull.Value : rule.AppendMemo.Trim());

        int newId = Convert.ToInt32(cmd.ExecuteScalar());
        return GetRules().First(r => r.Id == newId);
    }

    public bool ToggleRuleEnabled(int ruleId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE CategorizationRules
            SET IsEnabled = CASE WHEN IsEnabled = 1 THEN 0 ELSE 1 END
            WHERE Id = @id;
            SELECT IsEnabled FROM CategorizationRules WHERE Id = @id;
            """;
        cmd.Parameters.AddWithValue("@id", ruleId);
        object? val = cmd.ExecuteScalar();
        return Convert.ToInt32(val ?? 0) != 0;
    }

    public void DeleteRule(int ruleId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM CategorizationRules WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", ruleId);
        cmd.ExecuteNonQuery();
    }

    public int ApplyRulesToExistingTransactions(int? accountId = null)
    {
        var activeRules = GetRules().Where(r => r.IsEnabled).OrderBy(r => r.Priority).ToList();
        var transactions = GetTransactions(accountId);
        int updatedCount = 0;

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var dbTx = conn.BeginTransaction();

        foreach (var tx in transactions)
        {
            foreach (var rule in activeRules)
            {
                if (!RulesAndDeduplicationService.DoesRuleMatchTransaction(rule, tx))
                {
                    continue;
                }

                string newPayee = !string.IsNullOrWhiteSpace(rule.RenamePayeeTo) ? rule.RenamePayeeTo.Trim() : tx.PayeeName;
                int? newCatId = !tx.IsSplit && rule.AssignCategoryId.HasValue ? rule.AssignCategoryId : tx.CategoryId;

                bool changed = !string.Equals(newPayee, tx.PayeeName, StringComparison.Ordinal) ||
                               newCatId != tx.CategoryId;

                if (changed)
                {
                    using var updateCmd = conn.CreateCommand();
                    updateCmd.Transaction = dbTx;
                    updateCmd.CommandText = """
                        UPDATE Transactions
                        SET PayeeName = @payee,
                            CategoryId = @catId
                        WHERE Id = @id;
                        """;
                    updateCmd.Parameters.AddWithValue("@payee", newPayee);
                    updateCmd.Parameters.AddWithValue("@catId", newCatId.HasValue ? newCatId.Value : DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@id", tx.Id);
                    updateCmd.ExecuteNonQuery();
                    updatedCount++;
                }

                break;
            }
        }

        dbTx.Commit();
        return updatedCount;
    }

    public int CommitImportedTransactions(int accountId, List<ImportedStatementItem> selectedItems)
    {
        int committed = 0;
        foreach (var item in selectedItems.Where(i => i.SelectedForImport))
        {
            CreateTransaction(
                accountId: accountId,
                transactionDate: item.TransactionDate,
                payeeName: string.IsNullOrWhiteSpace(item.NormalizedPayee) ? item.RawPayee : item.NormalizedPayee,
                categoryId: item.SuggestedCategoryId,
                memo: item.FinalMemo ?? item.RawMemo,
                signedAmount: item.Amount,
                status: string.IsNullOrWhiteSpace(item.SuggestedStatus) ? TransactionStatus.Cleared : item.SuggestedStatus,
                referenceNumber: item.ReferenceNumber ?? item.ExternalFitId);
            committed++;
        }

        return committed;
    }

    public List<Transaction> GetTransactions(int? accountId = null, string? searchQuery = null)
    {
        var accountsById = GetAccountsWithBalances().ToDictionary(a => a.Id);
        var categoriesById = GetCategories().ToDictionary(c => c.Id);

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        string sql = """
            SELECT
                t.Id,
                t.AccountId,
                a.Name AS AccountName,
                t.TransactionDate,
                t.ReferenceNumber,
                t.PayeeId,
                t.PayeeName,
                t.CategoryId,
                t.Memo,
                t.Amount,
                t.Status,
                t.IsSplit,
                t.Tags,
                t.ReceiptRef
            FROM Transactions t
            INNER JOIN Accounts a ON a.Id = t.AccountId
            WHERE (@accountId IS NULL OR t.AccountId = @accountId)
            ORDER BY t.TransactionDate ASC, t.Id ASC;
            """;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@accountId", accountId.HasValue ? accountId.Value : DBNull.Value);

        var transactions = new List<Transaction>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                int? catId = reader.IsDBNull(7) ? null : reader.GetInt32(7);
                bool isSplit = reader.GetInt32(11) != 0;

                string categoryDisplay = isSplit
                    ? "-- Split Transaction --"
                    : (catId.HasValue && categoriesById.TryGetValue(catId.Value, out var cat)
                        ? cat.FullPath
                        : "Uncategorized");

                transactions.Add(new Transaction
                {
                    Id = reader.GetInt32(0),
                    AccountId = reader.GetInt32(1),
                    AccountName = reader.GetString(2),
                    TransactionDate = reader.GetString(3),
                    ReferenceNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                    PayeeId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    PayeeName = reader.GetString(6),
                    CategoryId = catId,
                    CategoryName = categoryDisplay,
                    Memo = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Amount = reader.GetDecimal(9),
                    Status = reader.GetString(10),
                    IsSplit = isSplit,
                    Tags = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    ReceiptRef = reader.IsDBNull(13) ? null : reader.GetString(13)
                });
            }
        }

        // Load splits for split transactions
        var splitTxIds = transactions.Where(t => t.IsSplit).Select(t => t.Id).ToHashSet();
        if (splitTxIds.Count > 0)
        {
            const string splitSql = """
                SELECT Id, TransactionId, CategoryId, Memo, Amount, TransferAccountId
                FROM TransactionSplits
                ORDER BY Id ASC;
                """;
            using var splitCmd = conn.CreateCommand();
            splitCmd.CommandText = splitSql;
            using var splitReader = splitCmd.ExecuteReader();

            var txById = transactions.ToDictionary(t => t.Id);
            while (splitReader.Read())
            {
                int txId = splitReader.GetInt32(1);
                if (!txById.TryGetValue(txId, out var parentTx))
                {
                    continue;
                }

                int? splitCatId = splitReader.IsDBNull(2) ? null : splitReader.GetInt32(2);
                string splitCatName = splitCatId.HasValue && categoriesById.TryGetValue(splitCatId.Value, out var sc)
                    ? sc.FullPath
                    : "Uncategorized";

                parentTx.Splits.Add(new Split
                {
                    Id = splitReader.GetInt32(0),
                    TransactionId = txId,
                    CategoryId = splitCatId,
                    CategoryName = splitCatName,
                    Memo = splitReader.IsDBNull(3) ? null : splitReader.GetString(3),
                    Amount = splitReader.GetDecimal(4),
                    TransferAccountId = splitReader.IsDBNull(5) ? null : splitReader.GetInt32(5)
                });
            }
        }

        // Calculate chronological running balance per account
        var runningByAccount = accountsById.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OpeningBalance);
        foreach (var tx in transactions)
        {
            if (!runningByAccount.ContainsKey(tx.AccountId))
            {
                runningByAccount[tx.AccountId] = 0m;
            }

            runningByAccount[tx.AccountId] += tx.Amount;
            tx.RunningBalance = runningByAccount[tx.AccountId];
        }

        // Filter by search query after computing accurate running balances
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            string q = searchQuery.Trim();
            transactions = transactions.Where(t =>
                t.PayeeName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                t.CategoryName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (t.Memo?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.ReferenceNumber?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                t.Tags.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (t.ReceiptRef?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                t.Splits.Any(s =>
                    s.CategoryName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (s.Memo?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            ).ToList();
        }

        // Return in reverse-chronological order for display while keeping accurate RunningBalance
        transactions.Reverse();
        return transactions;
    }

    public Transaction CreateTransaction(
        int accountId,
        string transactionDate,
        string payeeName,
        int? categoryId,
        string? memo,
        decimal signedAmount,
        string status = TransactionStatus.Uncleared,
        string? referenceNumber = null,
        List<Split>? splits = null,
        string? tags = null,
        string? receiptRef = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var dbTx = conn.BeginTransaction();

        // Ensure Payee exists or retrieve its Id
        int? payeeId = null;
        if (!string.IsNullOrWhiteSpace(payeeName))
        {
            using var findPayee = conn.CreateCommand();
            findPayee.Transaction = dbTx;
            findPayee.CommandText = "SELECT Id FROM Payees WHERE Name = @name LIMIT 1;";
            findPayee.Parameters.AddWithValue("@name", payeeName.Trim());
            object? existing = findPayee.ExecuteScalar();

            if (existing != null && existing != DBNull.Value)
            {
                payeeId = Convert.ToInt32(existing);
            }
            else
            {
                using var insertPayee = conn.CreateCommand();
                insertPayee.Transaction = dbTx;
                insertPayee.CommandText = """
                    INSERT INTO Payees (Name, DefaultCategoryId)
                    VALUES (@name, @catId);
                    SELECT last_insert_rowid();
                    """;
                insertPayee.Parameters.AddWithValue("@name", payeeName.Trim());
                insertPayee.Parameters.AddWithValue("@catId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
                payeeId = Convert.ToInt32(insertPayee.ExecuteScalar());
            }
        }

        bool isSplit = splits is { Count: > 0 };

        using var cmd = conn.CreateCommand();
        cmd.Transaction = dbTx;
        cmd.CommandText = """
            INSERT INTO Transactions (
                AccountId, TransactionDate, ReferenceNumber, PayeeId, PayeeName,
                CategoryId, Memo, Tags, ReceiptRef, Amount, Status, IsSplit
            )
            VALUES (
                @accountId, @date, @refNum, @payeeId, @payeeName,
                @catId, @memo, @tags, @receiptRef, @amount, @status, @isSplit
            );
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@date", string.IsNullOrWhiteSpace(transactionDate)
            ? DateTime.Today.ToString("yyyy-MM-dd")
            : transactionDate);
        cmd.Parameters.AddWithValue("@refNum", string.IsNullOrWhiteSpace(referenceNumber) ? DBNull.Value : referenceNumber.Trim());
        cmd.Parameters.AddWithValue("@payeeId", payeeId.HasValue ? payeeId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@payeeName", payeeName.Trim());
        cmd.Parameters.AddWithValue("@catId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@memo", string.IsNullOrWhiteSpace(memo) ? DBNull.Value : memo.Trim());
        cmd.Parameters.AddWithValue("@tags", NormalizeTagsString(tags));
        cmd.Parameters.AddWithValue("@receiptRef", string.IsNullOrWhiteSpace(receiptRef) ? DBNull.Value : receiptRef.Trim());
        cmd.Parameters.AddWithValue("@amount", signedAmount);
        cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(status) ? TransactionStatus.Uncleared : status);
        cmd.Parameters.AddWithValue("@isSplit", isSplit ? 1 : 0);

        int newId = Convert.ToInt32(cmd.ExecuteScalar());

        if (isSplit && splits != null)
        {
            foreach (var s in splits)
            {
                using var splitCmd = conn.CreateCommand();
                splitCmd.Transaction = dbTx;
                splitCmd.CommandText = """
                    INSERT INTO TransactionSplits (TransactionId, CategoryId, Memo, Amount, TransferAccountId)
                    VALUES (@txId, @catId, @memo, @amount, @xferId);
                    """;
                splitCmd.Parameters.AddWithValue("@txId", newId);
                splitCmd.Parameters.AddWithValue("@catId", s.CategoryId.HasValue ? s.CategoryId.Value : DBNull.Value);
                splitCmd.Parameters.AddWithValue("@memo", string.IsNullOrWhiteSpace(s.Memo) ? DBNull.Value : s.Memo.Trim());
                splitCmd.Parameters.AddWithValue("@amount", s.Amount);
                splitCmd.Parameters.AddWithValue("@xferId", s.TransferAccountId.HasValue ? s.TransferAccountId.Value : DBNull.Value);
                splitCmd.ExecuteNonQuery();
            }
        }

        dbTx.Commit();

        return GetTransactions(accountId).First(t => t.Id == newId);
    }

    public void UpdateTransactionInline(
        int transactionId,
        string transactionDate,
        string? referenceNumber,
        string payeeName,
        int? categoryId,
        string? memo,
        decimal signedAmount,
        string status,
        string? tags = null,
        string? receiptRef = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var dbTx = conn.BeginTransaction();

        int? payeeId = null;
        if (!string.IsNullOrWhiteSpace(payeeName))
        {
            using var findPayee = conn.CreateCommand();
            findPayee.Transaction = dbTx;
            findPayee.CommandText = "SELECT Id FROM Payees WHERE Name = @name LIMIT 1;";
            findPayee.Parameters.AddWithValue("@name", payeeName.Trim());
            object? existing = findPayee.ExecuteScalar();

            if (existing != null && existing != DBNull.Value)
            {
                payeeId = Convert.ToInt32(existing);
            }
            else
            {
                using var insertPayee = conn.CreateCommand();
                insertPayee.Transaction = dbTx;
                insertPayee.CommandText = """
                    INSERT INTO Payees (Name, DefaultCategoryId)
                    VALUES (@name, @catId);
                    SELECT last_insert_rowid();
                    """;
                insertPayee.Parameters.AddWithValue("@name", payeeName.Trim());
                insertPayee.Parameters.AddWithValue("@catId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
                payeeId = Convert.ToInt32(insertPayee.ExecuteScalar());
            }
        }

        using var cmd = conn.CreateCommand();
        cmd.Transaction = dbTx;
        cmd.CommandText = """
            UPDATE Transactions
            SET TransactionDate = @date,
                ReferenceNumber = @refNum,
                PayeeId = @payeeId,
                PayeeName = @payeeName,
                CategoryId = CASE WHEN IsSplit = 1 THEN CategoryId ELSE @catId END,
                Memo = @memo,
                Tags = COALESCE(@tags, Tags),
                ReceiptRef = @receiptRef,
                Amount = @amount,
                Status = @status
            WHERE Id = @id;
            """;

        cmd.Parameters.AddWithValue("@date", string.IsNullOrWhiteSpace(transactionDate)
            ? DateTime.Today.ToString("yyyy-MM-dd")
            : transactionDate.Trim());
        cmd.Parameters.AddWithValue("@refNum", string.IsNullOrWhiteSpace(referenceNumber) ? DBNull.Value : referenceNumber.Trim());
        cmd.Parameters.AddWithValue("@payeeId", payeeId.HasValue ? payeeId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@payeeName", payeeName.Trim());
        cmd.Parameters.AddWithValue("@catId", categoryId.HasValue ? categoryId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@memo", string.IsNullOrWhiteSpace(memo) ? DBNull.Value : memo.Trim());
        cmd.Parameters.AddWithValue("@tags", tags is null ? DBNull.Value : NormalizeTagsString(tags));
        cmd.Parameters.AddWithValue("@receiptRef", string.IsNullOrWhiteSpace(receiptRef) ? DBNull.Value : receiptRef.Trim());
        cmd.Parameters.AddWithValue("@amount", signedAmount);
        cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(status) ? TransactionStatus.Uncleared : status.Trim());
        cmd.Parameters.AddWithValue("@id", transactionId);
        cmd.ExecuteNonQuery();

        dbTx.Commit();
    }

    private static string NormalizeTagsString(string? rawTags)
    {
        if (string.IsNullOrWhiteSpace(rawTags))
        {
            return string.Empty;
        }

        var parts = rawTags
            .Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.StartsWith('#') ? p : "#" + p)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(",", parts);
    }

    public void DeleteTransaction(int transactionId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Transactions WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", transactionId);
        cmd.ExecuteNonQuery();
    }

    public int CompleteReconciliationSession(ReconciliationSessionCommitRequest request)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var dbTx = conn.BeginTransaction();

        int reconciledCount = 0;

        foreach (int txId in request.ReconciledTransactionIds)
        {
            using var recCmd = conn.CreateCommand();
            recCmd.Transaction = dbTx;
            recCmd.CommandText = "UPDATE Transactions SET Status = 'Reconciled' WHERE Id = @id AND AccountId = @accId;";
            recCmd.Parameters.AddWithValue("@id", txId);
            recCmd.Parameters.AddWithValue("@accId", request.AccountId);
            reconciledCount += recCmd.ExecuteNonQuery();
        }

        foreach (int txId in request.UncheckedTransactionIds)
        {
            using var uncheckCmd = conn.CreateCommand();
            uncheckCmd.Transaction = dbTx;
            uncheckCmd.CommandText = "UPDATE Transactions SET Status = 'Uncleared' WHERE Id = @id AND AccountId = @accId AND Status != 'Reconciled';";
            uncheckCmd.Parameters.AddWithValue("@id", txId);
            uncheckCmd.Parameters.AddWithValue("@accId", request.AccountId);
            uncheckCmd.ExecuteNonQuery();
        }

        if (request.CreateAdjustmentIfNeeded && request.AdjustmentAmount != 0m)
        {
            using var adjCmd = conn.CreateCommand();
            adjCmd.Transaction = dbTx;
            adjCmd.CommandText = """
                INSERT INTO Transactions (
                    AccountId, TransactionDate, ReferenceNumber, PayeeName,
                    CategoryId, Memo, Amount, Status, IsSplit
                )
                VALUES (
                    @accId, @date, 'ADJ', 'Statement Balance Adjustment',
                    NULL, 'Automated reconciliation variance adjustment', @amount, 'Reconciled', 0
                );
                """;
            adjCmd.Parameters.AddWithValue("@accId", request.AccountId);
            adjCmd.Parameters.AddWithValue("@date", string.IsNullOrWhiteSpace(request.StatementDate)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : request.StatementDate);
            adjCmd.Parameters.AddWithValue("@amount", request.AdjustmentAmount);
            adjCmd.ExecuteNonQuery();
            reconciledCount++;
        }

        using (var histCmd = conn.CreateCommand())
        {
            histCmd.Transaction = dbTx;
            histCmd.CommandText = """
                INSERT INTO ReconciliationHistory (AccountId, StatementDate, StatementEndingBalance, ReconciledCount)
                VALUES (@accId, @date, @balance, @count);
                """;
            histCmd.Parameters.AddWithValue("@accId", request.AccountId);
            histCmd.Parameters.AddWithValue("@date", string.IsNullOrWhiteSpace(request.StatementDate)
                ? DateTime.Today.ToString("yyyy-MM-dd")
                : request.StatementDate);
            histCmd.Parameters.AddWithValue("@balance", request.StatementEndingBalance);
            histCmd.Parameters.AddWithValue("@count", reconciledCount);
            histCmd.ExecuteNonQuery();
        }

        dbTx.Commit();
        return reconciledCount;
    }

    public List<ReconciliationHistoryItem> GetReconciliationHistory(int? accountId = null)
    {
        var items = new List<ReconciliationHistoryItem>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = """
            SELECT
                r.Id,
                r.AccountId,
                a.Name AS AccountName,
                r.StatementDate,
                r.StatementEndingBalance,
                r.ReconciledCount,
                r.CompletedAt
            FROM ReconciliationHistory r
            INNER JOIN Accounts a ON a.Id = r.AccountId
            WHERE (@accId IS NULL OR r.AccountId = @accId)
            ORDER BY r.Id DESC
            LIMIT 20;
            """;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@accId", accountId.HasValue ? accountId.Value : DBNull.Value);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new ReconciliationHistoryItem
            {
                Id = reader.GetInt32(0),
                AccountId = reader.GetInt32(1),
                AccountName = reader.GetString(2),
                StatementDate = reader.GetString(3),
                StatementEndingBalance = reader.GetDecimal(4),
                ReconciledCount = reader.GetInt32(5),
                CompletedAt = reader.GetString(6)
            });
        }

        return items;
    }

    public string CycleTransactionStatus(int transactionId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        string currentStatus = TransactionStatus.Uncleared;
        using (var getCmd = conn.CreateCommand())
        {
            getCmd.CommandText = "SELECT Status FROM Transactions WHERE Id = @id;";
            getCmd.Parameters.AddWithValue("@id", transactionId);
            object? val = getCmd.ExecuteScalar();
            if (val != null && val != DBNull.Value)
            {
                currentStatus = Convert.ToString(val) ?? TransactionStatus.Uncleared;
            }
        }

        string nextStatus = TransactionStatus.NextStatus(currentStatus);

        using (var updateCmd = conn.CreateCommand())
        {
            updateCmd.CommandText = "UPDATE Transactions SET Status = @status WHERE Id = @id;";
            updateCmd.Parameters.AddWithValue("@status", nextStatus);
            updateCmd.Parameters.AddWithValue("@id", transactionId);
            updateCmd.ExecuteNonQuery();
        }

        return nextStatus;
    }

    public int ReconcileClearedTransactions(int? accountId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Transactions
            SET Status = 'Reconciled'
            WHERE Status = 'Cleared'
              AND (@accountId IS NULL OR AccountId = @accountId);
            """;
        cmd.Parameters.AddWithValue("@accountId", accountId.HasValue ? accountId.Value : DBNull.Value);
        return cmd.ExecuteNonQuery();
    }

    public DashboardMetrics GetDashboardMetrics()
    {
        var accounts = GetAccountsWithBalances();
        var transactions = GetTransactions();
        var categories = GetCategories();
        var categoriesById = categories.ToDictionary(c => c.Id);

        decimal totalAssets = accounts.Where(a => a.CurrentBalance > 0).Sum(a => a.CurrentBalance);
        decimal totalLiabilities = accounts.Where(a => a.CurrentBalance < 0).Sum(a => Math.Abs(a.CurrentBalance));
        decimal netWorth = totalAssets - totalLiabilities;
        decimal liquidCash = accounts
            .Where(a => string.Equals(a.AccountGroup, "Banking", StringComparison.OrdinalIgnoreCase))
            .Sum(a => a.CurrentBalance);

        // Current month Income & Expense totals (excluding Transfer category type)
        string currentMonthKey = CurrentMonthKey();
        decimal monthlyIncome = 0m;
        decimal monthlyExpenses = 0m;
        var spendByCategory = new Dictionary<int, decimal>();

        foreach (var tx in transactions.Where(t => !string.IsNullOrEmpty(t.TransactionDate) && t.TransactionDate.StartsWith(currentMonthKey, StringComparison.Ordinal)))
        {
            if (tx.IsSplit && tx.Splits.Count > 0)
            {
                foreach (var s in tx.Splits)
                {
                    AccumulateCategoryMetric(s.CategoryId, s.Amount, categoriesById, ref monthlyIncome, ref monthlyExpenses, spendByCategory);
                }
            }
            else
            {
                AccumulateCategoryMetric(tx.CategoryId, tx.Amount, categoriesById, ref monthlyIncome, ref monthlyExpenses, spendByCategory);
            }
        }

        decimal totalCategorizedSpend = spendByCategory.Values.Sum();
        var breakdown = spendByCategory
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp =>
            {
                categoriesById.TryGetValue(kvp.Key, out var cat);
                return new CategorySpendingItem
                {
                    CategoryName = cat?.FullPath ?? "Uncategorized",
                    ColorHex = cat?.ColorHex ?? "#38BDF8",
                    Spent = kvp.Value,
                    Budget = cat?.MonthlyBudget ?? 0m,
                    PercentageOfTotal = totalCategorizedSpend > 0
                        ? Math.Round(kvp.Value / totalCategorizedSpend * 100m, 1)
                        : 0m
                };
            })
            .ToList();

        var cashFlow = new List<CashFlowPoint>();
        var today = DateTime.Today;
        for (int back = 5; back >= 0; back--)
        {
            var mStart = new DateTime(today.Year, today.Month, 1).AddMonths(-back);
            string mKey = mStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            decimal mInc = 0m;
            decimal mExp = 0m;
            var dummySpend = new Dictionary<int, decimal>();
            foreach (var tx in transactions.Where(t => !string.IsNullOrEmpty(t.TransactionDate) && t.TransactionDate.StartsWith(mKey, StringComparison.Ordinal)))
            {
                if (tx.IsSplit && tx.Splits.Count > 0)
                {
                    foreach (var s in tx.Splits)
                    {
                        AccumulateCategoryMetric(s.CategoryId, s.Amount, categoriesById, ref mInc, ref mExp, dummySpend);
                    }
                }
                else
                {
                    AccumulateCategoryMetric(tx.CategoryId, tx.Amount, categoriesById, ref mInc, ref mExp, dummySpend);
                }
            }

            cashFlow.Add(new CashFlowPoint
            {
                Label = mStart.ToString("MMM", CultureInfo.InvariantCulture),
                Income = Math.Round(mInc, 2),
                Expense = Math.Round(mExp, 2)
            });
        }

        return new DashboardMetrics
        {
            NetWorth = netWorth,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            LiquidCash = liquidCash,
            MonthlyIncome = monthlyIncome,
            MonthlyExpenses = monthlyExpenses,
            CategoryBreakdown = breakdown,
            MonthlyCashFlow = cashFlow
        };
    }

    private static void AccumulateCategoryMetric(
        int? categoryId,
        decimal amount,
        Dictionary<int, Category> categoriesById,
        ref decimal monthlyIncome,
        ref decimal monthlyExpenses,
        Dictionary<int, decimal> spendByCategory)
    {
        Category? cat = null;
        if (categoryId.HasValue)
        {
            categoriesById.TryGetValue(categoryId.Value, out cat);
        }

        if (string.Equals(cat?.CategoryType, "Transfer", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (amount > 0)
        {
            monthlyIncome += amount;
        }
        else if (amount < 0)
        {
            decimal absSpend = Math.Abs(amount);
            monthlyExpenses += absSpend;
            int key = categoryId ?? 0;
            spendByCategory[key] = spendByCategory.GetValueOrDefault(key) + absSpend;
        }
    }

    public MonthlyBudgetSummary GetMonthlyBudgetSummary(string? monthKey = null)
    {
        monthKey = string.IsNullOrWhiteSpace(monthKey) ? CurrentMonthKey() : monthKey.Trim();
        var accounts = GetAccountsWithBalances();
        var categories = GetCategories();
        var categoriesById = categories.ToDictionary(c => c.Id);
        var transactions = GetTransactions();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var envelopeRows = new Dictionary<int, (decimal Rollover, decimal Assigned)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT CategoryId, RolloverAmount, AssignedAmount
                FROM BudgetEnvelopes
                WHERE MonthKey = @monthKey;
                """;
            cmd.Parameters.AddWithValue("@monthKey", monthKey);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                envelopeRows[reader.GetInt32(0)] = (reader.GetDecimal(1), reader.GetDecimal(2));
            }
        }

        // Compute activity (net outflows/inflows) per category for the target month
        var activityByCategory = new Dictionary<int, decimal>();
        decimal monthIncome = 0m;

        foreach (var tx in transactions)
        {
            if (string.IsNullOrWhiteSpace(tx.TransactionDate) ||
                !tx.TransactionDate.StartsWith(monthKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (tx.IsSplit && tx.Splits.Count > 0)
            {
                foreach (var s in tx.Splits)
                {
                    if (!s.CategoryId.HasValue)
                    {
                        continue;
                    }

                    if (categoriesById.TryGetValue(s.CategoryId.Value, out var splitCat))
                    {
                        if (string.Equals(splitCat.CategoryType, "Income", StringComparison.OrdinalIgnoreCase) && s.Amount > 0)
                        {
                            monthIncome += s.Amount;
                        }
                        else if (string.Equals(splitCat.CategoryType, "Expense", StringComparison.OrdinalIgnoreCase))
                        {
                            activityByCategory[splitCat.Id] = activityByCategory.GetValueOrDefault(splitCat.Id) + Math.Max(0m, -s.Amount);
                        }
                    }
                }
            }
            else if (tx.CategoryId.HasValue && categoriesById.TryGetValue(tx.CategoryId.Value, out var cat))
            {
                if (string.Equals(cat.CategoryType, "Income", StringComparison.OrdinalIgnoreCase) && tx.Amount > 0)
                {
                    monthIncome += tx.Amount;
                }
                else if (string.Equals(cat.CategoryType, "Expense", StringComparison.OrdinalIgnoreCase))
                {
                    activityByCategory[cat.Id] = activityByCategory.GetValueOrDefault(cat.Id) + Math.Max(0m, -tx.Amount);
                }
            }
        }

        // Leaf expense categories (categories that have ParentId != null and CategoryType == Expense)
        var leafExpenseCategories = categories
            .Where(c => string.Equals(c.CategoryType, "Expense", StringComparison.OrdinalIgnoreCase) && c.ParentId.HasValue)
            .OrderBy(c => c.ParentId)
            .ThenBy(c => c.Id)
            .ToList();

        var envelopes = new List<BudgetEnvelopeItem>();
        foreach (var cat in leafExpenseCategories)
        {
            string parentName = cat.ParentId.HasValue && categoriesById.TryGetValue(cat.ParentId.Value, out var parentCat)
                ? parentCat.Name
                : "General Expenses";

            var (rollover, assigned) = envelopeRows.TryGetValue(cat.Id, out var row)
                ? row
                : (0m, cat.MonthlyBudget);

            decimal activity = activityByCategory.GetValueOrDefault(cat.Id, 0m);

            envelopes.Add(new BudgetEnvelopeItem
            {
                CategoryId = cat.Id,
                CategoryName = cat.Name,
                ParentGroupName = parentName,
                ColorHex = cat.ColorHex,
                MonthlyTarget = cat.MonthlyBudget,
                RolloverAmount = rollover,
                AssignedAmount = assigned,
                ActivityAmount = activity
            });
        }

        decimal totalRollover = envelopes.Sum(e => e.RolloverAmount);
        decimal totalAssigned = envelopes.Sum(e => e.AssignedAmount);
        decimal totalActivity = envelopes.Sum(e => e.ActivityAmount);
        decimal totalAvailable = envelopes.Sum(e => e.AvailableAmount);
        decimal totalUnderfunded = envelopes
            .Where(e => e.AssignedAmount < e.MonthlyTarget)
            .Sum(e => e.MonthlyTarget - e.AssignedAmount);

        // Total assignable pool = Banking cash at month start (opening balances + prior transactions) + this month's income
        string monthStartStr = $"{monthKey}-01";
        var bankingAccountIds = accounts
            .Where(a => string.Equals(a.AccountGroup, "Banking", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Id)
            .ToHashSet();

        decimal bankingStartCash = accounts
            .Where(a => bankingAccountIds.Contains(a.Id))
            .Sum(a => a.OpeningBalance)
            + transactions
                .Where(t => bankingAccountIds.Contains(t.AccountId) && string.CompareOrdinal(t.TransactionDate, monthStartStr) < 0)
                .Sum(t => t.Amount);

        decimal inflowPool = Math.Max(0m, bankingStartCash + monthIncome);
        decimal readyToAssign = inflowPool - totalRollover - totalAssigned;

        decimal liquidCash = accounts
            .Where(a => bankingAccountIds.Contains(a.Id))
            .Sum(a => a.CurrentBalance);
        string thirtyDaysAgo = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        decimal outflow30Days = transactions
            .Where(t => t.Amount < 0m && string.CompareOrdinal(t.TransactionDate, thirtyDaysAgo) >= 0)
            .Sum(t => Math.Abs(t.Amount));
        decimal avgDailyOutflow = outflow30Days / 30m;
        int ageOfMoneyDays = (liquidCash > 0m && avgDailyOutflow > 0m)
            ? Math.Clamp((int)Math.Round(liquidCash / avgDailyOutflow), 0, 999)
            : 0;

        return new MonthlyBudgetSummary
        {
            MonthKey = monthKey,
            MonthDisplay = FormatMonthDisplay(monthKey),
            TotalInflowPool = Math.Round(inflowPool, 2),
            TotalRollover = totalRollover,
            TotalAssigned = totalAssigned,
            TotalActivitySpent = totalActivity,
            TotalAvailableInEnvelopes = totalAvailable,
            ReadyToAssign = Math.Round(readyToAssign, 2),
            TotalUnderfunded = totalUnderfunded,
            AgeOfMoneyDays = ageOfMoneyDays,
            Envelopes = envelopes
        };
    }

    public void UpdateEnvelopeAssignment(
        string monthKey,
        int categoryId,
        decimal assignedAmount,
        decimal? monthlyTarget = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var dbTx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = dbTx;
            cmd.CommandText = """
                INSERT INTO BudgetEnvelopes (MonthKey, CategoryId, RolloverAmount, AssignedAmount)
                VALUES (@monthKey, @catId, 0, @assigned)
                ON CONFLICT(MonthKey, CategoryId)
                DO UPDATE SET AssignedAmount = excluded.AssignedAmount;
                """;
            cmd.Parameters.AddWithValue("@monthKey", string.IsNullOrWhiteSpace(monthKey) ? CurrentMonthKey() : monthKey);
            cmd.Parameters.AddWithValue("@catId", categoryId);
            cmd.Parameters.AddWithValue("@assigned", Math.Max(0m, assignedAmount));
            cmd.ExecuteNonQuery();
        }

        if (monthlyTarget.HasValue)
        {
            using var targetCmd = conn.CreateCommand();
            targetCmd.Transaction = dbTx;
            targetCmd.CommandText = "UPDATE Categories SET MonthlyBudget = @target WHERE Id = @catId;";
            targetCmd.Parameters.AddWithValue("@target", Math.Max(0m, monthlyTarget.Value));
            targetCmd.Parameters.AddWithValue("@catId", categoryId);
            targetCmd.ExecuteNonQuery();
        }

        dbTx.Commit();
    }

    public int AutoFundUnderfundedEnvelopes(string? monthKey = null)
    {
        monthKey = string.IsNullOrWhiteSpace(monthKey) ? CurrentMonthKey() : monthKey.Trim();
        var summary = GetMonthlyBudgetSummary(monthKey);
        int fundedCount = 0;

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var dbTx = conn.BeginTransaction();

        foreach (var env in summary.Envelopes.Where(e => e.AssignedAmount < e.MonthlyTarget))
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = dbTx;
            cmd.CommandText = """
                INSERT INTO BudgetEnvelopes (MonthKey, CategoryId, RolloverAmount, AssignedAmount)
                VALUES (@monthKey, @catId, @rollover, @target)
                ON CONFLICT(MonthKey, CategoryId)
                DO UPDATE SET AssignedAmount = @target;
                """;
            cmd.Parameters.AddWithValue("@monthKey", monthKey);
            cmd.Parameters.AddWithValue("@catId", env.CategoryId);
            cmd.Parameters.AddWithValue("@rollover", env.RolloverAmount);
            cmd.Parameters.AddWithValue("@target", env.MonthlyTarget);
            cmd.ExecuteNonQuery();
            fundedCount++;
        }

        dbTx.Commit();
        return fundedCount;
    }

    public void MoveEnvelopeFunds(string monthKey, int fromCategoryId, int toCategoryId, decimal amount)
    {
        if (amount <= 0m || fromCategoryId == toCategoryId)
        {
            return;
        }

        var summary = GetMonthlyBudgetSummary(monthKey);
        var fromEnv = summary.Envelopes.FirstOrDefault(e => e.CategoryId == fromCategoryId);
        var toEnv = summary.Envelopes.FirstOrDefault(e => e.CategoryId == toCategoryId);
        if (fromEnv == null || toEnv == null)
        {
            return;
        }

        decimal newFromAssigned = Math.Max(0m, fromEnv.AssignedAmount - amount);
        decimal actualMoved = fromEnv.AssignedAmount - newFromAssigned;
        decimal newToAssigned = toEnv.AssignedAmount + actualMoved;

        UpdateEnvelopeAssignment(monthKey, fromCategoryId, newFromAssigned);
        UpdateEnvelopeAssignment(monthKey, toCategoryId, newToAssigned);
    }

    private static string FormatMonthDisplay(string monthKey)
    {
        if (DateTime.TryParse($"{monthKey}-01", out var dt))
        {
            return dt.ToString("MMMM yyyy");
        }

        return monthKey;
    }

    public InvestmentPortfolioSummary GetInvestmentPortfolioSummary(int? accountId = null)
    {
        var holdings = new List<InvestmentHolding>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = """
            SELECT
                h.Id,
                h.AccountId,
                a.Name AS AccountName,
                a.Institution,
                h.TickerSymbol,
                h.SecurityName,
                h.AssetClass,
                h.Shares,
                h.AverageCostPerShare,
                h.CurrentPrice,
                h.DayChangePercent,
                h.DividendYieldPercent,
                h.ExpenseRatioPercent,
                h.LastUpdated
            FROM InvestmentHoldings h
            INNER JOIN Accounts a ON a.Id = h.AccountId
            WHERE (@accId IS NULL OR h.AccountId = @accId)
            ORDER BY h.AccountId, (h.Shares * h.CurrentPrice) DESC;
            """;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@accId", accountId.HasValue ? accountId.Value : DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                holdings.Add(new InvestmentHolding
                {
                    Id = reader.GetInt32(0),
                    AccountId = reader.GetInt32(1),
                    AccountName = reader.GetString(2),
                    Institution = reader.GetString(3),
                    TickerSymbol = reader.GetString(4),
                    SecurityName = reader.GetString(5),
                    AssetClass = reader.GetString(6),
                    Shares = reader.GetDecimal(7),
                    AverageCostPerShare = reader.GetDecimal(8),
                    CurrentPrice = reader.GetDecimal(9),
                    DayChangePercent = reader.GetDecimal(10),
                    DividendYieldPercent = reader.GetDecimal(11),
                    ExpenseRatioPercent = reader.GetDecimal(12),
                    LastUpdated = reader.GetString(13)
                });
            }
        }

        decimal totalMarketValue = holdings.Sum(h => h.MarketValue);
        decimal totalCostBasis = holdings.Sum(h => h.CostBasis);
        decimal totalDayChangeDollar = holdings.Sum(h => h.DayChangeDollar);
        decimal totalAnnualDividends = holdings.Sum(h => h.EstimatedAnnualIncome);

        foreach (var h in holdings)
        {
            h.PortfolioWeightPercent = totalMarketValue > 0m
                ? Math.Round(h.MarketValue / totalMarketValue * 100m, 1)
                : 0m;
        }

        decimal weightedYield = totalMarketValue > 0m
            ? Math.Round(totalAnnualDividends / totalMarketValue * 100m, 2)
            : 0m;

        decimal weightedExpenseRatio = totalMarketValue > 0m
            ? Math.Round(holdings.Sum(h => h.MarketValue * h.ExpenseRatioPercent) / totalMarketValue, 3)
            : 0m;

        var targetPolicy = new Dictionary<string, (decimal TargetPct, string ColorHex)>(StringComparer.OrdinalIgnoreCase)
        {
            ["US Equity"] = (60.0m, "#38BDF8"),
            ["International Equity"] = (18.0m, "#818CF8"),
            ["Fixed Income / Bonds"] = (15.0m, "#10B981"),
            ["Real Estate / REIT"] = (5.0m, "#F59E0B"),
            ["Cash & Money Market"] = (2.0m, "#94A3B8")
        };

        var allocation = new List<AssetAllocationItem>();
        foreach (var kvp in targetPolicy)
        {
            decimal classValue = holdings
                .Where(h => string.Equals(h.AssetClass, kvp.Key, StringComparison.OrdinalIgnoreCase))
                .Sum(h => h.MarketValue);

            decimal weightPct = totalMarketValue > 0m
                ? Math.Round(classValue / totalMarketValue * 100m, 1)
                : 0m;

            allocation.Add(new AssetAllocationItem
            {
                AssetClass = kvp.Key,
                ColorHex = kvp.Value.ColorHex,
                MarketValue = classValue,
                WeightPercent = weightPct,
                TargetWeightPercent = kvp.Value.TargetPct
            });
        }

        return new InvestmentPortfolioSummary
        {
            TotalMarketValue = totalMarketValue,
            TotalCostBasis = totalCostBasis,
            TotalDayChangeDollar = totalDayChangeDollar,
            TotalDayChangePercent = totalMarketValue > 0m
                ? Math.Round(totalDayChangeDollar / totalMarketValue * 100m, 2)
                : 0m,
            EstimatedAnnualDividends = totalAnnualDividends,
            WeightedDividendYieldPercent = weightedYield,
            WeightedExpenseRatioPercent = weightedExpenseRatio,
            Holdings = holdings,
            AssetAllocation = allocation
        };
    }

    public void UpdateHoldingPosition(int holdingId, decimal shares, decimal averageCostPerShare, decimal currentPrice)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                UPDATE InvestmentHoldings
                SET Shares = @shares,
                    AverageCostPerShare = @avgCost,
                    CurrentPrice = @price,
                    LastUpdated = datetime('now')
                WHERE Id = @id;
                """;
            cmd.Parameters.AddWithValue("@shares", Math.Max(0m, shares));
            cmd.Parameters.AddWithValue("@avgCost", Math.Max(0m, averageCostPerShare));
            cmd.Parameters.AddWithValue("@price", Math.Max(0m, currentPrice));
            cmd.Parameters.AddWithValue("@id", holdingId);
            cmd.ExecuteNonQuery();
        }

        SyncInvestmentAccountBalances(conn);
    }

    public InvestmentHolding CreateInvestmentHolding(InvestmentHolding holding)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        int newId;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO InvestmentHoldings (
                    AccountId, TickerSymbol, SecurityName, AssetClass,
                    Shares, AverageCostPerShare, CurrentPrice,
                    DayChangePercent, DividendYieldPercent, ExpenseRatioPercent
                )
                VALUES (
                    @accId, @ticker, @name, @assetClass,
                    @shares, @avgCost, @price,
                    @dayChg, @divYield, @expRatio
                );
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("@accId", EnsureValidAccountId(conn, holding.AccountId, "Investment", "Brokerage"));
            cmd.Parameters.AddWithValue("@ticker", holding.TickerSymbol.Trim().ToUpperInvariant());
            cmd.Parameters.AddWithValue("@name", holding.SecurityName.Trim());
            cmd.Parameters.AddWithValue("@assetClass", string.IsNullOrWhiteSpace(holding.AssetClass) ? "US Equity" : holding.AssetClass.Trim());
            cmd.Parameters.AddWithValue("@shares", Math.Max(0m, holding.Shares));
            cmd.Parameters.AddWithValue("@avgCost", Math.Max(0m, holding.AverageCostPerShare));
            cmd.Parameters.AddWithValue("@price", Math.Max(0m, holding.CurrentPrice));
            cmd.Parameters.AddWithValue("@dayChg", holding.DayChangePercent);
            cmd.Parameters.AddWithValue("@divYield", holding.DividendYieldPercent);
            cmd.Parameters.AddWithValue("@expRatio", holding.ExpenseRatioPercent);
            newId = Convert.ToInt32(cmd.ExecuteScalar());
        }

        SyncInvestmentAccountBalances(conn);
        return GetInvestmentPortfolioSummary().Holdings.First(h => h.Id == newId);
    }

    public void DeleteInvestmentHolding(int holdingId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM InvestmentHoldings WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", holdingId);
            cmd.ExecuteNonQuery();
        }

        SyncInvestmentAccountBalances(conn);
    }

    public void SimulateMarketPulse()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                UPDATE InvestmentHoldings
                SET CurrentPrice = CASE
                        WHEN AssetClass = 'Cash & Money Market' THEN 1.00
                        WHEN AssetClass = 'Fixed Income / Bonds' THEN ROUND(CurrentPrice * 1.0035, 2)
                        ELSE ROUND(CurrentPrice * 1.0115, 2)
                    END,
                    DayChangePercent = CASE
                        WHEN AssetClass = 'Cash & Money Market' THEN 0.01
                        WHEN AssetClass = 'Fixed Income / Bonds' THEN 0.35
                        ELSE 1.15
                    END,
                    LastUpdated = datetime('now');
                """;
            cmd.ExecuteNonQuery();
        }

        SyncInvestmentAccountBalances(conn);
    }

    private static void SyncInvestmentAccountBalances(SqliteConnection conn)
    {
        using var syncCmd = conn.CreateCommand();
        syncCmd.CommandText = """
            UPDATE Accounts
            SET OpeningBalance = COALESCE(
                (SELECT ROUND(SUM(Shares * CurrentPrice), 2)
                 FROM InvestmentHoldings
                 WHERE InvestmentHoldings.AccountId = Accounts.Id),
                OpeningBalance)
            WHERE AccountGroup = 'Investments';
            """;
        syncCmd.ExecuteNonQuery();
    }

    public RecurringCashFlowSummary GetRecurringCashFlowSummary()
    {
        var accounts = GetAccountsWithBalances();
        var categories = GetCategories().ToDictionary(c => c.Id);
        var schedules = new List<RecurringSchedule>();

        var referenceDate = DateTime.Today;

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = """
            SELECT
                s.Id,
                s.AccountId,
                a.Name AS AccountName,
                s.PayeeName,
                s.CategoryId,
                s.Frequency,
                s.NextDueDate,
                s.Amount,
                s.IsAutoPay,
                s.IsActive,
                s.Memo
            FROM RecurringSchedules s
            INNER JOIN Accounts a ON a.Id = s.AccountId
            WHERE s.IsActive = 1
            ORDER BY s.NextDueDate ASC, s.Id ASC;
            """;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int? catId = reader.IsDBNull(4) ? null : reader.GetInt32(4);
                string catName = catId.HasValue && categories.TryGetValue(catId.Value, out var cat)
                    ? cat.FullPath
                    : "Uncategorized";

                string nextDueStr = reader.GetString(6);
                int daysUntil = DateTime.TryParse(nextDueStr, out var dueDt)
                    ? (int)(dueDt.Date - referenceDate.Date).TotalDays
                    : 0;

                schedules.Add(new RecurringSchedule
                {
                    Id = reader.GetInt32(0),
                    AccountId = reader.GetInt32(1),
                    AccountName = reader.GetString(2),
                    PayeeName = reader.GetString(3),
                    CategoryId = catId,
                    CategoryName = catName,
                    Frequency = reader.GetString(5),
                    NextDueDate = nextDueStr,
                    Amount = reader.GetDecimal(7),
                    IsAutoPay = reader.GetInt32(8) != 0,
                    IsActive = reader.GetInt32(9) != 0,
                    Memo = reader.IsDBNull(10) ? null : reader.GetString(10),
                    DaysUntilDue = daysUntil
                });
            }
        }

        var checkingAccount = accounts.FirstOrDefault(a => string.Equals(a.AccountType, "Checking", StringComparison.OrdinalIgnoreCase))
            ?? accounts.FirstOrDefault(a => string.Equals(a.AccountGroup, "Banking", StringComparison.OrdinalIgnoreCase));
        int checkingAccountId = checkingAccount?.Id ?? 0;
        decimal checkingBalance = checkingAccount?.CurrentBalance ?? 0m;
        decimal liquidBalance = accounts
            .Where(a => string.Equals(a.AccountGroup, "Banking", StringComparison.OrdinalIgnoreCase))
            .Sum(a => a.CurrentBalance);

        decimal runningChecking = checkingBalance;
        decimal runningLiquid = liquidBalance;
        decimal lowestChecking = checkingBalance;
        decimal inflows30 = 0m;
        decimal outflows30 = 0m;

        var timeline = new List<CashFlowProjectionPoint>();
        foreach (var s in schedules.Where(x => x.DaysUntilDue <= 35))
        {
            if (s.Amount >= 0m)
            {
                inflows30 += s.Amount;
            }
            else
            {
                outflows30 += Math.Abs(s.Amount);
            }

            if (checkingAccountId > 0 && s.AccountId == checkingAccountId)
            {
                runningChecking += s.Amount;
                if (runningChecking < lowestChecking)
                {
                    lowestChecking = runningChecking;
                }
            }

            runningLiquid += s.Amount;

            timeline.Add(new CashFlowProjectionPoint
            {
                ScheduleId = s.Id,
                Date = s.NextDueDate,
                PayeeName = s.PayeeName,
                AccountName = s.AccountName,
                CategoryName = s.CategoryName,
                Frequency = s.Frequency,
                DeltaAmount = s.Amount,
                ProjectedCheckingBalance = runningChecking,
                ProjectedLiquidBalance = runningLiquid
            });
        }

        decimal monthlyRecurringBills = schedules
            .Where(s => s.Amount < 0m)
            .Sum(s => Math.Abs(s.MonthlyEquivalentAmount));

        int dueWithin7Days = schedules.Count(s => s.DaysUntilDue <= 7);

        return new RecurringCashFlowSummary
        {
            CurrentCheckingBalance = checkingBalance,
            CurrentLiquidBalance = liquidBalance,
            Next30DaysInflows = inflows30,
            Next30DaysOutflows = outflows30,
            Projected30DayCheckingBalance = runningChecking,
            LowestProjectedCheckingBalance = lowestChecking,
            MonthlyRecurringBillsTotal = monthlyRecurringBills,
            DueWithin7DaysCount = dueWithin7Days,
            Schedules = schedules,
            ProjectionTimeline = timeline
        };
    }

    public Transaction? PostRecurringScheduleToRegister(int scheduleId)
    {
        var summary = GetRecurringCashFlowSummary();
        var sched = summary.Schedules.FirstOrDefault(s => s.Id == scheduleId);
        if (sched == null)
        {
            return null;
        }

        var createdTx = CreateTransaction(
            accountId: sched.AccountId,
            transactionDate: sched.NextDueDate,
            payeeName: sched.PayeeName,
            categoryId: sched.CategoryId,
            memo: string.IsNullOrWhiteSpace(sched.Memo)
                ? $"Scheduled {sched.Frequency} transaction"
                : sched.Memo,
            signedAmount: sched.Amount,
            status: TransactionStatus.Cleared,
            referenceNumber: "SCHED");

        AdvanceRecurringScheduleDueDate(scheduleId, sched.NextDueDate, sched.Frequency);
        return createdTx;
    }

    public string SkipRecurringScheduleCycle(int scheduleId)
    {
        var summary = GetRecurringCashFlowSummary();
        var sched = summary.Schedules.FirstOrDefault(s => s.Id == scheduleId);
        if (sched == null)
        {
            return string.Empty;
        }

        return AdvanceRecurringScheduleDueDate(scheduleId, sched.NextDueDate, sched.Frequency);
    }

    private string AdvanceRecurringScheduleDueDate(int scheduleId, string currentDueDate, string frequency)
    {
        if (!DateTime.TryParse(currentDueDate, out var dt))
        {
            dt = DateTime.Today;
        }

        DateTime nextDt = frequency switch
        {
            "Weekly" => dt.AddDays(7),
            "Bi-Weekly" => dt.AddDays(14),
            "Quarterly" => dt.AddMonths(3),
            "Annual" => dt.AddYears(1),
            _ => dt.AddMonths(1)
        };

        string nextDateStr = nextDt.ToString("yyyy-MM-dd");

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE RecurringSchedules SET NextDueDate = @nextDate WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@nextDate", nextDateStr);
        cmd.Parameters.AddWithValue("@id", scheduleId);
        cmd.ExecuteNonQuery();

        return nextDateStr;
    }

    public RecurringSchedule CreateRecurringSchedule(RecurringSchedule schedule)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO RecurringSchedules (
                AccountId, PayeeName, CategoryId, Frequency,
                NextDueDate, Amount, IsAutoPay, IsActive, Memo
            )
            VALUES (
                @accId, @payee, @catId, @freq,
                @dueDate, @amount, @autoPay, 1, @memo
            );
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@accId", EnsureValidAccountId(conn, schedule.AccountId, "Checking", "Primary Checking"));
        cmd.Parameters.AddWithValue("@payee", schedule.PayeeName.Trim());
        cmd.Parameters.AddWithValue("@catId", schedule.CategoryId.HasValue ? schedule.CategoryId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@freq", string.IsNullOrWhiteSpace(schedule.Frequency) ? "Monthly" : schedule.Frequency.Trim());
        cmd.Parameters.AddWithValue("@dueDate", string.IsNullOrWhiteSpace(schedule.NextDueDate) ? DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : schedule.NextDueDate.Trim());
        cmd.Parameters.AddWithValue("@amount", schedule.Amount);
        cmd.Parameters.AddWithValue("@autoPay", schedule.IsAutoPay ? 1 : 0);
        cmd.Parameters.AddWithValue("@memo", string.IsNullOrWhiteSpace(schedule.Memo) ? DBNull.Value : schedule.Memo.Trim());

        int newId = Convert.ToInt32(cmd.ExecuteScalar());
        return GetRecurringCashFlowSummary().Schedules.First(s => s.Id == newId);
    }

    public void DeleteRecurringSchedule(int scheduleId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM RecurringSchedules WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", scheduleId);
        cmd.ExecuteNonQuery();
    }

    public ExecutiveReportSummary GetExecutiveReportSummary()
    {
        var accounts = GetAccountsWithBalances();
        var categories = GetCategories().ToDictionary(c => c.Id);
        var transactions = GetTransactions();

        decimal totalAssets = accounts.Where(a => a.CurrentBalance >= 0m).Sum(a => a.CurrentBalance);
        decimal totalLiabilities = accounts.Where(a => a.CurrentBalance < 0m).Sum(a => Math.Abs(a.CurrentBalance));
        decimal netWorth = totalAssets - totalLiabilities;
        decimal debtToAssetRatio = totalAssets > 0m
            ? Math.Round(totalLiabilities / totalAssets * 100m, 2)
            : 0m;

        var incomeMap = new Dictionary<int, (string Group, string Name, string Color, decimal Amount, int Count)>();
        var expenseMap = new Dictionary<int, (string Group, string Name, string Color, decimal Amount, int Count)>();
        var payeeMap = new Dictionary<string, (string Category, decimal Amount, int Count)>(StringComparer.OrdinalIgnoreCase);
        var stewardshipReceipts = new List<CharitableStewardshipItem>();

        decimal grossIncome = 0m;
        decimal operatingExpenses = 0m;
        decimal debtAndTransfers = 0m;
        decimal charitableGiving = 0m;

        void AccumulateCategoryLine(Transaction tx, int? catId, decimal signedAmount, string? lineMemo)
        {
            string groupName = "Uncategorized";
            string catName = "Uncategorized";
            string colorHex = "#94A3B8";
            int keyId = catId ?? 0;

            if (catId.HasValue && categories.TryGetValue(catId.Value, out var cat))
            {
                catName = cat.FullPath;
                colorHex = cat.ColorHex;
                if (cat.ParentId.HasValue && categories.TryGetValue(cat.ParentId.Value, out var parent))
                {
                    groupName = parent.Name;
                }
                else
                {
                    groupName = cat.Name;
                }
            }

            if (signedAmount > 0m)
            {
                grossIncome += signedAmount;
                if (incomeMap.TryGetValue(keyId, out var existingInc))
                {
                    incomeMap[keyId] = (groupName, catName, colorHex, existingInc.Amount + signedAmount, existingInc.Count + 1);
                }
                else
                {
                    incomeMap[keyId] = (groupName, catName, colorHex, signedAmount, 1);
                }
                return;
            }

            decimal absAmt = Math.Abs(signedAmount);
            bool isTransferOrDebt = groupName.Contains("Transfer", StringComparison.OrdinalIgnoreCase)
                || catName.Contains("Transfer", StringComparison.OrdinalIgnoreCase)
                || catName.Contains("Credit Card Payment", StringComparison.OrdinalIgnoreCase);

            if (isTransferOrDebt)
            {
                debtAndTransfers += absAmt;
                return;
            }

            operatingExpenses += absAmt;

            if (expenseMap.TryGetValue(keyId, out var existingExp))
            {
                expenseMap[keyId] = (groupName, catName, colorHex, existingExp.Amount + absAmt, existingExp.Count + 1);
            }
            else
            {
                expenseMap[keyId] = (groupName, catName, colorHex, absAmt, 1);
            }

            if (payeeMap.TryGetValue(tx.PayeeName, out var existingPayee))
            {
                payeeMap[tx.PayeeName] = (catName, existingPayee.Amount + absAmt, existingPayee.Count + 1);
            }
            else
            {
                payeeMap[tx.PayeeName] = (catName, absAmt, 1);
            }

            if (groupName.Contains("Giving", StringComparison.OrdinalIgnoreCase)
                || catName.Contains("Giving", StringComparison.OrdinalIgnoreCase)
                || catName.Contains("Tithe", StringComparison.OrdinalIgnoreCase)
                || catName.Contains("Charity", StringComparison.OrdinalIgnoreCase))
            {
                charitableGiving += absAmt;
                stewardshipReceipts.Add(new CharitableStewardshipItem
                {
                    TransactionId = tx.Id,
                    TransactionDate = tx.TransactionDate,
                    PayeeName = tx.PayeeName,
                    CategoryName = catName,
                    AccountName = tx.AccountName,
                    ReferenceNumber = tx.ReferenceNumber ?? "RECEIPT",
                    Memo = lineMemo ?? tx.Memo ?? "Charitable stewardship contribution",
                    Status = tx.Status.ToString(),
                    Amount = absAmt
                });
            }
        }

        string ytdPrefix = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture) + "-";
        foreach (var tx in transactions.Where(t => !string.IsNullOrEmpty(t.TransactionDate) && t.TransactionDate.StartsWith(ytdPrefix, StringComparison.Ordinal)))
        {
            if (tx.IsSplit && tx.Splits.Count > 0)
            {
                foreach (var sp in tx.Splits)
                {
                    AccumulateCategoryLine(tx, sp.CategoryId, sp.Amount, sp.Memo);
                }
            }
            else
            {
                AccumulateCategoryLine(tx, tx.CategoryId, tx.Amount, tx.Memo);
            }
        }

        decimal netSurplus = grossIncome - operatingExpenses;
        decimal savingsRate = grossIncome > 0m
            ? Math.Round(netSurplus / grossIncome * 100m, 1)
            : 0m;
        decimal givingPct = grossIncome > 0m
            ? Math.Round(charitableGiving / grossIncome * 100m, 1)
            : 0m;

        var incomeList = incomeMap.Values
            .OrderByDescending(x => x.Amount)
            .Select(x => new CategoryFinancialBreakdownItem
            {
                GroupName = x.Group,
                CategoryName = x.Name,
                ColorHex = x.Color,
                Amount = x.Amount,
                TransactionCount = x.Count,
                SharePercent = grossIncome > 0m ? Math.Round(x.Amount / grossIncome * 100m, 1) : 0m
            })
            .ToList();

        var expenseList = expenseMap.Values
            .OrderByDescending(x => x.Amount)
            .Select(x => new CategoryFinancialBreakdownItem
            {
                GroupName = x.Group,
                CategoryName = x.Name,
                ColorHex = x.Color,
                Amount = x.Amount,
                TransactionCount = x.Count,
                SharePercent = operatingExpenses > 0m ? Math.Round(x.Amount / operatingExpenses * 100m, 1) : 0m
            })
            .ToList();

        var topPayees = payeeMap
            .OrderByDescending(kv => kv.Value.Amount)
            .Take(8)
            .Select(kv => new PayeeBreakdownItem
            {
                PayeeName = kv.Key,
                CategoryName = kv.Value.Category,
                TotalSpent = kv.Value.Amount,
                TransactionCount = kv.Value.Count,
                SharePercent = operatingExpenses > 0m ? Math.Round(kv.Value.Amount / operatingExpenses * 100m, 1) : 0m
            })
            .ToList();

        var netWorthHistory = new List<NetWorthTrendPoint>();
        var today = DateTime.Today;
        for (int back = 5; back >= 0; back--)
        {
            var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(-back);
            string monthEndKey = monthStart.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
            decimal assetsAt = 0m;
            decimal liabilitiesAt = 0m;
            foreach (var acct in accounts)
            {
                decimal bal = back == 0
                    ? acct.CurrentBalance
                    : acct.OpeningBalance + transactions
                        .Where(t => t.AccountId == acct.Id && string.CompareOrdinal(t.TransactionDate, monthEndKey) <= 0)
                        .Sum(t => t.Amount);
                if (bal >= 0m) assetsAt += bal; else liabilitiesAt += Math.Abs(bal);
            }
            netWorthHistory.Add(new NetWorthTrendPoint
            {
                MonthLabel = monthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                TotalAssets = Math.Round(assetsAt, 2),
                TotalLiabilities = Math.Round(liabilitiesAt, 2),
                NetWorth = Math.Round(assetsAt - liabilitiesAt, 2)
            });
        }

        return new ExecutiveReportSummary
        {
            ReportPeriodLabel = $"Year-to-Date {today.Year} • Executive P&L & Stewardship Audit",
            GrossIncome = grossIncome,
            TotalOperatingExpenses = operatingExpenses,
            TotalDebtServiceAndTransfers = debtAndTransfers,
            NetOperatingSurplus = netSurplus,
            SavingsRatePercent = savingsRate,
            CharitableGivingTotal = charitableGiving,
            CharitableGivingPercentOfIncome = givingPct,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            CurrentNetWorth = netWorth,
            DebtToAssetRatioPercent = debtToAssetRatio,
            IncomeByCategory = incomeList,
            ExpenseByCategory = expenseList,
            TopPayees = topPayees,
            StewardshipTaxReceipts = stewardshipReceipts,
            NetWorthHistory = netWorthHistory
        };
    }

    public string ExportTransactionsCsv()
    {
        var transactions = GetTransactions();
        var sb = new StringBuilder();
        sb.AppendLine("Id,Date,Account,ReferenceNumber,Status,Payee,Category,Memo,Tags,ReceiptRef,Payment,Deposit,SignedAmount,RunningBalance,ExternalFitId");

        foreach (var tx in transactions)
        {
            string payment = tx.PaymentAmount.HasValue ? tx.PaymentAmount.Value.ToString("0.00", CultureInfo.InvariantCulture) : "";
            string deposit = tx.DepositAmount.HasValue ? tx.DepositAmount.Value.ToString("0.00", CultureInfo.InvariantCulture) : "";
            string signed = tx.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            string running = tx.RunningBalance.ToString("0.00", CultureInfo.InvariantCulture);

            sb.AppendLine(string.Join(",",
                tx.Id,
                CsvEscape(tx.TransactionDate),
                CsvEscape(tx.AccountName),
                CsvEscape(tx.ReferenceNumber),
                CsvEscape(tx.Status.ToString()),
                CsvEscape(tx.PayeeName),
                CsvEscape(tx.CategoryName),
                CsvEscape(tx.Memo),
                CsvEscape(tx.Tags),
                CsvEscape(tx.ReceiptRef),
                payment,
                deposit,
                signed,
                running,
                CsvEscape(tx.ExternalFitId)));
        }

        return sb.ToString();
    }

    public string ExportStewardshipTaxReceiptsCsv()
    {
        var summary = GetExecutiveReportSummary();
        var sb = new StringBuilder();
        sb.AppendLine("TransactionId,Date,CharityOrParishPayee,Category,Account,ReferenceNumber,Status,Memo,TaxDeductibleAmount");

        foreach (var item in summary.StewardshipTaxReceipts)
        {
            sb.AppendLine(string.Join(",",
                item.TransactionId,
                CsvEscape(item.TransactionDate),
                CsvEscape(item.PayeeName),
                CsvEscape(item.CategoryName),
                CsvEscape(item.AccountName),
                CsvEscape(item.ReferenceNumber),
                CsvEscape(item.Status),
                CsvEscape(item.Memo),
                item.Amount.ToString("0.00", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    public string ExportFullWorkspaceJsonBackup()
    {
        var snapshot = new
        {
            application = "Full of Grace Money Manager",
            schemaVersion = "Phase 3B (net10.0-windows / SQLite)",
            exportedAtUtc = DateTime.UtcNow.ToString("O"),
            accounts = GetAccountsWithBalances(),
            categories = GetCategories(),
            payees = GetPayees(),
            rules = GetRules(),
            budgetSummary = GetMonthlyBudgetSummary(CurrentMonthKey()),
            portfolioSummary = GetInvestmentPortfolioSummary(),
            recurringSummary = GetRecurringCashFlowSummary(),
            reportSummary = GetExecutiveReportSummary(),
            reconciliationHistory = GetReconciliationHistory(),
            transactions = GetTransactions()
        };

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    public DebtPayoffWorkspaceSummary GetDebtPayoffSummary(string strategyMode = "Avalanche")
    {
        bool isSnowball = string.Equals(strategyMode, "Snowball", StringComparison.OrdinalIgnoreCase);
        var liabilities = new List<DebtLiabilityProfile>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        const string sql = """
            SELECT
                Id,
                AccountId,
                LiabilityName,
                LenderInstitution,
                LiabilityType,
                CurrentBalance,
                InterestRateApr,
                MinimumMonthlyPayment,
                EscrowAndTaxesMonthly,
                ExtraPrincipalMonthly,
                OriginalPrincipal
            FROM DebtLiabilities
            ORDER BY Id ASC;
            """;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                liabilities.Add(new DebtLiabilityProfile
                {
                    Id = reader.GetInt32(0),
                    AccountId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    LiabilityName = reader.GetString(2),
                    LenderInstitution = reader.GetString(3),
                    LiabilityType = reader.GetString(4),
                    CurrentBalance = reader.GetDecimal(5),
                    InterestRateApr = reader.GetDecimal(6),
                    MinimumMonthlyPayment = reader.GetDecimal(7),
                    EscrowAndTaxesMonthly = reader.GetDecimal(8),
                    ExtraPrincipalMonthly = reader.GetDecimal(9),
                    OriginalPrincipal = reader.GetDecimal(10)
                });
            }
        }

        var ordered = isSnowball
            ? liabilities.OrderBy(x => x.CurrentBalance).ThenByDescending(x => x.InterestRateApr).ToList()
            : liabilities.OrderByDescending(x => x.InterestRateApr).ThenBy(x => x.CurrentBalance).ToList();

        decimal totalPrincipal = 0m;
        decimal weightedAprNumerator = 0m;
        decimal totalMinPayment = 0m;
        decimal totalExtraPrincipal = 0m;
        decimal baselineTotalInterest = 0m;
        decimal acceleratedTotalInterest = 0m;
        int maxBaselineMonths = 0;
        int maxAcceleratedMonths = 0;

        var referenceDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);

        for (int i = 0; i < ordered.Count; i++)
        {
            var item = ordered[i];
            item.PayoffPriorityOrder = i + 1;

            var (baseMonths, baseInt) = SimulateSingleDebt(
                item.CurrentBalance,
                item.InterestRateApr,
                item.MinimumMonthlyPayment,
                0m);

            var (accMonths, accInt) = SimulateSingleDebt(
                item.CurrentBalance,
                item.InterestRateApr,
                item.MinimumMonthlyPayment,
                item.ExtraPrincipalMonthly);

            item.PayoffMonthsBaseline = baseMonths;
            item.TotalInterestBaseline = baseInt;
            item.PayoffMonthsAccelerated = accMonths;
            item.TotalInterestAccelerated = accInt;
            item.ProjectedPayoffDate = referenceDate.AddMonths(Math.Max(1, accMonths)).ToString("MMM yyyy");

            totalPrincipal += item.CurrentBalance;
            weightedAprNumerator += item.CurrentBalance * item.InterestRateApr;
            totalMinPayment += item.MinimumMonthlyPayment;
            totalExtraPrincipal += item.ExtraPrincipalMonthly;
            baselineTotalInterest += baseInt;
            acceleratedTotalInterest += accInt;
            if (baseMonths > maxBaselineMonths) maxBaselineMonths = baseMonths;
            if (accMonths > maxAcceleratedMonths) maxAcceleratedMonths = accMonths;
        }

        var schedule = BuildCombinedAmortizationSchedule(ordered, referenceDate, maxRows: 24);
        decimal weightedApr = totalPrincipal > 0m
            ? Math.Round(weightedAprNumerator / totalPrincipal, 2)
            : 0m;

        return new DebtPayoffWorkspaceSummary
        {
            StrategyMode = isSnowball ? "Snowball" : "Avalanche",
            TotalDebtPrincipal = totalPrincipal,
            WeightedAverageApr = weightedApr,
            TotalMinimumMonthlyPayment = totalMinPayment,
            TotalExtraPrincipalMonthly = totalExtraPrincipal,
            BaselineTotalInterest = baselineTotalInterest,
            AcceleratedTotalInterest = acceleratedTotalInterest,
            BaselineMonthsToDebtFree = maxBaselineMonths,
            AcceleratedMonthsToDebtFree = maxAcceleratedMonths,
            ProjectedDebtFreeDate = referenceDate.AddMonths(Math.Max(1, maxAcceleratedMonths)).ToString("MMM yyyy"),
            Liabilities = ordered,
            AmortizationSchedule = schedule
        };
    }

    private static (int Months, decimal TotalInterest) SimulateSingleDebt(
        decimal balance,
        decimal apr,
        decimal minPayment,
        decimal extraPrincipal)
    {
        if (balance <= 0m) return (0, 0m);

        decimal monthlyRate = apr / 100m / 12m;
        decimal effectivePayment = Math.Max(minPayment + extraPrincipal, balance * monthlyRate + 10m);
        decimal rem = balance;
        decimal totalInterest = 0m;
        int months = 0;

        while (rem > 0.01m && months < 480)
        {
            months++;
            decimal interest = Math.Round(rem * monthlyRate, 2);
            decimal principal = effectivePayment - interest;
            if (principal <= 0m)
            {
                principal = 10m;
            }
            if (principal > rem)
            {
                principal = rem;
            }

            rem = Math.Max(0m, rem - principal);
            totalInterest += interest;
        }

        return (months, Math.Round(totalInterest, 2));
    }

    private static List<AmortizationScheduleEntry> BuildCombinedAmortizationSchedule(
        List<DebtLiabilityProfile> orderedLiabilities,
        DateTime startDate,
        int maxRows)
    {
        var schedule = new List<AmortizationScheduleEntry>();
        var balances = orderedLiabilities.Select(x => x.CurrentBalance).ToArray();
        decimal cumulativeInterest = 0m;

        for (int m = 1; m <= maxRows; m++)
        {
            decimal startingTotal = balances.Sum();
            if (startingTotal <= 0.01m)
            {
                break;
            }

            decimal monthMinTotal = 0m;
            decimal monthExtraTotal = 0m;
            decimal monthInterestTotal = 0m;
            decimal monthPrincipalTotal = 0m;
            decimal rolloverPool = 0m;

            for (int i = 0; i < orderedLiabilities.Count; i++)
            {
                var d = orderedLiabilities[i];
                if (balances[i] <= 0.01m)
                {
                    rolloverPool += d.MinimumMonthlyPayment + d.ExtraPrincipalMonthly;
                    continue;
                }

                decimal rate = d.InterestRateApr / 100m / 12m;
                decimal interest = Math.Round(balances[i] * rate, 2);
                decimal minPay = Math.Min(balances[i] + interest, d.MinimumMonthlyPayment);
                decimal extraPay = d.ExtraPrincipalMonthly + rolloverPool;
                rolloverPool = 0m;

                decimal totalPay = minPay + extraPay;
                if (totalPay > balances[i] + interest)
                {
                    decimal unused = totalPay - (balances[i] + interest);
                    rolloverPool += unused;
                    totalPay = balances[i] + interest;
                    extraPay = Math.Max(0m, totalPay - minPay);
                }

                decimal principal = Math.Max(0m, totalPay - interest);
                balances[i] = Math.Max(0m, balances[i] - principal);

                monthMinTotal += minPay;
                monthExtraTotal += extraPay;
                monthInterestTotal += interest;
                monthPrincipalTotal += principal;
            }

            cumulativeInterest += monthInterestTotal;
            schedule.Add(new AmortizationScheduleEntry
            {
                MonthIndex = m,
                MonthDate = startDate.AddMonths(m - 1).ToString("yyyy-MM"),
                StartingBalance = Math.Round(startingTotal, 2),
                ScheduledMinimumPayment = Math.Round(monthMinTotal, 2),
                ExtraPrincipalPayment = Math.Round(monthExtraTotal, 2),
                InterestPaid = Math.Round(monthInterestTotal, 2),
                PrincipalPaid = Math.Round(monthPrincipalTotal, 2),
                EndingBalance = Math.Round(balances.Sum(), 2),
                CumulativeInterestPaid = Math.Round(cumulativeInterest, 2)
            });
        }

        return schedule;
    }

    public void UpdateDebtLiability(
        int id,
        decimal currentBalance,
        decimal interestRateApr,
        decimal minimumMonthlyPayment,
        decimal extraPrincipalMonthly)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE DebtLiabilities
            SET CurrentBalance = @bal,
                InterestRateApr = @apr,
                MinimumMonthlyPayment = @minPay,
                ExtraPrincipalMonthly = @extraPay
            WHERE Id = @id;
            """;
        cmd.Parameters.AddWithValue("@bal", Math.Max(0m, currentBalance));
        cmd.Parameters.AddWithValue("@apr", Math.Max(0m, interestRateApr));
        cmd.Parameters.AddWithValue("@minPay", Math.Max(0m, minimumMonthlyPayment));
        cmd.Parameters.AddWithValue("@extraPay", Math.Max(0m, extraPrincipalMonthly));
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public DebtLiabilityProfile CreateDebtLiability(DebtLiabilityProfile liability)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO DebtLiabilities (
                AccountId, LiabilityName, LenderInstitution, LiabilityType,
                CurrentBalance, InterestRateApr, MinimumMonthlyPayment,
                EscrowAndTaxesMonthly, ExtraPrincipalMonthly, OriginalPrincipal
            )
            VALUES (
                @accId, @name, @lender, @type,
                @bal, @apr, @minPay,
                @escrow, @extraPay, @orig
            );
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@accId", liability.AccountId.HasValue ? liability.AccountId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@name", liability.LiabilityName.Trim());
        cmd.Parameters.AddWithValue("@lender", string.IsNullOrWhiteSpace(liability.LenderInstitution) ? "Private Lender" : liability.LenderInstitution.Trim());
        cmd.Parameters.AddWithValue("@type", string.IsNullOrWhiteSpace(liability.LiabilityType) ? "Loan" : liability.LiabilityType.Trim());
        cmd.Parameters.AddWithValue("@bal", Math.Max(0m, liability.CurrentBalance));
        cmd.Parameters.AddWithValue("@apr", Math.Max(0m, liability.InterestRateApr));
        cmd.Parameters.AddWithValue("@minPay", Math.Max(0m, liability.MinimumMonthlyPayment));
        cmd.Parameters.AddWithValue("@escrow", Math.Max(0m, liability.EscrowAndTaxesMonthly));
        cmd.Parameters.AddWithValue("@extraPay", Math.Max(0m, liability.ExtraPrincipalMonthly));
        cmd.Parameters.AddWithValue("@orig", Math.Max(liability.CurrentBalance, liability.OriginalPrincipal));

        int newId = Convert.ToInt32(cmd.ExecuteScalar());
        return GetDebtPayoffSummary().Liabilities.First(x => x.Id == newId);
    }

    public void DeleteDebtLiability(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM DebtLiabilities WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // =========================================================================
    // Phase 4B: Savings Goals & Sinking Funds Planner
    // =========================================================================
    public SavingsGoalsWorkspaceSummary GetSavingsGoalsSummary()
    {
        var accounts = GetAccountsWithBalances();
        var hysa = accounts.FirstOrDefault(a => string.Equals(a.AccountType, "Savings", StringComparison.OrdinalIgnoreCase))
            ?? accounts.FirstOrDefault(a => string.Equals(a.AccountGroup, "Banking", StringComparison.OrdinalIgnoreCase));
        decimal hysaBalance = hysa?.CurrentBalance ?? 0m;

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                g.Id, g.AccountId, a.Name || ' ' || a.AccountNumberMask AS AccountLabel,
                g.Name, g.CategoryGroup, g.IconGlyph, g.ColorHex,
                g.TargetAmount, g.CurrentSavedAmount, g.MonthlyContributionTarget,
                g.TargetDate, g.PriorityOrder, g.Notes
            FROM SavingsGoals g
            LEFT JOIN Accounts a ON a.Id = g.AccountId
            ORDER BY g.PriorityOrder ASC, g.Id ASC;
            """;

        var goals = new List<SavingsGoal>();
        var anchorDate = DateTime.Today;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            decimal target = reader.GetDecimal(7);
            decimal saved = reader.GetDecimal(8);
            decimal monthlyPlan = reader.GetDecimal(9);
            string targetDateStr = reader.GetString(10);

            DateTime parsedTarget = DateTime.TryParse(targetDateStr, out var dt)
                ? dt
                : anchorDate.AddMonths(12);

            int monthsUntilTarget = Math.Max(1, ((parsedTarget.Year - anchorDate.Year) * 12) + (parsedTarget.Month - anchorDate.Month));
            decimal remaining = Math.Max(0m, target - saved);
            decimal requiredMonthly = remaining <= 0m
                ? 0m
                : Math.Round(remaining / monthsUntilTarget, 2);

            int monthsAtCurrentPlan = remaining <= 0m
                ? 0
                : (monthlyPlan > 0m ? (int)Math.Ceiling(remaining / monthlyPlan) : 99);

            string projectedLabel = remaining <= 0m
                ? "Fully Funded ✓"
                : anchorDate.AddMonths(monthsAtCurrentPlan).ToString("MMM yyyy");

            string badge = remaining <= 0m
                ? "Fully Funded"
                : (monthlyPlan >= requiredMonthly ? "On Track" : "Needs Boost");

            goals.Add(new SavingsGoal
            {
                Id = reader.GetInt32(0),
                AccountId = reader.GetInt32(1),
                AccountName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Name = reader.GetString(3),
                CategoryGroup = reader.GetString(4),
                IconGlyph = reader.GetString(5),
                ColorHex = reader.GetString(6),
                TargetAmount = target,
                CurrentSavedAmount = saved,
                MonthlyContributionTarget = monthlyPlan,
                TargetDate = targetDateStr,
                PriorityOrder = reader.GetInt32(11),
                Notes = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                MonthsRemaining = remaining <= 0m ? 0 : monthsUntilTarget,
                RequiredMonthlyToHitTargetDate = requiredMonthly,
                ProjectedCompletionDate = projectedLabel,
                StatusBadge = badge
            });
        }

        return new SavingsGoalsWorkspaceSummary
        {
            HighYieldSavingsBalance = hysaBalance,
            TotalTargetAmount = goals.Sum(g => g.TargetAmount),
            TotalCurrentSaved = goals.Sum(g => g.CurrentSavedAmount),
            TotalMonthlyContributionPlan = goals.Sum(g => g.MonthlyContributionTarget),
            TotalRequiredMonthlyForDeadlines = goals.Sum(g => g.RequiredMonthlyToHitTargetDate),
            FullyFundedCount = goals.Count(g => g.RemainingAmount <= 0m),
            OnTrackCount = goals.Count(g => g.StatusBadge == "On Track" || g.StatusBadge == "Fully Funded"),
            Goals = goals
        };
    }

    public void UpdateSavingsGoal(
        int id,
        decimal targetAmount,
        decimal currentSavedAmount,
        decimal monthlyContributionTarget,
        string targetDate)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE SavingsGoals
            SET TargetAmount = @target,
                CurrentSavedAmount = @saved,
                MonthlyContributionTarget = @monthly,
                TargetDate = @targetDate
            WHERE Id = @id;
            """;
        cmd.Parameters.AddWithValue("@target", Math.Max(0m, targetAmount));
        cmd.Parameters.AddWithValue("@saved", Math.Max(0m, currentSavedAmount));
        cmd.Parameters.AddWithValue("@monthly", Math.Max(0m, monthlyContributionTarget));
        cmd.Parameters.AddWithValue("@targetDate", string.IsNullOrWhiteSpace(targetDate) ? "2027-06-01" : targetDate.Trim());
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public SavingsGoal? RecordSavingsGoalContribution(int goalId, decimal contributionAmount, bool recordLedgerTransfer)
    {
        if (contributionAmount <= 0m)
        {
            return null;
        }

        var summary = GetSavingsGoalsSummary();
        var goal = summary.Goals.FirstOrDefault(g => g.Id == goalId);
        if (goal == null)
        {
            return null;
        }

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                UPDATE SavingsGoals
                SET CurrentSavedAmount = CurrentSavedAmount + @amt
                WHERE Id = @id;
                """;
            cmd.Parameters.AddWithValue("@amt", contributionAmount);
            cmd.Parameters.AddWithValue("@id", goalId);
            cmd.ExecuteNonQuery();
        }

        if (recordLedgerTransfer)
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            CreateTransaction(
                accountId: goal.AccountId,
                transactionDate: today,
                payeeName: $"Sinking Fund Allocation — {goal.Name}",
                categoryId: 71,
                memo: $"Phase 4B Sinking Fund Contribution to '{goal.Name}'",
                signedAmount: contributionAmount,
                status: TransactionStatus.Cleared,
                referenceNumber: "GOAL-XFER");
        }

        return GetSavingsGoalsSummary().Goals.FirstOrDefault(g => g.Id == goalId);
    }

    public SavingsGoal CreateSavingsGoal(SavingsGoal goal)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO SavingsGoals (
                AccountId, Name, CategoryGroup, IconGlyph, ColorHex,
                TargetAmount, CurrentSavedAmount, MonthlyContributionTarget,
                TargetDate, PriorityOrder, Notes
            )
            VALUES (
                @accId, @name, @grp, @icon, @color,
                @target, @saved, @monthly,
                @targetDate, @prio, @notes
            );
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("@accId", EnsureValidAccountId(conn, goal.AccountId, "Savings", "Savings"));
        cmd.Parameters.AddWithValue("@name", goal.Name.Trim());
        cmd.Parameters.AddWithValue("@grp", string.IsNullOrWhiteSpace(goal.CategoryGroup) ? "Sinking Fund" : goal.CategoryGroup.Trim());
        cmd.Parameters.AddWithValue("@icon", string.IsNullOrWhiteSpace(goal.IconGlyph) ? "🎯" : goal.IconGlyph.Trim());
        cmd.Parameters.AddWithValue("@color", string.IsNullOrWhiteSpace(goal.ColorHex) ? "#38BDF8" : goal.ColorHex.Trim());
        cmd.Parameters.AddWithValue("@target", Math.Max(1m, goal.TargetAmount));
        cmd.Parameters.AddWithValue("@saved", Math.Max(0m, goal.CurrentSavedAmount));
        cmd.Parameters.AddWithValue("@monthly", Math.Max(0m, goal.MonthlyContributionTarget));
        cmd.Parameters.AddWithValue("@targetDate", string.IsNullOrWhiteSpace(goal.TargetDate) ? "2027-06-01" : goal.TargetDate.Trim());
        cmd.Parameters.AddWithValue("@prio", goal.PriorityOrder <= 0 ? 50 : goal.PriorityOrder);
        cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(goal.Notes) ? DBNull.Value : goal.Notes.Trim());

        int newId = Convert.ToInt32(cmd.ExecuteScalar());
        return GetSavingsGoalsSummary().Goals.First(g => g.Id == newId);
    }

    public void DeleteSavingsGoal(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM SavingsGoals WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
