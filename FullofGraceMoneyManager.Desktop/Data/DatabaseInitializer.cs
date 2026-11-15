using System.IO;
using Microsoft.Data.Sqlite;

namespace FullofGraceMoneyManager.Desktop.Data;

public static class DatabaseInitializer
{
    public const string DatabaseFileName = "fullofgrace.db";

    public static string GetDatabasePath()
    {
#if DEBUG
        string baseDir = Path.Combine(AppContext.BaseDirectory, "Data");
#else
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string baseDir = Path.Combine(localAppData, "FullofGraceMoneyManager");
#endif
        Directory.CreateDirectory(baseDir);
        return Path.Combine(baseDir, DatabaseFileName);
    }

    public static string GetConnectionString()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabasePath(),
            ForeignKeys = true
        };
        return builder.ToString();
    }

    public static void Initialize()
    {
        using var connection = new SqliteConnection(GetConnectionString());
        connection.Open();

        CreateSchema(connection);
        SeedIfEmpty(connection);
        SeedRulesIfEmpty(connection);
        SeedBudgetEnvelopesIfEmpty(connection);
        SeedInvestmentHoldingsIfEmpty(connection);
        SeedRecurringSchedulesIfEmpty(connection);
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        const string schemaSql = """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Accounts (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                AccountNumberMask TEXT NOT NULL DEFAULT '',
                AccountType TEXT NOT NULL DEFAULT 'Checking',
                AccountGroup TEXT NOT NULL DEFAULT 'Banking',
                Institution TEXT NOT NULL DEFAULT '',
                OpeningBalance NUMERIC NOT NULL DEFAULT 0,
                Currency TEXT NOT NULL DEFAULT 'USD',
                IsLiability INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1,
                PortalUrl TEXT NULL,
                Notes TEXT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ParentId INTEGER NULL,
                CategoryType TEXT NOT NULL DEFAULT 'Expense',
                ColorHex TEXT NOT NULL DEFAULT '#38BDF8',
                Icon TEXT NOT NULL DEFAULT 'Tag',
                MonthlyBudget NUMERIC NOT NULL DEFAULT 0,
                FOREIGN KEY (ParentId) REFERENCES Categories(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS Payees (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE,
                DefaultCategoryId INTEGER NULL,
                NormalizedPattern TEXT NULL,
                WebsiteUrl TEXT NULL,
                Notes TEXT NULL,
                FOREIGN KEY (DefaultCategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountId INTEGER NOT NULL,
                TransactionDate TEXT NOT NULL,
                ReferenceNumber TEXT NULL,
                PayeeId INTEGER NULL,
                PayeeName TEXT NOT NULL DEFAULT '',
                CategoryId INTEGER NULL,
                Memo TEXT NULL,
                Amount NUMERIC NOT NULL DEFAULT 0,
                Status TEXT NOT NULL DEFAULT 'Uncleared' CHECK (Status IN ('Uncleared', 'Cleared', 'Reconciled')),
                IsSplit INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE,
                FOREIGN KEY (PayeeId) REFERENCES Payees(Id) ON DELETE SET NULL,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS TransactionSplits (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TransactionId INTEGER NOT NULL,
                CategoryId INTEGER NULL,
                Memo TEXT NULL,
                Amount NUMERIC NOT NULL DEFAULT 0,
                TransferAccountId INTEGER NULL,
                FOREIGN KEY (TransactionId) REFERENCES Transactions(Id) ON DELETE CASCADE,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL,
                FOREIGN KEY (TransferAccountId) REFERENCES Accounts(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS CategorizationRules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Priority INTEGER NOT NULL DEFAULT 100,
                IsEnabled INTEGER NOT NULL DEFAULT 1,
                MatchField TEXT NOT NULL DEFAULT 'Payee',
                MatchOperator TEXT NOT NULL DEFAULT 'Contains',
                MatchPattern TEXT NOT NULL,
                MinAmount NUMERIC NULL,
                MaxAmount NUMERIC NULL,
                TargetAccountId INTEGER NULL,
                RenamePayeeTo TEXT NULL,
                AssignCategoryId INTEGER NULL,
                SetStatus TEXT NULL,
                AppendMemo TEXT NULL,
                FOREIGN KEY (TargetAccountId) REFERENCES Accounts(Id) ON DELETE SET NULL,
                FOREIGN KEY (AssignCategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS ReconciliationHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountId INTEGER NOT NULL,
                StatementDate TEXT NOT NULL,
                StatementEndingBalance NUMERIC NOT NULL DEFAULT 0,
                ReconciledCount INTEGER NOT NULL DEFAULT 0,
                CompletedAt TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS BudgetEnvelopes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MonthKey TEXT NOT NULL,
                CategoryId INTEGER NOT NULL,
                RolloverAmount NUMERIC NOT NULL DEFAULT 0,
                AssignedAmount NUMERIC NOT NULL DEFAULT 0,
                Notes TEXT NULL,
                UNIQUE(MonthKey, CategoryId),
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS InvestmentHoldings (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountId INTEGER NOT NULL,
                TickerSymbol TEXT NOT NULL,
                SecurityName TEXT NOT NULL,
                AssetClass TEXT NOT NULL DEFAULT 'US Equity',
                Shares NUMERIC NOT NULL DEFAULT 0,
                AverageCostPerShare NUMERIC NOT NULL DEFAULT 0,
                CurrentPrice NUMERIC NOT NULL DEFAULT 0,
                DayChangePercent NUMERIC NOT NULL DEFAULT 0,
                DividendYieldPercent NUMERIC NOT NULL DEFAULT 0,
                ExpenseRatioPercent NUMERIC NOT NULL DEFAULT 0.03,
                LastUpdated TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS RecurringSchedules (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AccountId INTEGER NOT NULL,
                PayeeName TEXT NOT NULL,
                CategoryId INTEGER NULL,
                Frequency TEXT NOT NULL DEFAULT 'Monthly',
                NextDueDate TEXT NOT NULL,
                Amount NUMERIC NOT NULL DEFAULT 0,
                IsAutoPay INTEGER NOT NULL DEFAULT 1,
                IsActive INTEGER NOT NULL DEFAULT 1,
                Memo TEXT NULL,
                FOREIGN KEY (AccountId) REFERENCES Accounts(Id) ON DELETE CASCADE,
                FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Transactions_AccountId_Date ON Transactions(AccountId, TransactionDate);
            CREATE INDEX IF NOT EXISTS IX_TransactionSplits_TransactionId ON TransactionSplits(TransactionId);
            CREATE INDEX IF NOT EXISTS IX_CategorizationRules_Priority ON CategorizationRules(Priority, Id);
            CREATE INDEX IF NOT EXISTS IX_BudgetEnvelopes_MonthKey ON BudgetEnvelopes(MonthKey, CategoryId);
            CREATE INDEX IF NOT EXISTS IX_InvestmentHoldings_AccountId ON InvestmentHoldings(AccountId);
            CREATE INDEX IF NOT EXISTS IX_RecurringSchedules_NextDueDate ON RecurringSchedules(NextDueDate);
            """;

        using var command = connection.CreateCommand();
        command.CommandText = schemaSql;
        command.ExecuteNonQuery();
    }

    private static void SeedIfEmpty(SqliteConnection connection)
    {
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM Accounts;";
            long count = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0L);
            if (count > 0)
            {
                return;
            }
        }

        using var tx = connection.BeginTransaction();

        const string seedSql = """
            -- 1. Seed Accounts across Banking, Credit Cards, Investments, Assets/Loans
            INSERT INTO Accounts (Id, Name, AccountNumberMask, AccountType, AccountGroup, Institution, OpeningBalance, Currency, IsLiability, IsActive, PortalUrl, Notes) VALUES
                (1, 'Primary Checking', '••4821', 'Checking', 'Banking', 'Chase Private Client', 4250.00, 'USD', 0, 1, 'https://www.chase.com', 'Main household operating checking account'),
                (2, 'High-Yield Savings', '••9014', 'Savings', 'Banking', 'Marcus by Goldman Sachs', 28500.00, 'USD', 0, 1, 'https://www.marcus.com', '4.65% APY emergency & sinking fund reserve'),
                (3, 'Rewards Visa', '••7390', 'CreditCard', 'Credit Cards', 'Chase Sapphire Reserve', -640.00, 'USD', 1, 1, 'https://www.chase.com', '2x-3x travel & dining rewards card'),
                (4, 'Vanguard Index Brokerage', '••3318', 'Investment', 'Investments', 'Vanguard Group', 84200.00, 'USD', 0, 1, 'https://investor.vanguard.com', 'VTSAX / VTIAX taxable wealth portfolio'),
                (5, 'Stewardship 401(k)', '••6102', 'Investment', 'Investments', 'Fidelity NetBenefits', 142800.00, 'USD', 0, 1, 'https://nb.fidelity.com', 'Employer-matched retirement plan'),
                (6, 'Home Residence Mortgage', '••1108', 'Loan', 'Assets/Loans', 'Rocket Mortgage', -218400.00, 'USD', 1, 1, 'https://www.rocketmortgage.com', '30-Yr Fixed @ 3.25% APR');

            -- 2. Seed Hierarchical Categories (Parent categories first, then Child categories)
            INSERT INTO Categories (Id, Name, ParentId, CategoryType, ColorHex, Icon, MonthlyBudget) VALUES
                (1, 'Income', NULL, 'Income', '#10B981', 'TrendingUp', 0),
                (2, 'Housing & Utilities', NULL, 'Expense', '#38BDF8', 'Home', 2400),
                (3, 'Food & Household', NULL, 'Expense', '#F59E0B', 'ShoppingBag', 1100),
                (4, 'Transportation', NULL, 'Expense', '#818CF8', 'Car', 450),
                (5, 'Giving & Ministry', NULL, 'Expense', '#F472B6', 'Heart', 850),
                (6, 'Lifestyle & Tech', NULL, 'Expense', '#A78BFA', 'Cpu', 500),
                (7, 'Transfers & Debt', NULL, 'Transfer', '#94A3B8', 'Repeat', 0),

                -- Child Categories
                (10, 'Salary & Payroll', 1, 'Income', '#10B981', 'Briefcase', 0),
                (11, 'Consulting & Advisory', 1, 'Income', '#34D399', 'Award', 0),
                (12, 'Interest & Dividends', 1, 'Income', '#6EE7B7', 'Percent', 0),
                (20, 'Mortgage & Rent', 2, 'Expense', '#38BDF8', 'Home', 1850),
                (21, 'Electric & Water', 2, 'Expense', '#0EA5E9', 'Zap', 210),
                (22, 'Fiber Internet', 2, 'Expense', '#0284C7', 'Wifi', 85),
                (23, 'Home Maintenance', 2, 'Expense', '#7DD3FC', 'Tool', 255),
                (30, 'Groceries', 3, 'Expense', '#F59E0B', 'ShoppingCart', 720),
                (31, 'Coffee & Dining', 3, 'Expense', '#FBBF24', 'Coffee', 240),
                (32, 'Household Supplies', 3, 'Expense', '#D97706', 'Package', 140),
                (40, 'Fuel & EV Charging', 4, 'Expense', '#818CF8', 'BatteryCharging', 220),
                (41, 'Auto Insurance & Service', 4, 'Expense', '#6366F1', 'Shield', 230),
                (50, 'Parish Tithe & Offerings', 5, 'Expense', '#F472B6', 'Heart', 650),
                (51, 'Community Charity', 5, 'Expense', '#EC4899', 'Gift', 200),
                (60, 'Cloud & Software', 6, 'Expense', '#A78BFA', 'Cloud', 120),
                (61, 'Equipment & Books', 6, 'Expense', '#8B5CF6', 'BookOpen', 220),
                (62, 'Apparel & Personal', 6, 'Expense', '#C4B5FD', 'User', 160),
                (70, 'Credit Card Payment', 7, 'Transfer', '#94A3B8', 'CreditCard', 0),
                (71, 'Savings Transfer', 7, 'Transfer', '#64748B', 'ArrowRightLeft', 0);

            -- 3. Seed Payees
            INSERT INTO Payees (Id, Name, DefaultCategoryId, NormalizedPattern, WebsiteUrl, Notes) VALUES
                (1, 'Grace Sovereign Systems Payroll', 10, 'GRACE SOVEREIGN*PAYROLL', 'https://adp.com', 'Bi-weekly direct deposit'),
                (2, 'St. Mary Parish Stewardship', 50, 'ST MARY PARISH*', 'https://stmary.org', 'Monthly parish tithe'),
                (3, 'Rocket Mortgage Servicing', 20, 'ROCKET MORTGAGE*ACH', 'https://rocketmortgage.com', 'Monthly home loan draft'),
                (4, 'Whole Foods Market', 30, 'WHOLEFDS*', 'https://wholefoodsmarket.com', 'Organic groceries'),
                (5, 'Costco Wholesale', 30, 'COSTCO WHSE*', 'https://costco.com', 'Bulk groceries & household split'),
                (6, 'Pacific Gas & Electric', 21, 'PG&E*WEB ONLINE', 'https://pge.com', 'Monthly electric & gas utility'),
                (7, 'Sonic Fiber Telecom', 22, 'SONIC.NET*FIBER', 'https://sonic.com', '10Gbps symmetrical fiber'),
                (8, 'Lighthouse Advisory Partners', 11, 'LIGHTHOUSE ADV*INV', NULL, 'Architecture consulting retainer'),
                (9, 'Blue Bottle Coffee Roasters', 31, 'SQ *BLUE BOTTLE', 'https://bluebottlecoffee.com', 'Espresso & meetings'),
                (10, 'GitHub & JetBrains Cloud', 60, 'GITHUB*JETBRAINS', 'https://github.com', 'Developer tooling subscriptions'),
                (11, 'Shell Clean Energy Station', 40, 'SHELL OIL*', 'https://shell.com', 'Fuel station'),
                (12, 'Marcus High-Yield Interest', 12, 'MARCUS GS*INTEREST', 'https://marcus.com', 'Monthly savings yield'),
                (13, 'Target Superstore', 32, 'TARGET T-*', 'https://target.com', 'Department store split'),
                (14, 'Chase Card Services Transfer', 70, 'CHASE CREDIT CRD*AUTOPAY', 'https://chase.com', 'Monthly card payment');

            -- 4. Seed 16 Realistic Transactions across Primary Checking (1), High-Yield Savings (2), and Rewards Visa (3)
            INSERT INTO Transactions (Id, AccountId, TransactionDate, ReferenceNumber, PayeeId, PayeeName, CategoryId, Memo, Amount, Status, IsSplit) VALUES
                (1, 1, '2026-11-01', 'DD-901', 1, 'Grace Sovereign Systems Payroll', 10, 'Bi-weekly net salary direct deposit', 3650.00, 'Reconciled', 0),
                (2, 1, '2026-11-02', 'ACH-104', 3, 'Rocket Mortgage Servicing', 20, 'November principal, interest & escrow', -1845.00, 'Reconciled', 0),
                (3, 1, '2026-11-03', 'ACH-108', 2, 'St. Mary Parish Stewardship', 50, 'Firstfruits monthly parish offering', -550.00, 'Reconciled', 0),
                (4, 1, '2026-11-05', 'Split', 5, 'Costco Wholesale', 30, 'Monthly bulk pantry + household stockup', -248.60, 'Cleared', 1),
                (5, 1, '2026-11-06', 'ACH-215', 6, 'Pacific Gas & Electric', 21, 'Autumn utility cycle statement', -142.35, 'Cleared', 0),
                (6, 1, '2026-11-08', 'INV-412', 8, 'Lighthouse Advisory Partners', 11, 'Q4 FinTech architecture milestone', 1250.00, 'Cleared', 0),
                (7, 1, '2026-11-10', 'XFER-88', 14, 'Chase Card Services Transfer', 70, 'Payment to Rewards Visa ••7390', -500.00, 'Cleared', 0),
                (8, 1, '2026-11-11', 'Split', 13, 'Target Superstore', 32, 'Home essentials, books & winter coat', -164.80, 'Cleared', 1),
                (9, 1, '2026-11-13', 'POS-334', 4, 'Whole Foods Market', 30, 'Fresh produce, poultry & bakery', -138.45, 'Uncleared', 0),
                (10, 1, '2026-11-14', 'ACH-992', 7, 'Sonic Fiber Telecom', 22, 'November gigabit fiber service', -79.99, 'Uncleared', 0),

                -- High-Yield Savings (Account 2)
                (11, 2, '2026-11-01', 'XFER-01', 12, 'Marcus High-Yield Interest', 12, 'October APY compounded interest payout', 112.40, 'Reconciled', 0),
                (12, 2, '2026-11-09', 'SAV-402', 1, 'Grace Sovereign Systems Payroll', 71, 'Automated emergency reserve allocation', 750.00, 'Cleared', 0),

                -- Rewards Visa (Account 3)
                (13, 3, '2026-11-04', 'CC-882', 10, 'GitHub & JetBrains Cloud', 60, 'Annual DotUltimate + Copilot Pro renewal', -189.00, 'Reconciled', 0),
                (14, 3, '2026-11-07', 'CC-901', 11, 'Shell Clean Energy Station', 40, 'Premium fuel fill-up', -58.90, 'Cleared', 0),
                (15, 3, '2026-11-10', 'XFER-88', 14, 'Chase Card Services Transfer', 70, 'Payment received from Primary Checking', 500.00, 'Cleared', 0),
                (16, 3, '2026-11-13', 'CC-945', 9, 'Blue Bottle Coffee Roasters', 31, 'Client coffee catch-up & espresso beans', -36.50, 'Uncleared', 0);

            -- 5. Seed TransactionSplits for Split Transactions (#4 and #8)
            INSERT INTO TransactionSplits (Id, TransactionId, CategoryId, Memo, Amount, TransferAccountId) VALUES
                (1, 4, 30, 'Costco organic groceries & meat', -168.60, NULL),
                (2, 4, 32, 'Paper towels, detergent & kitchenware', -80.00, NULL),
                (3, 8, 32, 'Household cleaning supplies', -44.80, NULL),
                (4, 8, 61, 'Technical reference books', -45.00, NULL),
                (5, 8, 62, 'Kids warm fleece jacket', -75.00, NULL);
            """;

        using (var seedCmd = connection.CreateCommand())
        {
            seedCmd.Transaction = tx;
            seedCmd.CommandText = seedSql;
            seedCmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private static void SeedRulesIfEmpty(SqliteConnection connection)
    {
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM CategorizationRules;";
            long count = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0L);
            if (count > 0)
            {
                return;
            }
        }

        const string rulesSeedSql = """
            INSERT INTO CategorizationRules
                (Id, Name, Priority, IsEnabled, MatchField, MatchOperator, MatchPattern, MinAmount, MaxAmount, TargetAccountId, RenamePayeeTo, AssignCategoryId, SetStatus, AppendMemo)
            VALUES
                (1, 'Whole Foods Market Normalization', 10, 1, 'Payee', 'Contains', 'WHOLEFDS', NULL, NULL, NULL, 'Whole Foods Market', 30, 'Cleared', NULL),
                (2, 'Blue Bottle Coffee Square POS', 20, 1, 'Payee', 'Contains', 'BLUE BOTTLE', 1.00, 75.00, NULL, 'Blue Bottle Coffee Roasters', 31, 'Cleared', NULL),
                (3, 'Sonic Gigabit Fiber Utility', 30, 1, 'Payee', 'Contains', 'SONIC.NET', NULL, NULL, NULL, 'Sonic Fiber Telecom', 22, 'Cleared', NULL),
                (4, 'Costco Wholesale Club', 40, 1, 'Payee', 'Contains', 'COSTCO WHSE', NULL, NULL, NULL, 'Costco Wholesale', 30, 'Cleared', NULL),
                (5, 'Amazon Marketplace Tech & Books', 50, 1, 'Payee', 'Contains', 'AMZN MKTP', NULL, NULL, NULL, 'Amazon Marketplace', 61, 'Cleared', 'Auto-categorized via AMZN rule'),
                (6, 'Shell Fuel & Energy Stations', 60, 1, 'Payee', 'Contains', 'SHELL OIL', NULL, NULL, NULL, 'Shell Clean Energy Station', 40, 'Cleared', NULL),
                (7, 'Trader Joe''s Grocery Rule', 70, 1, 'Payee', 'Contains', 'TRADER JOE', NULL, NULL, NULL, 'Trader Joe''s Market', 30, 'Cleared', NULL),
                (8, 'Grace Sovereign Direct Deposit', 80, 1, 'Payee', 'Contains', 'GRACE SOVEREIGN', 500.00, NULL, NULL, 'Grace Sovereign Systems Payroll', 10, 'Cleared', NULL);
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = rulesSeedSql;
        cmd.ExecuteNonQuery();
    }

    private static void SeedBudgetEnvelopesIfEmpty(SqliteConnection connection)
    {
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM BudgetEnvelopes;";
            long count = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0L);
            if (count > 0)
            {
                return;
            }
        }

        const string envelopesSeedSql = """
            INSERT INTO BudgetEnvelopes (MonthKey, CategoryId, RolloverAmount, AssignedAmount, Notes) VALUES
                ('2026-11', 20, 0.00, 1850.00, 'Full November mortgage & escrow funded'),
                ('2026-11', 21, 35.00, 180.00, 'Autumn electric & water utility envelope'),
                ('2026-11', 22, 0.00, 85.00, 'Gigabit fiber internet envelope'),
                ('2026-11', 23, 420.00, 200.00, 'Sinking fund for home repairs'),
                ('2026-11', 30, 65.00, 650.00, 'Monthly groceries envelope'),
                ('2026-11', 31, 18.00, 180.00, 'Coffee & dining out envelope'),
                ('2026-11', 32, 25.00, 140.00, 'Household supplies'),
                ('2026-11', 40, 30.00, 190.00, 'Fuel & EV charging'),
                ('2026-11', 41, 150.00, 230.00, 'Six-month auto insurance reserve'),
                ('2026-11', 50, 0.00, 650.00, 'Firstfruits parish tithe'),
                ('2026-11', 51, 50.00, 150.00, 'Community charity giving'),
                ('2026-11', 60, 0.00, 120.00, 'Annual software & cloud renewals'),
                ('2026-11', 61, 80.00, 180.00, 'Books & workstation gear'),
                ('2026-11', 62, 40.00, 160.00, 'Family winter apparel');
            """;

        using var envCmd = connection.CreateCommand();
        envCmd.CommandText = envelopesSeedSql;
        envCmd.ExecuteNonQuery();
    }

    private static void SeedInvestmentHoldingsIfEmpty(SqliteConnection connection)
    {
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM InvestmentHoldings;";
            long count = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0L);
            if (count > 0)
            {
                return;
            }
        }

        const string holdingsSeedSql = """
            INSERT INTO InvestmentHoldings
                (Id, AccountId, TickerSymbol, SecurityName, AssetClass, Shares, AverageCostPerShare, CurrentPrice, DayChangePercent, DividendYieldPercent, ExpenseRatioPercent)
            VALUES
                -- Account 4: Vanguard Index Brokerage ($84,200.00 total market value)
                (1, 4, 'VTI',   'Vanguard Total Stock Market ETF',         'US Equity',            160.000, 218.50, 285.00,  0.84, 1.38, 0.03),
                (2, 4, 'VXUS',  'Vanguard Total International Stock ETF',  'International Equity', 250.000,  54.20,  62.40,  0.42, 3.05, 0.07),
                (3, 4, 'BND',   'Vanguard Total Bond Market ETF',          'Fixed Income / Bonds', 180.000,  76.10,  74.00, -0.15, 3.65, 0.03),
                (4, 4, 'VNQ',   'Vanguard Real Estate Index ETF',          'Real Estate / REIT',    80.000,  82.00,  91.00,  0.61, 3.92, 0.12),
                (5, 4, 'VMFXX', 'Vanguard Federal Money Market Fund',      'Cash & Money Market', 2400.000,   1.00,   1.00,  0.01, 4.95, 0.11),

                -- Account 5: Stewardship 401(k) ($142,800.00 total market value)
                (6, 5, 'FXAIX', 'Fidelity 500 Index Fund',                 'US Equity',            450.000, 152.00, 204.00,  0.91, 1.45, 0.015),
                (7, 5, 'FSPSX', 'Fidelity International Index Fund',       'International Equity', 480.000,  43.50,  51.25,  0.38, 2.88, 0.035),
                (8, 5, 'FXNAX', 'Fidelity U.S. Bond Index Fund',           'Fixed Income / Bonds', 2100.000, 10.80,  10.40, -0.12, 3.55, 0.025),
                (9, 5, 'FSRNX', 'Fidelity Real Estate Index Fund',         'Real Estate / REIT',   240.000,  16.90,  19.00,  0.55, 3.70, 0.07);
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = holdingsSeedSql;
        cmd.ExecuteNonQuery();
    }

    private static void SeedRecurringSchedulesIfEmpty(SqliteConnection connection)
    {
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(1) FROM RecurringSchedules;";
            long count = Convert.ToInt64(checkCmd.ExecuteScalar() ?? 0L);
            if (count > 0)
            {
                return;
            }
        }

        const string recurringSeedSql = """
            INSERT INTO RecurringSchedules
                (Id, AccountId, PayeeName, CategoryId, Frequency, NextDueDate, Amount, IsAutoPay, IsActive, Memo)
            VALUES
                (1, 1, 'Grace Sovereign Systems Payroll', 10, 'Bi-Weekly', '2026-11-15',  3650.00, 1, 1, 'Bi-weekly direct deposit salary'),
                (2, 1, 'GEICO Auto & Umbrella Insurance', 41, 'Monthly',   '2026-11-18',  -215.00, 1, 1, 'Monthly auto & personal umbrella policy draft'),
                (3, 3, 'Apple One Premier & iCloud+ 2TB', 60, 'Monthly',   '2026-11-20',   -37.95, 1, 1, 'Family cloud storage & Apple services'),
                (4, 1, 'Grace Sovereign Systems Payroll', 10, 'Bi-Weekly', '2026-11-29',  3650.00, 1, 1, 'Month-end bi-weekly salary direct deposit'),
                (5, 1, 'Rocket Mortgage Servicing',       20, 'Monthly',   '2026-12-01', -1845.00, 1, 1, 'December principal, interest & property tax escrow'),
                (6, 1, 'St. Mary Parish Stewardship',     50, 'Monthly',   '2026-12-03',  -650.00, 1, 1, 'Firstfruits monthly parish tithe'),
                (7, 1, 'Pacific Gas & Electric',          21, 'Monthly',   '2026-12-06',  -155.00, 1, 1, 'Estimated winter utility draft'),
                (8, 1, 'Sonic Fiber Telecom',             22, 'Monthly',   '2026-12-14',   -79.99, 1, 1, '10Gbps fiber internet autopay');
            """;

        using var cmd = connection.CreateCommand();
        cmd.CommandText = recurringSeedSql;
        cmd.ExecuteNonQuery();
    }
}
