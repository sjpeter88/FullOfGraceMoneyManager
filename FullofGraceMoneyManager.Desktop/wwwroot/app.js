/**
 * Full of Grace Money Manager — WebView2 Frontend Application & Bridge (Phase 1 + Phase 2A + Phase 2B)
 * Supports Dashboard, Checkbook Register with Inline Editing & Quick Entry, Guided Statement Reconciliation
 * with Real-Time $0.00 Variance Tracking, OFX/QFX/QIF/CSV Importer, and IF/THEN Rules Engine.
 */

const AppBridge = (() => {
  let state = {
    activeView: "Dashboard",
    selectedAccountId: null,
    selectedAccount: null,
    searchQuery: "",
    statusFilter: "ALL",
    tagFilter: "ALL",
    categoryFilter: "ALL",
    minAmountFilter: "",
    maxAmountFilter: "",
    onlyWithReceiptFilter: false,
    expandedSplitIds: { 4: true, 8: true },
    editingTransactionId: null,
    activeSampleFormat: "OFX",
    statementPreview: null,
    reconcileSession: {
      accountId: 1,
      statementDate: new Date().toISOString().slice(0, 10),
      targetEndingBalance: 5249.25,
      checkedTxIds: {}
    },
    reconciliationHistory: [],
    metrics: {
      netWorth: 41689.26,
      totalAssets: 260890.41,
      totalLiabilities: 219201.15,
      liquidCash: 33890.41,
      monthlyIncome: 5012.40,
      monthlyExpenses: 3453.59,
      savingsRatePercent: 31.1,
      categoryBreakdown: [
        { categoryName: "Housing & Utilities:Mortgage & Rent", colorHex: "#38BDF8", spent: 1845.00, budget: 1850, percentageOfTotal: 53.4 },
        { categoryName: "Giving & Ministry:Parish Tithe & Offerings", colorHex: "#F472B6", spent: 550.00, budget: 650, percentageOfTotal: 15.9 },
        { categoryName: "Food & Household:Groceries", colorHex: "#F59E0B", spent: 307.05, budget: 720, percentageOfTotal: 8.9 },
        { categoryName: "Lifestyle & Tech:Cloud & Software", colorHex: "#A78BFA", spent: 189.00, budget: 120, percentageOfTotal: 5.5 },
        { categoryName: "Housing & Utilities:Electric & Water", colorHex: "#0EA5E9", spent: 142.35, budget: 210, percentageOfTotal: 4.1 },
        { categoryName: "Food & Household:Household Supplies", colorHex: "#D97706", spent: 124.80, budget: 140, percentageOfTotal: 3.6 }
      ],
      monthlyCashFlow: [
        { label: "Jun", income: 7850, expense: 4920, net: 2930 },
        { label: "Jul", income: 8100, expense: 5180, net: 2920 },
        { label: "Aug", income: 7850, expense: 4740, net: 3110 },
        { label: "Sep", income: 8450, expense: 5290, net: 3160 },
        { label: "Oct", income: 8200, expense: 4860, net: 3340 },
        { label: "Nov", income: 5012.40, expense: 3453.59, net: 1558.81 }
      ]
    },
    accounts: [
      { id: 1, name: "Primary Checking", accountNumberMask: "••4821", accountType: "Checking", accountGroup: "Banking", institution: "Chase Private Client", openingBalance: 4250.00, clearedBalance: 5249.25, pendingTotal: -218.44, currentBalance: 5030.81, portalUrl: "https://www.chase.com" },
      { id: 2, name: "High-Yield Savings", accountNumberMask: "••9014", accountType: "Savings", accountGroup: "Banking", institution: "Marcus by Goldman Sachs", openingBalance: 28500.00, clearedBalance: 29362.40, pendingTotal: 0.00, currentBalance: 29362.40, portalUrl: "https://www.marcus.com" },
      { id: 3, name: "Rewards Visa", accountNumberMask: "••7390", accountType: "CreditCard", accountGroup: "Credit Cards", institution: "Chase Sapphire Reserve", openingBalance: -640.00, clearedBalance: -387.90, pendingTotal: -36.50, currentBalance: -424.40, portalUrl: "https://www.chase.com" },
      { id: 4, name: "Vanguard Index Brokerage", accountNumberMask: "••3318", accountType: "Investment", accountGroup: "Investments", institution: "Vanguard Group", openingBalance: 84200.00, clearedBalance: 84200.00, pendingTotal: 0.00, currentBalance: 84200.00, portalUrl: "https://investor.vanguard.com" },
      { id: 5, name: "Stewardship 401(k)", accountNumberMask: "••6102", accountType: "Investment", accountGroup: "Investments", institution: "Fidelity NetBenefits", openingBalance: 142800.00, clearedBalance: 142800.00, pendingTotal: 0.00, currentBalance: 142800.00, portalUrl: "https://nb.fidelity.com" },
      { id: 6, name: "Home Residence Mortgage", accountNumberMask: "••1108", accountType: "Loan", accountGroup: "Assets/Loans", institution: "Rocket Mortgage", openingBalance: -218400.00, clearedBalance: -218400.00, pendingTotal: 0.00, currentBalance: -218400.00, portalUrl: "https://www.rocketmortgage.com" }
    ],
    categories: [
      { id: 10, name: "Salary & Payroll", parentId: 1, parentName: "Income", categoryType: "Income", fullPath: "Income:Salary & Payroll", monthlyBudget: 0 },
      { id: 11, name: "Consulting & Advisory", parentId: 1, parentName: "Income", categoryType: "Income", fullPath: "Income:Consulting & Advisory", monthlyBudget: 0 },
      { id: 12, name: "Interest & Dividends", parentId: 1, parentName: "Income", categoryType: "Income", fullPath: "Income:Interest & Dividends", monthlyBudget: 0 },
      { id: 20, name: "Mortgage & Rent", parentId: 2, parentName: "Housing & Utilities", categoryType: "Expense", fullPath: "Housing & Utilities:Mortgage & Rent", monthlyBudget: 1850 },
      { id: 21, name: "Electric & Water", parentId: 2, parentName: "Housing & Utilities", categoryType: "Expense", fullPath: "Housing & Utilities:Electric & Water", monthlyBudget: 210 },
      { id: 22, name: "Fiber Internet", parentId: 2, parentName: "Housing & Utilities", categoryType: "Expense", fullPath: "Housing & Utilities:Fiber Internet", monthlyBudget: 85 },
      { id: 30, name: "Groceries", parentId: 3, parentName: "Food & Household", categoryType: "Expense", fullPath: "Food & Household:Groceries", monthlyBudget: 720 },
      { id: 31, name: "Coffee & Dining", parentId: 3, parentName: "Food & Household", categoryType: "Expense", fullPath: "Food & Household:Coffee & Dining", monthlyBudget: 240 },
      { id: 32, name: "Household Supplies", parentId: 3, parentName: "Food & Household", categoryType: "Expense", fullPath: "Food & Household:Household Supplies", monthlyBudget: 140 },
      { id: 40, name: "Fuel & EV Charging", parentId: 4, parentName: "Transportation", categoryType: "Expense", fullPath: "Transportation:Fuel & EV Charging", monthlyBudget: 220 },
      { id: 50, name: "Parish Tithe & Offerings", parentId: 5, parentName: "Giving & Ministry", categoryType: "Expense", fullPath: "Giving & Ministry:Parish Tithe & Offerings", monthlyBudget: 650 },
      { id: 60, name: "Cloud & Software", parentId: 6, parentName: "Lifestyle & Tech", categoryType: "Expense", fullPath: "Lifestyle & Tech:Cloud & Software", monthlyBudget: 120, colorHex: "#A78BFA" },
      { id: 61, name: "Equipment & Books", parentId: 6, parentName: "Lifestyle & Tech", categoryType: "Expense", fullPath: "Lifestyle & Tech:Equipment & Books", monthlyBudget: 220, colorHex: "#8B5CF6" },
      { id: 62, name: "Apparel & Personal", parentId: 6, parentName: "Lifestyle & Tech", categoryType: "Expense", fullPath: "Lifestyle & Tech:Apparel & Personal", monthlyBudget: 160, colorHex: "#7C3AED" },
      { id: 70, name: "Credit Card Payment", parentId: 7, parentName: "Transfers & Debt", categoryType: "Transfer", fullPath: "Transfers & Debt:Credit Card Payment", monthlyBudget: 0, colorHex: "#94A3B8" },
      { id: 71, name: "Savings Transfer", parentId: 7, parentName: "Transfers & Debt", categoryType: "Transfer", fullPath: "Transfers & Debt:Savings Transfer", monthlyBudget: 0, colorHex: "#64748B" }
    ],
    budgetMonthKey: "2026-11",
    budgetSummary: null,
    budgetEnvelopeState: {
      20: { rollover: 0.00, assigned: 1850.00 },
      21: { rollover: 24.50, assigned: 185.00 },
      22: { rollover: 0.00, assigned: 85.00 },
      30: { rollover: 42.10, assigned: 680.00 },
      31: { rollover: 15.00, assigned: 190.00 },
      32: { rollover: 0.00, assigned: 140.00 },
      40: { rollover: 18.40, assigned: 160.00 },
      50: { rollover: 0.00, assigned: 650.00 },
      60: { rollover: 0.00, assigned: 120.00 },
      61: { rollover: 35.00, assigned: 185.00 },
      62: { rollover: 10.00, assigned: 150.00 }
    },
    investmentAccountFilter: null,
    portfolioSummary: null,
    investmentHoldings: [
      { id: 1, accountId: 4, accountName: "Vanguard Index Brokerage", institution: "Vanguard Group", tickerSymbol: "VTI", securityName: "Vanguard Total Stock Market ETF", assetClass: "US Equity", shares: 160.000, averageCostPerShare: 218.50, currentPrice: 285.00, dayChangePercent: 0.84, dividendYieldPercent: 1.38, expenseRatioPercent: 0.03 },
      { id: 2, accountId: 4, accountName: "Vanguard Index Brokerage", institution: "Vanguard Group", tickerSymbol: "VXUS", securityName: "Vanguard Total International Stock ETF", assetClass: "International Equity", shares: 250.000, averageCostPerShare: 54.20, currentPrice: 62.40, dayChangePercent: 0.42, dividendYieldPercent: 3.05, expenseRatioPercent: 0.07 },
      { id: 3, accountId: 4, accountName: "Vanguard Index Brokerage", institution: "Vanguard Group", tickerSymbol: "BND", securityName: "Vanguard Total Bond Market ETF", assetClass: "Fixed Income / Bonds", shares: 180.000, averageCostPerShare: 76.10, currentPrice: 74.00, dayChangePercent: -0.15, dividendYieldPercent: 3.65, expenseRatioPercent: 0.03 },
      { id: 4, accountId: 4, accountName: "Vanguard Index Brokerage", institution: "Vanguard Group", tickerSymbol: "VNQ", securityName: "Vanguard Real Estate Index ETF", assetClass: "Real Estate / REIT", shares: 80.000, averageCostPerShare: 82.00, currentPrice: 91.00, dayChangePercent: 0.61, dividendYieldPercent: 3.92, expenseRatioPercent: 0.12 },
      { id: 5, accountId: 4, accountName: "Vanguard Index Brokerage", institution: "Vanguard Group", tickerSymbol: "VMFXX", securityName: "Vanguard Federal Money Market Fund", assetClass: "Cash & Money Market", shares: 2400.000, averageCostPerShare: 1.00, currentPrice: 1.00, dayChangePercent: 0.01, dividendYieldPercent: 4.95, expenseRatioPercent: 0.11 },
      { id: 6, accountId: 5, accountName: "Stewardship 401(k)", institution: "Fidelity NetBenefits", tickerSymbol: "FXAIX", securityName: "Fidelity 500 Index Fund", assetClass: "US Equity", shares: 450.000, averageCostPerShare: 152.00, currentPrice: 204.00, dayChangePercent: 0.91, dividendYieldPercent: 1.45, expenseRatioPercent: 0.015 },
      { id: 7, accountId: 5, accountName: "Stewardship 401(k)", institution: "Fidelity NetBenefits", tickerSymbol: "FSPSX", securityName: "Fidelity International Index Fund", assetClass: "International Equity", shares: 480.000, averageCostPerShare: 43.50, currentPrice: 51.25, dayChangePercent: 0.38, dividendYieldPercent: 2.88, expenseRatioPercent: 0.035 },
      { id: 8, accountId: 5, accountName: "Stewardship 401(k)", institution: "Fidelity NetBenefits", tickerSymbol: "FXNAX", securityName: "Fidelity U.S. Bond Index Fund", assetClass: "Fixed Income / Bonds", shares: 2100.000, averageCostPerShare: 10.80, currentPrice: 10.40, dayChangePercent: -0.12, dividendYieldPercent: 3.55, expenseRatioPercent: 0.025 },
      { id: 9, accountId: 5, accountName: "Stewardship 401(k)", institution: "Fidelity NetBenefits", tickerSymbol: "FSRNX", securityName: "Fidelity Real Estate Index Fund", assetClass: "Real Estate / REIT", shares: 240.000, averageCostPerShare: 16.90, currentPrice: 19.00, dayChangePercent: 0.55, dividendYieldPercent: 3.70, expenseRatioPercent: 0.07 }
    ],
    recurringSummary: null,
    recurringSchedules: [
      { id: 1, accountId: 1, accountName: "Primary Checking", payeeName: "Grace Sovereign Systems Payroll", categoryId: 10, categoryName: "Income:Salary & Payroll", frequency: "Bi-Weekly", nextDueDate: "2026-11-15", amount: 3650.00, isAutoPay: true, isActive: true, memo: "Bi-weekly direct deposit salary" },
      { id: 2, accountId: 1, accountName: "Primary Checking", payeeName: "GEICO Auto & Umbrella Insurance", categoryId: 41, categoryName: "Transportation:Auto Insurance & Service", frequency: "Monthly", nextDueDate: "2026-11-18", amount: -215.00, isAutoPay: true, isActive: true, memo: "Monthly auto & personal umbrella policy draft" },
      { id: 3, accountId: 3, accountName: "Rewards Visa", payeeName: "Apple One Premier & iCloud+ 2TB", categoryId: 60, categoryName: "Lifestyle & Tech:Cloud & Software", frequency: "Monthly", nextDueDate: "2026-11-20", amount: -37.95, isAutoPay: true, isActive: true, memo: "Family cloud storage & Apple services" },
      { id: 4, accountId: 1, accountName: "Primary Checking", payeeName: "Grace Sovereign Systems Payroll", categoryId: 10, categoryName: "Income:Salary & Payroll", frequency: "Bi-Weekly", nextDueDate: "2026-11-29", amount: 3650.00, isAutoPay: true, isActive: true, memo: "Month-end bi-weekly salary direct deposit" },
      { id: 5, accountId: 1, accountName: "Primary Checking", payeeName: "Rocket Mortgage Servicing", categoryId: 20, categoryName: "Housing & Utilities:Mortgage & Rent", frequency: "Monthly", nextDueDate: "2026-12-01", amount: -1845.00, isAutoPay: true, isActive: true, memo: "December principal, interest & property tax escrow" },
      { id: 6, accountId: 1, accountName: "Primary Checking", payeeName: "St. Mary Parish Stewardship", categoryId: 50, categoryName: "Giving & Ministry:Parish Tithe & Offerings", frequency: "Monthly", nextDueDate: "2026-12-03", amount: -650.00, isAutoPay: true, isActive: true, memo: "Firstfruits monthly parish tithe" },
      { id: 7, accountId: 1, accountName: "Primary Checking", payeeName: "Pacific Gas & Electric", categoryId: 21, categoryName: "Housing & Utilities:Electric & Water", frequency: "Monthly", nextDueDate: "2026-12-06", amount: -155.00, isAutoPay: true, isActive: true, memo: "Estimated winter utility draft" },
      { id: 8, accountId: 1, accountName: "Primary Checking", payeeName: "Sonic Fiber Telecom", categoryId: 22, categoryName: "Housing & Utilities:Fiber Internet", frequency: "Monthly", nextDueDate: "2026-12-14", amount: -79.99, isAutoPay: true, isActive: true, memo: "10Gbps fiber internet autopay" }
    ],
    reportSummary: null,
    debtStrategyMode: "Avalanche",
    debtSummary: null,
    debtLiabilities: [
      { id: 1, accountId: 6, name: "Home Residence Mortgage ••1108", institution: "Rocket Mortgage", categoryType: "Mortgage", originalPrincipal: 245000.00, currentBalance: 218400.00, interestRateApr: 5.375, minimumMonthlyPayment: 1375.85, extraPrincipalMonthly: 250.00, remainingTermMonths: 312, nextDueDate: "2026-12-01", escrowMonthly: 469.15, notes: "30-yr fixed conventional mortgage (5.375% APR) + $469.15/mo tax & hazard escrow" },
      { id: 2, accountId: null, name: "Family Hybrid SUV Auto Note ••8419", institution: "Toyota Financial Services", categoryType: "Auto Loan", originalPrincipal: 28500.00, currentBalance: 14250.00, interestRateApr: 4.49, minimumMonthlyPayment: 468.20, extraPrincipalMonthly: 150.00, remainingTermMonths: 33, nextDueDate: "2026-11-24", escrowMonthly: 0.00, notes: "60-month hybrid SUV note — accelerated principal payoff target" },
      { id: 3, accountId: 3, name: "Rewards Visa Statement Balance ••7390", institution: "Chase Sapphire Reserve", categoryType: "Credit Card", originalPrincipal: 1200.00, currentBalance: 424.40, interestRateApr: 21.99, minimumMonthlyPayment: 85.00, extraPrincipalMonthly: 339.40, remainingTermMonths: 6, nextDueDate: "2026-11-28", escrowMonthly: 0.00, notes: "Paid in full monthly via grace-period statement autopay" }
    ],
    savingsGoalsSummary: null,
    savingsGoals: [
      { id: 1, accountId: 2, accountName: "High-Yield Savings ••9014", name: "6-Month Household Emergency Reserve", categoryGroup: "Emergency & Reserves", iconGlyph: "🛡️", colorHex: "#10B981", targetAmount: 24000.00, currentSavedAmount: 18500.00, monthlyContributionTarget: 750.00, targetDate: "2027-06-01", priorityOrder: 10, notes: "Core 6-month operating expense safety net in Marcus 4.65% APY" },
      { id: 2, accountId: 2, accountName: "High-Yield Savings ••9014", name: "Next Family Vehicle Cash Sinking Fund", categoryGroup: "Major Purchases", iconGlyph: "🚗", colorHex: "#38BDF8", targetAmount: 15000.00, currentSavedAmount: 4800.00, monthlyContributionTarget: 550.00, targetDate: "2028-03-01", priorityOrder: 20, notes: "Debt-free down payment & replacement reserve" },
      { id: 3, accountId: 2, accountName: "High-Yield Savings ••9014", name: "Rome & Assisi Jubilee Pilgrimage", categoryGroup: "Family & Ministry", iconGlyph: "⛪", colorHex: "#F59E0B", targetAmount: 6500.00, currentSavedAmount: 3462.40, monthlyContributionTarget: 400.00, targetDate: "2027-05-01", priorityOrder: 30, notes: "Airfare, lodging & parish pilgrimage fund" },
      { id: 4, accountId: 2, accountName: "High-Yield Savings ••9014", name: "Property Tax & Roof Sinking Reserve", categoryGroup: "Home & Escrow", iconGlyph: "🏡", colorHex: "#A78BFA", targetAmount: 5000.00, currentSavedAmount: 2600.00, monthlyContributionTarget: 300.00, targetDate: "2027-04-01", priorityOrder: 40, notes: "Supplemental home maintenance & annual county assessment buffer" }
    ],
    payees: [
      { id: 1, name: "Grace Sovereign Systems Payroll" },
      { id: 2, name: "St. Mary Parish Stewardship" },
      { id: 3, name: "Rocket Mortgage Servicing" },
      { id: 4, name: "Whole Foods Market" },
      { id: 5, name: "Costco Wholesale" },
      { id: 6, name: "Pacific Gas & Electric" },
      { id: 7, name: "Sonic Fiber Telecom" },
      { id: 8, name: "Lighthouse Advisory Partners" },
      { id: 9, name: "Blue Bottle Coffee Roasters" },
      { id: 13, name: "Target Superstore" }
    ],
    rules: [
      { id: 1, name: "Whole Foods Market Normalization", priority: 10, isEnabled: true, matchField: "Payee", matchOperator: "Contains", matchPattern: "WHOLEFDS", minAmount: null, maxAmount: null, renamePayeeTo: "Whole Foods Market", assignCategoryId: 30, assignCategoryName: "Food & Household:Groceries", setStatus: "Cleared" },
      { id: 2, name: "Blue Bottle Coffee Square POS", priority: 20, isEnabled: true, matchField: "Payee", matchOperator: "Contains", matchPattern: "BLUE BOTTLE", minAmount: 1.00, maxAmount: 75.00, renamePayeeTo: "Blue Bottle Coffee Roasters", assignCategoryId: 31, assignCategoryName: "Food & Household:Coffee & Dining", setStatus: "Cleared" },
      { id: 3, name: "Sonic Gigabit Fiber Utility", priority: 30, isEnabled: true, matchField: "Payee", matchOperator: "Contains", matchPattern: "SONIC.NET", minAmount: null, maxAmount: null, renamePayeeTo: "Sonic Fiber Telecom", assignCategoryId: 22, assignCategoryName: "Housing & Utilities:Fiber Internet", setStatus: "Cleared" },
      { id: 4, name: "Costco Wholesale Club", priority: 40, isEnabled: true, matchField: "Payee", matchOperator: "Contains", matchPattern: "COSTCO WHSE", minAmount: null, maxAmount: null, renamePayeeTo: "Costco Wholesale", assignCategoryId: 30, assignCategoryName: "Food & Household:Groceries", setStatus: "Cleared" },
      { id: 5, name: "Amazon Marketplace Tech & Books", priority: 50, isEnabled: true, matchField: "Payee", matchOperator: "Contains", matchPattern: "AMZN MKTP", minAmount: null, maxAmount: null, renamePayeeTo: "Amazon Marketplace", assignCategoryId: 61, assignCategoryName: "Lifestyle & Tech:Equipment & Books", setStatus: "Cleared" },
      { id: 6, name: "Shell Fuel & Energy Stations", priority: 60, isEnabled: true, matchField: "Payee", matchOperator: "Contains", matchPattern: "SHELL OIL", minAmount: null, maxAmount: null, renamePayeeTo: "Shell Clean Energy Station", assignCategoryId: 40, assignCategoryName: "Transportation:Fuel & EV Charging", setStatus: "Cleared" },
      { id: 7, name: "Trader Joe's Grocery Rule", priority: 70, isEnabled: true, matchField: "Payee", matchOperator: "Contains", matchPattern: "TRADER JOE", minAmount: null, maxAmount: null, renamePayeeTo: "Trader Joe's Market", assignCategoryId: 30, assignCategoryName: "Food & Household:Groceries", setStatus: "Cleared" },
      { id: 8, name: "Grace Sovereign Direct Deposit", priority: 80, isEnabled: true, matchField: "Payee", matchOperator: "Contains", matchPattern: "GRACE SOVEREIGN", minAmount: 500.00, maxAmount: null, renamePayeeTo: "Grace Sovereign Systems Payroll", assignCategoryId: 10, assignCategoryName: "Income:Salary & Payroll", setStatus: "Cleared" }
    ],
    transactions: [
      { id: 10, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-14", referenceNumber: "ACH-992", payeeName: "Sonic Fiber Telecom", categoryId: 22, categoryName: "Housing & Utilities:Fiber Internet", memo: "November gigabit fiber service", tags: "#FixedBill,#Reimbursable", receiptRef: "SONIC-INV-992.PDF", amount: -79.99, payment: 79.99, deposit: 0, runningBalance: 5030.81, status: "Uncleared", isSplit: false, splits: [] },
      { id: 9, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-13", referenceNumber: "POS-334", payeeName: "Whole Foods Market", categoryId: 30, categoryName: "Food & Household:Groceries", memo: "Fresh produce, poultry & bakery", tags: "#Family", receiptRef: "WFM-POS-334.JPG", amount: -138.45, payment: 138.45, deposit: 0, runningBalance: 5110.80, status: "Uncleared", isSplit: false, splits: [] },
      { id: 8, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-11", referenceNumber: "Split", payeeName: "Target Superstore", categoryId: 32, categoryName: "-- Split Transaction --", memo: "Home essentials, books & winter coat", tags: "#Family,#Education", receiptRef: "TARGET-1111-SPLIT.JPG", amount: -164.80, payment: 164.80, deposit: 0, runningBalance: 5249.25, status: "Cleared", isSplit: true, splits: [
        { id: 3, categoryName: "Food & Household:Household Supplies", memo: "Household cleaning supplies", amount: -44.80 },
        { id: 4, categoryName: "Lifestyle & Tech:Equipment & Books", memo: "Technical reference books", amount: -45.00 },
        { id: 5, categoryName: "Lifestyle & Tech:Apparel & Personal", memo: "Kids warm fleece jacket", amount: -75.00 }
      ] },
      { id: 7, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-10", referenceNumber: "XFER-88", payeeName: "Chase Card Services Transfer", categoryId: 70, categoryName: "Transfers & Debt:Credit Card Payment", memo: "Payment to Rewards Visa ••7390", tags: "#InternalTransfer", receiptRef: null, amount: -500.00, payment: 500.00, deposit: 0, runningBalance: 5414.05, status: "Cleared", isSplit: false, splits: [] },
      { id: 6, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-08", referenceNumber: "INV-412", payeeName: "Lighthouse Advisory Partners", categoryId: 11, categoryName: "Income:Consulting & Advisory", memo: "Q4 FinTech architecture milestone", tags: "#1099Income,#Advisory", receiptRef: "INV-412-SIGNED.PDF", amount: 1250.00, payment: 0, deposit: 1250.00, runningBalance: 5914.05, status: "Cleared", isSplit: false, splits: [] },
      { id: 5, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-06", referenceNumber: "ACH-215", payeeName: "Pacific Gas & Electric", categoryId: 21, categoryName: "Housing & Utilities:Electric & Water", memo: "Autumn utility cycle statement", tags: "#FixedBill", receiptRef: "PGE-2026-11.PDF", amount: -142.35, payment: 142.35, deposit: 0, runningBalance: 4664.05, status: "Cleared", isSplit: false, splits: [] },
      { id: 4, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-05", referenceNumber: "Split", payeeName: "Costco Wholesale", categoryId: 30, categoryName: "-- Split Transaction --", memo: "Monthly bulk pantry + household stockup", tags: "#Family,#BulkStockup", receiptRef: "COSTCO-WHSE-1105.JPG", amount: -248.60, payment: 248.60, deposit: 0, runningBalance: 4806.40, status: "Cleared", isSplit: true, splits: [
        { id: 1, categoryName: "Food & Household:Groceries", memo: "Costco organic groceries & meat", amount: -168.60 },
        { id: 2, categoryName: "Food & Household:Household Supplies", memo: "Paper towels, detergent & kitchenware", amount: -80.00 }
      ] },
      { id: 3, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-03", referenceNumber: "ACH-108", payeeName: "St. Mary Parish Stewardship", categoryId: 50, categoryName: "Giving & Ministry:Parish Tithe & Offerings", memo: "Firstfruits monthly parish offering", tags: "#TaxDeductible,#Ministry", receiptRef: "RCPT-STMARY-1103.PDF", amount: -550.00, payment: 550.00, deposit: 0, runningBalance: 5055.00, status: "Reconciled", isSplit: false, splits: [] },
      { id: 2, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-02", referenceNumber: "ACH-104", payeeName: "Rocket Mortgage Servicing", categoryId: 20, categoryName: "Housing & Utilities:Mortgage & Rent", memo: "November principal, interest & escrow", tags: "#FixedBill,#TaxEscrow", receiptRef: "STMT-MORT-1126.PDF", amount: -1845.00, payment: 1845.00, deposit: 0, runningBalance: 5605.00, status: "Reconciled", isSplit: false, splits: [] },
      { id: 1, accountId: 1, accountName: "Primary Checking", transactionDate: "2026-11-01", referenceNumber: "DD-901", payeeName: "Grace Sovereign Systems Payroll", categoryId: 10, categoryName: "Income:Salary & Payroll", memo: "Bi-weekly net salary direct deposit", tags: "#Payroll,#W2Income", receiptRef: "PAYSTUB-2026-22.PDF", amount: 3650.00, payment: 0, deposit: 3650.00, runningBalance: 7450.00, status: "Reconciled", isSplit: false, splits: [] }
    ]
  };

  function isWebViewHost() {
    return Boolean(window.chrome && window.chrome.webview);
  }

  function postToHost(payload) {
    if (isWebViewHost()) {
      window.chrome.webview.postMessage(payload);
    }
  }

  function fmtCurrency(val) {
    const num = Number(val || 0);
    const absStr = Math.abs(num).toLocaleString("en-US", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
    return num < 0 ? `-$${absStr}` : `$${absStr}`;
  }

  function escapeHtml(str) {
    return String(str ?? "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function showToast(msg) {
    const banner = document.getElementById("toast-banner");
    if (!banner) return;
    banner.textContent = msg;
    banner.classList.remove("hidden");
    clearTimeout(banner._timer);
    banner._timer = setTimeout(() => banner.classList.add("hidden"), 3400);
  }

  function recalcStandaloneBalances() {
    const byAcc = {};
    state.accounts.forEach((a) => {
      byAcc[a.id] = a.openingBalance;
    });
    const chronological = [...state.transactions].reverse();
    chronological.forEach((t) => {
      if (byAcc[t.accountId] === undefined) byAcc[t.accountId] = 0;
      byAcc[t.accountId] += t.amount;
      t.runningBalance = byAcc[t.accountId];
    });
  }

  function updateHeader() {
    const titleEl = document.getElementById("view-title");
    const subtitleEl = document.getElementById("view-subtitle");
    const kickerEl = document.getElementById("view-kicker");

    document.querySelectorAll(".tab-btn").forEach((btn) => {
      const v = btn.getAttribute("data-view");
      const isMatch =
        v === state.activeView ||
        (state.activeView === "Register" && v === "AllTransactions");
      btn.classList.toggle("active", isMatch);
    });

    if (state.activeView === "Register" && state.selectedAccount) {
      const a = state.selectedAccount;
      kickerEl.textContent = `${a.accountGroup.toUpperCase()} • ${a.institution.toUpperCase()}`;
      titleEl.textContent = `${a.name} (${a.accountNumberMask}) — Checkbook Register`;
      subtitleEl.textContent = `Cleared: ${fmtCurrency(a.clearedBalance)}  |  Pending: ${fmtCurrency(a.pendingTotal)}  |  Ending Balance: ${fmtCurrency(a.currentBalance)}`;
    } else if (state.activeView === "AllTransactions") {
      kickerEl.textContent = "MASTER LEDGER REGISTER";
      titleEl.textContent = "All Accounts Checkbook Register";
      subtitleEl.textContent = "Inline editing (✎ / Double-Click), quick-entry row, and guided statement reconciliation";
    } else if (state.activeView === "Rules") {
      kickerEl.textContent = "PHASE 2A • AUTOMATION & PAYEE NORMALIZATION";
      titleEl.textContent = "IF/THEN Categorization & Payee Rules Engine";
      subtitleEl.textContent = "Automatically strip bank payload noise, rename merchants, and assign categories during statement import";
    } else if (state.activeView === "Budgets") {
      kickerEl.textContent = "PHASE 2C • ZERO-BASED STEWARDSHIP ENVELOPES";
      titleEl.textContent = "Zero-Based Envelope Budgeting Grid";
      subtitleEl.textContent = "Give every dollar a job: inline target & assignment editing, rollover tracking, and underfunded auto-fill";
    } else if (state.activeView === "CashFlow") {
      kickerEl.textContent = "PHASE 3A • RECURRING BILLS & LIQUIDITY FORECAST";
      titleEl.textContent = "Recurring Bills, Subscriptions & 30-Day Cash-Flow Projection";
      subtitleEl.textContent = "Upcoming scheduled transactions, 1-click register posting, and 30-day checking low-water-mark forecast";
    } else if (state.activeView === "SavingsGoals") {
      kickerEl.textContent = "PHASE 4B • SAVINGS GOALS & SINKING FUNDS PLANNER";
      titleEl.textContent = "Savings Goals & Sinking Funds Planner (Emergency, Vehicle, Pilgrimage & Escrow)";
      subtitleEl.textContent = "Target dates, required monthly funding math, visual progress rings, and 1-click High-Yield Savings contribution transfers";
    } else if (state.activeView === "Investments") {
      kickerEl.textContent = "PHASE 2D • PORTFOLIO & ASSET ALLOCATION";
      titleEl.textContent = "Investment Portfolio & IPS Rebalancing Tracker";
      subtitleEl.textContent = "Taxable index brokerage and 401(k) holdings, cost basis, unrealized gain/loss, and policy target drift";
    } else if (state.activeView === "DebtOptimizer") {
      kickerEl.textContent = "PHASE 4A • DEBT PAYOFF & MORTGAGE AMORTIZATION OPTIMIZER";
      titleEl.textContent = "Debt Payoff & Mortgage Amortization Optimizer (Avalanche vs. Snowball)";
      subtitleEl.textContent = "Compare Avalanche (Highest APR) vs. Snowball (Smallest Balance), model extra monthly principal, and inspect 24-month rollover amortization";
    } else if (state.activeView === "Reports") {
      kickerEl.textContent = "PHASE 3B • EXECUTIVE REPORTS, TAX RECEIPTS & DATA EXPORT";
      titleEl.textContent = "Executive P&L Statement, Stewardship Tax Summary & Data Export";
      subtitleEl.textContent = "Income vs. operating expense audit, 6-month net worth progression, charitable receipts & CSV/JSON export";
    } else if (state.activeView === "WebPortal") {
      kickerEl.textContent = "DIRECT INSTITUTION ACCESS";
      titleEl.textContent = "Online Banking Statement Portals";
      subtitleEl.textContent = "Launch institution portals directly in the right pane to download OFX/QFX/CSV statements";
    } else {
      kickerEl.textContent = "SUNRISE DARK • PHASE 4B";
      titleEl.textContent = "Executive Financial Dashboard";
      subtitleEl.textContent = "Live SQLite net worth aggregation, monthly cash flow & hierarchical category breakdown";
    }
  }

  function renderCashFlowSvg(points) {
    if (!points || points.length === 0) return "";
    const width = 560;
    const height = 220;
    const padLeft = 48;
    const padBottom = 28;
    const padTop = 18;
    const chartW = width - padLeft - 16;
    const chartH = height - padTop - padBottom;

    const maxVal = Math.max(...points.map((p) => Math.max(p.income, p.expense)), 1000);
    const groupW = chartW / points.length;
    const barW = Math.min(22, groupW * 0.32);

    let gridLines = "";
    for (let i = 0; i <= 4; i++) {
      const y = padTop + (chartH / 4) * i;
      const val = Math.round(maxVal * (1 - i / 4));
      gridLines += `
        <line x1="${padLeft}" y1="${y}" x2="${width - 12}" y2="${y}" stroke="#1E293B" stroke-dasharray="3,3" />
        <text x="${padLeft - 8}" y="${y + 4}" text-anchor="end" fill="#64748B" font-size="10" font-family="Consolas">$${(val / 1000).toFixed(1)}k</text>
      `;
    }

    let bars = "";
    points.forEach((p, idx) => {
      const cx = padLeft + idx * groupW + groupW / 2;
      const incH = (p.income / maxVal) * chartH;
      const expH = (p.expense / maxVal) * chartH;
      const incY = padTop + chartH - incH;
      const expY = padTop + chartH - expH;

      bars += `
        <g>
          <rect x="${cx - barW - 2}" y="${incY}" width="${barW}" height="${incH}" rx="4" fill="#10B981">
            <title>${p.label} Income: ${fmtCurrency(p.income)}</title>
          </rect>
          <rect x="${cx + 2}" y="${expY}" width="${barW}" height="${expH}" rx="4" fill="#F59E0B">
            <title>${p.label} Outflow: ${fmtCurrency(p.expense)}</title>
          </rect>
          <text x="${cx}" y="${height - 8}" text-anchor="middle" fill="#94A3B8" font-size="11" font-weight="600">${escapeHtml(p.label)}</text>
        </g>
      `;
    });

    return `
      <svg viewBox="0 0 ${width} ${height}" width="100%" height="220" role="img" aria-label="Monthly Cash Flow Chart">
        ${gridLines}
        ${bars}
      </svg>
    `;
  }

  function renderCategoryDonutSvg(items, totalExpense) {
    const size = 160;
    const cx = size / 2;
    const cy = size / 2;
    const r = 56;
    const circumference = 2 * Math.PI * r;

    let offset = 0;
    let segments = "";
    const topItems = (items || []).slice(0, 6);

    topItems.forEach((item) => {
      const pct = Math.max(item.percentageOfTotal || 0, 1) / 100;
      const dash = pct * circumference;
      segments += `
        <circle cx="${cx}" cy="${cy}" r="${r}"
                fill="transparent"
                stroke="${item.colorHex || '#38BDF8'}"
                stroke-width="18"
                stroke-dasharray="${dash} ${circumference - dash}"
                stroke-dashoffset="${-offset}"
                transform="rotate(-90 ${cx} ${cy})">
          <title>${escapeHtml(item.categoryName)}: ${fmtCurrency(item.spent)} (${item.percentageOfTotal}%)</title>
        </circle>
      `;
      offset += dash;
    });

    return `
      <svg viewBox="0 0 ${size} ${size}" width="160" height="160">
        <circle cx="${cx}" cy="${cy}" r="${r}" fill="transparent" stroke="#1E293B" stroke-width="18" />
        ${segments}
        <text x="${cx}" y="${cy - 4}" text-anchor="middle" fill="#94A3B8" font-size="10" font-weight="700">SPENT</text>
        <text x="${cx}" y="${cy + 14}" text-anchor="middle" fill="#F8FAFC" font-size="13" font-weight="700" font-family="Consolas">${fmtCurrency(totalExpense)}</text>
      </svg>
    `;
  }

  function renderDashboardView() {
    const m = state.metrics;
    const breakdownRows = (m.categoryBreakdown || []).slice(0, 5).map((c) => `
      <div class="category-item">
        <div class="category-item-top">
          <span><span class="category-dot" style="background:${c.colorHex}"></span>${escapeHtml(c.categoryName)}</span>
          <strong style="font-family:var(--font-mono)">${fmtCurrency(c.spent)} (${c.percentageOfTotal}%)</strong>
        </div>
        <div class="progress-track">
          <div class="progress-fill" style="width:${Math.min(100, c.percentageOfTotal)}%; background:${c.colorHex}"></div>
        </div>
      </div>
    `).join("");

    const accountCards = state.accounts.map((a) => `
      <div class="account-tile" onclick="AppBridge.selectAccount(${a.id})">
        <div class="account-tile-header">
          <span>${escapeHtml(a.accountGroup)}</span>
          <span>${escapeHtml(a.accountNumberMask)}</span>
        </div>
        <div class="account-tile-name">${escapeHtml(a.name)}</div>
        <div style="font-size:11px; color:var(--text-secondary)">${escapeHtml(a.institution)}</div>
        <div class="account-tile-balance ${a.currentBalance < 0 ? 'amount-negative' : 'amount-positive'}">
          ${fmtCurrency(a.currentBalance)}
        </div>
      </div>
    `).join("");

    const currentMonthLabel = new Date().toLocaleDateString("en-US", { month: "short", year: "numeric" });

    if (!state.accounts || state.accounts.length === 0) {
      return `
        <section class="panel-card" style="padding:36px 28px; text-align:center; border:1px dashed rgba(245,158,11,0.45); background:linear-gradient(135deg, rgba(15,23,42,0.92), rgba(30,41,59,0.72));">
          <div class="kicker-pill" style="margin:0 auto 10px;">CLEAN SLATE • $0.00 READY</div>
          <h2 style="font-size:22px; margin:0 0 8px;">Welcome to Full of Grace Money Manager</h2>
          <p style="max-width:560px; margin:0 auto 20px; color:var(--text-secondary); font-size:13.5px; line-height:1.55;">
            Your ledger starts at <strong>$0.00</strong> with no pre-filled numbers. Add your first checking, savings, credit card, or investment account to start tracking your real finances—or load sample demo data if you want to explore first.
          </p>
          <div style="display:flex; gap:10px; justify-content:center; flex-wrap:wrap;">
            <button type="button" class="btn btn-gold" onclick="AppBridge.openAccountModal()">＋ Add Your First Account</button>
            <button type="button" class="btn btn-outline" onclick="AppBridge.openWorkspaceDataModal()">🧪 Load Sample Demo Data</button>
          </div>
        </section>

        <section class="kpi-grid" style="margin-top:16px;">
          <div class="kpi-card">
            <div class="kpi-label">Total Net Worth</div>
            <div class="kpi-value">${fmtCurrency(0)}</div>
            <div class="kpi-sub"><span>Assets: ${fmtCurrency(0)}</span><span>No accounts yet</span></div>
          </div>
          <div class="kpi-card">
            <div class="kpi-label">Liquid Banking Cash</div>
            <div class="kpi-value">${fmtCurrency(0)}</div>
            <div class="kpi-sub"><span>Checking &amp; Savings</span><span>Ready</span></div>
          </div>
          <div class="kpi-card">
            <div class="kpi-label">Month-to-Date Inflow</div>
            <div class="kpi-value amount-positive">${fmtCurrency(0)}</div>
            <div class="kpi-sub"><span>Current Month</span><span>${escapeHtml(currentMonthLabel)}</span></div>
          </div>
          <div class="kpi-card">
            <div class="kpi-label">Month-to-Date Outflow</div>
            <div class="kpi-value" style="color:var(--accent-gold)">${fmtCurrency(0)}</div>
            <div class="kpi-sub"><span>Savings Rate</span><span>0% Saved</span></div>
          </div>
        </section>
      `;
    }

    return `
      <section class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-label">Total Net Worth</div>
          <div class="kpi-value">${fmtCurrency(m.netWorth)}</div>
          <div class="kpi-sub">
            <span>Assets: ${fmtCurrency(m.totalAssets)}</span>
            <span class="amount-positive">Live SQLite</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Liquid Banking Cash</div>
          <div class="kpi-value">${fmtCurrency(m.liquidCash)}</div>
          <div class="kpi-sub">
            <span>Checking + Savings Reserve</span>
            <span style="color:var(--accent-blue)">Ready</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Month-to-Date Inflow</div>
          <div class="kpi-value amount-positive">${fmtCurrency(m.monthlyIncome)}</div>
          <div class="kpi-sub">
            <span>Income &amp; Yield</span>
            <span>${escapeHtml(currentMonthLabel)}</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Month-to-Date Outflow</div>
          <div class="kpi-value" style="color:var(--accent-gold)">${fmtCurrency(m.monthlyExpenses)}</div>
          <div class="kpi-sub">
            <span>Savings Rate</span>
            <span class="amount-positive">${m.savingsRatePercent}% Saved</span>
          </div>
        </div>
      </section>

      <section class="charts-grid">
        <div class="panel-card">
          <div class="panel-header">
            <div>
              <div class="panel-title">Cash-Flow Velocity (Income vs. Outflow)</div>
              <div class="panel-subtitle">6-month operating cash flow across all active accounts</div>
            </div>
            <div style="display:flex; gap:12px; font-size:11.5px;">
              <span><span class="category-dot" style="background:#10B981"></span>Inflow</span>
              <span><span class="category-dot" style="background:#F59E0B"></span>Outflow</span>
            </div>
          </div>
          ${renderCashFlowSvg(m.monthlyCashFlow)}
        </div>

        <div class="panel-card">
          <div class="panel-header">
            <div>
              <div class="panel-title">Category Spending Breakdown</div>
              <div class="panel-subtitle">Hierarchical category distribution (including split allocations)</div>
            </div>
          </div>
          <div class="category-breakdown-layout">
            ${renderCategoryDonutSvg(m.categoryBreakdown, m.monthlyExpenses)}
            <div class="category-list">
              ${breakdownRows}
            </div>
          </div>
        </div>
      </section>

      <section class="panel-card">
        <div class="panel-header">
          <div>
            <div class="panel-title">Accounts Overview — Click Any Account to Open Checkbook Register</div>
            <div class="panel-subtitle">Synchronized with the WPF Left Sidebar TreeView</div>
          </div>
          <button type="button" class="btn btn-outline btn-sm" onclick="AppBridge.openAccountModal()">＋ Add Account</button>
        </div>
        <div class="accounts-strip">
          ${accountCards}
        </div>
      </section>

      ${renderRegisterSection(true)}
    `;
  }

  function renderStatusBadge(tx) {
    if (tx.status === "Reconciled") {
      return `<span class="status-badge status-reconciled" title="Click to cycle status" onclick="AppBridge.cycleStatus(${tx.id})">[R] Reconciled</span>`;
    }
    if (tx.status === "Cleared") {
      return `<span class="status-badge status-cleared" title="Click to cycle status" onclick="AppBridge.cycleStatus(${tx.id})">[*] Cleared</span>`;
    }
    return `<span class="status-badge status-uncleared" title="Click to cycle status" onclick="AppBridge.cycleStatus(${tx.id})">[ ] Uncleared</span>`;
  }

  function parseTagList(rawTags) {
    if (!rawTags || typeof rawTags !== "string") return [];
    return rawTags
      .split(/[,;\s]+/)
      .map((t) => t.trim())
      .filter(Boolean)
      .map((t) => (t.startsWith("#") ? t : `#${t}`));
  }

  function getTagColorStyle(tag) {
    const lower = tag.toLowerCase();
    if (lower.includes("tax") || lower.includes("ministry")) {
      return "background:rgba(244,114,182,0.14); color:#F472B6; border:1px solid rgba(244,114,182,0.35);";
    }
    if (lower.includes("reimburs") || lower.includes("advisory") || lower.includes("1099")) {
      return "background:rgba(245,158,11,0.14); color:#FBBF24; border:1px solid rgba(245,158,11,0.35);";
    }
    if (lower.includes("payroll") || lower.includes("w2") || lower.includes("yield")) {
      return "background:rgba(16,185,129,0.14); color:#34D399; border:1px solid rgba(16,185,129,0.35);";
    }
    if (lower.includes("fixed") || lower.includes("escrow")) {
      return "background:rgba(56,189,248,0.14); color:#38BDF8; border:1px solid rgba(56,189,248,0.35);";
    }
    return "background:rgba(167,139,250,0.14); color:#C4B5FD; border:1px solid rgba(167,139,250,0.35);";
  }

  function renderTagAndReceiptBadges(tx) {
    const tags = parseTagList(tx.tags);
    const tagPills = tags.map((tag) => {
      const isSelected = state.tagFilter.toLowerCase() === tag.toLowerCase();
      return `<button type="button" onclick="event.stopPropagation(); AppBridge.setTagFilter('${escapeHtml(isSelected ? "ALL" : tag)}')"
                title="Click to filter register by ${escapeHtml(tag)}"
                style="cursor:pointer; font-family:var(--font-mono); font-size:10px; font-weight:600; padding:1px 6px; border-radius:999px; ${getTagColorStyle(tag)} ${isSelected ? 'box-shadow:0 0 0 1px #F8FAFC;' : ''}">
                ${escapeHtml(tag)}
              </button>`;
    }).join("");

    const receiptBadge = tx.receiptRef
      ? `<span title="Attached Receipt / Statement Reference: ${escapeHtml(tx.receiptRef)}"
               style="font-family:var(--font-mono); font-size:10px; padding:1px 6px; border-radius:4px; background:rgba(148,163,184,0.14); color:#E2E8F0; border:1px solid rgba(148,163,184,0.3);">
           📎 ${escapeHtml(tx.receiptRef)}
         </span>`
      : "";

    if (!tagPills && !receiptBadge) return "";
    return `<div style="display:flex; gap:5px; flex-wrap:wrap; align-items:center; margin-top:4px;">${tagPills}${receiptBadge}</div>`;
  }

  function renderQuickEntryRow() {
    const today = new Date().toISOString().slice(0, 10);
    const catOptions = state.categories.map((c) =>
      `<option value="${c.id}" ${c.id === 30 ? "selected" : ""}>${escapeHtml(c.fullPath || c.name)}</option>`
    ).join("");

    return `
      <tr class="quick-entry-row" onkeydown="AppBridge.handleQuickEntryKeyDown(event)">
        <td><input type="date" id="qe-date" class="inline-input" value="${today}" /></td>
        <td><input type="text" id="qe-ref" class="inline-input" placeholder="Ref #" /></td>
        <td>
          <select id="qe-status" class="inline-select">
            <option value="Cleared" selected>[*] Cleared</option>
            <option value="Uncleared">[ ] Uncleared</option>
          </select>
        </td>
        <td>
          <div style="display:grid; grid-template-columns:1.1fr 0.9fr 0.85fr 0.75fr; gap:5px;">
            <input type="text" id="qe-payee" class="inline-input" placeholder="Quick-Add Payee..." />
            <input type="text" id="qe-memo" class="inline-input" placeholder="Memo..." />
            <input type="text" id="qe-tags" class="inline-input" placeholder="#Tag1,#Tag2" />
            <input type="text" id="qe-receipt" class="inline-input" placeholder="📎 Receipt #" />
          </div>
        </td>
        <td><select id="qe-category" class="inline-select">${catOptions}</select></td>
        <td><input type="number" id="qe-payment" step="0.01" min="0" class="inline-input inline-input-num" placeholder="0.00" /></td>
        <td><input type="number" id="qe-deposit" step="0.01" min="0" class="inline-input inline-input-num" placeholder="0.00" /></td>
        <td class="num-col"><span style="font-size:10.5px; color:var(--accent-blue);">QUICK ENTRY</span></td>
        <td style="text-align:right; white-space:nowrap;">
          <button type="button" class="btn btn-gold btn-sm" onclick="AppBridge.submitQuickInlineEntry()">＋ Add</button>
        </td>
      </tr>
    `;
  }

  function renderInlineEditingRow(tx) {
    const paymentVal = tx.amount < 0 ? Math.abs(tx.amount).toFixed(2) : "";
    const depositVal = tx.amount > 0 ? tx.amount.toFixed(2) : "";
    const catOptions = state.categories.map((c) =>
      `<option value="${c.id}" ${(tx.categoryId === c.id || tx.categoryName === c.fullPath) ? "selected" : ""}>
        ${escapeHtml(c.fullPath || c.name)}
      </option>`
    ).join("");

    return `
      <tr class="tx-row editing-row" onkeydown="AppBridge.handleInlineEditKeyDown(event, ${tx.id})">
        <td><input type="date" id="edit-date-${tx.id}" class="inline-input" value="${escapeHtml(tx.transactionDate)}" /></td>
        <td><input type="text" id="edit-ref-${tx.id}" class="inline-input" value="${escapeHtml(tx.referenceNumber || "")}" /></td>
        <td>
          <select id="edit-status-${tx.id}" class="inline-select">
            <option value="Uncleared" ${tx.status === "Uncleared" ? "selected" : ""}>[ ] Uncleared</option>
            <option value="Cleared" ${tx.status === "Cleared" ? "selected" : ""}>[*] Cleared</option>
            <option value="Reconciled" ${tx.status === "Reconciled" ? "selected" : ""}>[R] Reconciled</option>
          </select>
        </td>
        <td>
          <div style="display:grid; grid-template-columns:1.1fr 0.9fr 0.85fr 0.75fr; gap:5px;">
            <input type="text" id="edit-payee-${tx.id}" class="inline-input" value="${escapeHtml(tx.payeeName)}" />
            <input type="text" id="edit-memo-${tx.id}" class="inline-input" value="${escapeHtml(tx.memo || "")}" placeholder="Memo" />
            <input type="text" id="edit-tags-${tx.id}" class="inline-input" value="${escapeHtml(tx.tags || "")}" placeholder="#Tags" />
            <input type="text" id="edit-receipt-${tx.id}" class="inline-input" value="${escapeHtml(tx.receiptRef || "")}" placeholder="📎 Receipt #" />
          </div>
        </td>
        <td>
          ${tx.isSplit
            ? `<span class="split-badge">-- Split Transaction --</span>`
            : `<select id="edit-cat-${tx.id}" class="inline-select">${catOptions}</select>`}
        </td>
        <td><input type="number" id="edit-pay-${tx.id}" step="0.01" min="0" class="inline-input inline-input-num" value="${paymentVal}" placeholder="0.00" /></td>
        <td><input type="number" id="edit-dep-${tx.id}" step="0.01" min="0" class="inline-input inline-input-num" value="${depositVal}" placeholder="0.00" /></td>
        <td class="num-col" style="font-weight:700;">${fmtCurrency(tx.runningBalance)}</td>
        <td style="text-align:right; white-space:nowrap;">
          <button type="button" class="btn btn-gold btn-sm" title="Save (Enter)" onclick="AppBridge.saveInlineEdit(${tx.id})">✓</button>
          <button type="button" class="btn btn-outline btn-sm" title="Cancel (Esc)" onclick="AppBridge.cancelInlineEdit()">✕</button>
        </td>
      </tr>
    `;
  }

  function renderRegisterSection(isCompactPreview = false) {
    const allRows = state.transactions || [];
    let rows = [...allRows];

    if (state.statusFilter !== "ALL") {
      rows = rows.filter((t) => t.status === state.statusFilter);
    }
    if (state.tagFilter && state.tagFilter !== "ALL") {
      const targetTag = state.tagFilter.toLowerCase();
      rows = rows.filter((t) =>
        parseTagList(t.tags).some((tg) => tg.toLowerCase() === targetTag)
      );
    }
    if (state.categoryFilter && state.categoryFilter !== "ALL") {
      const targetCatId = Number(state.categoryFilter);
      rows = rows.filter((t) =>
        t.categoryId === targetCatId ||
        (Array.isArray(t.splits) && t.splits.some((s) => s.categoryId === targetCatId))
      );
    }
    if (state.minAmountFilter !== "" && !Number.isNaN(Number(state.minAmountFilter))) {
      const minAbs = Math.abs(Number(state.minAmountFilter));
      rows = rows.filter((t) => Math.abs(Number(t.amount || 0)) >= minAbs);
    }
    if (state.maxAmountFilter !== "" && !Number.isNaN(Number(state.maxAmountFilter))) {
      const maxAbs = Math.abs(Number(state.maxAmountFilter));
      rows = rows.filter((t) => Math.abs(Number(t.amount || 0)) <= maxAbs);
    }
    if (state.onlyWithReceiptFilter) {
      rows = rows.filter((t) => Boolean(t.receiptRef && String(t.receiptRef).trim() !== ""));
    }
    if (state.searchQuery && state.searchQuery.trim() !== "") {
      const q = state.searchQuery.trim().toLowerCase();
      rows = rows.filter((t) =>
        (t.payeeName || "").toLowerCase().includes(q) ||
        (t.categoryName || "").toLowerCase().includes(q) ||
        (t.memo || "").toLowerCase().includes(q) ||
        (t.referenceNumber || "").toLowerCase().includes(q) ||
        (t.tags || "").toLowerCase().includes(q) ||
        (t.receiptRef || "").toLowerCase().includes(q)
      );
    }

    const totalFilteredCount = rows.length;
    const filteredInflows = rows.filter((t) => t.amount > 0).reduce((s, t) => s + t.amount, 0);
    const filteredOutflows = rows.filter((t) => t.amount < 0).reduce((s, t) => s + Math.abs(t.amount), 0);
    const filteredNet = filteredInflows - filteredOutflows;

    if (isCompactPreview) {
      rows = rows.slice(0, 8);
    }

    // Build dynamic tag cloud counts across allRows
    const tagCountMap = {};
    allRows.forEach((tx) => {
      parseTagList(tx.tags).forEach((tg) => {
        tagCountMap[tg] = (tagCountMap[tg] || 0) + 1;
      });
    });
    const featuredTags = ["#TaxDeductible", "#Ministry", "#Reimbursable", "#FixedBill", "#Family", "#1099Income", "#Payroll"];
    Object.keys(tagCountMap).forEach((tg) => {
      if (!featuredTags.some((f) => f.toLowerCase() === tg.toLowerCase())) {
        featuredTags.push(tg);
      }
    });

    const tagFilterButtonsHtml = featuredTags
      .filter((tg) => (tagCountMap[tg] || 0) > 0 || state.tagFilter.toLowerCase() === tg.toLowerCase())
      .map((tg) => {
        const active = state.tagFilter.toLowerCase() === tg.toLowerCase();
        const cnt = tagCountMap[tg] || 0;
        return `<button type="button" class="filter-pill ${active ? 'active' : ''}" onclick="AppBridge.setTagFilter('${escapeHtml(active ? "ALL" : tg)}')">${escapeHtml(tg)} (${cnt})</button>`;
      }).join("");

    const categoryFilterOptionsHtml = state.categories.map((c) =>
      `<option value="${c.id}" ${String(state.categoryFilter) === String(c.id) ? "selected" : ""}>${escapeHtml(c.fullPath || c.name)}</option>`
    ).join("");

    const hasActiveCustomFilter =
      state.statusFilter !== "ALL" ||
      state.tagFilter !== "ALL" ||
      state.categoryFilter !== "ALL" ||
      state.minAmountFilter !== "" ||
      state.maxAmountFilter !== "" ||
      state.onlyWithReceiptFilter ||
      Boolean(state.searchQuery && state.searchQuery.trim());

    let tableBodyHtml = isCompactPreview ? "" : renderQuickEntryRow();

    rows.forEach((tx) => {
      if (state.editingTransactionId === tx.id) {
        tableBodyHtml += renderInlineEditingRow(tx);
        return;
      }

      const paymentVal = tx.amount < 0 ? Math.abs(tx.amount) : 0;
      const depositVal = tx.amount > 0 ? tx.amount : 0;
      const isExpanded = Boolean(state.expandedSplitIds[tx.id]);

      const categoryCell = tx.isSplit
        ? `<button class="split-badge" onclick="AppBridge.toggleExpandSplit(${tx.id})">
             ⚡ SPLIT (${(tx.splits || []).length}) ${isExpanded ? "▾" : "▸"}
           </button>`
        : `<span class="category-chip">${escapeHtml(tx.categoryName)}</span>`;

      tableBodyHtml += `
        <tr class="tx-row" ondblclick="AppBridge.startInlineEdit(${tx.id})">
          <td style="font-family:var(--font-mono); white-space:nowrap;">${escapeHtml(tx.transactionDate)}</td>
          <td style="font-family:var(--font-mono); color:var(--text-muted);">${escapeHtml(tx.referenceNumber || "—")}</td>
          <td>${renderStatusBadge(tx)}</td>
          <td>
            <div style="font-weight:600; color:var(--text-primary);">${escapeHtml(tx.payeeName)}</div>
            <div style="font-size:11px; color:var(--text-muted);">
              ${state.selectedAccountId ? "" : `<span style="color:var(--accent-blue)">[${escapeHtml(tx.accountName)}]</span> `}
              ${escapeHtml(tx.memo || "")}
            </div>
            ${renderTagAndReceiptBadges(tx)}
          </td>
          <td>${categoryCell}</td>
          <td class="num-col ${paymentVal > 0 ? 'amount-negative' : ''}">${paymentVal > 0 ? fmtCurrency(paymentVal) : ""}</td>
          <td class="num-col ${depositVal > 0 ? 'amount-positive' : ''}">${depositVal > 0 ? fmtCurrency(depositVal) : ""}</td>
          <td class="num-col" style="font-weight:700;">${fmtCurrency(tx.runningBalance)}</td>
          <td style="text-align:right; white-space:nowrap;">
            <button class="row-action-btn" title="Inline Edit (or Double-Click Row)" onclick="AppBridge.startInlineEdit(${tx.id})">✎</button>
            <button class="row-action-btn delete" title="Delete Transaction" onclick="AppBridge.deleteTransaction(${tx.id})">🗑</button>
          </td>
        </tr>
      `;

      if (tx.isSplit && isExpanded && Array.isArray(tx.splits)) {
        tx.splits.forEach((s) => {
          const sPay = s.amount < 0 ? Math.abs(s.amount) : 0;
          const sDep = s.amount > 0 ? s.amount : 0;
          tableBodyHtml += `
            <tr class="split-subrow">
              <td></td>
              <td style="font-family:var(--font-mono); color:var(--accent-indigo);">↳ SPLIT</td>
              <td></td>
              <td style="padding-left:24px;">├─ ${escapeHtml(s.memo || "Split allocation line")}</td>
              <td><span class="category-chip">${escapeHtml(s.categoryName)}</span></td>
              <td class="num-col">${sPay > 0 ? fmtCurrency(sPay) : ""}</td>
              <td class="num-col">${sDep > 0 ? fmtCurrency(sDep) : ""}</td>
              <td></td>
              <td></td>
            </tr>
          `;
        });
      }
    });

    if (!rows.length) {
      tableBodyHtml += `
        <tr>
          <td colspan="9" style="text-align:center; padding:28px; color:var(--text-muted);">
            No transactions match the active multi-criteria filter. <button type="button" class="btn btn-outline btn-sm" style="margin-left:8px;" onclick="AppBridge.resetRegisterFilters()">Reset Filters</button>
          </td>
        </tr>
      `;
    }

    return `
      <section class="panel-card">
        <div class="register-toolbar">
          <div>
            <div class="panel-title">
              ${isCompactPreview ? "Recent Checkbook Register Activity" : "Interactive Checkbook Ledger Register"}
            </div>
            <div class="panel-subtitle">
              Double-click any row (or click ✎) to edit Payee, Memo, #Tags &amp; 📎 Receipt Ref inline • Click any #Tag pill to filter
            </div>
          </div>
          <div style="display:flex; gap:8px; align-items:center; flex-wrap:wrap;">
            <div class="filter-pills">
              <button class="filter-pill ${state.statusFilter === 'ALL' ? 'active' : ''}" onclick="AppBridge.setStatusFilter('ALL')">All (${allRows.length})</button>
              <button class="filter-pill ${state.statusFilter === 'Uncleared' ? 'active' : ''}" onclick="AppBridge.setStatusFilter('Uncleared')">Uncleared</button>
              <button class="filter-pill ${state.statusFilter === 'Cleared' ? 'active' : ''}" onclick="AppBridge.setStatusFilter('Cleared')">Cleared</button>
              <button class="filter-pill ${state.statusFilter === 'Reconciled' ? 'active' : ''}" onclick="AppBridge.setStatusFilter('Reconciled')">Reconciled</button>
            </div>
            <button class="btn btn-outline btn-sm" onclick="AppBridge.openImportStudioModal()">📥 Import OFX/QIF/CSV</button>
            <button class="btn btn-gold btn-sm" onclick="AppBridge.openReconcileModal()">✓ Reconcile Statement</button>
          </div>
        </div>

        <!-- Phase 4C: Multi-Tag Cloud & Multi-Criteria Ledger Filter Bar -->
        <div style="margin-bottom:12px; padding:10px 12px; border-radius:10px; background:rgba(15,23,42,0.65); border:1px solid var(--border-subtle); display:flex; flex-direction:column; gap:8px;">
          <div style="display:flex; justify-content:space-between; align-items:center; flex-wrap:wrap; gap:8px;">
            <div style="display:flex; align-items:center; gap:6px; flex-wrap:wrap;">
              <span style="font-size:10.5px; font-weight:700; text-transform:uppercase; letter-spacing:0.06em; color:var(--accent-gold);">🏷️ Tag Filter:</span>
              <button type="button" class="filter-pill ${state.tagFilter === 'ALL' ? 'active' : ''}" onclick="AppBridge.setTagFilter('ALL')">All Tags</button>
              ${tagFilterButtonsHtml}
            </div>
            <div style="font-family:var(--font-mono); font-size:11px; color:var(--text-secondary);">
              Showing <strong style="color:var(--text-primary);">${totalFilteredCount}</strong> of ${allRows.length} tx •
              Out: <span class="amount-negative">-${fmtCurrency(filteredOutflows)}</span> •
              In: <span class="amount-positive">+${fmtCurrency(filteredInflows)}</span> •
              Net: <strong style="color:${filteredNet >= 0 ? 'var(--accent-emerald)' : 'var(--accent-rose)'};">${fmtCurrency(filteredNet)}</strong>
            </div>
          </div>

          ${isCompactPreview ? "" : `
            <div style="display:flex; gap:10px; align-items:center; flex-wrap:wrap; padding-top:6px; border-top:1px solid rgba(148,163,184,0.12);">
              <div style="display:flex; align-items:center; gap:6px;">
                <label style="font-size:11px; color:var(--text-muted);">Category:</label>
                <select class="inline-select" style="min-width:190px;" onchange="AppBridge.setCategoryFilter(this.value)">
                  <option value="ALL" ${state.categoryFilter === "ALL" ? "selected" : ""}>All Categories</option>
                  ${categoryFilterOptionsHtml}
                </select>
              </div>
              <div style="display:flex; align-items:center; gap:6px;">
                <label style="font-size:11px; color:var(--text-muted);">Min $:</label>
                <input type="number" step="10" min="0" class="inline-input inline-input-num" style="width:85px;" placeholder="0" value="${escapeHtml(state.minAmountFilter)}" oninput="AppBridge.setAmountRangeFilter(this.value, state.maxAmountFilter)" />
                <label style="font-size:11px; color:var(--text-muted);">Max $:</label>
                <input type="number" step="10" min="0" class="inline-input inline-input-num" style="width:85px;" placeholder="Any" value="${escapeHtml(state.maxAmountFilter)}" oninput="AppBridge.setAmountRangeFilter(state.minAmountFilter, this.value)" />
              </div>
              <button type="button" class="filter-pill ${state.onlyWithReceiptFilter ? 'active' : ''}" onclick="AppBridge.toggleReceiptOnlyFilter()">
                📎 With Receipt Ref Only
              </button>
              ${hasActiveCustomFilter ? `
                <button type="button" class="btn btn-outline btn-sm" onclick="AppBridge.resetRegisterFilters()">✕ Reset Filters</button>
              ` : ""}
            </div>
          `}
        </div>

        <div class="ledger-table-wrap">
          <table class="ledger-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Ref #</th>
                <th>Status</th>
                <th>Payee, Memo, #Tags &amp; 📎 Receipt</th>
                <th>Category</th>
                <th class="num-col">Payment (−)</th>
                <th class="num-col">Deposit (+)</th>
                <th class="num-col">Running Balance</th>
                <th style="text-align:right; width:74px;">Actions</th>
              </tr>
            </thead>
            <tbody>
              ${tableBodyHtml}
            </tbody>
          </table>
        </div>
      </section>
    `;
  }

  function setTagFilter(tag) {
    state.tagFilter = tag || "ALL";
    render();
  }

  function setCategoryFilter(catIdOrAll) {
    state.categoryFilter = catIdOrAll || "ALL";
    render();
  }

  function setAmountRangeFilter(minVal, maxVal) {
    state.minAmountFilter = minVal ?? "";
    state.maxAmountFilter = maxVal ?? "";
    render();
  }

  function toggleReceiptOnlyFilter() {
    state.onlyWithReceiptFilter = !state.onlyWithReceiptFilter;
    render();
  }

  function resetRegisterFilters() {
    state.statusFilter = "ALL";
    state.tagFilter = "ALL";
    state.categoryFilter = "ALL";
    state.minAmountFilter = "";
    state.maxAmountFilter = "";
    state.onlyWithReceiptFilter = false;
    state.searchQuery = "";
    render();
  }

  // Inline Edit & Quick Entry Implementation
  function startInlineEdit(txId) {
    state.editingTransactionId = txId;
    render();
    setTimeout(() => {
      document.getElementById(`edit-payee-${txId}`)?.focus();
    }, 20);
  }

  function cancelInlineEdit() {
    state.editingTransactionId = null;
    render();
  }

  function handleInlineEditKeyDown(event, txId) {
    if (event.key === "Enter") {
      event.preventDefault();
      saveInlineEdit(txId);
    } else if (event.key === "Escape") {
      event.preventDefault();
      cancelInlineEdit();
    }
  }

  function saveInlineEdit(txId) {
    const tx = state.transactions.find((t) => t.id === txId);
    if (!tx) return;

    const dateVal = document.getElementById(`edit-date-${txId}`)?.value || tx.transactionDate;
    const refVal = document.getElementById(`edit-ref-${txId}`)?.value.trim() || "";
    const statusVal = document.getElementById(`edit-status-${txId}`)?.value || tx.status;
    const payeeVal = document.getElementById(`edit-payee-${txId}`)?.value.trim() || tx.payeeName;
    const memoVal = document.getElementById(`edit-memo-${txId}`)?.value.trim() || "";
    const tagsVal = document.getElementById(`edit-tags-${txId}`)?.value.trim() || "";
    const receiptVal = document.getElementById(`edit-receipt-${txId}`)?.value.trim() || "";
    const catEl = document.getElementById(`edit-cat-${txId}`);
    const catId = catEl ? Number(catEl.value) : tx.categoryId;

    const payVal = Math.abs(Number(document.getElementById(`edit-pay-${txId}`)?.value || 0));
    const depVal = Math.abs(Number(document.getElementById(`edit-dep-${txId}`)?.value || 0));
    const signedAmount = depVal > 0 && payVal === 0 ? depVal : -payVal;
    const normalizedTags = parseTagList(tagsVal).join(",");

    state.editingTransactionId = null;

    if (isWebViewHost()) {
      postToHost({
        type: "updateTransaction",
        id: txId,
        transactionDate: dateVal,
        referenceNumber: refVal,
        payeeName: payeeVal,
        categoryId: catId,
        memo: memoVal,
        tags: normalizedTags,
        receiptRef: receiptVal || null,
        amount: signedAmount,
        status: statusVal
      });
    } else {
      tx.transactionDate = dateVal;
      tx.referenceNumber = refVal;
      tx.payeeName = payeeVal;
      tx.memo = memoVal;
      tx.tags = normalizedTags;
      tx.receiptRef = receiptVal || null;
      tx.amount = signedAmount;
      tx.status = statusVal;
      if (!tx.isSplit && catId) {
        const catObj = state.categories.find((c) => c.id === catId);
        tx.categoryId = catId;
        tx.categoryName = catObj ? catObj.fullPath : tx.categoryName;
      }
      recalcStandaloneBalances();
      render();
      showToast(`Updated transaction #${txId} (${payeeVal})`);
    }
  }

  function handleQuickEntryKeyDown(event) {
    if (event.key === "Enter") {
      event.preventDefault();
      submitQuickInlineEntry();
    }
  }

  function submitQuickInlineEntry() {
    const payeeName = document.getElementById("qe-payee")?.value.trim();
    if (!payeeName) {
      showToast("Please enter a Payee name for the quick-entry row.");
      document.getElementById("qe-payee")?.focus();
      return;
    }

    const accountId = state.selectedAccountId || 1;
    const transactionDate = document.getElementById("qe-date")?.value || new Date().toISOString().slice(0, 10);
    const referenceNumber = document.getElementById("qe-ref")?.value.trim() || "REG";
    const status = document.getElementById("qe-status")?.value || "Cleared";
    const memo = document.getElementById("qe-memo")?.value.trim() || "";
    const rawTags = document.getElementById("qe-tags")?.value.trim() || "";
    const receiptRef = document.getElementById("qe-receipt")?.value.trim() || null;
    const normalizedTags = parseTagList(rawTags).join(",");
    const categoryId = Number(document.getElementById("qe-category")?.value || 30);
    const payVal = Math.abs(Number(document.getElementById("qe-payment")?.value || 0));
    const depVal = Math.abs(Number(document.getElementById("qe-deposit")?.value || 0));

    if (payVal === 0 && depVal === 0) {
      showToast("Enter either a Payment (−) or Deposit (+) amount.");
      document.getElementById("qe-payment")?.focus();
      return;
    }

    const signedAmount = depVal > 0 && payVal === 0 ? depVal : -payVal;

    if (isWebViewHost()) {
      postToHost({
        type: "createTransaction",
        accountId,
        transactionDate,
        payeeName,
        categoryId,
        referenceNumber,
        status,
        memo,
        tags: normalizedTags,
        receiptRef,
        amount: signedAmount,
        splits: []
      });
    } else {
      const catObj = state.categories.find((c) => c.id === categoryId);
      state.transactions.unshift({
        id: Date.now(),
        accountId,
        accountName: (state.accounts.find((a) => a.id === accountId) || {}).name || "Primary Checking",
        transactionDate,
        referenceNumber,
        payeeName,
        categoryId,
        categoryName: catObj ? catObj.fullPath : "Uncategorized",
        memo,
        tags: normalizedTags,
        receiptRef,
        amount: signedAmount,
        runningBalance: 0,
        status,
        isSplit: false,
        splits: []
      });
      recalcStandaloneBalances();
      render();
      showToast(`Added ${payeeName} (${fmtCurrency(signedAmount)})`);
    }
  }

  function deleteTransaction(txId) {
    if (isWebViewHost()) {
      postToHost({ type: "deleteTransaction", transactionId: txId });
    } else {
      state.transactions = state.transactions.filter((t) => t.id !== txId);
      recalcStandaloneBalances();
      render();
      showToast(`Deleted transaction #${txId}`);
    }
  }

  // Guided Statement Reconciliation Workflow (Phase 2B)
  function openReconcileModal(defaultAccountId) {
    const targetAccId = defaultAccountId || state.selectedAccountId || 1;
    state.reconcileSession.accountId = targetAccId;

    const accSelect = document.getElementById("rec-account");
    if (accSelect) {
      accSelect.innerHTML = state.accounts.map((a) =>
        `<option value="${a.id}" ${a.id === targetAccId ? "selected" : ""}>
          ${escapeHtml(a.name)} (${escapeHtml(a.accountNumberMask)})
        </option>`
      ).join("");
    }

    const dateInput = document.getElementById("rec-date");
    if (dateInput) {
      dateInput.value = new Date().toISOString().slice(0, 10);
    }

    initializeReconcileAccountState(targetAccId);
    document.getElementById("reconcile-modal-overlay").classList.remove("hidden");
  }

  function closeReconcileModal() {
    document.getElementById("reconcile-modal-overlay").classList.add("hidden");
  }

  function onReconcileAccountChanged() {
    const accId = Number(document.getElementById("rec-account")?.value || 1);
    initializeReconcileAccountState(accId);
  }

  function initializeReconcileAccountState(accId) {
    state.reconcileSession.accountId = accId;
    const acc = state.accounts.find((a) => a.id === accId) || state.accounts[0];
    const checkedMap = {};

    state.transactions
      .filter((t) => t.accountId === accId && t.status !== "Reconciled")
      .forEach((t) => {
        checkedMap[t.id] = t.status === "Cleared";
      });

    state.reconcileSession.checkedTxIds = checkedMap;

    const targetInput = document.getElementById("rec-target-balance");
    if (targetInput && acc) {
      targetInput.value = Number(acc.clearedBalance || 0).toFixed(2);
    }

    recalcReconciliationMath();
  }

  function getReconciliationMetrics() {
    const accId = state.reconcileSession.accountId;
    const acc = state.accounts.find((a) => a.id === accId) || { openingBalance: 0 };
    const accTransactions = state.transactions.filter((t) => t.accountId === accId);

    const reconciledDelta = accTransactions
      .filter((t) => t.status === "Reconciled")
      .reduce((sum, t) => sum + Number(t.amount || 0), 0);

    const beginningReconciledBalance = Number(acc.openingBalance || 0) + reconciledDelta;

    const candidates = accTransactions.filter((t) => t.status !== "Reconciled");
    let clearedDeposits = 0;
    let clearedPayments = 0;
    let checkedCount = 0;

    candidates.forEach((t) => {
      if (state.reconcileSession.checkedTxIds[t.id]) {
        checkedCount++;
        if (t.amount > 0) clearedDeposits += t.amount;
        else clearedPayments += Math.abs(t.amount);
      }
    });

    const calculatedClearedBalance = beginningReconciledBalance + clearedDeposits - clearedPayments;
    const targetEndingBalance = Number(document.getElementById("rec-target-balance")?.value || 0);
    // Variance = Target Statement Ending Balance - Calculated Cleared Balance
    const variance = Math.round((targetEndingBalance - calculatedClearedBalance) * 100) / 100;

    return {
      accId,
      beginningReconciledBalance,
      clearedDeposits,
      clearedPayments,
      calculatedClearedBalance,
      targetEndingBalance,
      variance,
      checkedCount,
      candidates
    };
  }

  function recalcReconciliationMath() {
    const m = getReconciliationMetrics();
    const isZeroVariance = Math.abs(m.variance) < 0.005;

    const scoreboard = document.getElementById("reconcile-scoreboard");
    if (scoreboard) {
      scoreboard.innerHTML = `
        <div class="import-stat-pill">
          <div class="kpi-label">Beginning Reconciled</div>
          <div style="font-family:var(--font-mono); font-weight:700; font-size:14px; margin-top:2px;">${fmtCurrency(m.beginningReconciledBalance)}</div>
        </div>
        <div class="import-stat-pill">
          <div class="kpi-label">+ Cleared Deposits</div>
          <div class="amount-positive" style="font-family:var(--font-mono); font-weight:700; font-size:14px; margin-top:2px;">+${fmtCurrency(m.clearedDeposits)}</div>
        </div>
        <div class="import-stat-pill">
          <div class="kpi-label">− Cleared Payments</div>
          <div class="amount-negative" style="font-family:var(--font-mono); font-weight:700; font-size:14px; margin-top:2px;">−${fmtCurrency(m.clearedPayments)}</div>
        </div>
        <div class="import-stat-pill">
          <div class="kpi-label">= Calculated Cleared</div>
          <div style="font-family:var(--font-mono); font-weight:700; font-size:14px; color:var(--accent-blue); margin-top:2px;">${fmtCurrency(m.calculatedClearedBalance)}</div>
        </div>
        <div class="import-stat-pill ${isZeroVariance ? "variance-zero" : "variance-nonzero"}">
          <div class="kpi-label">Variance (Target − Cleared)</div>
          <div style="font-family:var(--font-mono); font-weight:800; font-size:15px; color:${isZeroVariance ? "var(--accent-emerald)" : "var(--accent-gold)"}; margin-top:2px;">
            ${fmtCurrency(m.variance)} ${isZeroVariance ? "✓ BALANCED" : "Δ"}
          </div>
        </div>
      `;
    }

    const tbody = document.getElementById("reconcile-tbody");
    if (tbody) {
      if (m.candidates.length === 0) {
        tbody.innerHTML = `
          <tr>
            <td colspan="8" style="text-align:center; padding:24px; color:var(--text-muted);">
              All transactions in this account are already [R] Reconciled!
            </td>
          </tr>
        `;
      } else {
        tbody.innerHTML = m.candidates.map((t) => {
          const checked = Boolean(state.reconcileSession.checkedTxIds[t.id]);
          const pay = t.amount < 0 ? Math.abs(t.amount) : 0;
          const dep = t.amount > 0 ? t.amount : 0;
          return `
            <tr class="tx-row" onclick="AppBridge.toggleReconcileRow(${t.id})" style="cursor:pointer; ${checked ? "background:rgba(16, 185, 129, 0.08);" : ""}">
              <td style="text-align:center;" onclick="event.stopPropagation()">
                <input type="checkbox" ${checked ? "checked" : ""} onchange="AppBridge.toggleReconcileRow(${t.id}, this.checked)" />
              </td>
              <td style="font-family:var(--font-mono);">${escapeHtml(t.transactionDate)}</td>
              <td style="font-family:var(--font-mono); color:var(--text-muted);">${escapeHtml(t.referenceNumber || "—")}</td>
              <td>
                <span class="status-badge ${t.status === "Cleared" ? "status-cleared" : "status-uncleared"}">
                  ${t.status === "Cleared" ? "[*] Cleared" : "[ ] Uncleared"}
                </span>
              </td>
              <td>
                <div style="font-weight:600;">${escapeHtml(t.payeeName)}</div>
                <div style="font-size:11px; color:var(--text-muted);">${escapeHtml(t.memo || "")}</div>
              </td>
              <td><span class="category-chip">${escapeHtml(t.categoryName)}</span></td>
              <td class="num-col amount-negative">${pay > 0 ? fmtCurrency(pay) : ""}</td>
              <td class="num-col amount-positive">${dep > 0 ? fmtCurrency(dep) : ""}</td>
            </tr>
          `;
        }).join("");
      }
    }

    const hintEl = document.getElementById("rec-variance-hint");
    const adjustBtn = document.getElementById("rec-adjust-btn");
    if (hintEl) {
      hintEl.innerHTML = isZeroVariance
        ? `<span style="color:var(--accent-emerald); font-weight:700;">✓ $0.00 Variance achieved!</span> Ready to lock ${m.checkedCount} checked transaction(s) as [R] Reconciled.`
        : `<span style="color:var(--accent-gold); font-weight:600;">Variance is ${fmtCurrency(m.variance)}.</span> Check/uncheck items above or click <strong>Auto-Adjust Variance &amp; Lock</strong> to insert a ${fmtCurrency(m.variance)} adjustment.`;
    }
    if (adjustBtn) {
      adjustBtn.classList.toggle("hidden", isZeroVariance);
    }
  }

  function toggleReconcileRow(txId, explicitVal) {
    const curr = Boolean(state.reconcileSession.checkedTxIds[txId]);
    state.reconcileSession.checkedTxIds[txId] = explicitVal !== undefined ? explicitVal : !curr;
    recalcReconciliationMath();
  }

  function reconcileCheckAll(flag) {
    const accId = state.reconcileSession.accountId;
    state.transactions
      .filter((t) => t.accountId === accId && t.status !== "Reconciled")
      .forEach((t) => {
        state.reconcileSession.checkedTxIds[t.id] = flag;
      });
    recalcReconciliationMath();
  }

  function snapStatementTargetToCleared() {
    const m = getReconciliationMetrics();
    const targetInput = document.getElementById("rec-target-balance");
    if (targetInput) {
      targetInput.value = m.calculatedClearedBalance.toFixed(2);
    }
    recalcReconciliationMath();
  }

  function finishReconciliation(createAdjustmentIfNeeded) {
    const m = getReconciliationMetrics();
    if (!createAdjustmentIfNeeded && Math.abs(m.variance) >= 0.005) {
      showToast(`Variance is ${fmtCurrency(m.variance)}. Reach $0.00 variance or click 'Auto-Adjust Variance & Lock'.`);
      return;
    }

    const reconciledIds = [];
    const uncheckedIds = [];
    m.candidates.forEach((t) => {
      if (state.reconcileSession.checkedTxIds[t.id]) reconciledIds.push(t.id);
      else uncheckedIds.push(t.id);
    });

    const statementDate = document.getElementById("rec-date")?.value || new Date().toISOString().slice(0, 10);

    if (isWebViewHost()) {
      postToHost({
        type: "completeReconciliation",
        accountId: m.accId,
        statementDate,
        statementEndingBalance: m.targetEndingBalance,
        reconciledTransactionIds: reconciledIds,
        uncheckedTransactionIds: uncheckedIds,
        createAdjustmentIfNeeded: Boolean(createAdjustmentIfNeeded),
        adjustmentAmount: m.variance
      });
    } else {
      state.transactions.forEach((t) => {
        if (reconciledIds.includes(t.id)) t.status = "Reconciled";
        else if (uncheckedIds.includes(t.id) && t.status !== "Reconciled") t.status = "Uncleared";
      });

      if (createAdjustmentIfNeeded && Math.abs(m.variance) >= 0.005) {
        state.transactions.unshift({
          id: Date.now(),
          accountId: m.accId,
          accountName: (state.accounts.find((a) => a.id === m.accId) || {}).name || "Primary Checking",
          transactionDate: statementDate,
          referenceNumber: "ADJ",
          payeeName: "Statement Balance Adjustment",
          categoryName: "Uncategorized",
          memo: "Automated reconciliation variance adjustment",
          amount: m.variance,
          runningBalance: m.targetEndingBalance,
          status: "Reconciled",
          isSplit: false,
          splits: []
        });
      }

      recalcStandaloneBalances();
      render();
      showToast(`Statement reconciled! Locked ${reconciledIds.length} transaction(s) at ${fmtCurrency(m.targetEndingBalance)}.`);
    }

    closeReconcileModal();
  }

  function renderRulesEngineView() {
    const catOptions = state.categories.map((c) =>
      `<option value="${c.id}">${escapeHtml(c.fullPath || c.name)}</option>`
    ).join("");

    const rulesRows = (state.rules || []).map((r) => {
      const amtRange = (r.minAmount != null || r.maxAmount != null)
        ? ` AND Amount ${r.minAmount != null ? `≥ $${r.minAmount}` : ""} ${r.maxAmount != null ? `≤ $${r.maxAmount}` : ""}`
        : "";

      return `
        <div class="rule-card ${r.isEnabled ? "" : "disabled"}">
          <div style="display:flex; flex-direction:column; gap:6px;">
            <div style="display:flex; align-items:center; gap:8px;">
              <span class="kicker-pill" style="margin:0;">PRIORITY #${r.priority}</span>
              <strong style="font-size:14px;">${escapeHtml(r.name)}</strong>
            </div>
            <div style="display:flex; gap:8px; flex-wrap:wrap; align-items:center;">
              <span class="rule-clause">
                <span class="clause-tag clause-if">IF</span>
                <span>${escapeHtml(r.matchField)} ${escapeHtml(r.matchOperator)} <code style="color:var(--accent-gold)">"${escapeHtml(r.matchPattern)}"</code>${escapeHtml(amtRange)}</span>
              </span>
              <span style="color:var(--text-muted)">→</span>
              <span class="rule-clause">
                <span class="clause-tag clause-then">THEN</span>
                <span>Rename to <strong>"${escapeHtml(r.renamePayeeTo || "Keep Original")}"</strong> • Assign <span style="color:var(--accent-blue)">${escapeHtml(r.assignCategoryName)}</span></span>
              </span>
            </div>
          </div>
          <div style="display:flex; align-items:center; gap:8px;">
            <button class="btn btn-outline btn-sm" onclick="AppBridge.toggleRule(${r.id})">
              ${r.isEnabled ? "✓ Enabled" : "○ Disabled"}
            </button>
            <button class="btn btn-outline btn-sm" style="color:var(--accent-rose);" onclick="AppBridge.deleteRule(${r.id})">✕</button>
          </div>
        </div>
      `;
    }).join("");

    return `
      <section class="panel-card">
        <div class="panel-header">
          <div>
            <div class="panel-title">Create New IF/THEN Payee &amp; Categorization Rule</div>
            <div class="panel-subtitle">Rules execute automatically in priority order during OFX/QFX/QIF/CSV import and on-demand across the register</div>
          </div>
          <button class="btn btn-gold btn-sm" onclick="AppBridge.applyRulesToRegister()">⚡ Run Active Rules on Register Now</button>
        </div>

        <form onsubmit="AppBridge.submitNewRule(event)" class="form-grid" style="grid-template-columns: repeat(auto-fit, minmax(175px, 1fr)); align-items:end;">
          <div class="form-field">
            <label>Rule Name</label>
            <input type="text" id="rule-name" placeholder="e.g., Starbucks Coffee Rule" required />
          </div>
          <div class="form-field">
            <label>IF Match Field</label>
            <select id="rule-field">
              <option value="Payee">Payee String</option>
              <option value="Memo">Memo Line</option>
              <option value="PayeeOrMemo">Payee or Memo</option>
            </select>
          </div>
          <div class="form-field">
            <label>Operator</label>
            <select id="rule-op">
              <option value="Contains">Contains</option>
              <option value="StartsWith">Starts With</option>
              <option value="Equals">Equals</option>
              <option value="Regex">Regex Pattern</option>
            </select>
          </div>
          <div class="form-field">
            <label>Match Pattern</label>
            <input type="text" id="rule-pattern" placeholder="e.g., TST* SBUX" required />
          </div>
          <div class="form-field">
            <label>THEN Rename Payee To</label>
            <input type="text" id="rule-rename" placeholder="e.g., Starbucks Coffee" required />
          </div>
          <div class="form-field">
            <label>Assign Category</label>
            <select id="rule-cat">${catOptions}</select>
          </div>
          <div class="form-field">
            <label>Priority</label>
            <input type="number" id="rule-priority" value="25" min="1" max="999" />
          </div>
          <div class="form-field">
            <button type="submit" class="btn btn-gold" style="justify-content:center; height:36px;">＋ Save Rule to SQLite</button>
          </div>
        </form>
      </section>

      <section class="panel-card">
        <div class="panel-header">
          <div>
            <div class="panel-title">Active Rules Pipeline (${state.rules.length} Rules Configured)</div>
            <div class="panel-subtitle">Lower priority number executes first</div>
          </div>
        </div>
        <div class="rules-list">
          ${rulesRows}
        </div>
      </section>
    `;
  }

  function computeBudgetSummary() {
    if (isWebViewHost() && state.budgetSummary && Array.isArray(state.budgetSummary.envelopes)) {
      return state.budgetSummary;
    }

    const expenseCats = state.categories.filter((c) => c.categoryType === "Expense" && c.parentId != null);
    const activityMap = {};

    state.transactions.forEach((tx) => {
      if (!String(tx.transactionDate || "").startsWith(state.budgetMonthKey)) return;
      if (tx.isSplit && Array.isArray(tx.splits) && tx.splits.length > 0) {
        tx.splits.forEach((s) => {
          const cat = state.categories.find((c) => c.id === s.categoryId || c.fullPath === s.categoryName);
          if (cat && cat.categoryType === "Expense") {
            activityMap[cat.id] = (activityMap[cat.id] || 0) + Math.max(0, -Number(s.amount || 0));
          }
        });
      } else {
        const cat = state.categories.find((c) => c.id === tx.categoryId || c.fullPath === tx.categoryName);
        if (cat && cat.categoryType === "Expense") {
          activityMap[cat.id] = (activityMap[cat.id] || 0) + Math.max(0, -Number(tx.amount || 0));
        }
      }
    });

    const envelopes = expenseCats.map((cat) => {
      const envState = state.budgetEnvelopeState[cat.id] || { rollover: 0, assigned: cat.monthlyBudget || 0 };
      const rolloverAmount = Number(envState.rollover || 0);
      const assignedAmount = Number(envState.assigned || 0);
      const monthlyTarget = Number(cat.monthlyBudget || 0);
      const activityAmount = Math.round((activityMap[cat.id] || 0) * 100) / 100;
      const availableAmount = Math.round((rolloverAmount + assignedAmount - activityAmount) * 100) / 100;
      const totalFunded = rolloverAmount + assignedAmount;
      const fundingProgressPercent = monthlyTarget > 0 ? Math.min(100, Math.round((assignedAmount / monthlyTarget) * 100)) : 100;
      const spendProgressPercent = totalFunded > 0 ? Math.min(100, Math.round((activityAmount / totalFunded) * 100)) : (activityAmount > 0 ? 100 : 0);
      const fundingStatus = availableAmount < 0 ? "Overspent" : (assignedAmount < monthlyTarget ? "Underfunded" : "Funded");

      return {
        categoryId: cat.id,
        categoryName: cat.name,
        parentGroupName: cat.parentName || (cat.fullPath ? cat.fullPath.split(":")[0] : "Expenses"),
        colorHex: cat.colorHex || "#38BDF8",
        monthlyTarget,
        rolloverAmount,
        assignedAmount,
        activityAmount,
        availableAmount,
        fundingProgressPercent,
        spendProgressPercent,
        fundingStatus
      };
    });

    const totalRollover = envelopes.reduce((acc, e) => acc + e.rolloverAmount, 0);
    const totalAssigned = envelopes.reduce((acc, e) => acc + e.assignedAmount, 0);
    const totalActivitySpent = envelopes.reduce((acc, e) => acc + e.activityAmount, 0);
    const totalAvailableInEnvelopes = envelopes.reduce((acc, e) => acc + e.availableAmount, 0);
    const totalUnderfunded = envelopes
      .filter((e) => e.assignedAmount < e.monthlyTarget)
      .reduce((acc, e) => acc + (e.monthlyTarget - e.assignedAmount), 0);

    const totalInflowPool = 5650.00;
    const readyToAssign = Math.round((totalInflowPool - totalAssigned) * 100) / 100;

    return {
      monthKey: state.budgetMonthKey,
      monthDisplay: "November 2026",
      totalInflowPool,
      totalRollover,
      totalAssigned,
      totalActivitySpent,
      totalAvailableInEnvelopes,
      readyToAssign,
      totalUnderfunded,
      ageOfMoneyDays: 38,
      envelopes
    };
  }

  function updateEnvelopeAssigned(categoryId, valueStr) {
    const assignedAmount = Math.max(0, Number(valueStr || 0));
    if (isWebViewHost()) {
      postToHost({
        type: "updateBudgetEnvelope",
        monthKey: state.budgetMonthKey,
        categoryId,
        assignedAmount
      });
    } else {
      if (!state.budgetEnvelopeState[categoryId]) {
        state.budgetEnvelopeState[categoryId] = { rollover: 0, assigned: 0 };
      }
      state.budgetEnvelopeState[categoryId].assigned = assignedAmount;
      render();
      showToast(`Assigned ${fmtCurrency(assignedAmount)} to envelope.`);
    }
  }

  function updateEnvelopeTarget(categoryId, valueStr) {
    const monthlyTarget = Math.max(0, Number(valueStr || 0));
    const cat = state.categories.find((c) => c.id === categoryId);
    const currentAssigned = state.budgetEnvelopeState[categoryId]?.assigned ?? (cat?.monthlyBudget || 0);

    if (isWebViewHost()) {
      postToHost({
        type: "updateBudgetEnvelope",
        monthKey: state.budgetMonthKey,
        categoryId,
        assignedAmount: currentAssigned,
        monthlyTarget
      });
    } else {
      if (cat) cat.monthlyBudget = monthlyTarget;
      render();
      showToast(`Updated monthly target goal to ${fmtCurrency(monthlyTarget)}.`);
    }
  }

  function quickFundEnvelope(categoryId, mode) {
    const summary = computeBudgetSummary();
    const env = summary.envelopes.find((e) => e.categoryId === categoryId);
    if (!env) return;

    let nextAssigned = env.assignedAmount;
    if (mode === "target") {
      nextAssigned = env.monthlyTarget;
    } else if (mode === "cover") {
      nextAssigned = Math.max(env.assignedAmount, env.activityAmount - env.rolloverAmount);
    } else {
      nextAssigned = env.assignedAmount + Number(mode || 50);
    }

    updateEnvelopeAssigned(categoryId, nextAssigned);
  }

  function autoFundAllEnvelopes() {
    if (isWebViewHost()) {
      postToHost({
        type: "autoFundBudget",
        monthKey: state.budgetMonthKey
      });
    } else {
      const summary = computeBudgetSummary();
      let fundedCount = 0;
      summary.envelopes.forEach((env) => {
        if (env.assignedAmount < env.monthlyTarget) {
          if (!state.budgetEnvelopeState[env.categoryId]) {
            state.budgetEnvelopeState[env.categoryId] = { rollover: env.rolloverAmount, assigned: env.monthlyTarget };
          } else {
            state.budgetEnvelopeState[env.categoryId].assigned = env.monthlyTarget;
          }
          fundedCount++;
        }
      });
      render();
      showToast(`Auto-funded ${fundedCount} underfunded envelope(s) to 100% of their monthly targets.`);
    }
  }

  function submitMoveEnvelopeFunds(e) {
    e.preventDefault();
    const fromCategoryId = Number(document.getElementById("move-from-cat")?.value || 0);
    const toCategoryId = Number(document.getElementById("move-to-cat")?.value || 0);
    const amount = Math.abs(Number(document.getElementById("move-amt")?.value || 0));

    if (!fromCategoryId || !toCategoryId || fromCategoryId === toCategoryId || amount <= 0) {
      showToast("Choose two distinct envelopes and a positive dollar amount to move.");
      return;
    }

    if (isWebViewHost()) {
      postToHost({
        type: "moveEnvelopeFunds",
        monthKey: state.budgetMonthKey,
        fromCategoryId,
        toCategoryId,
        amount
      });
    } else {
      const fromEnv = state.budgetEnvelopeState[fromCategoryId] || { rollover: 0, assigned: 0 };
      const toEnv = state.budgetEnvelopeState[toCategoryId] || { rollover: 0, assigned: 0 };
      const actualMoved = Math.min(fromEnv.assigned, amount);
      fromEnv.assigned = Math.max(0, fromEnv.assigned - actualMoved);
      toEnv.assigned += actualMoved;
      state.budgetEnvelopeState[fromCategoryId] = fromEnv;
      state.budgetEnvelopeState[toCategoryId] = toEnv;
      render();
      showToast(`Moved ${fmtCurrency(actualMoved)} between envelopes.`);
    }
  }

  function renderBudgetsView() {
    const b = computeBudgetSummary();
    const isOverassigned = b.readyToAssign < -0.005;
    const isZeroBasedDone = Math.abs(b.readyToAssign) <= 0.005;

    // Group envelopes by ParentGroupName
    const groups = {};
    b.envelopes.forEach((env) => {
      if (!groups[env.parentGroupName]) {
        groups[env.parentGroupName] = [];
      }
      groups[env.parentGroupName].push(env);
    });

    const envelopeOptions = b.envelopes.map((e) =>
      `<option value="${e.categoryId}">${escapeHtml(e.parentGroupName)}: ${escapeHtml(e.categoryName)} (Avail: ${fmtCurrency(e.availableAmount)})</option>`
    ).join("");

    const groupRowsHtml = Object.entries(groups).map(([groupName, items]) => {
      const grpTarget = items.reduce((s, x) => s + x.monthlyTarget, 0);
      const grpRollover = items.reduce((s, x) => s + x.rolloverAmount, 0);
      const grpAssigned = items.reduce((s, x) => s + x.assignedAmount, 0);
      const grpActivity = items.reduce((s, x) => s + x.activityAmount, 0);
      const grpAvail = items.reduce((s, x) => s + x.availableAmount, 0);

      const headerRow = `
        <tr class="budget-group-row">
          <td>▾ ${escapeHtml(groupName)} (${items.length} Envelopes)</td>
          <td class="num-col">${fmtCurrency(grpTarget)}</td>
          <td class="num-col">${fmtCurrency(grpRollover)}</td>
          <td class="num-col">${fmtCurrency(grpAssigned)}</td>
          <td class="num-col">-${fmtCurrency(grpActivity)}</td>
          <td></td>
          <td class="num-col">${fmtCurrency(grpAvail)}</td>
          <td></td>
        </tr>
      `;

      const itemRows = items.map((env) => {
        const pillClass = env.fundingStatus === "Overspent"
          ? "avail-overspent"
          : env.fundingStatus === "Underfunded"
            ? "avail-underfunded"
            : "avail-funded";

        const statusLabel = env.fundingStatus === "Overspent"
          ? "⚠ Overspent"
          : env.fundingStatus === "Underfunded"
            ? `Under by ${fmtCurrency(env.monthlyTarget - env.assignedAmount)}`
            : "✓ Funded";

        const barColor = env.fundingStatus === "Overspent"
          ? "#F43F5E"
          : env.spendProgressPercent >= 85
            ? "#F59E0B"
            : env.colorHex || "#10B981";

        return `
          <tr class="tx-row">
            <td>
              <div style="display:flex; align-items:center; gap:8px;">
                <span class="legend-dot" style="background:${escapeHtml(env.colorHex)}"></span>
                <div>
                  <div style="font-weight:600; color:var(--text-primary);">${escapeHtml(env.categoryName)}</div>
                  <div style="font-size:10.5px; color:var(--text-muted);">${escapeHtml(statusLabel)}</div>
                </div>
              </div>
            </td>
            <td class="num-col" style="width:120px;">
              <input type="number" step="5" class="inline-input inline-input-num"
                     style="width:96px; border-color:var(--border-subtle);"
                     value="${env.monthlyTarget.toFixed(2)}"
                     title="Edit Monthly Target Goal"
                     onchange="AppBridge.updateEnvelopeTarget(${env.categoryId}, this.value)" />
            </td>
            <td class="num-col" style="color:var(--text-secondary);">
              ${env.rolloverAmount > 0 ? `+${fmtCurrency(env.rolloverAmount)}` : "$0.00"}
            </td>
            <td class="num-col" style="width:130px;">
              <input type="number" step="10" class="inline-input inline-input-num"
                     style="width:104px;"
                     value="${env.assignedAmount.toFixed(2)}"
                     title="Assign Dollars to Envelope"
                     onchange="AppBridge.updateEnvelopeAssigned(${env.categoryId}, this.value)" />
            </td>
            <td class="num-col ${env.activityAmount > 0 ? "amount-negative" : ""}">
              ${env.activityAmount > 0 ? `-${fmtCurrency(env.activityAmount)}` : "$0.00"}
            </td>
            <td style="width:155px;">
              <div class="progress-track" style="height:8px; margin-bottom:4px;">
                <div class="progress-fill" style="width:${env.spendProgressPercent}%; background:${barColor}"></div>
              </div>
              <div style="display:flex; justify-content:space-between; font-size:10px; color:var(--text-muted);">
                <span>Funded ${env.fundingProgressPercent}%</span>
                <span>Spent ${env.spendProgressPercent}%</span>
              </div>
            </td>
            <td class="num-col">
              <span class="envelope-avail-pill ${pillClass}">
                ${fmtCurrency(env.availableAmount)}
              </span>
            </td>
            <td style="text-align:right; white-space:nowrap;">
              <button class="row-action-btn" title="Assign +$50.00" onclick="AppBridge.quickFundEnvelope(${env.categoryId}, 50)">+$50</button>
              <button class="row-action-btn" title="Match Monthly Target (${fmtCurrency(env.monthlyTarget)})" onclick="AppBridge.quickFundEnvelope(${env.categoryId}, 'target')">Target</button>
              ${env.availableAmount < 0 ? `<button class="row-action-btn" style="color:var(--accent-rose); font-weight:700;" title="Cover Overspending" onclick="AppBridge.quickFundEnvelope(${env.categoryId}, 'cover')">Cover</button>` : ""}
            </td>
          </tr>
        `;
      }).join("");

      return headerRow + itemRows;
    }).join("");

    return `
      <!-- YNAB-Style Ready to Assign Scoreboard -->
      <section class="rta-banner ${isOverassigned ? "overassigned" : ""}">
        <div>
          <div class="kicker-pill" style="margin-bottom:4px;">ZERO-BASED ENVELOPE BUDGET • ${escapeHtml(b.monthDisplay.toUpperCase())}</div>
          <div style="display:flex; align-items:baseline; gap:14px; flex-wrap:wrap;">
            <div style="font-size:28px; font-weight:800; font-family:var(--font-mono); color:${isOverassigned ? "var(--accent-rose)" : isZeroBasedDone ? "var(--accent-gold)" : "var(--accent-emerald)"};">
              ${fmtCurrency(b.readyToAssign)}
            </div>
            <div>
              <div style="font-weight:700; font-size:14px;">
                ${isOverassigned
                  ? "⚠ Over-Assigned! Reduce envelope assignments to restore a non-negative pool."
                  : isZeroBasedDone
                    ? "✓ Every Dollar Has a Job! ($0.00 Unassigned)"
                    : "Ready to Assign — Give Every Dollar a Job"}
              </div>
              <div style="font-size:12px; color:var(--text-secondary);">
                Assignable Inflow Pool: <strong>${fmtCurrency(b.totalInflowPool)}</strong> − Assigned This Month: <strong>${fmtCurrency(b.totalAssigned)}</strong>
                • Rollover Carried In: <strong>+${fmtCurrency(b.totalRollover)}</strong>
              </div>
            </div>
          </div>
        </div>
        <div style="display:flex; align-items:center; gap:10px; flex-wrap:wrap;">
          <div class="import-stat-pill" style="padding:8px 14px;">
            <div class="kpi-label">Age of Money</div>
            <div style="font-family:var(--font-mono); font-size:15px; font-weight:800; color:var(--accent-gold);">${b.ageOfMoneyDays} Days</div>
          </div>
          <button type="button" class="btn btn-outline" onclick="AppBridge.openCategoryModal()">＋ Add Category</button>
          <button class="btn btn-gold" onclick="AppBridge.autoFundAllEnvelopes()">
            ⚡ Auto-Fund Underfunded (${fmtCurrency(b.totalUnderfunded)})
          </button>
        </div>
      </section>

      <!-- Move Money Between Envelopes Bar -->
      <section class="panel-card" style="padding:12px 16px;">
        <form onsubmit="AppBridge.submitMoveEnvelopeFunds(event)" style="display:flex; align-items:center; justify-content:space-between; gap:12px; flex-wrap:wrap;">
          <div style="font-size:12.5px; font-weight:700; color:var(--accent-blue);">
            ⇄ Roll With The Punches (Move Envelope Funds)
          </div>
          <div style="display:flex; align-items:center; gap:8px; flex-wrap:wrap; flex:1; justify-content:flex-end;">
            <span style="font-size:11.5px; color:var(--text-secondary);">From:</span>
            <select id="move-from-cat" class="inline-select" style="width:220px;">${envelopeOptions}</select>
            <span style="font-size:11.5px; color:var(--text-secondary);">To:</span>
            <select id="move-to-cat" class="inline-select" style="width:220px;">
              ${b.envelopes.slice().reverse().map((e) =>
                `<option value="${e.categoryId}">${escapeHtml(e.parentGroupName)}: ${escapeHtml(e.categoryName)} (Avail: ${fmtCurrency(e.availableAmount)})</option>`
              ).join("")}
            </select>
            <input type="number" id="move-amt" step="5" value="50.00" class="inline-input inline-input-num" style="width:95px;" placeholder="Amount" required />
            <button type="submit" class="btn btn-outline btn-sm">⇄ Move Dollars</button>
          </div>
        </form>
      </section>

      <!-- Zero-Based Envelope Grid -->
      <section class="register-container">
        <div class="register-toolbar">
          <div style="font-size:12.5px; font-weight:600;">
            Envelope Categories (${b.envelopes.length} Active Envelopes)
            <span style="color:var(--text-muted); font-weight:400; margin-left:8px;">
              Available = Rollover (${fmtCurrency(b.totalRollover)}) + Assigned (${fmtCurrency(b.totalAssigned)}) − Activity (${fmtCurrency(b.totalActivitySpent)}) = <strong>${fmtCurrency(b.totalAvailableInEnvelopes)}</strong>
            </span>
          </div>
          <div style="display:flex; gap:8px;">
            <span class="envelope-avail-pill avail-funded">✓ Funded</span>
            <span class="envelope-avail-pill avail-underfunded">Underfunded</span>
            <span class="envelope-avail-pill avail-overspent">⚠ Overspent</span>
          </div>
        </div>
        <table class="ledger-table">
          <thead>
            <tr>
              <th>Envelope Category</th>
              <th class="num-col">Monthly Target</th>
              <th class="num-col">Rollover</th>
              <th class="num-col">Assigned</th>
              <th class="num-col">Activity (Spent)</th>
              <th>Utilization</th>
              <th class="num-col">Available</th>
              <th style="text-align:right; width:135px;">Quick Assign</th>
            </tr>
          </thead>
          <tbody>
            ${groupRowsHtml}
          </tbody>
        </table>
      </section>
    `;
  }

  function computePortfolioSummary() {
    const rawHoldings = (isWebViewHost() && state.portfolioSummary && Array.isArray(state.portfolioSummary.holdings))
      ? state.portfolioSummary.holdings
      : state.investmentHoldings;

    const filtered = state.investmentAccountFilter
      ? rawHoldings.filter((h) => h.accountId === state.investmentAccountFilter)
      : rawHoldings;

    const holdings = filtered.map((h) => {
      const shares = Number(h.shares || 0);
      const averageCostPerShare = Number(h.averageCostPerShare || 0);
      const currentPrice = Number(h.currentPrice || 0);
      const costBasis = Math.round(shares * averageCostPerShare * 100) / 100;
      const marketValue = Math.round(shares * currentPrice * 100) / 100;
      const unrealizedGainLoss = Math.round((marketValue - costBasis) * 100) / 100;
      const unrealizedGainLossPercent = costBasis > 0 ? Math.round((unrealizedGainLoss / costBasis) * 10000) / 100 : 0;
      const dayChangePercent = Number(h.dayChangePercent || 0);
      const dayChangeDollar = Math.round(marketValue * (dayChangePercent / 100) * 100) / 100;
      const dividendYieldPercent = Number(h.dividendYieldPercent || 0);
      const estimatedAnnualIncome = Math.round(marketValue * (dividendYieldPercent / 100) * 100) / 100;
      const expenseRatioPercent = Number(h.expenseRatioPercent || 0.03);

      return {
        ...h,
        shares,
        averageCostPerShare,
        currentPrice,
        costBasis,
        marketValue,
        unrealizedGainLoss,
        unrealizedGainLossPercent,
        dayChangePercent,
        dayChangeDollar,
        dividendYieldPercent,
        estimatedAnnualIncome,
        expenseRatioPercent,
        portfolioWeightPercent: 0
      };
    });

    const totalMarketValue = holdings.reduce((s, h) => s + h.marketValue, 0);
    const totalCostBasis = holdings.reduce((s, h) => s + h.costBasis, 0);
    const totalUnrealizedGainLoss = Math.round((totalMarketValue - totalCostBasis) * 100) / 100;
    const totalUnrealizedGainLossPercent = totalCostBasis > 0
      ? Math.round((totalUnrealizedGainLoss / totalCostBasis) * 10000) / 100
      : 0;
    const totalDayChangeDollar = holdings.reduce((s, h) => s + h.dayChangeDollar, 0);
    const totalDayChangePercent = totalMarketValue > 0
      ? Math.round((totalDayChangeDollar / totalMarketValue) * 10000) / 100
      : 0;
    const estimatedAnnualDividends = holdings.reduce((s, h) => s + h.estimatedAnnualIncome, 0);
    const weightedDividendYieldPercent = totalMarketValue > 0
      ? Math.round((estimatedAnnualDividends / totalMarketValue) * 10000) / 100
      : 0;
    const weightedExpenseRatioPercent = totalMarketValue > 0
      ? Math.round((holdings.reduce((s, h) => s + h.marketValue * h.expenseRatioPercent, 0) / totalMarketValue) * 1000) / 1000
      : 0;

    holdings.forEach((h) => {
      h.portfolioWeightPercent = totalMarketValue > 0
        ? Math.round((h.marketValue / totalMarketValue) * 1000) / 10
        : 0;
    });

    const policyTargets = [
      { assetClass: "US Equity", targetWeightPercent: 60.0, colorHex: "#38BDF8" },
      { assetClass: "International Equity", targetWeightPercent: 18.0, colorHex: "#818CF8" },
      { assetClass: "Fixed Income / Bonds", targetWeightPercent: 15.0, colorHex: "#10B981" },
      { assetClass: "Real Estate / REIT", targetWeightPercent: 5.0, colorHex: "#F59E0B" },
      { assetClass: "Cash & Money Market", targetWeightPercent: 2.0, colorHex: "#94A3B8" }
    ];

    const assetAllocation = policyTargets.map((pt) => {
      const marketValue = holdings
        .filter((h) => h.assetClass === pt.assetClass)
        .reduce((s, h) => s + h.marketValue, 0);
      const weightPercent = totalMarketValue > 0
        ? Math.round((marketValue / totalMarketValue) * 1000) / 10
        : 0;
      const driftPercent = Math.round((weightPercent - pt.targetWeightPercent) * 10) / 10;
      return {
        ...pt,
        marketValue,
        weightPercent,
        driftPercent
      };
    });

    return {
      totalMarketValue,
      totalCostBasis,
      totalUnrealizedGainLoss,
      totalUnrealizedGainLossPercent,
      totalDayChangeDollar,
      totalDayChangePercent,
      estimatedAnnualDividends,
      weightedDividendYieldPercent,
      weightedExpenseRatioPercent,
      holdings,
      assetAllocation
    };
  }

  function setInvestmentAccountFilter(accountId) {
    state.investmentAccountFilter = accountId || null;
    render();
  }

  function updateHoldingInline(holdingId) {
    const shares = Math.max(0, Number(document.getElementById(`hold-shares-${holdingId}`)?.value || 0));
    const averageCostPerShare = Math.max(0, Number(document.getElementById(`hold-cost-${holdingId}`)?.value || 0));
    const currentPrice = Math.max(0, Number(document.getElementById(`hold-price-${holdingId}`)?.value || 0));

    if (isWebViewHost()) {
      postToHost({
        type: "updateHolding",
        holdingId,
        shares,
        averageCostPerShare,
        currentPrice
      });
    } else {
      const h = state.investmentHoldings.find((x) => x.id === holdingId);
      if (h) {
        h.shares = shares;
        h.averageCostPerShare = averageCostPerShare;
        h.currentPrice = currentPrice;
      }
      render();
      showToast(`Updated ${h?.tickerSymbol || "holding"} (${shares} shares @ ${fmtCurrency(currentPrice)})`);
    }
  }

  function submitNewHolding(e) {
    e.preventDefault();
    if (!state.accounts || state.accounts.length === 0) {
      showToast("Please add an investment account first.");
      openAccountModal();
      return;
    }
    const defaultInvAcc = state.accounts.find((a) => a.accountGroup === "Investments") || state.accounts[0];
    const accountId = Number(document.getElementById("new-hold-acc")?.value || defaultInvAcc?.id || 0);
    const tickerSymbol = (document.getElementById("new-hold-ticker")?.value || "").trim().toUpperCase();
    const securityName = (document.getElementById("new-hold-name")?.value || "").trim();
    const assetClass = document.getElementById("new-hold-class")?.value || "US Equity";
    const shares = Math.max(0.001, Number(document.getElementById("new-hold-shares")?.value || 0));
    const averageCostPerShare = Math.max(0, Number(document.getElementById("new-hold-cost")?.value || 0));
    const currentPrice = Math.max(0, Number(document.getElementById("new-hold-price")?.value || 0));

    if (!tickerSymbol || !securityName) return;

    if (isWebViewHost()) {
      postToHost({
        type: "createHolding",
        accountId,
        tickerSymbol,
        securityName,
        assetClass,
        shares,
        averageCostPerShare,
        currentPrice,
        dayChangePercent: 0.45,
        dividendYieldPercent: 1.80,
        expenseRatioPercent: 0.04
      });
    } else {
      const acc = state.accounts.find((a) => a.id === accountId) || { name: "Vanguard Index Brokerage", institution: "Vanguard Group" };
      state.investmentHoldings.push({
        id: Date.now(),
        accountId,
        accountName: acc.name,
        institution: acc.institution,
        tickerSymbol,
        securityName,
        assetClass,
        shares,
        averageCostPerShare,
        currentPrice,
        dayChangePercent: 0.45,
        dividendYieldPercent: 1.80,
        expenseRatioPercent: 0.04
      });
      render();
      showToast(`Added ${tickerSymbol} (${shares} shares @ ${fmtCurrency(currentPrice)}) to ${acc.name}.`);
    }
  }

  function deleteHolding(holdingId) {
    if (isWebViewHost()) {
      postToHost({ type: "deleteHolding", holdingId });
    } else {
      state.investmentHoldings = state.investmentHoldings.filter((h) => h.id !== holdingId);
      render();
      showToast(`Removed holding #${holdingId} from portfolio.`);
    }
  }

  function simulateMarketPulse() {
    if (isWebViewHost()) {
      postToHost({ type: "simulateMarketPulse" });
    } else {
      state.investmentHoldings.forEach((h) => {
        if (h.assetClass === "Cash & Money Market") {
          h.currentPrice = 1.00;
          h.dayChangePercent = 0.01;
        } else if (h.assetClass === "Fixed Income / Bonds") {
          h.currentPrice = Math.round(h.currentPrice * 1.0035 * 100) / 100;
          h.dayChangePercent = 0.35;
        } else {
          h.currentPrice = Math.round(h.currentPrice * 1.0115 * 100) / 100;
          h.dayChangePercent = 1.15;
        }
      });
      render();
      showToast("Simulated live market quote pulse across all holdings.");
    }
  }

  function renderInvestmentsView() {
    const p = computePortfolioSummary();
    const invAccounts = state.accounts.filter((a) => a.accountGroup === "Investments");
    const isGain = p.totalUnrealizedGainLoss >= 0;

    const stackedBarHtml = p.assetAllocation
      .filter((a) => a.weightPercent > 0)
      .map((a) => `<div class="allocation-segment" style="width:${a.weightPercent}%; background:${a.colorHex};" title="${escapeHtml(a.assetClass)}: ${a.weightPercent}% (${fmtCurrency(a.marketValue)})"></div>`)
      .join("");

    const allocationRowsHtml = p.assetAllocation.map((a) => {
      const driftSign = a.driftPercent > 0 ? "+" : "";
      const driftColor = Math.abs(a.driftPercent) >= 3.0
        ? "var(--accent-gold)"
        : "var(--accent-emerald)";

      return `
        <tr>
          <td>
            <div style="display:flex; align-items:center; gap:8px;">
              <span class="legend-dot" style="background:${escapeHtml(a.colorHex)}"></span>
              <span style="font-weight:600;">${escapeHtml(a.assetClass)}</span>
            </div>
          </td>
          <td class="num-col">${fmtCurrency(a.marketValue)}</td>
          <td class="num-col" style="font-weight:700;">${a.weightPercent.toFixed(1)}%</td>
          <td class="num-col" style="color:var(--text-secondary);">${a.targetWeightPercent.toFixed(1)}%</td>
          <td class="num-col" style="color:${driftColor}; font-weight:700;">
            ${driftSign}${a.driftPercent.toFixed(1)}% ${Math.abs(a.driftPercent) >= 3.0 ? "⚠ Rebalance" : "✓ On Target"}
          </td>
        </tr>
      `;
    }).join("");

    const holdingsRowsHtml = p.holdings.map((h) => {
      const gainPositive = h.unrealizedGainLoss >= 0;
      const dayPositive = h.dayChangePercent >= 0;
      return `
        <tr class="tx-row">
          <td>
            <div style="display:flex; align-items:center; gap:10px;">
              <span class="ticker-badge">${escapeHtml(h.tickerSymbol)}</span>
              <div>
                <div style="font-weight:700; color:var(--text-primary);">${escapeHtml(h.securityName)}</div>
                <div style="font-size:10.5px; color:var(--text-muted);">
                  ${escapeHtml(h.accountName)} • ER ${h.expenseRatioPercent}% • Yield ${h.dividendYieldPercent}%
                </div>
              </div>
            </div>
          </td>
          <td><span class="category-chip">${escapeHtml(h.assetClass)}</span></td>
          <td class="num-col" style="width:105px;">
            <input type="number" step="0.001" id="hold-shares-${h.id}"
                   class="inline-input inline-input-num" style="width:92px;"
                   value="${h.shares}"
                   onchange="AppBridge.updateHoldingInline(${h.id})" />
          </td>
          <td class="num-col" style="width:105px;">
            <input type="number" step="0.01" id="hold-cost-${h.id}"
                   class="inline-input inline-input-num" style="width:92px; border-color:var(--border-subtle);"
                   value="${h.averageCostPerShare.toFixed(2)}"
                   onchange="AppBridge.updateHoldingInline(${h.id})" />
          </td>
          <td class="num-col" style="width:105px;">
            <input type="number" step="0.01" id="hold-price-${h.id}"
                   class="inline-input inline-input-num" style="width:92px;"
                   value="${h.currentPrice.toFixed(2)}"
                   onchange="AppBridge.updateHoldingInline(${h.id})" />
          </td>
          <td class="num-col" style="color:var(--text-secondary);">${fmtCurrency(h.costBasis)}</td>
          <td class="num-col" style="font-weight:700;">${fmtCurrency(h.marketValue)}</td>
          <td class="num-col ${gainPositive ? "amount-positive" : "amount-negative"}">
            <div>${gainPositive ? "+" : ""}${fmtCurrency(h.unrealizedGainLoss)}</div>
            <div style="font-size:10.5px;">(${gainPositive ? "+" : ""}${h.unrealizedGainLossPercent.toFixed(2)}%)</div>
          </td>
          <td class="num-col ${dayPositive ? "amount-positive" : "amount-negative"}" style="font-size:11.5px;">
            ${dayPositive ? "+" : ""}${h.dayChangePercent.toFixed(2)}%
          </td>
          <td class="num-col" style="font-weight:600; color:var(--accent-blue);">${h.portfolioWeightPercent.toFixed(1)}%</td>
          <td style="text-align:right;">
            <button class="row-action-btn delete" title="Remove Position" onclick="AppBridge.deleteHolding(${h.id})">🗑</button>
          </td>
        </tr>
      `;
    }).join("");

    return `
      <!-- Portfolio KPI Cards -->
      <section class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-label">Total Portfolio Value</div>
          <div class="kpi-value" style="color:var(--accent-blue)">${fmtCurrency(p.totalMarketValue)}</div>
          <div class="kpi-sub">Cost Basis: <strong>${fmtCurrency(p.totalCostBasis)}</strong></div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Total Unrealized Gain / Loss</div>
          <div class="kpi-value ${isGain ? "amount-positive" : "amount-negative"}">
            ${isGain ? "+" : ""}${fmtCurrency(p.totalUnrealizedGainLoss)}
          </div>
          <div class="kpi-sub">Total Return: <strong>${isGain ? "+" : ""}${p.totalUnrealizedGainLossPercent.toFixed(2)}%</strong></div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Day's Market Change</div>
          <div class="kpi-value ${p.totalDayChangeDollar >= 0 ? "amount-positive" : "amount-negative"}">
            ${p.totalDayChangeDollar >= 0 ? "+" : ""}${fmtCurrency(p.totalDayChangeDollar)}
          </div>
          <div class="kpi-sub">Session Move: <strong>${p.totalDayChangePercent >= 0 ? "+" : ""}${p.totalDayChangePercent.toFixed(2)}%</strong></div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Est. Annual Dividend Income</div>
          <div class="kpi-value" style="color:var(--accent-gold)">${fmtCurrency(p.estimatedAnnualDividends)}</div>
          <div class="kpi-sub">Yield: <strong>${p.weightedDividendYieldPercent.toFixed(2)}%</strong> • Expense Ratio: <strong>${p.weightedExpenseRatioPercent.toFixed(3)}%</strong></div>
        </div>
      </section>

      <!-- Asset Allocation & Policy Drift Panel -->
      <section class="panel-card">
        <div class="panel-header">
          <div>
            <div class="panel-title">Asset Class Allocation &amp; Policy Target Rebalancing</div>
            <div class="panel-subtitle">Compares live market weights against your 60/18/15/5/2 IPS (Investment Policy Statement) targets</div>
          </div>
          <div style="display:flex; gap:8px;">
            <button class="btn btn-gold btn-sm" onclick="AppBridge.simulateMarketPulse()">⚡ Simulate Market Quote Pulse</button>
          </div>
        </div>

        <div class="allocation-stacked-bar" style="margin-bottom:14px;">
          ${stackedBarHtml}
        </div>

        <table class="ledger-table">
          <thead>
            <tr>
              <th>Asset Class</th>
              <th class="num-col">Market Value</th>
              <th class="num-col">Actual Weight %</th>
              <th class="num-col">Target Policy %</th>
              <th class="num-col">Drift / Rebalance Status</th>
            </tr>
          </thead>
          <tbody>
            ${allocationRowsHtml}
          </tbody>
        </table>
      </section>

      <!-- Add New Security Holding Form -->
      <section class="panel-card" style="padding:14px 16px;">
        <form onsubmit="AppBridge.submitNewHolding(event)" style="display:flex; align-items:end; gap:10px; flex-wrap:wrap;">
          <div class="form-field" style="min-width:170px;">
            <label>Account</label>
            <select id="new-hold-acc">
              ${invAccounts.map((a) => `<option value="${a.id}">${escapeHtml(a.name)} (${escapeHtml(a.accountNumberMask)})</option>`).join("")}
            </select>
          </div>
          <div class="form-field" style="width:95px;">
            <label>Ticker</label>
            <input type="text" id="new-hold-ticker" placeholder="SCHD" required />
          </div>
          <div class="form-field" style="flex:1; min-width:180px;">
            <label>Security Name</label>
            <input type="text" id="new-hold-name" placeholder="Schwab U.S. Dividend Equity ETF" required />
          </div>
          <div class="form-field" style="width:165px;">
            <label>Asset Class</label>
            <select id="new-hold-class">
              <option value="US Equity">US Equity</option>
              <option value="International Equity">International Equity</option>
              <option value="Fixed Income / Bonds">Fixed Income / Bonds</option>
              <option value="Real Estate / REIT">Real Estate / REIT</option>
              <option value="Cash & Money Market">Cash &amp; Money Market</option>
            </select>
          </div>
          <div class="form-field" style="width:95px;">
            <label>Shares</label>
            <input type="number" id="new-hold-shares" step="0.001" placeholder="50" required />
          </div>
          <div class="form-field" style="width:105px;">
            <label>Avg Cost ($)</label>
            <input type="number" id="new-hold-cost" step="0.01" placeholder="76.50" required />
          </div>
          <div class="form-field" style="width:105px;">
            <label>Market Price ($)</label>
            <input type="number" id="new-hold-price" step="0.01" placeholder="84.20" required />
          </div>
          <button type="submit" class="btn btn-gold" style="height:35px;">＋ Add Holding</button>
        </form>
      </section>

      <!-- Holdings & Tax-Lot Table -->
      <section class="register-container">
        <div class="register-toolbar">
          <div class="filter-pills">
            <button class="filter-pill ${state.investmentAccountFilter === null ? "active" : ""}"
                    onclick="AppBridge.setInvestmentAccountFilter(null)">
              All Investment Accounts (${p.holdings.length})
            </button>
            ${invAccounts.map((a) => `
              <button class="filter-pill ${state.investmentAccountFilter === a.id ? "active" : ""}"
                      onclick="AppBridge.setInvestmentAccountFilter(${a.id})">
                ${escapeHtml(a.name)} (${escapeHtml(a.accountNumberMask)})
              </button>
            `).join("")}
          </div>
          <div style="font-size:11.5px; color:var(--text-secondary);">
            💡 Edit <strong>Shares</strong>, <strong>Avg Cost</strong>, or <strong>Market Price</strong> inline to recalculate Cost Basis &amp; Unrealized Gain/Loss
          </div>
        </div>

        <table class="ledger-table">
          <thead>
            <tr>
              <th>Security &amp; Account</th>
              <th>Asset Class</th>
              <th class="num-col">Shares</th>
              <th class="num-col">Avg Cost</th>
              <th class="num-col">Price</th>
              <th class="num-col">Cost Basis</th>
              <th class="num-col">Market Value</th>
              <th class="num-col">Unrealized Gain/Loss</th>
              <th class="num-col">Day %</th>
              <th class="num-col">Weight</th>
              <th style="text-align:right; width:50px;"></th>
            </tr>
          </thead>
          <tbody>
            ${holdingsRowsHtml}
          </tbody>
        </table>
      </section>
    `;
  }

  function advanceDueDateString(dateStr, frequency) {
    const parts = String(dateStr || "2026-11-15").split("-").map(Number);
    const dt = new Date(Date.UTC(parts[0] || 2026, (parts[1] || 11) - 1, parts[2] || 15));
    if (frequency === "Weekly") dt.setUTCDate(dt.getUTCDate() + 7);
    else if (frequency === "Bi-Weekly") dt.setUTCDate(dt.getUTCDate() + 14);
    else if (frequency === "Quarterly") dt.setUTCMonth(dt.getUTCMonth() + 3);
    else if (frequency === "Annual") dt.setUTCFullYear(dt.getUTCFullYear() + 1);
    else dt.setUTCMonth(dt.getUTCMonth() + 1);
    return dt.toISOString().slice(0, 10);
  }

  function computeRecurringSummary() {
    if (isWebViewHost() && state.recurringSummary && Array.isArray(state.recurringSummary.schedules)) {
      return state.recurringSummary;
    }

    const refMs = Date.UTC(2026, 10, 15);
    const sorted = [...state.recurringSchedules].sort((a, b) =>
      String(a.nextDueDate).localeCompare(String(b.nextDueDate))
    );

    const schedules = sorted.map((s) => {
      const p = String(s.nextDueDate || "2026-11-15").split("-").map(Number);
      const dueMs = Date.UTC(p[0] || 2026, (p[1] || 11) - 1, p[2] || 15);
      const daysUntilDue = Math.round((dueMs - refMs) / 86400000);
      const dueStatus = daysUntilDue < 0 ? "Overdue" : (daysUntilDue <= 7 ? "DueSoon" : "Upcoming");
      const mult = s.frequency === "Weekly" ? 52 / 12
        : s.frequency === "Bi-Weekly" ? 26 / 12
        : s.frequency === "Quarterly" ? 1 / 3
        : s.frequency === "Annual" ? 1 / 12
        : 1;
      const monthlyEquivalentAmount = Math.round(Number(s.amount || 0) * mult * 100) / 100;

      return {
        ...s,
        daysUntilDue,
        dueStatus,
        monthlyEquivalentAmount
      };
    });

    const checkingAcc = state.accounts.find((a) => a.id === 1);
    const currentCheckingBalance = checkingAcc ? checkingAcc.currentBalance : 5030.81;
    const currentLiquidBalance = state.accounts
      .filter((a) => a.accountGroup === "Banking")
      .reduce((sum, a) => sum + a.currentBalance, 0);

    let runningChecking = currentCheckingBalance;
    let runningLiquid = currentLiquidBalance;
    let lowestChecking = currentCheckingBalance;
    let next30DaysInflows = 0;
    let next30DaysOutflows = 0;

    const projectionTimeline = [];
    schedules.filter((s) => s.daysUntilDue <= 35).forEach((s) => {
      if (s.amount >= 0) next30DaysInflows += s.amount;
      else next30DaysOutflows += Math.abs(s.amount);

      if (s.accountId === 1) {
        runningChecking = Math.round((runningChecking + s.amount) * 100) / 100;
        if (runningChecking < lowestChecking) lowestChecking = runningChecking;
      }
      runningLiquid = Math.round((runningLiquid + s.amount) * 100) / 100;

      projectionTimeline.push({
        scheduleId: s.id,
        date: s.nextDueDate,
        payeeName: s.payeeName,
        accountName: s.accountName,
        categoryName: s.categoryName,
        frequency: s.frequency,
        deltaAmount: s.amount,
        projectedCheckingBalance: runningChecking,
        projectedLiquidBalance: runningLiquid
      });
    });

    const monthlyRecurringBillsTotal = schedules
      .filter((s) => s.amount < 0)
      .reduce((sum, s) => sum + Math.abs(s.monthlyEquivalentAmount), 0);

    const dueWithin7DaysCount = schedules.filter((s) => s.daysUntilDue <= 7).length;

    return {
      currentCheckingBalance,
      currentLiquidBalance,
      next30DaysInflows,
      next30DaysOutflows,
      next30DaysNetCashFlow: next30DaysInflows - next30DaysOutflows,
      projected30DayCheckingBalance: runningChecking,
      lowestProjectedCheckingBalance: lowestChecking,
      monthlyRecurringBillsTotal,
      dueWithin7DaysCount,
      schedules,
      projectionTimeline
    };
  }

  function postRecurringToRegister(scheduleId) {
    if (isWebViewHost()) {
      postToHost({ type: "postRecurringSchedule", scheduleId });
    } else {
      const s = state.recurringSchedules.find((x) => x.id === scheduleId);
      if (!s) return;

      state.transactions.unshift({
        id: Date.now(),
        accountId: s.accountId,
        accountName: s.accountName,
        transactionDate: s.nextDueDate,
        referenceNumber: "SCHED",
        payeeName: s.payeeName,
        categoryId: s.categoryId,
        categoryName: s.categoryName,
        memo: s.memo || `Scheduled ${s.frequency} transaction`,
        amount: s.amount,
        runningBalance: 0,
        status: "Cleared",
        isSplit: false,
        splits: []
      });

      s.nextDueDate = advanceDueDateString(s.nextDueDate, s.frequency);
      recalcStandaloneBalances();
      render();
      showToast(`Posted '${s.payeeName}' (${fmtCurrency(s.amount)}) to register & advanced due date to ${s.nextDueDate}.`);
    }
  }

  function skipRecurringCycle(scheduleId) {
    if (isWebViewHost()) {
      postToHost({ type: "skipRecurringSchedule", scheduleId });
    } else {
      const s = state.recurringSchedules.find((x) => x.id === scheduleId);
      if (!s) return;
      s.nextDueDate = advanceDueDateString(s.nextDueDate, s.frequency);
      render();
      showToast(`Skipped current cycle for '${s.payeeName}'. Next due: ${s.nextDueDate}.`);
    }
  }

  function submitNewRecurringSchedule(e) {
    e.preventDefault();
    if (!state.accounts || state.accounts.length === 0) {
      showToast("Please add an account first.");
      openAccountModal();
      return;
    }
    const accountId = Number(document.getElementById("rec-sched-acc")?.value || state.accounts[0]?.id || 0);
    const payeeName = (document.getElementById("rec-sched-payee")?.value || "").trim();
    const categoryId = Number(document.getElementById("rec-sched-cat")?.value || 20);
    const frequency = document.getElementById("rec-sched-freq")?.value || "Monthly";
    const nextDueDate = document.getElementById("rec-sched-date")?.value || new Date().toISOString().slice(0, 10);
    const flow = document.getElementById("rec-sched-flow")?.value || "bill";
    const rawAmt = Math.abs(Number(document.getElementById("rec-sched-amt")?.value || 0));
    const amount = flow === "income" ? rawAmt : -rawAmt;

    if (!payeeName || rawAmt <= 0) return;

    if (isWebViewHost()) {
      postToHost({
        type: "createRecurringSchedule",
        accountId,
        payeeName,
        categoryId,
        frequency,
        nextDueDate,
        amount,
        isAutoPay: true,
        isActive: true,
        memo: `Scheduled ${frequency} ${flow}`
      });
    } else {
      const acc = state.accounts.find((a) => a.id === accountId) || { name: "Primary Checking" };
      const cat = state.categories.find((c) => c.id === categoryId) || { fullPath: "Uncategorized" };
      state.recurringSchedules.push({
        id: Date.now(),
        accountId,
        accountName: acc.name,
        payeeName,
        categoryId,
        categoryName: cat.fullPath,
        frequency,
        nextDueDate,
        amount,
        isAutoPay: true,
        isActive: true,
        memo: `Scheduled ${frequency} ${flow}`
      });
      render();
      showToast(`Added ${frequency} schedule for ${payeeName} (${fmtCurrency(amount)})`);
    }
  }

  function deleteRecurringSchedule(scheduleId) {
    if (isWebViewHost()) {
      postToHost({ type: "deleteRecurringSchedule", scheduleId });
    } else {
      state.recurringSchedules = state.recurringSchedules.filter((s) => s.id !== scheduleId);
      render();
      showToast(`Removed recurring schedule #${scheduleId}.`);
    }
  }

  function renderCashFlowView() {
    const r = computeRecurringSummary();
    const catOptions = state.categories.map((c) =>
      `<option value="${c.id}">${escapeHtml(c.fullPath || c.name)}</option>`
    ).join("");
    const accOptions = state.accounts
      .filter((a) => a.accountGroup === "Banking" || a.accountGroup === "Credit Cards")
      .map((a) => `<option value="${a.id}">${escapeHtml(a.name)} (${escapeHtml(a.accountNumberMask)})</option>`)
      .join("");

    const scheduleRowsHtml = r.schedules.map((s) => {
      const isDueSoon = s.daysUntilDue <= 7;
      const dueBadgeClass = s.daysUntilDue < 0
        ? "avail-overspent"
        : isDueSoon
          ? "avail-underfunded"
          : "avail-funded";
      const dueLabel = s.daysUntilDue === 0
        ? "DUE TODAY"
        : s.daysUntilDue < 0
          ? `${Math.abs(s.daysUntilDue)}d Overdue`
          : `In ${s.daysUntilDue} day(s)`;

      return `
        <tr class="tx-row">
          <td style="font-family:var(--font-mono); font-weight:700;">${escapeHtml(s.nextDueDate)}</td>
          <td>
            <span class="envelope-avail-pill ${dueBadgeClass}">${escapeHtml(dueLabel)}</span>
          </td>
          <td>
            <div style="font-weight:700; color:var(--text-primary);">${escapeHtml(s.payeeName)}</div>
            <div style="font-size:10.5px; color:var(--text-muted);">${escapeHtml(s.accountName)} • ${escapeHtml(s.memo || "")}</div>
          </td>
          <td><span class="category-chip">${escapeHtml(s.categoryName)}</span></td>
          <td>
            <span class="status-badge status-cleared">${escapeHtml(s.frequency)}</span>
            ${s.isAutoPay ? `<span style="font-size:10px; color:var(--accent-emerald); margin-left:4px;">⚡ Auto</span>` : ""}
          </td>
          <td class="num-col ${s.amount >= 0 ? "amount-positive" : "amount-negative"}">
            ${s.amount >= 0 ? "+" : ""}${fmtCurrency(s.amount)}
          </td>
          <td style="text-align:right; white-space:nowrap;">
            <button class="btn btn-gold btn-sm" onclick="AppBridge.postRecurringToRegister(${s.id})" title="Post to Checkbook Register & Advance Next Due Date">
              ✓ Post to Register
            </button>
            <button class="row-action-btn" onclick="AppBridge.skipRecurringCycle(${s.id})" title="Skip This Cycle">⏭ Skip</button>
            <button class="row-action-btn delete" onclick="AppBridge.deleteRecurringSchedule(${s.id})" title="Delete Schedule">🗑</button>
          </td>
        </tr>
      `;
    }).join("");

    const timelineRowsHtml = r.projectionTimeline.map((pt) => `
      <tr>
        <td style="font-family:var(--font-mono);">${escapeHtml(pt.date)}</td>
        <td style="font-weight:600;">${escapeHtml(pt.payeeName)}</td>
        <td><span class="category-chip">${escapeHtml(pt.accountName)}</span></td>
        <td class="num-col ${pt.deltaAmount >= 0 ? "amount-positive" : "amount-negative"}">
          ${pt.deltaAmount >= 0 ? "+" : ""}${fmtCurrency(pt.deltaAmount)}
        </td>
        <td class="num-col" style="font-weight:700; color:var(--accent-blue);">${fmtCurrency(pt.projectedCheckingBalance)}</td>
        <td class="num-col" style="color:var(--text-secondary);">${fmtCurrency(pt.projectedLiquidBalance)}</td>
      </tr>
    `).join("");

    return `
      <!-- 30-Day Cash Flow KPI Strip -->
      <section class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-label">Current Checking Balance</div>
          <div class="kpi-value" style="color:var(--accent-blue)">${fmtCurrency(r.currentCheckingBalance)}</div>
          <div class="kpi-sub">Total Liquid Banking: <strong>${fmtCurrency(r.currentLiquidBalance)}</strong></div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">30-Day Scheduled Net Flow</div>
          <div class="kpi-value ${r.next30DaysNetCashFlow >= 0 ? "amount-positive" : "amount-negative"}">
            ${r.next30DaysNetCashFlow >= 0 ? "+" : ""}${fmtCurrency(r.next30DaysNetCashFlow)}
          </div>
          <div class="kpi-sub">Inflows: <strong>+${fmtCurrency(r.next30DaysInflows)}</strong> • Bills: <strong>-${fmtCurrency(r.next30DaysOutflows)}</strong></div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Projected 30-Day Checking</div>
          <div class="kpi-value amount-positive">${fmtCurrency(r.projected30DayCheckingBalance)}</div>
          <div class="kpi-sub">30-Day Low Water Mark: <strong>${fmtCurrency(r.lowestProjectedCheckingBalance)}</strong> ✓ Safe</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Monthly Fixed Bills &amp; Subs</div>
          <div class="kpi-value" style="color:var(--accent-gold)">${fmtCurrency(r.monthlyRecurringBillsTotal)}</div>
          <div class="kpi-sub">Due Within 7 Days: <strong>${r.dueWithin7DaysCount} Schedule(s)</strong></div>
        </div>
      </section>

      <!-- Add New Recurring Bill / Income Form -->
      <section class="panel-card" style="padding:14px 16px;">
        <form onsubmit="AppBridge.submitNewRecurringSchedule(event)" style="display:flex; align-items:end; gap:10px; flex-wrap:wrap;">
          <div class="form-field" style="width:165px;">
            <label>Account</label>
            <select id="rec-sched-acc">${accOptions}</select>
          </div>
          <div class="form-field" style="width:115px;">
            <label>Type</label>
            <select id="rec-sched-flow">
              <option value="bill">Bill / Sub (−)</option>
              <option value="income">Income (+)</option>
            </select>
          </div>
          <div class="form-field" style="flex:1; min-width:175px;">
            <label>Payee / Subscription</label>
            <input type="text" id="rec-sched-payee" placeholder="e.g., Netflix 4K or Water Utility" required />
          </div>
          <div class="form-field" style="width:185px;">
            <label>Category</label>
            <select id="rec-sched-cat">${catOptions}</select>
          </div>
          <div class="form-field" style="width:120px;">
            <label>Frequency</label>
            <select id="rec-sched-freq">
              <option value="Monthly">Monthly</option>
              <option value="Bi-Weekly">Bi-Weekly</option>
              <option value="Weekly">Weekly</option>
              <option value="Quarterly">Quarterly</option>
              <option value="Annual">Annual</option>
            </select>
          </div>
          <div class="form-field" style="width:130px;">
            <label>Next Due Date</label>
            <input type="date" id="rec-sched-date" value="${new Date().toISOString().slice(0, 10)}" required />
          </div>
          <div class="form-field" style="width:105px;">
            <label>Amount ($)</label>
            <input type="number" id="rec-sched-amt" step="0.01" placeholder="24.99" required />
          </div>
          <button type="submit" class="btn btn-gold" style="height:35px;">＋ Add Schedule</button>
        </form>
      </section>

      <!-- Upcoming Recurring Bills & Paychecks Table -->
      <section class="register-container">
        <div class="register-toolbar">
          <div style="font-size:12.5px; font-weight:700;">
            📅 Scheduled Bills, Subscriptions &amp; Paychecks (${r.schedules.length} Active Schedules)
          </div>
          <div style="font-size:11.5px; color:var(--text-secondary);">
            Click <strong>✓ Post to Register</strong> to record the transaction in SQLite and automatically advance the next due date
          </div>
        </div>
        <table class="ledger-table">
          <thead>
            <tr>
              <th>Next Due Date</th>
              <th>Countdown</th>
              <th>Payee &amp; Account</th>
              <th>Category</th>
              <th>Frequency</th>
              <th class="num-col">Scheduled Amount</th>
              <th style="text-align:right; width:220px;">Actions</th>
            </tr>
          </thead>
          <tbody>
            ${scheduleRowsHtml}
          </tbody>
        </table>
      </section>

      <!-- 30-Day Running Cash-Flow Projection Ledger -->
      <section class="panel-card">
        <div class="panel-header">
          <div>
            <div class="panel-title">30-Day Chronological Running Cash-Flow Projection</div>
            <div class="panel-subtitle">Forecasts Primary Checking and Total Liquid Banking balances after each scheduled inflow/outflow</div>
          </div>
        </div>
        <table class="ledger-table">
          <thead>
            <tr>
              <th>Projected Date</th>
              <th>Scheduled Event</th>
              <th>Account</th>
              <th class="num-col">Cash Flow Delta</th>
              <th class="num-col">Projected Checking</th>
              <th class="num-col">Projected Total Liquid</th>
            </tr>
          </thead>
          <tbody>
            ${timelineRowsHtml}
          </tbody>
        </table>
      </section>
    `;
  }

  function computeExecutiveReportSummary() {
    if (isWebViewHost() && state.reportSummary && Array.isArray(state.reportSummary.expenseByCategory)) {
      return state.reportSummary;
    }

    const totalAssets = state.accounts
      .filter((a) => a.currentBalance >= 0)
      .reduce((sum, a) => sum + a.currentBalance, 0);
    const totalLiabilities = state.accounts
      .filter((a) => a.currentBalance < 0)
      .reduce((sum, a) => sum + Math.abs(a.currentBalance), 0);
    const currentNetWorth = Math.round((totalAssets - totalLiabilities) * 100) / 100;
    const debtToAssetRatioPercent = totalAssets > 0
      ? Math.round((totalLiabilities / totalAssets) * 10000) / 100
      : 0;

    let grossIncome = 0;
    let totalOperatingExpenses = 0;
    let totalDebtServiceAndTransfers = 0;
    let charitableGivingTotal = 0;

    const incomeMap = {};
    const expenseMap = {};
    const payeeMap = {};
    const stewardshipTaxReceipts = [];

    function processLine(tx, catNameRaw, signedAmount, lineMemo) {
      const catName = String(catNameRaw || "Uncategorized");
      const groupName = catName.includes(":") ? catName.split(":")[0] : catName;
      const catObj = state.categories.find((c) => c.fullPath === catName || c.name === catName);
      const colorHex = (catObj && catObj.colorHex) || "#38BDF8";

      if (signedAmount > 0) {
        grossIncome += signedAmount;
        if (!incomeMap[catName]) {
          incomeMap[catName] = { groupName, categoryName: catName, colorHex: "#10B981", amount: 0, transactionCount: 0 };
        }
        incomeMap[catName].amount += signedAmount;
        incomeMap[catName].transactionCount += 1;
        return;
      }

      const absAmt = Math.abs(signedAmount);
      const isTransfer = groupName.toLowerCase().includes("transfer") ||
        catName.toLowerCase().includes("credit card payment");

      if (isTransfer) {
        totalDebtServiceAndTransfers += absAmt;
        return;
      }

      totalOperatingExpenses += absAmt;

      if (!expenseMap[catName]) {
        expenseMap[catName] = { groupName, categoryName: catName, colorHex, amount: 0, transactionCount: 0 };
      }
      expenseMap[catName].amount += absAmt;
      expenseMap[catName].transactionCount += 1;

      if (!payeeMap[tx.payeeName]) {
        payeeMap[tx.payeeName] = { payeeName: tx.payeeName, categoryName: catName, totalSpent: 0, transactionCount: 0 };
      }
      payeeMap[tx.payeeName].totalSpent += absAmt;
      payeeMap[tx.payeeName].transactionCount += 1;

      if (
        groupName.toLowerCase().includes("giving") ||
        catName.toLowerCase().includes("tithe") ||
        catName.toLowerCase().includes("charity")
      ) {
        charitableGivingTotal += absAmt;
        stewardshipTaxReceipts.push({
          transactionId: tx.id,
          transactionDate: tx.transactionDate,
          payeeName: tx.payeeName,
          categoryName: catName,
          accountName: tx.accountName || "Primary Checking",
          referenceNumber: tx.referenceNumber || "RECEIPT",
          memo: lineMemo || tx.memo || "Charitable stewardship contribution",
          status: tx.status,
          amount: absAmt
        });
      }
    }

    (state.transactions || []).forEach((tx) => {
      if (tx.isSplit && Array.isArray(tx.splits) && tx.splits.length > 0) {
        tx.splits.forEach((sp) => processLine(tx, sp.categoryName, sp.amount, sp.memo));
      } else {
        processLine(tx, tx.categoryName, tx.amount, tx.memo);
      }
    });

    const netOperatingSurplus = Math.round((grossIncome - totalOperatingExpenses) * 100) / 100;
    const savingsRatePercent = grossIncome > 0
      ? Math.round((netOperatingSurplus / grossIncome) * 1000) / 10
      : 0;
    const charitableGivingPercentOfIncome = grossIncome > 0
      ? Math.round((charitableGivingTotal / grossIncome) * 1000) / 10
      : 0;

    const incomeByCategory = Object.values(incomeMap)
      .sort((a, b) => b.amount - a.amount)
      .map((x) => ({
        ...x,
        sharePercent: grossIncome > 0 ? Math.round((x.amount / grossIncome) * 1000) / 10 : 0
      }));

    const expenseByCategory = Object.values(expenseMap)
      .sort((a, b) => b.amount - a.amount)
      .map((x) => ({
        ...x,
        sharePercent: totalOperatingExpenses > 0 ? Math.round((x.amount / totalOperatingExpenses) * 1000) / 10 : 0
      }));

    const topPayees = Object.values(payeeMap)
      .sort((a, b) => b.totalSpent - a.totalSpent)
      .slice(0, 8)
      .map((p) => ({
        ...p,
        sharePercent: totalOperatingExpenses > 0 ? Math.round((p.totalSpent / totalOperatingExpenses) * 1000) / 10 : 0
      }));

    const netWorthHistory = [
      { monthLabel: "Jun 2026", totalAssets: totalAssets - 18400, totalLiabilities: totalLiabilities + 4200, netWorth: currentNetWorth - 22600 },
      { monthLabel: "Jul 2026", totalAssets: totalAssets - 14900, totalLiabilities: totalLiabilities + 3350, netWorth: currentNetWorth - 18250 },
      { monthLabel: "Aug 2026", totalAssets: totalAssets - 11200, totalLiabilities: totalLiabilities + 2500, netWorth: currentNetWorth - 13700 },
      { monthLabel: "Sep 2026", totalAssets: totalAssets - 7600, totalLiabilities: totalLiabilities + 1680, netWorth: currentNetWorth - 9280 },
      { monthLabel: "Oct 2026", totalAssets: totalAssets - 3850, totalLiabilities: totalLiabilities + 840, netWorth: currentNetWorth - 4690 },
      { monthLabel: "Nov 2026", totalAssets, totalLiabilities, netWorth: currentNetWorth }
    ];

    return {
      reportPeriodLabel: "November 2026 • Executive P&L & Stewardship Audit",
      grossIncome,
      totalOperatingExpenses,
      totalDebtServiceAndTransfers,
      netOperatingSurplus,
      savingsRatePercent,
      charitableGivingTotal,
      charitableGivingPercentOfIncome,
      totalAssets,
      totalLiabilities,
      currentNetWorth,
      debtToAssetRatioPercent,
      incomeByCategory,
      expenseByCategory,
      topPayees,
      stewardshipTaxReceipts,
      netWorthHistory
    };
  }

  function downloadTextFile(filename, content, mimeType) {
    const blob = new Blob([content], { type: mimeType || "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  function csvCell(val) {
    const s = String(val ?? "");
    if (s.includes(",") || s.includes("\"") || s.includes("\n")) {
      return `"${s.replace(/"/g, "\"\"")}"`;
    }
    return s;
  }

  function exportLedgerCsv() {
    if (isWebViewHost()) {
      postToHost({ type: "exportLedgerCsv" });
      return;
    }

    const header = "Id,Date,Account,ReferenceNumber,Status,Payee,Category,Memo,Payment,Deposit,SignedAmount,RunningBalance";
    const lines = (state.transactions || []).map((t) => [
      t.id,
      csvCell(t.transactionDate),
      csvCell(t.accountName),
      csvCell(t.referenceNumber),
      csvCell(t.status),
      csvCell(t.payeeName),
      csvCell(t.categoryName),
      csvCell(t.memo),
      t.amount < 0 ? Math.abs(t.amount).toFixed(2) : "",
      t.amount > 0 ? t.amount.toFixed(2) : "",
      Number(t.amount || 0).toFixed(2),
      Number(t.runningBalance || 0).toFixed(2)
    ].join(","));

    downloadTextFile("FullOfGrace_Ledger_Export_2026-11.csv", [header, ...lines].join("\n"), "text/csv;charset=utf-8");
    showToast(`Exported ${lines.length} ledger transactions to FullOfGrace_Ledger_Export_2026-11.csv`);
  }

  function exportStewardshipTaxCsv() {
    if (isWebViewHost()) {
      postToHost({ type: "exportStewardshipTaxCsv" });
      return;
    }

    const rep = computeExecutiveReportSummary();
    const header = "TransactionId,Date,CharityOrParishPayee,Category,Account,ReferenceNumber,Status,Memo,TaxDeductibleAmount";
    const lines = (rep.stewardshipTaxReceipts || []).map((r) => [
      r.transactionId,
      csvCell(r.transactionDate),
      csvCell(r.payeeName),
      csvCell(r.categoryName),
      csvCell(r.accountName),
      csvCell(r.referenceNumber),
      csvCell(r.status),
      csvCell(r.memo),
      Number(r.amount || 0).toFixed(2)
    ].join(","));

    downloadTextFile("FullOfGrace_Stewardship_Tax_Receipts_2026.csv", [header, ...lines].join("\n"), "text/csv;charset=utf-8");
    showToast(`Exported ${lines.length} tax-deductible stewardship receipt(s) to CSV.`);
  }

  function exportWorkspaceJsonBackup() {
    if (isWebViewHost()) {
      postToHost({ type: "exportWorkspaceJsonBackup" });
      return;
    }

    const snapshot = {
      application: "Full of Grace Money Manager",
      schemaVersion: "Phase 3B (net10.0-windows / SQLite)",
      exportedAtUtc: new Date().toISOString(),
      accounts: state.accounts,
      categories: state.categories,
      payees: state.payees,
      rules: state.rules,
      budgetSummary: computeBudgetSummary(),
      portfolioSummary: computePortfolioSummary(),
      recurringSummary: computeRecurringSummary(),
      reportSummary: computeExecutiveReportSummary(),
      transactions: state.transactions
    };

    const json = JSON.stringify(snapshot, null, 2);
    downloadTextFile("FullOfGrace_WorkspaceBackup_2026-11.json", json, "application/json;charset=utf-8");
    showToast(`Saved full JSON workspace backup (${json.length.toLocaleString()} bytes).`);
  }

  function renderReportsView() {
    const rep = computeExecutiveReportSummary();

    const incomeRowsHtml = rep.incomeByCategory.map((item) => `
      <tr>
        <td>
          <span class="category-dot" style="background:#10B981;"></span>
          <strong>${escapeHtml(item.categoryName)}</strong>
        </td>
        <td class="num-col">${item.transactionCount}</td>
        <td class="num-col amount-positive">${fmtCurrency(item.amount)}</td>
        <td class="num-col" style="font-weight:700;">${item.sharePercent.toFixed(1)}%</td>
      </tr>
    `).join("");

    const expenseRowsHtml = rep.expenseByCategory.map((item) => `
      <tr>
        <td>
          <span class="category-dot" style="background:${escapeHtml(item.colorHex || '#F59E0B')};"></span>
          <strong>${escapeHtml(item.categoryName)}</strong>
        </td>
        <td class="num-col">${item.transactionCount}</td>
        <td class="num-col amount-negative">-${fmtCurrency(item.amount)}</td>
        <td class="num-col" style="font-weight:700;">${item.sharePercent.toFixed(1)}%</td>
      </tr>
    `).join("");

    const payeeRowsHtml = rep.topPayees.map((p) => `
      <tr>
        <td style="font-weight:700;">${escapeHtml(p.payeeName)}</td>
        <td><span class="category-chip">${escapeHtml(p.categoryName)}</span></td>
        <td class="num-col">${p.transactionCount}</td>
        <td class="num-col amount-negative">-${fmtCurrency(p.totalSpent)}</td>
        <td class="num-col" style="color:var(--accent-blue); font-weight:700;">${p.sharePercent.toFixed(1)}%</td>
      </tr>
    `).join("");

    const nwRowsHtml = rep.netWorthHistory.map((pt, idx) => {
      const prev = idx > 0 ? rep.netWorthHistory[idx - 1].netWorth : pt.netWorth;
      const delta = Math.round((pt.netWorth - prev) * 100) / 100;
      return `
        <tr>
          <td style="font-family:var(--font-mono); font-weight:700;">${escapeHtml(pt.monthLabel)}</td>
          <td class="num-col amount-positive">${fmtCurrency(pt.totalAssets)}</td>
          <td class="num-col amount-negative">-${fmtCurrency(pt.totalLiabilities)}</td>
          <td class="num-col" style="font-weight:800; color:var(--accent-blue);">${fmtCurrency(pt.netWorth)}</td>
          <td class="num-col ${delta >= 0 ? "amount-positive" : "amount-negative"}">
            ${idx === 0 ? "Baseline" : `${delta >= 0 ? "+" : ""}${fmtCurrency(delta)}`}
          </td>
        </tr>
      `;
    }).join("");

    const receiptRowsHtml = rep.stewardshipTaxReceipts.length > 0
      ? rep.stewardshipTaxReceipts.map((r) => `
          <tr>
            <td style="font-family:var(--font-mono);">${escapeHtml(r.transactionDate)}</td>
            <td style="font-family:var(--font-mono); color:var(--text-muted);">${escapeHtml(r.referenceNumber)}</td>
            <td style="font-weight:700;">${escapeHtml(r.payeeName)}</td>
            <td><span class="category-chip">${escapeHtml(r.categoryName)}</span></td>
            <td>${escapeHtml(r.accountName)}</td>
            <td style="color:var(--text-secondary);">${escapeHtml(r.memo)}</td>
            <td class="num-col amount-positive" style="font-weight:800;">${fmtCurrency(r.amount)}</td>
          </tr>
        `).join("")
      : `<tr><td colspan="7" style="text-align:center; padding:18px; color:var(--text-muted);">No charitable stewardship entries recorded in this period.</td></tr>`;

    return `
      <!-- Executive P&L & Stewardship Ratios KPI Strip -->
      <section class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-label">Net Operating Surplus (P&amp;L)</div>
          <div class="kpi-value ${rep.netOperatingSurplus >= 0 ? "amount-positive" : "amount-negative"}">
            ${rep.netOperatingSurplus >= 0 ? "+" : ""}${fmtCurrency(rep.netOperatingSurplus)}
          </div>
          <div class="kpi-sub">Gross Income: <strong>${fmtCurrency(rep.grossIncome)}</strong> • OpEx: <strong>-${fmtCurrency(rep.totalOperatingExpenses)}</strong></div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Operating Savings Rate</div>
          <div class="kpi-value" style="color:var(--accent-blue)">${rep.savingsRatePercent.toFixed(1)}%</div>
          <div class="kpi-sub">Debt/Card Transfers: <strong>${fmtCurrency(rep.totalDebtServiceAndTransfers)}</strong></div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Firstfruits Stewardship &amp; Giving</div>
          <div class="kpi-value" style="color:var(--accent-gold)">${fmtCurrency(rep.charitableGivingTotal)}</div>
          <div class="kpi-sub">Giving Share of Income: <strong>${rep.charitableGivingPercentOfIncome.toFixed(1)}%</strong> (${rep.stewardshipTaxReceipts.length} Receipt)</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Balance Sheet Solvency</div>
          <div class="kpi-value amount-positive">${fmtCurrency(rep.currentNetWorth)}</div>
          <div class="kpi-sub">Debt-to-Asset Ratio: <strong>${rep.debtToAssetRatioPercent.toFixed(1)}%</strong></div>
        </div>
      </section>

      <!-- Data Export & Full SQLite/JSON Backup Bar -->
      <section class="panel-card" style="display:flex; align-items:center; justify-content:space-between; flex-wrap:wrap; gap:12px;">
        <div>
          <div class="panel-title">📥 CPA Tax Export &amp; Full Workspace Backup Studio</div>
          <div class="panel-subtitle">Export RFC-4180 CSV spreadsheets for Excel/CPA review or save a complete JSON backup snapshot of all SQLite tables</div>
        </div>
        <div style="display:flex; gap:8px; flex-wrap:wrap;">
          <button class="btn btn-outline" onclick="AppBridge.exportLedgerCsv()">📄 Export Checkbook Ledger (.CSV)</button>
          <button class="btn btn-outline" onclick="AppBridge.exportStewardshipTaxCsv()">🏛 Export Tax Giving Receipts (.CSV)</button>
          <button class="btn btn-gold" onclick="AppBridge.exportWorkspaceJsonBackup()">💾 Save Full Workspace Backup (.JSON)</button>
        </div>
      </section>

      <!-- Income & Operating Expense P&L Statement + Top Payees -->
      <section class="charts-grid">
        <div class="panel-card">
          <div class="panel-header">
            <div>
              <div class="panel-title">Income &amp; Operating Expense Statement (P&amp;L)</div>
              <div class="panel-subtitle">Split-aware category aggregation excluding internal account transfers</div>
            </div>
          </div>
          <table class="ledger-table">
            <thead>
              <tr>
                <th>Income / Expense Category</th>
                <th class="num-col">Tx Count</th>
                <th class="num-col">Total Amount</th>
                <th class="num-col">Share %</th>
              </tr>
            </thead>
            <tbody>
              <tr class="envelope-group-row">
                <td colspan="2">gross inflows &amp; income</td>
                <td class="num-col amount-positive">+${fmtCurrency(rep.grossIncome)}</td>
                <td class="num-col">100.0%</td>
              </tr>
              ${incomeRowsHtml}
              <tr class="envelope-group-row">
                <td colspan="2">operating expenses (split-adjusted)</td>
                <td class="num-col amount-negative">-${fmtCurrency(rep.totalOperatingExpenses)}</td>
                <td class="num-col">100.0%</td>
              </tr>
              ${expenseRowsHtml}
            </tbody>
          </table>
        </div>

        <div class="panel-card">
          <div class="panel-header">
            <div>
              <div class="panel-title">Top Merchants &amp; Payees by Spend</div>
              <div class="panel-subtitle">Ranked by total operating outflow share</div>
            </div>
          </div>
          <table class="ledger-table">
            <thead>
              <tr>
                <th>Payee / Merchant</th>
                <th>Primary Category</th>
                <th class="num-col">Count</th>
                <th class="num-col">Total Outflow</th>
                <th class="num-col">OpEx %</th>
              </tr>
            </thead>
            <tbody>
              ${payeeRowsHtml}
            </tbody>
          </table>
        </div>
      </section>

      <!-- 6-Month Net Worth Trajectory -->
      <section class="panel-card">
        <div class="panel-header">
          <div>
            <div class="panel-title">6-Month Balance Sheet &amp; Net Worth Progression</div>
            <div class="panel-subtitle">Monthly assets, mortgage/card liabilities, and net worth growth trajectory</div>
          </div>
        </div>
        <table class="ledger-table">
          <thead>
            <tr>
              <th>Month</th>
              <th class="num-col">Total Assets</th>
              <th class="num-col">Total Liabilities</th>
              <th class="num-col">Ending Net Worth</th>
              <th class="num-col">Monthly Net Worth Change</th>
            </tr>
          </thead>
          <tbody>
            ${nwRowsHtml}
          </tbody>
        </table>
      </section>

      <!-- Charitable Stewardship & Tax-Deductible Giving Receipt Log -->
      <section class="panel-card">
        <div class="panel-header">
          <div>
            <div class="panel-title">🏛 Charitable Stewardship &amp; Tax-Deductible Giving Receipt Ledger</div>
            <div class="panel-subtitle">Year-end tax preparation schedule of parish tithes and 501(c)(3) contributions</div>
          </div>
          <button class="btn btn-outline btn-sm" onclick="AppBridge.exportStewardshipTaxCsv()">🏛 Download Tax Receipt CSV</button>
        </div>
        <table class="ledger-table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Receipt / Ref #</th>
              <th>Organization / Parish Payee</th>
              <th>Category</th>
              <th>Paid From Account</th>
              <th>Stewardship Memo</th>
              <th class="num-col">Tax-Deductible Amount</th>
            </tr>
          </thead>
          <tbody>
            ${receiptRowsHtml}
          </tbody>
        </table>
      </section>
    `;
  }

  function renderWebPortalView() {
    const portalCards = state.accounts.map((a) => `
      <div class="panel-card">
        <div class="kicker-pill">${escapeHtml(a.accountGroup)}</div>
        <h3 style="font-size:15px; margin:4px 0;">${escapeHtml(a.name)}</h3>
        <p style="font-size:12px; color:var(--text-secondary); margin-bottom:12px;">
          Institution: <strong>${escapeHtml(a.institution)}</strong><br/>
          Portal: <code>${escapeHtml(a.portalUrl || "https://www.chase.com")}</code>
        </p>
        <button class="btn btn-gold btn-sm" onclick="AppBridge.openExternalPortal('${escapeHtml(a.portalUrl || 'https://www.chase.com')}')">
          🌐 Launch Portal in Built-in Browser
        </button>
      </div>
    `).join("");

    return `<div class="kpi-grid">${portalCards}</div>`;
  }

  // ============================================================================
  // Phase 4A: Debt Payoff & Mortgage Amortization Optimizer (Avalanche vs. Snowball)
  // ============================================================================
  function simulateSingleDebtJS(balance, apr, monthlyPayment) {
    if (balance <= 0 || monthlyPayment <= 0) return { months: 0, totalInterest: 0 };
    const r = (apr / 100) / 12;
    let rem = balance;
    let totalInterest = 0;
    let months = 0;
    while (rem > 0.01 && months < 600) {
      months++;
      const interest = Math.round(rem * r * 100) / 100;
      let principal = monthlyPayment - interest;
      if (principal <= 0) {
        return { months: 600, totalInterest: Math.round(interest * 600 * 100) / 100 };
      }
      if (principal > rem) principal = rem;
      totalInterest += interest;
      rem -= principal;
    }
    return { months, totalInterest: Math.round(totalInterest * 100) / 100 };
  }

  function computeDebtPayoffSummary() {
    if (isWebViewHost() && state.debtSummary) {
      return state.debtSummary;
    }

    const mode = state.debtStrategyMode === "Snowball" ? "Snowball" : "Avalanche";
    const sorted = [...state.debtLiabilities].sort((a, b) => {
      if (mode === "Snowball") {
        return a.currentBalance - b.currentBalance || b.interestRateApr - a.interestRateApr;
      }
      return b.interestRateApr - a.interestRateApr || a.currentBalance - b.currentBalance;
    });

    const anchorDate = new Date("2026-11-01T00:00:00Z");
    const liabilities = sorted.map((d, idx) => {
      const baseSim = simulateSingleDebtJS(d.currentBalance, d.interestRateApr, d.minimumMonthlyPayment);
      const accelSim = simulateSingleDebtJS(d.currentBalance, d.interestRateApr, d.minimumMonthlyPayment + d.extraPrincipalMonthly);
      const monthsSaved = Math.max(0, baseSim.months - accelSim.months);
      const interestSaved = Math.max(0, Math.round((baseSim.totalInterest - accelSim.totalInterest) * 100) / 100);
      const payoffDate = new Date(anchorDate);
      payoffDate.setUTCMonth(payoffDate.getUTCMonth() + accelSim.months);
      const paidOffPercent = d.originalPrincipal > 0
        ? Math.min(100, Math.max(0, Math.round(((d.originalPrincipal - d.currentBalance) / d.originalPrincipal) * 1000) / 10))
        : 0;

      return {
        ...d,
        totalMonthlyPayment: d.minimumMonthlyPayment + d.extraPrincipalMonthly + d.escrowMonthly,
        paidOffPercent,
        strategyPriorityRank: idx + 1,
        baselineMonthsToPayoff: baseSim.months,
        acceleratedMonthsToPayoff: accelSim.months,
        monthsSaved,
        baselineTotalInterest: baseSim.totalInterest,
        acceleratedTotalInterest: accelSim.totalInterest,
        interestSaved,
        projectedPayoffDate: payoffDate.toLocaleString("en-US", { month: "short", year: "numeric", timeZone: "UTC" })
      };
    });

    const totalPrincipal = liabilities.reduce((s, d) => s + d.currentBalance, 0);
    const totalOrig = liabilities.reduce((s, d) => s + d.originalPrincipal, 0);
    const weightedApr = totalPrincipal > 0
      ? Math.round((liabilities.reduce((s, d) => s + d.currentBalance * d.interestRateApr, 0) / totalPrincipal) * 100) / 100
      : 0;

    // 24-month combined rollover schedule
    const simStates = liabilities.map((l) => ({
      name: l.name,
      balance: l.currentBalance,
      monthlyRate: (l.interestRateApr / 100) / 12,
      minPayment: l.minimumMonthlyPayment,
      extraPayment: l.extraPrincipalMonthly,
      rank: l.strategyPriorityRank
    }));

    const schedule = [];
    let cumInterest = 0;
    for (let m = 1; m <= 24; m++) {
      const mDate = new Date(anchorDate);
      mDate.setUTCMonth(mDate.getUTCMonth() + m);
      const mLabel = mDate.toLocaleString("en-US", { month: "short", year: "numeric", timeZone: "UTC" });
      const startBal = simStates.reduce((s, x) => s + x.balance, 0);
      if (startBal <= 0.01) break;

      let freed = simStates.filter((x) => x.balance <= 0.01).reduce((s, x) => s + x.minPayment + x.extraPayment, 0);
      const activeFocus = simStates.filter((x) => x.balance > 0.01).sort((a, b) => a.rank - b.rank)[0];
      const focusName = activeFocus ? activeFocus.name : "All Debts Paid";

      let mPayment = 0, mPrincipal = 0, mInterest = 0, mExtra = 0;
      simStates.sort((a, b) => a.rank - b.rank).forEach((st) => {
        if (st.balance <= 0.01) return;
        const interest = Math.round(st.balance * st.monthlyRate * 100) / 100;
        let extra = st.extraPayment;
        if (activeFocus && st.name === activeFocus.name) {
          extra += freed;
          freed = 0;
        }
        let targetPay = st.minPayment + extra;
        let principal = targetPay - interest;
        if (principal > st.balance) {
          const unused = principal - st.balance;
          principal = st.balance;
          targetPay = interest + principal;
          freed += Math.max(0, unused);
        }
        st.balance = Math.max(0, Math.round((st.balance - principal) * 100) / 100);
        mPayment += targetPay;
        mPrincipal += principal;
        mInterest += interest;
        mExtra += Math.max(0, principal - Math.max(0, st.minPayment - interest));
      });

      cumInterest += mInterest;
      schedule.push({
        monthNumber: m,
        monthLabel: mLabel,
        startingBalance: startBal,
        totalPayment: Math.round(mPayment * 100) / 100,
        principalPaid: Math.round(mPrincipal * 100) / 100,
        interestPaid: Math.round(mInterest * 100) / 100,
        extraPrincipalApplied: Math.round(mExtra * 100) / 100,
        endingBalance: simStates.reduce((s, x) => s + x.balance, 0),
        cumulativeInterest: Math.round(cumInterest * 100) / 100,
        activeTargetDebtName: focusName
      });
    }

    const maxBaseMonths = liabilities.length ? Math.max(...liabilities.map((l) => l.baselineMonthsToPayoff)) : 0;
    const maxAccelMonths = liabilities.length ? Math.max(...liabilities.map((l) => l.acceleratedMonthsToPayoff)) : 0;
    const debtFreeDate = new Date(anchorDate);
    debtFreeDate.setUTCMonth(debtFreeDate.getUTCMonth() + maxAccelMonths);

    return {
      activeStrategy: mode,
      totalPrincipalBalance: totalPrincipal,
      totalOriginalPrincipal: totalOrig,
      overallPaidOffPercent: totalOrig > 0 ? Math.round(((totalOrig - totalPrincipal) / totalOrig) * 1000) / 10 : 0,
      weightedAverageApr: weightedApr,
      totalMinimumPaymentMonthly: liabilities.reduce((s, d) => s + d.minimumMonthlyPayment, 0),
      totalExtraPrincipalMonthly: liabilities.reduce((s, d) => s + d.extraPrincipalMonthly, 0),
      totalEscrowMonthly: liabilities.reduce((s, d) => s + d.escrowMonthly, 0),
      totalBaselineInterest: liabilities.reduce((s, d) => s + d.baselineTotalInterest, 0),
      totalAcceleratedInterest: liabilities.reduce((s, d) => s + d.acceleratedTotalInterest, 0),
      totalInterestSaved: liabilities.reduce((s, d) => s + d.interestSaved, 0),
      maxMonthsSaved: Math.max(0, maxBaseMonths - maxAccelMonths),
      debtFreeDateLabel: debtFreeDate.toLocaleString("en-US", { month: "short", year: "numeric", timeZone: "UTC" }),
      liabilities,
      combinedSchedule: schedule
    };
  }

  function setDebtStrategy(mode) {
    state.debtStrategyMode = mode === "Snowball" ? "Snowball" : "Avalanche";
    if (isWebViewHost()) {
      postToHost({ type: "setDebtStrategy", strategyMode: state.debtStrategyMode });
    } else {
      state.debtSummary = null;
      render();
      showToast(`Switched debt payoff cascade to ${state.debtStrategyMode} strategy.`);
    }
  }

  function updateDebtLiabilityInline(id) {
    const balEl = document.getElementById(`debt-bal-${id}`);
    const aprEl = document.getElementById(`debt-apr-${id}`);
    const minEl = document.getElementById(`debt-min-${id}`);
    const extEl = document.getElementById(`debt-ext-${id}`);
    if (!balEl || !aprEl || !minEl || !extEl) return;

    const currentBalance = Math.max(0, Number(balEl.value || 0));
    const interestRateApr = Math.max(0, Number(aprEl.value || 0));
    const minimumMonthlyPayment = Math.max(0, Number(minEl.value || 0));
    const extraPrincipalMonthly = Math.max(0, Number(extEl.value || 0));

    if (isWebViewHost()) {
      postToHost({
        type: "updateDebtLiability",
        id,
        currentBalance,
        interestRateApr,
        minimumMonthlyPayment,
        extraPrincipalMonthly
      });
    } else {
      const item = state.debtLiabilities.find((d) => d.id === id);
      if (item) {
        item.currentBalance = currentBalance;
        item.interestRateApr = interestRateApr;
        item.minimumMonthlyPayment = minimumMonthlyPayment;
        item.extraPrincipalMonthly = extraPrincipalMonthly;
      }
      state.debtSummary = null;
      render();
      showToast(`Updated ${item ? item.name : "liability"} (+${fmtCurrency(extraPrincipalMonthly)}/mo extra principal).`);
    }
  }

  function submitNewDebtLiability(e) {
    e.preventDefault();
    const name = document.getElementById("new-debt-name")?.value.trim();
    const institution = document.getElementById("new-debt-inst")?.value.trim() || "Lender";
    const categoryType = document.getElementById("new-debt-type")?.value || "Loan";
    const currentBalance = Math.max(0, Number(document.getElementById("new-debt-bal")?.value || 0));
    const interestRateApr = Math.max(0, Number(document.getElementById("new-debt-apr")?.value || 0));
    const minimumMonthlyPayment = Math.max(1, Number(document.getElementById("new-debt-min")?.value || 100));
    const extraPrincipalMonthly = Math.max(0, Number(document.getElementById("new-debt-ext")?.value || 0));

    if (!name || currentBalance <= 0) return;

    if (isWebViewHost()) {
      postToHost({
        type: "createDebtLiability",
        name,
        institution,
        categoryType,
        originalPrincipal: currentBalance,
        currentBalance,
        interestRateApr,
        minimumMonthlyPayment,
        extraPrincipalMonthly,
        remainingTermMonths: 60,
        nextDueDate: "2026-12-01",
        escrowMonthly: 0
      });
    } else {
      state.debtLiabilities.push({
        id: Date.now(),
        accountId: null,
        name,
        institution,
        categoryType,
        originalPrincipal: currentBalance,
        currentBalance,
        interestRateApr,
        minimumMonthlyPayment,
        extraPrincipalMonthly,
        remainingTermMonths: 60,
        nextDueDate: "2026-12-01",
        escrowMonthly: 0,
        notes: "Custom liability added to payoff optimizer"
      });
      state.debtSummary = null;
      render();
      showToast(`Added '${name}' (${fmtCurrency(currentBalance)} @ ${interestRateApr.toFixed(2)}% APR).`);
    }
  }

  function deleteDebtLiability(id) {
    if (isWebViewHost()) {
      postToHost({ type: "deleteDebtLiability", liabilityId: id });
    } else {
      state.debtLiabilities = state.debtLiabilities.filter((d) => d.id !== id);
      state.debtSummary = null;
      render();
      showToast(`Removed debt profile #${id}.`);
    }
  }

  function renderDebtOptimizerView() {
    const summary = computeDebtPayoffSummary();
    const isAvalanche = summary.activeStrategy !== "Snowball";

    const debtRows = summary.liabilities.map((d) => {
      const rankBadgeColor = d.strategyPriorityRank === 1
        ? "background:rgba(212,175,55,0.2);color:#F3E5AB;border:1px solid rgba(212,175,55,0.5);"
        : "background:rgba(56,189,248,0.12);color:#38BDF8;border:1px solid rgba(56,189,248,0.3);";

      return `
        <tr>
          <td style="text-align:center;">
            <span class="badge-pill" style="${rankBadgeColor} font-weight:700;">#${d.strategyPriorityRank}</span>
          </td>
          <td>
            <div style="font-weight:700; color:var(--text-primary);">${escapeHtml(d.name)}</div>
            <div style="font-size:11px; color:var(--text-muted);">${escapeHtml(d.institution)} • <span style="color:var(--gold-light);">${escapeHtml(d.categoryType)}</span> • Due ${escapeHtml(d.nextDueDate)}</div>
            <div style="margin-top:5px; width:160px; height:5px; background:#090D14; border-radius:999px; overflow:hidden; border:1px solid var(--border-subtle);">
              <div style="width:${Math.min(100, d.paidOffPercent)}%; height:100%; background:linear-gradient(90deg, #10B981, #D4AF37);"></div>
            </div>
            <div style="font-size:10px; color:var(--text-muted); margin-top:2px;">${d.paidOffPercent.toFixed(1)}% of ${fmtCurrency(d.originalPrincipal)} original retired</div>
          </td>
          <td class="col-num">
            <input type="number" id="debt-bal-${d.id}" class="inline-edit-input" style="width:110px; text-align:right;" step="10" value="${d.currentBalance.toFixed(2)}" onchange="AppBridge.updateDebtLiabilityInline(${d.id})" />
          </td>
          <td class="col-num">
            <input type="number" id="debt-apr-${d.id}" class="inline-edit-input" style="width:74px; text-align:right;" step="0.05" value="${d.interestRateApr}" onchange="AppBridge.updateDebtLiabilityInline(${d.id})" />
          </td>
          <td class="col-num">
            <input type="number" id="debt-min-${d.id}" class="inline-edit-input" style="width:92px; text-align:right;" step="5" value="${d.minimumMonthlyPayment.toFixed(2)}" onchange="AppBridge.updateDebtLiabilityInline(${d.id})" />
          </td>
          <td class="col-num">
            <input type="number" id="debt-ext-${d.id}" class="inline-edit-input" style="width:92px; text-align:right; border-color:rgba(16,185,129,0.5); color:#10B981; font-weight:700;" step="25" value="${d.extraPrincipalMonthly.toFixed(2)}" onchange="AppBridge.updateDebtLiabilityInline(${d.id})" />
          </td>
          <td class="col-num" style="font-weight:700; color:var(--text-primary);">
            ${fmtCurrency(d.minimumMonthlyPayment + d.extraPrincipalMonthly)}
            ${d.escrowMonthly > 0 ? `<div style="font-size:10px; color:var(--text-muted);">+${fmtCurrency(d.escrowMonthly)} escrow</div>` : ""}
          </td>
          <td class="col-num">
            <div style="font-weight:700; color:#38BDF8;">${escapeHtml(d.projectedPayoffDate)}</div>
            <div style="font-size:10px; color:#10B981;">${d.monthsSaved > 0 ? `▼ ${d.monthsSaved} mo faster (${d.acceleratedMonthsToPayoff} vs ${d.baselineMonthsToPayoff} mo)` : `${d.acceleratedMonthsToPayoff} mo remaining`}</div>
          </td>
          <td class="col-num">
            <div style="font-weight:700; color:#10B981;">${fmtCurrency(d.interestSaved)}</div>
            <div style="font-size:10px; color:var(--text-muted);">Rem Interest: ${fmtCurrency(d.acceleratedTotalInterest)}</div>
          </td>
          <td style="text-align:right;">
            <button class="btn btn-outline btn-sm" title="Remove liability" onclick="AppBridge.deleteDebtLiability(${d.id})">✕</button>
          </td>
        </tr>
      `;
    }).join("");

    const scheduleRows = (summary.combinedSchedule || []).map((row) => `
      <tr>
        <td style="font-family:var(--font-mono); color:var(--text-muted);">Month #${row.monthNumber}</td>
        <td style="font-weight:600;">${escapeHtml(row.monthLabel)}</td>
        <td><span class="badge-pill" style="background:rgba(212,175,55,0.14); color:var(--gold-light); border:1px solid rgba(212,175,55,0.35);">🎯 ${escapeHtml(row.activeTargetDebtName)}</span></td>
        <td class="col-num">${fmtCurrency(row.startingBalance)}</td>
        <td class="col-num" style="font-weight:700;">${fmtCurrency(row.totalPayment)}</td>
        <td class="col-num amount-positive">${fmtCurrency(row.principalPaid)}</td>
        <td class="col-num amount-negative">${fmtCurrency(row.interestPaid)}</td>
        <td class="col-num" style="color:#38BDF8; font-weight:600;">+${fmtCurrency(row.extraPrincipalApplied)}</td>
        <td class="col-num" style="font-weight:700; color:var(--gold-light);">${fmtCurrency(row.endingBalance)}</td>
      </tr>
    `).join("");

    return `
      <!-- KPI Scoreboard -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-label">TOTAL PRINCIPAL LIABILITY</div>
          <div class="kpi-value negative">${fmtCurrency(summary.totalPrincipalBalance)}</div>
          <div class="kpi-sub">
            <span class="tag-emerald">${summary.overallPaidOffPercent.toFixed(1)}% Principal Retired</span>
            <span>Weighted ${summary.weightedAverageApr.toFixed(2)}% APR</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">MONTHLY PRINCIPAL + EXTRA SNOWBALL</div>
          <div class="kpi-value gold">${fmtCurrency(summary.totalMinimumPaymentMonthly + summary.totalExtraPrincipalMonthly)}</div>
          <div class="kpi-sub">
            <span class="tag-emerald">+${fmtCurrency(summary.totalExtraPrincipalMonthly)}/mo Extra Principal</span>
            <span>Min P&amp;I: ${fmtCurrency(summary.totalMinimumPaymentMonthly)}</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">LIFETIME INTEREST SAVED</div>
          <div class="kpi-value positive">${fmtCurrency(summary.totalInterestSaved)}</div>
          <div class="kpi-sub">
            <span class="tag-emerald">Accel: ${fmtCurrency(summary.totalAcceleratedInterest)}</span>
            <span>Baseline: ${fmtCurrency(summary.totalBaselineInterest)}</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">PROJECTED DEBT-FREE MILESTONE</div>
          <div class="kpi-value" style="color:#38BDF8;">${escapeHtml(summary.debtFreeDateLabel)}</div>
          <div class="kpi-sub">
            <span class="tag-gold">▼ ${summary.maxMonthsSaved} Months Shaved Off</span>
            <span>Strategy: ${escapeHtml(summary.activeStrategy)}</span>
          </div>
        </div>
      </div>

      <!-- Strategy Switcher & Interactive Liability Table -->
      <div class="panel-card" style="margin-bottom:16px;">
        <div class="panel-header">
          <div>
            <div class="panel-title">🏦 Interactive Debt Payoff &amp; Mortgage Amortization Optimizer</div>
            <div class="panel-subtitle">Edit Current Balance, APR %, Minimum Payment, or Extra Monthly Principal inline to recalculate interest savings and payoff dates in real time</div>
          </div>
          <div style="display:flex; gap:8px; align-items:center;">
            <button class="btn ${isAvalanche ? 'btn-gold' : 'btn-outline'} btn-sm" onclick="AppBridge.setDebtStrategy('Avalanche')">
              🏔️ Avalanche (Highest APR First)
            </button>
            <button class="btn ${!isAvalanche ? 'btn-gold' : 'btn-outline'} btn-sm" onclick="AppBridge.setDebtStrategy('Snowball')">
              ❄️ Snowball (Smallest Balance First)
            </button>
          </div>
        </div>

        <div class="register-table-wrap">
          <table class="register-table">
            <thead>
              <tr>
                <th style="width:65px; text-align:center;">Order</th>
                <th>Liability / Mortgage / Note</th>
                <th class="col-num">Current Principal</th>
                <th class="col-num">APR (%)</th>
                <th class="col-num">Min P&amp;I Payment</th>
                <th class="col-num">Extra Principal / Mo</th>
                <th class="col-num">Total Monthly Draft</th>
                <th class="col-num">Projected Payoff</th>
                <th class="col-num">Interest Saved</th>
                <th style="width:50px;"></th>
              </tr>
            </thead>
            <tbody>
              ${debtRows}
            </tbody>
          </table>
        </div>

        <!-- Quick-Add Liability Bar -->
        <form onsubmit="AppBridge.submitNewDebtLiability(event)" style="display:grid; grid-template-columns: 1.5fr 1.1fr 0.9fr 0.9fr 0.7fr 0.9fr 0.9fr auto; gap:8px; padding:12px 16px; border-top:1px solid var(--border-subtle); background:rgba(15,21,32,0.75); align-items:center;">
          <input type="text" id="new-debt-name" class="inline-edit-input" placeholder="Liability Name (e.g., Solar Loan)" required />
          <input type="text" id="new-debt-inst" class="inline-edit-input" placeholder="Lender / Bank" required />
          <select id="new-debt-type" class="inline-edit-select">
            <option value="Mortgage">Mortgage</option>
            <option value="Auto Loan">Auto Loan</option>
            <option value="Credit Card">Credit Card</option>
            <option value="Student / Personal">Student / Personal</option>
          </select>
          <input type="number" id="new-debt-bal" class="inline-edit-input" step="10" min="1" placeholder="Balance ($)" required />
          <input type="number" id="new-debt-apr" class="inline-edit-input" step="0.01" min="0" placeholder="APR %" required />
          <input type="number" id="new-debt-min" class="inline-edit-input" step="5" min="1" placeholder="Min Pay ($)" required />
          <input type="number" id="new-debt-ext" class="inline-edit-input" step="10" min="0" placeholder="Extra ($)" value="100" />
          <button type="submit" class="btn btn-gold btn-sm">＋ Add Debt</button>
        </form>
      </div>

      <!-- 24-Month Rollover Amortization Schedule -->
      <div class="panel-card">
        <div class="panel-header">
          <div>
            <div class="panel-title">📅 24-Month Combined Rollover Amortization Schedule (${escapeHtml(summary.activeStrategy)} Cascade)</div>
            <div class="panel-subtitle">When a higher-priority debt is paid off, its entire minimum + extra payment automatically cascades into the next target liability</div>
          </div>
          <span class="kicker-pill">${(summary.combinedSchedule || []).length} MONTHS PROJECTED</span>
        </div>
        <div class="register-table-wrap" style="max-height:380px; overflow-y:auto;">
          <table class="register-table">
            <thead>
              <tr>
                <th>Cycle</th>
                <th>Month</th>
                <th>Active Rollover Target</th>
                <th class="col-num">Starting Principal</th>
                <th class="col-num">Total P&amp;I Draft</th>
                <th class="col-num">Principal Retired</th>
                <th class="col-num">Interest Cost</th>
                <th class="col-num">Extra + Rollover</th>
                <th class="col-num">Ending Principal</th>
              </tr>
            </thead>
            <tbody>
              ${scheduleRows}
            </tbody>
          </table>
        </div>
      </div>
    `;
  }

  // ============================================================================
  // Phase 4B: Savings Goals & Sinking Funds Planner
  // ============================================================================
  function computeSavingsGoalsSummary() {
    if (isWebViewHost() && state.savingsGoalsSummary) {
      return state.savingsGoalsSummary;
    }

    const hysa = state.accounts.find((a) => a.id === 2) || state.accounts[0];
    const hysaBalance = hysa ? hysa.currentBalance : 29362.40;
    const anchorDate = new Date("2026-11-15T00:00:00Z");

    const goals = [...state.savingsGoals]
      .sort((a, b) => (a.priorityOrder || 10) - (b.priorityOrder || 10))
      .map((g) => {
        const tDate = new Date(`${g.targetDate || "2027-06-01"}T00:00:00Z`);
        const monthsUntilTarget = Math.max(
          1,
          (tDate.getUTCFullYear() - anchorDate.getUTCFullYear()) * 12 +
            (tDate.getUTCMonth() - anchorDate.getUTCMonth())
        );
        const remainingAmount = Math.max(0, g.targetAmount - g.currentSavedAmount);
        const progressPercent = g.targetAmount > 0
          ? Math.min(100, Math.max(0, Math.round((g.currentSavedAmount / g.targetAmount) * 1000) / 10))
          : 100;
        const requiredMonthlyToHitTargetDate = remainingAmount <= 0
          ? 0
          : Math.round((remainingAmount / monthsUntilTarget) * 100) / 100;
        const monthsAtPlan = remainingAmount <= 0
          ? 0
          : (g.monthlyContributionTarget > 0 ? Math.ceil(remainingAmount / g.monthlyContributionTarget) : 99);
        const projDate = new Date(anchorDate);
        projDate.setUTCMonth(projDate.getUTCMonth() + monthsAtPlan);

        const projectedCompletionDate = remainingAmount <= 0
          ? "Fully Funded ✓"
          : projDate.toLocaleString("en-US", { month: "short", year: "numeric", timeZone: "UTC" });

        const statusBadge = remainingAmount <= 0
          ? "Fully Funded"
          : (g.monthlyContributionTarget >= requiredMonthlyToHitTargetDate ? "On Track" : "Needs Boost");

        return {
          ...g,
          remainingAmount,
          progressPercent,
          monthsRemaining: remainingAmount <= 0 ? 0 : monthsUntilTarget,
          requiredMonthlyToHitTargetDate,
          projectedCompletionDate,
          statusBadge
        };
      });

    const totalTargetAmount = goals.reduce((s, g) => s + g.targetAmount, 0);
    const totalCurrentSaved = goals.reduce((s, g) => s + g.currentSavedAmount, 0);

    return {
      highYieldSavingsBalance: hysaBalance,
      totalTargetAmount,
      totalCurrentSaved,
      totalRemainingToFund: Math.max(0, totalTargetAmount - totalCurrentSaved),
      overallFundedPercent: totalTargetAmount > 0 ? Math.round((totalCurrentSaved / totalTargetAmount) * 1000) / 10 : 0,
      unallocatedSavingsBuffer: Math.max(0, hysaBalance - totalCurrentSaved),
      totalMonthlyContributionPlan: goals.reduce((s, g) => s + g.monthlyContributionTarget, 0),
      totalRequiredMonthlyForDeadlines: goals.reduce((s, g) => s + g.requiredMonthlyToHitTargetDate, 0),
      fullyFundedCount: goals.filter((g) => g.remainingAmount <= 0).length,
      onTrackCount: goals.filter((g) => g.statusBadge === "On Track" || g.statusBadge === "Fully Funded").length,
      goals
    };
  }

  function updateSavingsGoalInline(id) {
    const tgtEl = document.getElementById(`goal-tgt-${id}`);
    const savEl = document.getElementById(`goal-sav-${id}`);
    const monEl = document.getElementById(`goal-mon-${id}`);
    const dtEl = document.getElementById(`goal-dt-${id}`);
    if (!tgtEl || !savEl || !monEl || !dtEl) return;

    const targetAmount = Math.max(1, Number(tgtEl.value || 0));
    const currentSavedAmount = Math.max(0, Number(savEl.value || 0));
    const monthlyContributionTarget = Math.max(0, Number(monEl.value || 0));
    const targetDate = dtEl.value || "2027-06-01";

    if (isWebViewHost()) {
      postToHost({
        type: "updateSavingsGoal",
        id,
        targetAmount,
        currentSavedAmount,
        monthlyContributionTarget,
        targetDate
      });
    } else {
      const g = state.savingsGoals.find((x) => x.id === id);
      if (g) {
        g.targetAmount = targetAmount;
        g.currentSavedAmount = currentSavedAmount;
        g.monthlyContributionTarget = monthlyContributionTarget;
        g.targetDate = targetDate;
      }
      state.savingsGoalsSummary = null;
      render();
      showToast(`Updated '${g ? g.name : "goal"}' (${fmtCurrency(currentSavedAmount)} / ${fmtCurrency(targetAmount)}).`);
    }
  }

  function contributeSavingsGoal(goalId, customAmount) {
    const g = state.savingsGoals.find((x) => x.id === goalId);
    const amount = Number(customAmount || (g ? g.monthlyContributionTarget : 250) || 250);
    if (amount <= 0) return;

    if (isWebViewHost()) {
      postToHost({
        type: "contributeSavingsGoal",
        goalId,
        amount,
        recordLedgerTransfer: true
      });
    } else {
      if (g) {
        g.currentSavedAmount = Math.round((g.currentSavedAmount + amount) * 100) / 100;
        const hysa = state.accounts.find((a) => a.id === 2);
        if (hysa) {
          hysa.currentBalance = Math.round((hysa.currentBalance + amount) * 100) / 100;
          hysa.clearedBalance = Math.round((hysa.clearedBalance + amount) * 100) / 100;
        }
        state.transactions.unshift({
          id: Date.now(),
          accountId: 2,
          accountName: "High-Yield Savings",
          transactionDate: "2026-11-15",
          referenceNumber: "GOAL-XFER",
          payeeName: `Sinking Fund Allocation — ${g.name}`,
          categoryId: 71,
          categoryName: "Transfers & Debt:Savings Transfer",
          memo: `Phase 4B Sinking Fund Contribution to '${g.name}'`,
          amount,
          payment: 0,
          deposit: amount,
          runningBalance: hysa ? hysa.currentBalance : 29362.40 + amount,
          status: "Cleared",
          isSplit: false,
          splits: []
        });
      }
      state.savingsGoalsSummary = null;
      render();
      showToast(`Allocated +${fmtCurrency(amount)} to '${g ? g.name : "Sinking Fund"}' & recorded ledger transfer.`);
    }
  }

  function submitNewSavingsGoal(e) {
    e.preventDefault();
    if (!state.accounts || state.accounts.length === 0) {
      showToast("Please add a savings or checking account first.");
      openAccountModal();
      return;
    }
    const defaultSavingsAcc = state.accounts.find((a) => a.accountType === "Savings")
      || state.accounts.find((a) => a.accountGroup === "Banking")
      || state.accounts[0];
    const name = document.getElementById("new-goal-name")?.value.trim();
    const categoryGroup = document.getElementById("new-goal-group")?.value || "Sinking Fund";
    const iconGlyph = document.getElementById("new-goal-icon")?.value || "🎯";
    const targetAmount = Math.max(1, Number(document.getElementById("new-goal-target")?.value || 0));
    const currentSavedAmount = Math.max(0, Number(document.getElementById("new-goal-saved")?.value || 0));
    const monthlyContributionTarget = Math.max(0, Number(document.getElementById("new-goal-monthly")?.value || 250));
    const targetDate = document.getElementById("new-goal-date")?.value || `${new Date().getFullYear() + 1}-06-01`;

    if (!name || targetAmount <= 0) return;

    if (isWebViewHost()) {
      postToHost({
        type: "createSavingsGoal",
        accountId: defaultSavingsAcc?.id || 0,
        name,
        categoryGroup,
        iconGlyph,
        colorHex: "#38BDF8",
        targetAmount,
        currentSavedAmount,
        monthlyContributionTarget,
        targetDate,
        priorityOrder: (state.savingsGoals.length + 1) * 10
      });
    } else {
      state.savingsGoals.push({
        id: Date.now(),
        accountId: 2,
        accountName: "High-Yield Savings ••9014",
        name,
        categoryGroup,
        iconGlyph,
        colorHex: "#38BDF8",
        targetAmount,
        currentSavedAmount,
        monthlyContributionTarget,
        targetDate,
        priorityOrder: (state.savingsGoals.length + 1) * 10,
        notes: "Custom sinking fund goal"
      });
      state.savingsGoalsSummary = null;
      render();
      showToast(`Created sinking fund '${name}' (${fmtCurrency(currentSavedAmount)} / ${fmtCurrency(targetAmount)}).`);
    }
  }

  function deleteSavingsGoal(id) {
    if (isWebViewHost()) {
      postToHost({ type: "deleteSavingsGoal", goalId: id });
    } else {
      state.savingsGoals = state.savingsGoals.filter((g) => g.id !== id);
      state.savingsGoalsSummary = null;
      render();
      showToast(`Deleted savings goal #${id}.`);
    }
  }

  function renderSavingsGoalsView() {
    const summary = computeSavingsGoalsSummary();

    const goalCards = summary.goals.map((g) => {
      const badgeStyle = g.statusBadge === "Fully Funded"
        ? "background:rgba(16,185,129,0.2); color:#10B981; border:1px solid rgba(16,185,129,0.5);"
        : g.statusBadge === "On Track"
        ? "background:rgba(56,189,248,0.15); color:#38BDF8; border:1px solid rgba(56,189,248,0.4);"
        : "background:rgba(245,158,11,0.18); color:#F59E0B; border:1px solid rgba(245,158,11,0.45);";

      return `
        <div class="panel-card" style="padding:16px; border-top:3px solid ${escapeHtml(g.colorHex || '#10B981')};">
          <div style="display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:10px;">
            <div style="display:flex; gap:10px; align-items:center;">
              <div style="font-size:24px; width:42px; height:42px; border-radius:10px; background:rgba(15,21,32,0.9); border:1px solid var(--border-subtle); display:flex; align-items:center; justify-content:center;">
                ${escapeHtml(g.iconGlyph)}
              </div>
              <div>
                <div style="font-weight:700; font-size:14px; color:var(--text-primary);">${escapeHtml(g.name)}</div>
                <div style="font-size:11px; color:var(--text-muted);">${escapeHtml(g.categoryGroup)} • ${escapeHtml(g.accountName)}</div>
              </div>
            </div>
            <span class="badge-pill" style="${badgeStyle} font-weight:700;">${escapeHtml(g.statusBadge)}</span>
          </div>

          <div style="display:flex; justify-content:space-between; align-items:baseline; margin-bottom:6px;">
            <div>
              <span style="font-family:var(--font-mono); font-size:20px; font-weight:800; color:var(--emerald);">${fmtCurrency(g.currentSavedAmount)}</span>
              <span style="font-size:12px; color:var(--text-muted);"> of ${fmtCurrency(g.targetAmount)} target</span>
            </div>
            <div style="font-family:var(--font-mono); font-size:13px; font-weight:700; color:var(--gold-light);">${g.progressPercent.toFixed(1)}%</div>
          </div>

          <div style="width:100%; height:8px; background:#090D14; border-radius:999px; overflow:hidden; border:1px solid var(--border-subtle); margin-bottom:10px;">
            <div style="width:${Math.min(100, g.progressPercent)}%; height:100%; background:linear-gradient(90deg, ${escapeHtml(g.colorHex || '#10B981')}, #D4AF37);"></div>
          </div>

          <div style="display:grid; grid-template-columns:repeat(3, 1fr); gap:8px; font-size:11px; padding:8px 10px; border-radius:8px; background:rgba(9,13,20,0.65); border:1px solid var(--border-subtle); margin-bottom:10px;">
            <div>
              <div style="color:var(--text-muted); font-size:10px;">REMAINING</div>
              <div style="font-family:var(--font-mono); font-weight:700;">${fmtCurrency(g.remainingAmount)}</div>
            </div>
            <div>
              <div style="color:var(--text-muted); font-size:10px;">REQ. / MONTH</div>
              <div style="font-family:var(--font-mono); font-weight:700; color:${g.monthlyContributionTarget >= g.requiredMonthlyToHitTargetDate ? '#10B981' : '#F59E0B'};">${fmtCurrency(g.requiredMonthlyToHitTargetDate)}/mo</div>
            </div>
            <div>
              <div style="color:var(--text-muted); font-size:10px;">PROJECTED HIT</div>
              <div style="font-family:var(--font-mono); font-weight:700; color:#38BDF8;">${escapeHtml(g.projectedCompletionDate)}</div>
            </div>
          </div>

          <div style="display:flex; justify-content:space-between; align-items:center; gap:8px;">
            <div style="font-size:11px; color:var(--text-muted);">Target Date: <strong>${escapeHtml(g.targetDate)}</strong> (${g.monthsRemaining} mo)</div>
            <div style="display:flex; gap:6px;">
              <button class="btn btn-gold btn-sm" onclick="AppBridge.contributeSavingsGoal(${g.id}, ${g.monthlyContributionTarget})">
                ＋ Fund Monthly (${fmtCurrency(g.monthlyContributionTarget)})
              </button>
              <button class="btn btn-outline btn-sm" onclick="AppBridge.contributeSavingsGoal(${g.id}, 250)">
                +$250
              </button>
            </div>
          </div>
        </div>
      `;
    }).join("");

    const tableRows = summary.goals.map((g) => `
      <tr>
        <td>
          <div style="font-weight:700; color:var(--text-primary);">${escapeHtml(g.iconGlyph)} ${escapeHtml(g.name)}</div>
          <div style="font-size:11px; color:var(--text-muted);">${escapeHtml(g.categoryGroup)} • ${escapeHtml(g.notes || '')}</div>
        </td>
        <td>
          <input type="date" id="goal-dt-${g.id}" class="inline-edit-input" style="width:130px;" value="${escapeHtml(g.targetDate)}" onchange="AppBridge.updateSavingsGoalInline(${g.id})" />
        </td>
        <td class="col-num">
          <input type="number" id="goal-sav-${g.id}" class="inline-edit-input" style="width:110px; text-align:right; color:#10B981; font-weight:700;" step="50" value="${g.currentSavedAmount.toFixed(2)}" onchange="AppBridge.updateSavingsGoalInline(${g.id})" />
        </td>
        <td class="col-num">
          <input type="number" id="goal-tgt-${g.id}" class="inline-edit-input" style="width:110px; text-align:right;" step="100" value="${g.targetAmount.toFixed(2)}" onchange="AppBridge.updateSavingsGoalInline(${g.id})" />
        </td>
        <td class="col-num" style="font-weight:700;">${fmtCurrency(g.remainingAmount)}</td>
        <td class="col-num">
          <input type="number" id="goal-mon-${g.id}" class="inline-edit-input" style="width:95px; text-align:right; border-color:rgba(212,175,55,0.5); color:var(--gold-light); font-weight:700;" step="25" value="${g.monthlyContributionTarget.toFixed(2)}" onchange="AppBridge.updateSavingsGoalInline(${g.id})" />
        </td>
        <td class="col-num" style="color:${g.monthlyContributionTarget >= g.requiredMonthlyToHitTargetDate ? '#10B981' : '#F59E0B'}; font-weight:700;">
          ${fmtCurrency(g.requiredMonthlyToHitTargetDate)}/mo
        </td>
        <td style="text-align:right; white-space:nowrap;">
          <button class="btn btn-gold btn-sm" onclick="AppBridge.contributeSavingsGoal(${g.id}, ${g.monthlyContributionTarget})">＋ Contribute</button>
          <button class="btn btn-outline btn-sm" onclick="AppBridge.deleteSavingsGoal(${g.id})">✕</button>
        </td>
      </tr>
    `).join("");

    return `
      <!-- KPI Scoreboard -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-label">TOTAL SINKING FUNDS SAVED</div>
          <div class="kpi-value positive">${fmtCurrency(summary.totalCurrentSaved)}</div>
          <div class="kpi-sub">
            <span class="tag-emerald">${summary.overallFundedPercent.toFixed(1)}% of ${fmtCurrency(summary.totalTargetAmount)} Goal</span>
            <span>HYSA: ${fmtCurrency(summary.highYieldSavingsBalance)}</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">REMAINING TO FULLY FUND</div>
          <div class="kpi-value gold">${fmtCurrency(summary.totalRemainingToFund)}</div>
          <div class="kpi-sub">
            <span class="tag-gold">${summary.onTrackCount}/${summary.goals.length} Goals On Track</span>
            <span>Unallocated Buffer: ${fmtCurrency(summary.unallocatedSavingsBuffer)}</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">PLANNED MONTHLY CONTRIBUTIONS</div>
          <div class="kpi-value" style="color:#38BDF8;">${fmtCurrency(summary.totalMonthlyContributionPlan)}/mo</div>
          <div class="kpi-sub">
            <span class="tag-emerald">Req for Deadlines: ${fmtCurrency(summary.totalRequiredMonthlyForDeadlines)}/mo</span>
          </div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">HIGH-YIELD SAVINGS APY</div>
          <div class="kpi-value positive">4.65% APY</div>
          <div class="kpi-sub">
            <span class="tag-gold">Marcus ••9014</span>
            <span>Est. +${fmtCurrency(summary.totalCurrentSaved * 0.0465 / 12)}/mo Yield</span>
          </div>
        </div>
      </div>

      <!-- Visual Sinking Fund Cards Grid -->
      <div style="display:grid; grid-template-columns:repeat(2, 1fr); gap:14px; margin-bottom:16px;">
        ${goalCards}
      </div>

      <!-- Inline Sinking Fund Allocation Table & Quick-Add Form -->
      <div class="panel-card">
        <div class="panel-header">
          <div>
            <div class="panel-title">🏛️ Sinking Fund Target &amp; Contribution Ledger (SQLite SavingsGoals)</div>
            <div class="panel-subtitle">Edit Target Dates, Saved Balances, Targets, or Monthly Contributions inline, or click ＋ Contribute to record a Cleared Savings Transfer</div>
          </div>
          <span class="kicker-pill">${summary.goals.length} ACTIVE SINKING FUNDS</span>
        </div>
        <div class="register-table-wrap">
          <table class="register-table">
            <thead>
              <tr>
                <th>Savings Goal / Sinking Fund</th>
                <th>Target Date</th>
                <th class="col-num">Current Saved</th>
                <th class="col-num">Target Goal</th>
                <th class="col-num">Remaining</th>
                <th class="col-num">Monthly Plan</th>
                <th class="col-num">Required / Mo</th>
                <th style="text-align:right;">Actions</th>
              </tr>
            </thead>
            <tbody>
              ${tableRows}
            </tbody>
          </table>
        </div>

        <form onsubmit="AppBridge.submitNewSavingsGoal(event)" style="display:grid; grid-template-columns: 0.45fr 1.5fr 1fr 0.9fr 0.9fr 0.9fr 0.95fr auto; gap:8px; padding:12px 16px; border-top:1px solid var(--border-subtle); background:rgba(15,21,32,0.75); align-items:center;">
          <input type="text" id="new-goal-icon" class="inline-edit-input" value="🎯" style="text-align:center;" />
          <input type="text" id="new-goal-name" class="inline-edit-input" placeholder="New Goal Name (e.g., Parish Tuition Reserve)" required />
          <select id="new-goal-group" class="inline-edit-select">
            <option value="Emergency & Reserves">Emergency &amp; Reserves</option>
            <option value="Major Purchases">Major Purchases</option>
            <option value="Family & Ministry">Family &amp; Ministry</option>
            <option value="Home & Escrow">Home &amp; Escrow</option>
          </select>
          <input type="number" id="new-goal-saved" class="inline-edit-input" step="50" min="0" placeholder="Saved ($)" value="500" required />
          <input type="number" id="new-goal-target" class="inline-edit-input" step="100" min="100" placeholder="Target ($)" required />
          <input type="number" id="new-goal-monthly" class="inline-edit-input" step="25" min="10" placeholder="Monthly ($)" value="250" required />
          <input type="date" id="new-goal-date" class="inline-edit-input" value="2027-12-01" required />
          <button type="submit" class="btn btn-gold btn-sm">＋ Add Goal</button>
        </form>
      </div>
    `;
  }

  function render() {
    updateHeader();
    const container = document.getElementById("workspace-content");
    if (!container) return;

    if (state.activeView === "Register" || state.activeView === "AllTransactions") {
      container.innerHTML = renderRegisterSection(false);
    } else if (state.activeView === "Rules") {
      container.innerHTML = renderRulesEngineView();
    } else if (state.activeView === "Budgets") {
      container.innerHTML = renderBudgetsView();
    } else if (state.activeView === "CashFlow") {
      container.innerHTML = renderCashFlowView();
    } else if (state.activeView === "SavingsGoals") {
      container.innerHTML = renderSavingsGoalsView();
    } else if (state.activeView === "Investments") {
      container.innerHTML = renderInvestmentsView();
    } else if (state.activeView === "DebtOptimizer") {
      container.innerHTML = renderDebtOptimizerView();
    } else if (state.activeView === "Reports") {
      container.innerHTML = renderReportsView();
    } else if (state.activeView === "WebPortal") {
      container.innerHTML = renderWebPortalView();
    } else {
      container.innerHTML = renderDashboardView();
    }
  }

  // Statement Import & Multi-Pass Deduplication Studio
  function generateStandaloneSamplePreview(format, targetAccountId) {
    const sampleItems = [
      {
        tempId: "imp1",
        externalFitId: "POS-334",
        transactionDate: "2026-11-13",
        rawPayee: "WHOLEFDS MKT #10248",
        normalizedPayee: "Whole Foods Market",
        rawMemo: "Fresh produce, poultry & bakery",
        finalMemo: "Fresh produce, poultry & bakery",
        referenceNumber: "POS-334",
        amount: -138.45,
        suggestedCategoryId: 30,
        suggestedCategoryName: "Food & Household:Groceries",
        suggestedStatus: "Cleared",
        appliedRuleId: 1,
        appliedRuleName: "Whole Foods Market Normalization",
        duplicateMatchType: "ExactFitId",
        matchedTransactionId: 9,
        matchConfidenceScore: 100,
        matchReason: "Exact FITID/Ref match (#9 'Whole Foods Market' on 2026-11-13)",
        selectedForImport: false,
        isDuplicate: true
      },
      {
        tempId: "imp2",
        externalFitId: "ACH-992",
        transactionDate: "2026-11-14",
        rawPayee: "SONIC.NET*FIBER TELECOM",
        normalizedPayee: "Sonic Fiber Telecom",
        rawMemo: "November gigabit fiber service",
        finalMemo: "November gigabit fiber service",
        referenceNumber: "ACH-992",
        amount: -79.99,
        suggestedCategoryId: 22,
        suggestedCategoryName: "Housing & Utilities:Fiber Internet",
        suggestedStatus: "Cleared",
        appliedRuleId: 3,
        appliedRuleName: "Sonic Gigabit Fiber Utility",
        duplicateMatchType: "FuzzyMatch",
        matchedTransactionId: 10,
        matchConfidenceScore: 96,
        matchReason: "Fuzzy match (96%): #10 'Sonic Fiber Telecom' (same day, exact $79.99)",
        selectedForImport: false,
        isDuplicate: true
      },
      {
        tempId: "imp3",
        externalFitId: "OFX-20261114-01",
        transactionDate: "2026-11-14",
        rawPayee: "SQ *BLUE BOTTLE COFFEE",
        normalizedPayee: "Blue Bottle Coffee Roasters",
        rawMemo: "Afternoon roast & cortado",
        finalMemo: "Afternoon roast & cortado",
        referenceNumber: "OFX-20261114-01",
        amount: -19.50,
        suggestedCategoryId: 31,
        suggestedCategoryName: "Food & Household:Coffee & Dining",
        suggestedStatus: "Cleared",
        appliedRuleId: 2,
        appliedRuleName: "Blue Bottle Coffee Square POS",
        duplicateMatchType: "None",
        matchedTransactionId: null,
        matchConfidenceScore: 0,
        matchReason: null,
        selectedForImport: true,
        isDuplicate: false
      },
      {
        tempId: "imp4",
        externalFitId: "OFX-20261115-02",
        transactionDate: "2026-11-15",
        rawPayee: "COSTCO WHSE #0482",
        normalizedPayee: "Costco Wholesale",
        rawMemo: "Household bulk stockup",
        finalMemo: "Household bulk stockup",
        referenceNumber: "OFX-20261115-02",
        amount: -114.20,
        suggestedCategoryId: 30,
        suggestedCategoryName: "Food & Household:Groceries",
        suggestedStatus: "Cleared",
        appliedRuleId: 4,
        appliedRuleName: "Costco Wholesale Club",
        duplicateMatchType: "None",
        matchedTransactionId: null,
        matchConfidenceScore: 0,
        matchReason: null,
        selectedForImport: true,
        isDuplicate: false
      },
      {
        tempId: "imp5",
        externalFitId: "OFX-20261115-03",
        transactionDate: "2026-11-15",
        rawPayee: "AMZN MKTP US*99104",
        normalizedPayee: "Amazon Marketplace",
        rawMemo: "Clean Architecture book & stand",
        finalMemo: "Clean Architecture book & stand • Auto-categorized via AMZN rule",
        referenceNumber: "OFX-20261115-03",
        amount: -49.99,
        suggestedCategoryId: 61,
        suggestedCategoryName: "Lifestyle & Tech:Equipment & Books",
        suggestedStatus: "Cleared",
        appliedRuleId: 5,
        appliedRuleName: "Amazon Marketplace Tech & Books",
        duplicateMatchType: "None",
        matchedTransactionId: null,
        matchConfidenceScore: 0,
        matchReason: null,
        selectedForImport: true,
        isDuplicate: false
      }
    ];

    return {
      sourceFileName: `chase_statement_sample.${format.toLowerCase()}`,
      detectedFormat: format === "OFX" ? "OFX/QFX" : format,
      targetAccountId: targetAccountId || 1,
      totalParsedCount: sampleItems.length,
      newItemsCount: sampleItems.filter((i) => !i.isDuplicate).length,
      duplicateItemsCount: sampleItems.filter((i) => i.isDuplicate).length,
      rulesMatchedCount: sampleItems.filter((i) => i.appliedRuleId).length,
      items: sampleItems
    };
  }

  function renderStatementPreviewModal() {
    const p = state.statementPreview;
    if (!p) return;

    const summaryEl = document.getElementById("import-summary-strip");
    const tbodyEl = document.getElementById("import-preview-tbody");
    const selectedCount = (p.items || []).filter((i) => i.selectedForImport).length;

    summaryEl.innerHTML = `
      <div class="import-stat-pill">
        <div class="kpi-label">Detected Format</div>
        <div style="font-weight:700; font-size:14px; color:var(--accent-blue); margin-top:2px;">${escapeHtml(p.detectedFormat)} (${escapeHtml(p.sourceFileName)})</div>
      </div>
      <div class="import-stat-pill">
        <div class="kpi-label">New Entries Selected</div>
        <div style="font-weight:700; font-size:14px; color:var(--accent-emerald); margin-top:2px;">${selectedCount} of ${p.items.length} Rows</div>
      </div>
      <div class="import-stat-pill">
        <div class="kpi-label">Duplicates Auto-Skipped</div>
        <div style="font-weight:700; font-size:14px; color:var(--accent-gold); margin-top:2px;">${p.duplicateItemsCount} Fuzzy/FITID Matches</div>
      </div>
      <div class="import-stat-pill">
        <div class="kpi-label">IF/THEN Rules Matched</div>
        <div style="font-weight:700; font-size:14px; color:#a5b4fc; margin-top:2px;">⚡ ${p.rulesMatchedCount} Payees Normalized</div>
      </div>
    `;

    tbodyEl.innerHTML = (p.items || []).map((item, idx) => {
      const dupBadge = item.duplicateMatchType !== "None"
        ? `<span class="status-badge status-uncleared" title="${escapeHtml(item.matchReason || '')}">
             ⚠ ${escapeHtml(item.duplicateMatchType)} (${item.matchConfidenceScore}%)
           </span>
           <div style="font-size:10.5px; color:var(--accent-gold); margin-top:2px;">${escapeHtml(item.matchReason || "")}</div>`
        : `<span class="status-badge status-reconciled">✓ NEW LEDGER ENTRY</span>`;

      const ruleBadge = item.appliedRuleName
        ? `<div style="font-size:10.5px; color:var(--accent-blue); margin-top:2px;">⚡ Rule: ${escapeHtml(item.appliedRuleName)}</div>`
        : "";

      return `
        <tr class="tx-row" style="${item.selectedForImport ? "" : "opacity:0.58;"}">
          <td style="text-align:center;">
            <input type="checkbox" ${item.selectedForImport ? "checked" : ""} onchange="AppBridge.toggleImportRow(${idx}, this.checked)" />
          </td>
          <td style="font-family:var(--font-mono); font-size:11.5px;">
            <div>${escapeHtml(item.transactionDate)}</div>
            <div style="color:var(--text-muted); font-size:10px;">${escapeHtml(item.referenceNumber || item.externalFitId || "")}</div>
          </td>
          <td>
            <div style="font-size:11px; color:var(--text-muted); text-decoration:line-through;">${escapeHtml(item.rawPayee)}</div>
            <div style="font-weight:700; color:var(--text-primary);">→ ${escapeHtml(item.normalizedPayee)}</div>
            <div style="font-size:11px; color:var(--text-secondary);">${escapeHtml(item.finalMemo || item.rawMemo || "")}</div>
          </td>
          <td>
            <span class="category-chip">${escapeHtml(item.suggestedCategoryName)}</span>
            ${ruleBadge}
          </td>
          <td>${dupBadge}</td>
          <td class="num-col ${item.amount < 0 ? "amount-negative" : "amount-positive"}">${fmtCurrency(item.amount)}</td>
        </tr>
      `;
    }).join("");
  }

  function openImportStudioModal(defaultAccountId) {
    const accSelect = document.getElementById("import-account");
    const targetAccId = defaultAccountId || state.selectedAccountId || 1;
    if (accSelect) {
      accSelect.innerHTML = state.accounts.map((a) =>
        `<option value="${a.id}" ${a.id === targetAccId ? "selected" : ""}>
          ${escapeHtml(a.name)} (${escapeHtml(a.accountNumberMask)})
        </option>`
      ).join("");
    }

    document.getElementById("import-modal-overlay").classList.remove("hidden");
    loadSampleStatement(state.activeSampleFormat || "OFX");
  }

  function closeImportStudioModal() {
    document.getElementById("import-modal-overlay").classList.add("hidden");
  }

  function loadSampleStatement(format) {
    state.activeSampleFormat = format;
    const accId = Number(document.getElementById("import-account")?.value || state.selectedAccountId || 1);
    if (isWebViewHost()) {
      postToHost({
        type: "parseStatement",
        accountId: accId,
        sampleFormat: format
      });
    } else {
      state.statementPreview = generateStandaloneSamplePreview(format, accId);
      renderStatementPreviewModal();
    }
  }

  function handleLocalFileUpload(event) {
    const file = event.target.files?.[0];
    if (!file) return;
    const accId = Number(document.getElementById("import-account")?.value || state.selectedAccountId || 1);
    const reader = new FileReader();
    reader.onload = () => {
      const content = String(reader.result || "");
      if (isWebViewHost()) {
        postToHost({
          type: "parseStatement",
          accountId: accId,
          fileName: file.name,
          content
        });
      } else {
        state.statementPreview = generateStandaloneSamplePreview("CSV", accId);
        state.statementPreview.sourceFileName = file.name;
        renderStatementPreviewModal();
      }
    };
    reader.readAsText(file);
  }

  function browseNativeStatementFile() {
    const accId = Number(document.getElementById("import-account")?.value || state.selectedAccountId || 1);
    if (isWebViewHost()) {
      postToHost({
        type: "browseStatementFile",
        accountId: accId
      });
    } else {
      document.getElementById("import-file-input")?.click();
    }
  }

  function toggleImportRow(idx, checked) {
    if (!state.statementPreview || !state.statementPreview.items[idx]) return;
    state.statementPreview.items[idx].selectedForImport = checked;
    renderStatementPreviewModal();
  }

  function commitSelectedImports() {
    if (!state.statementPreview) return;
    const accId = Number(document.getElementById("import-account")?.value || state.statementPreview.targetAccountId || 1);
    const selected = state.statementPreview.items.filter((i) => i.selectedForImport);

    if (selected.length === 0) {
      showToast("No statement rows selected for import.");
      return;
    }

    if (isWebViewHost()) {
      postToHost({
        type: "commitStatementImport",
        accountId: accId,
        items: state.statementPreview.items
      });
    } else {
      selected.forEach((item, i) => {
        state.transactions.unshift({
          id: Date.now() + i,
          accountId: accId,
          accountName: (state.accounts.find((a) => a.id === accId) || {}).name || "Primary Checking",
          transactionDate: item.transactionDate,
          referenceNumber: item.referenceNumber || "OFX",
          payeeName: item.normalizedPayee,
          categoryName: item.suggestedCategoryName,
          memo: item.finalMemo || item.rawMemo,
          amount: item.amount,
          runningBalance: 0,
          status: item.suggestedStatus || "Cleared",
          isSplit: false,
          splits: []
        });
      });
      recalcStandaloneBalances();
      render();
      showToast(`Committed ${selected.length} imported statement transaction(s) to SQLite.`);
    }

    closeImportStudioModal();
  }

  // Rules Engine Handlers
  function submitNewRule(e) {
    e.preventDefault();
    const name = document.getElementById("rule-name").value.trim();
    const matchField = document.getElementById("rule-field").value;
    const matchOperator = document.getElementById("rule-op").value;
    const matchPattern = document.getElementById("rule-pattern").value.trim();
    const renamePayeeTo = document.getElementById("rule-rename").value.trim();
    const assignCategoryId = Number(document.getElementById("rule-cat").value);
    const priority = Number(document.getElementById("rule-priority").value || 25);

    if (isWebViewHost()) {
      postToHost({
        type: "createRule",
        name,
        priority,
        isEnabled: true,
        matchField,
        matchOperator,
        matchPattern,
        renamePayeeTo,
        assignCategoryId,
        setStatus: "Cleared"
      });
    } else {
      const cat = state.categories.find((c) => c.id === assignCategoryId);
      state.rules.push({
        id: Date.now(),
        name,
        priority,
        isEnabled: true,
        matchField,
        matchOperator,
        matchPattern,
        renamePayeeTo,
        assignCategoryId,
        assignCategoryName: cat ? cat.fullPath : "Uncategorized",
        setStatus: "Cleared"
      });
      state.rules.sort((a, b) => a.priority - b.priority);
      render();
      showToast(`Created rule '${name}'`);
    }
  }

  function toggleRule(ruleId) {
    if (isWebViewHost()) {
      postToHost({ type: "toggleRule", ruleId });
    } else {
      const r = state.rules.find((x) => x.id === ruleId);
      if (r) {
        r.isEnabled = !r.isEnabled;
        render();
      }
    }
  }

  function deleteRule(ruleId) {
    if (isWebViewHost()) {
      postToHost({ type: "deleteRule", ruleId });
    } else {
      state.rules = state.rules.filter((x) => x.id !== ruleId);
      render();
      showToast(`Deleted rule #${ruleId}`);
    }
  }

  function applyRulesToRegister() {
    if (isWebViewHost()) {
      postToHost({ type: "applyRulesToRegister" });
    } else {
      showToast("Rules Engine evaluated all active rules across the register.");
    }
  }

  // Modal & Form Handlers
  function populateModalSelects(defaultAccountId) {
    const accSelect = document.getElementById("tx-account");
    const catSelect = document.getElementById("tx-category");
    const payeeList = document.getElementById("payee-suggestions");
    const dateInput = document.getElementById("tx-date");

    if (dateInput && !dateInput.value) {
      dateInput.value = new Date().toISOString().slice(0, 10);
    }

    if (accSelect) {
      accSelect.innerHTML = state.accounts.map((a) =>
        `<option value="${a.id}" ${a.id === (defaultAccountId || state.selectedAccountId || 1) ? "selected" : ""}>
          ${escapeHtml(a.name)} (${escapeHtml(a.accountNumberMask)})
        </option>`
      ).join("");
    }

    if (catSelect) {
      catSelect.innerHTML = state.categories.map((c) =>
        `<option value="${c.id}">${escapeHtml(c.fullPath || c.name)}</option>`
      ).join("");
    }

    if (payeeList) {
      payeeList.innerHTML = state.payees.map((p) =>
        `<option value="${escapeHtml(p.name)}"></option>`
      ).join("");
    }
  }

  function openAccountModal(accountId = null) {
    const overlay = document.getElementById("account-modal-overlay");
    if (!overlay) return;
    const titleEl = document.getElementById("account-modal-title");
    const editIdEl = document.getElementById("acct-edit-id");
    const nameEl = document.getElementById("acct-name");
    const typeEl = document.getElementById("acct-type");
    const instEl = document.getElementById("acct-institution");
    const maskEl = document.getElementById("acct-mask");
    const balEl = document.getElementById("acct-opening-balance");
    const portalEl = document.getElementById("acct-portal");
    const notesEl = document.getElementById("acct-notes");

    const existing = accountId ? state.accounts.find((a) => a.id === accountId) : null;
    if (existing) {
      titleEl.textContent = `Edit Account — ${existing.name}`;
      editIdEl.value = String(existing.id);
      nameEl.value = existing.name || "";
      typeEl.value = existing.accountType || "Checking";
      instEl.value = existing.institution || "";
      maskEl.value = (existing.accountNumberMask || "").replace(/^•+/, "");
      const isLiab = typeEl.value === "CreditCard" || typeEl.value === "Loan";
      balEl.value = isLiab ? Math.abs(Number(existing.openingBalance || 0)).toFixed(2) : Number(existing.openingBalance || 0).toFixed(2);
      portalEl.value = existing.portalUrl || "";
      notesEl.value = existing.notes || "";
    } else {
      titleEl.textContent = "Add Account";
      editIdEl.value = "";
      document.getElementById("account-form")?.reset();
      typeEl.value = "Checking";
      balEl.value = "0.00";
    }
    onAccountTypeChanged();
    overlay.classList.remove("hidden");
  }

  function closeAccountModal() {
    document.getElementById("account-modal-overlay")?.classList.add("hidden");
  }

  function onAccountTypeChanged() {
    const type = document.getElementById("acct-type")?.value || "Checking";
    const labelEl = document.getElementById("acct-balance-label");
    const hintEl = document.getElementById("acct-balance-hint");
    if (!labelEl || !hintEl) return;
    if (type === "CreditCard" || type === "Loan") {
      labelEl.textContent = "Current Amount Owed ($)";
      hintEl.textContent = "Enter as a positive number (e.g. 500.00). Stored as a liability (−$500.00).";
    } else {
      labelEl.textContent = "Opening Balance ($)";
      hintEl.textContent = "Current starting balance in this account.";
    }
  }

  function submitAccountForm(e) {
    e.preventDefault();
    const editId = Number(document.getElementById("acct-edit-id")?.value || 0);
    const name = (document.getElementById("acct-name")?.value || "").trim();
    const accountType = document.getElementById("acct-type")?.value || "Checking";
    const institution = (document.getElementById("acct-institution")?.value || "").trim();
    const accountNumberMask = (document.getElementById("acct-mask")?.value || "").trim();
    const rawBal = Number(document.getElementById("acct-opening-balance")?.value || 0);
    const isLiabilityType = accountType === "CreditCard" || accountType === "Loan";
    const openingBalance = isLiabilityType ? -Math.abs(rawBal) : rawBal;
    const portalUrl = (document.getElementById("acct-portal")?.value || "").trim() || null;
    const notes = (document.getElementById("acct-notes")?.value || "").trim() || null;

    if (!name) return;

    if (isWebViewHost()) {
      postToHost({
        type: editId > 0 ? "updateAccount" : "createAccount",
        accountId: editId,
        name,
        accountType,
        institution,
        accountNumberMask,
        openingBalance,
        portalUrl,
        notes
      });
    } else {
      const group = accountType === "CreditCard" ? "Credit Cards" : (accountType === "Investment" ? "Investments" : (accountType === "Loan" || accountType === "Asset" ? "Assets/Loans" : "Banking"));
      const mask = accountNumberMask ? (accountNumberMask.startsWith("••") ? accountNumberMask : "••" + accountNumberMask) : "";
      if (editId > 0) {
        const idx = state.accounts.findIndex((a) => a.id === editId);
        if (idx >= 0) {
          state.accounts[idx] = { ...state.accounts[idx], name, accountType, accountGroup: group, institution: institution || "Personal", accountNumberMask: mask, openingBalance, portalUrl, notes };
        }
        showToast(`Updated account '${name}'.`);
      } else {
        const newId = Date.now();
        state.accounts.push({ id: newId, name, accountNumberMask: mask, accountType, accountGroup: group, institution: institution || "Personal", openingBalance, currentBalance: openingBalance, clearedBalance: openingBalance, pendingTotal: 0, isLiability: isLiabilityType, portalUrl, notes });
        state.selectedAccountId = newId;
        state.selectedAccount = state.accounts[state.accounts.length - 1];
        showToast(`Created account '${name}'.`);
      }
      recalcStandaloneBalances();
      render();
    }
    closeAccountModal();
  }

  function deleteAccount(accountId) {
    const acc = state.accounts.find((a) => a.id === accountId);
    const label = acc ? acc.name : `Account #${accountId}`;
    if (!window.confirm(`Delete '${label}' and all of its transactions? This cannot be undone.`)) {
      return;
    }
    if (isWebViewHost()) {
      postToHost({ type: "deleteAccount", accountId });
    } else {
      state.accounts = state.accounts.filter((a) => a.id !== accountId);
      state.transactions = state.transactions.filter((t) => t.accountId !== accountId);
      if (state.selectedAccountId === accountId) {
        state.selectedAccountId = null;
        state.selectedAccount = null;
        state.activeView = "Dashboard";
      }
      recalcStandaloneBalances();
      render();
      showToast(`Deleted '${label}'.`);
    }
  }

  function openCategoryModal() {
    const overlay = document.getElementById("category-modal-overlay");
    if (!overlay) return;
    const parentSelect = document.getElementById("cat-parent");
    if (parentSelect) {
      const parentGroups = (state.categories || []).filter((c) => !c.parentId && c.categoryType === "Expense");
      parentSelect.innerHTML = `<option value="">(Top-Level Expense Group)</option>` +
        parentGroups.map((g) => `<option value="${g.id}">${escapeHtml(g.name)}</option>`).join("");
    }
    document.getElementById("category-form")?.reset();
    overlay.classList.remove("hidden");
  }

  function closeCategoryModal() {
    document.getElementById("category-modal-overlay")?.classList.add("hidden");
  }

  function submitCategoryForm(e) {
    e.preventDefault();
    const name = (document.getElementById("cat-name")?.value || "").trim();
    const parentVal = document.getElementById("cat-parent")?.value;
    const parentId = parentVal ? Number(parentVal) : null;
    const monthlyBudget = Math.max(0, Number(document.getElementById("cat-budget")?.value || 0));
    const colorHex = document.getElementById("cat-color")?.value || "#38BDF8";
    if (!name) return;

    if (isWebViewHost()) {
      postToHost({
        type: "createCategory",
        name,
        parentId,
        categoryType: "Expense",
        monthlyBudget,
        colorHex
      });
    } else {
      const parentObj = parentId ? state.categories.find((c) => c.id === parentId) : null;
      state.categories.push({
        id: Date.now(),
        name,
        parentId,
        parentName: parentObj ? parentObj.name : null,
        fullPath: parentObj ? `${parentObj.name}:${name}` : name,
        categoryType: "Expense",
        colorHex,
        monthlyBudget
      });
      render();
      showToast(`Added category '${name}'.`);
    }
    closeCategoryModal();
  }

  function openWorkspaceDataModal() {
    document.getElementById("workspace-data-modal-overlay")?.classList.remove("hidden");
  }

  function closeWorkspaceDataModal() {
    document.getElementById("workspace-data-modal-overlay")?.classList.add("hidden");
  }

  function resetToCleanSlate() {
    if (!window.confirm("Reset the entire workspace to a clean $0.00 slate? All accounts, transactions, holdings, bills, debts, and goals will be deleted.")) {
      return;
    }
    if (isWebViewHost()) {
      postToHost({ type: "resetCleanSlate" });
    } else {
      clearHostStateBeforeSync();
      render();
      showToast("Workspace reset to a clean $0.00 slate.");
    }
    closeWorkspaceDataModal();
  }

  function loadSampleDemoData() {
    if (!window.confirm("Load sample demo data? This will replace your current accounts and transactions with sample data.")) {
      return;
    }
    if (isWebViewHost()) {
      postToHost({ type: "loadDemoData" });
    } else {
      showToast("Demo data is already active in standalone preview.");
    }
    closeWorkspaceDataModal();
  }

  function openNewTransactionModal(defaultAccountId) {
    if (!state.accounts || state.accounts.length === 0) {
      showToast("Please add an account first before recording a transaction.");
      openAccountModal();
      return;
    }
    populateModalSelects(defaultAccountId);
    document.getElementById("modal-overlay").classList.remove("hidden");
  }

  function closeModal() {
    document.getElementById("modal-overlay").classList.add("hidden");
  }

  function toggleSplitEditor(enabled) {
    const container = document.getElementById("splits-container");
    const addBtn = document.getElementById("add-split-btn");
    container.classList.toggle("hidden", !enabled);
    addBtn.classList.toggle("hidden", !enabled);
    if (enabled && container.children.length === 0) {
      addSplitRow();
      addSplitRow();
    }
  }

  function addSplitRow() {
    const container = document.getElementById("splits-container");
    const catOptions = state.categories.map((c) =>
      `<option value="${c.id}">${escapeHtml(c.fullPath || c.name)}</option>`
    ).join("");

    const row = document.createElement("div");
    row.className = "split-row-editor";
    row.innerHTML = `
      <select class="split-cat">${catOptions}</select>
      <input type="text" class="split-memo" placeholder="Split line memo" />
      <input type="number" class="split-amt" step="0.01" placeholder="0.00" />
      <button type="button" class="btn btn-outline btn-sm" onclick="this.parentElement.remove()">✕</button>
    `;
    container.appendChild(row);
  }

  function submitNewTransaction(e) {
    e.preventDefault();
    const accountId = Number(document.getElementById("tx-account").value);
    const transactionDate = document.getElementById("tx-date").value;
    const flowType = document.getElementById("tx-type").value;
    const rawAmount = Math.abs(Number(document.getElementById("tx-amount").value || 0));
    const signedAmount = flowType === "payment" ? -rawAmount : rawAmount;
    const payeeName = document.getElementById("tx-payee").value.trim();
    const categoryId = Number(document.getElementById("tx-category").value);
    const referenceNumber = document.getElementById("tx-ref").value.trim();
    const status = document.getElementById("tx-status").value;
    const memo = document.getElementById("tx-memo").value.trim();
    const rawTags = document.getElementById("tx-tags")?.value.trim() || "";
    const receiptRef = document.getElementById("tx-receipt")?.value.trim() || null;
    const normalizedTags = parseTagList(rawTags).join(",");
    const isSplit = document.getElementById("tx-is-split").checked;

    let splits = [];
    if (isSplit) {
      document.querySelectorAll("#splits-container .split-row-editor").forEach((row) => {
        const sCatId = Number(row.querySelector(".split-cat").value);
        const sMemo = row.querySelector(".split-memo").value.trim();
        const sRaw = Math.abs(Number(row.querySelector(".split-amt").value || 0));
        if (sRaw > 0) {
          splits.push({
            categoryId: sCatId,
            memo: sMemo,
            amount: flowType === "payment" ? -sRaw : sRaw
          });
        }
      });
    }

    if (isWebViewHost()) {
      postToHost({
        type: "createTransaction",
        accountId,
        transactionDate,
        payeeName,
        categoryId,
        referenceNumber,
        status,
        memo,
        tags: normalizedTags,
        receiptRef,
        amount: signedAmount,
        splits
      });
    } else {
      const newId = Date.now();
      const catObj = state.categories.find((c) => c.id === categoryId);
      state.transactions.unshift({
        id: newId,
        accountId,
        accountName: (state.accounts.find((a) => a.id === accountId) || {}).name || "Primary Checking",
        transactionDate,
        referenceNumber: referenceNumber || "MAN",
        payeeName,
        categoryId,
        categoryName: isSplit ? "-- Split Transaction --" : (catObj ? catObj.fullPath : "Uncategorized"),
        memo,
        tags: normalizedTags,
        receiptRef,
        amount: signedAmount,
        runningBalance: 0,
        status,
        isSplit,
        splits: splits.map((s, idx) => ({
          id: idx + 1,
          categoryName: (state.categories.find((c) => c.id === s.categoryId) || {}).fullPath || "Split",
          memo: s.memo,
          amount: s.amount
        }))
      });
      recalcStandaloneBalances();
      render();
      showToast(`Saved transaction for ${payeeName} (${fmtCurrency(signedAmount)})`);
    }

    document.getElementById("tx-form").reset();
    closeModal();
  }

  function clearHostStateBeforeSync() {
    const todayKey = new Date().toISOString().slice(0, 7);
    state.selectedAccountId = null;
    state.selectedAccount = null;
    state.expandedSplitIds = {};
    state.budgetMonthKey = todayKey;
    state.reconcileSession = {
      accountId: 0,
      statementDate: new Date().toISOString().slice(0, 10),
      targetEndingBalance: 0,
      checkedTxIds: {}
    };
    state.reconciliationHistory = [];
    state.metrics = {
      netWorth: 0,
      totalAssets: 0,
      totalLiabilities: 0,
      liquidCash: 0,
      monthlyIncome: 0,
      monthlyExpenses: 0,
      savingsRatePercent: 0,
      categoryBreakdown: [],
      monthlyCashFlow: []
    };
    state.accounts = [];
    state.payees = [];
    state.rules = [];
    state.transactions = [];
    state.investmentHoldings = [];
    state.recurringSchedules = [];
    state.debtLiabilities = [];
    state.savingsGoals = [];
    state.budgetSummary = null;
    state.portfolioSummary = null;
    state.recurringSummary = null;
    state.reportSummary = null;
    state.debtSummary = null;
    state.savingsGoalsSummary = null;
  }

  function initBridge() {
    if (isWebViewHost()) {
      clearHostStateBeforeSync();
      window.chrome.webview.addEventListener("message", (event) => {
        const data = typeof event.data === "string" ? JSON.parse(event.data) : event.data;
        if (!data || !data.type) return;

        if (data.type === "stateSync") {
          state.activeView = data.activeView || "Dashboard";
          state.selectedAccountId = data.selectedAccountId ?? null;
          state.selectedAccount = data.selectedAccount ?? null;
          state.searchQuery = data.searchQuery ?? "";
          if (data.metrics) state.metrics = data.metrics;
          if (data.budgetSummary) {
            state.budgetSummary = data.budgetSummary;
            if (data.budgetSummary.monthKey) state.budgetMonthKey = data.budgetSummary.monthKey;
          }
          if (data.portfolioSummary) state.portfolioSummary = data.portfolioSummary;
          if (data.recurringSummary) state.recurringSummary = data.recurringSummary;
          if (data.reportSummary) state.reportSummary = data.reportSummary;
          if (data.debtSummary) {
            state.debtSummary = data.debtSummary;
            state.debtStrategyMode = data.debtSummary.activeStrategy || state.debtStrategyMode;
          }
          if (data.savingsGoalsSummary) {
            state.savingsGoalsSummary = data.savingsGoalsSummary;
          }
          if (Array.isArray(data.accounts)) state.accounts = data.accounts;
          if (Array.isArray(data.categories)) state.categories = data.categories;
          if (Array.isArray(data.payees)) state.payees = data.payees;
          if (Array.isArray(data.rules)) state.rules = data.rules;
          if (Array.isArray(data.reconciliationHistory)) state.reconciliationHistory = data.reconciliationHistory;
          if (Array.isArray(data.transactions)) state.transactions = data.transactions;
          render();
        } else if (data.type === "statementPreview" && data.preview) {
          state.statementPreview = data.preview;
          renderStatementPreviewModal();
        } else if (data.type === "openModal") {
          if (data.modal === "newTransaction") {
            openNewTransactionModal(data.selectedAccountId);
          } else if (data.modal === "reconcile") {
            openReconcileModal(data.selectedAccountId);
          } else if (data.modal === "importStatement") {
            openImportStudioModal(data.selectedAccountId);
          } else if (data.modal === "newAccount") {
            openAccountModal();
          } else if (data.modal === "workspaceData") {
            openWorkspaceDataModal();
          }
        } else if (data.type === "toast" && data.message) {
          showToast(data.message);
        }
      });

      postToHost({ type: "ready" });
    }

    render();
  }

  function switchView(viewKey) {
    state.activeView = viewKey;
    state.editingTransactionId = null;
    if (viewKey !== "Register") {
      state.selectedAccountId = null;
      state.selectedAccount = null;
    }
    postToHost({ type: "navigate", view: viewKey });
    render();
  }

  function selectAccount(accountId) {
    const acc = state.accounts.find((a) => a.id === accountId);
    state.selectedAccountId = accountId;
    state.selectedAccount = acc || null;
    state.activeView = "Register";
    state.editingTransactionId = null;
    postToHost({ type: "selectAccount", accountId });
    render();
  }

  function cycleStatus(transactionId) {
    if (isWebViewHost()) {
      postToHost({ type: "cycleStatus", transactionId });
    } else {
      const tx = state.transactions.find((t) => t.id === transactionId);
      if (tx) {
        tx.status = tx.status === "Uncleared" ? "Cleared" : tx.status === "Cleared" ? "Reconciled" : "Uncleared";
        render();
        showToast(`Transaction #${transactionId} marked as ${tx.status}`);
      }
    }
  }

  function openExternalPortal(url) {
    postToHost({ type: "openExternalPortal", url });
    showToast(`Opening banking portal: ${url}`);
  }

  document.addEventListener("DOMContentLoaded", initBridge);

  return {
    switchView,
    selectAccount,
    cycleStatus,
    openAccountModal,
    closeAccountModal,
    onAccountTypeChanged,
    submitAccountForm,
    deleteAccount,
    openCategoryModal,
    closeCategoryModal,
    submitCategoryForm,
    openWorkspaceDataModal,
    closeWorkspaceDataModal,
    resetToCleanSlate,
    loadSampleDemoData,
    setStatusFilter: (f) => { state.statusFilter = f; render(); },
    setTagFilter,
    setCategoryFilter,
    setAmountRangeFilter,
    toggleReceiptOnlyFilter,
    resetRegisterFilters,
    toggleExpandSplit: (id) => { state.expandedSplitIds[id] = !state.expandedSplitIds[id]; render(); },
    startInlineEdit,
    cancelInlineEdit,
    saveInlineEdit,
    handleInlineEditKeyDown,
    handleQuickEntryKeyDown,
    submitQuickInlineEntry,
    deleteTransaction,
    openReconcileModal,
    closeReconcileModal,
    onReconcileAccountChanged,
    recalcReconciliationMath,
    toggleReconcileRow,
    reconcileCheckAll,
    snapStatementTargetToCleared,
    finishReconciliation,
    updateEnvelopeAssigned,
    updateEnvelopeTarget,
    quickFundEnvelope,
    autoFundAllEnvelopes,
    submitMoveEnvelopeFunds,
    setInvestmentAccountFilter,
    updateHoldingInline,
    submitNewHolding,
    deleteHolding,
    simulateMarketPulse,
    postRecurringToRegister,
    skipRecurringCycle,
    submitNewRecurringSchedule,
    deleteRecurringSchedule,
    exportLedgerCsv,
    exportStewardshipTaxCsv,
    exportWorkspaceJsonBackup,
    setDebtStrategy,
    updateDebtLiabilityInline,
    submitNewDebtLiability,
    deleteDebtLiability,
    updateSavingsGoalInline,
    contributeSavingsGoal,
    submitNewSavingsGoal,
    deleteSavingsGoal,
    openNewTransactionModal,
    closeModal,
    toggleSplitEditor,
    addSplitRow,
    recalcSplitRemainder: () => {},
    submitNewTransaction,
    openImportStudioModal,
    closeImportStudioModal,
    loadSampleStatement,
    reparseCurrentSample: () => loadSampleStatement(state.activeSampleFormat || "OFX"),
    handleLocalFileUpload,
    browseNativeStatementFile,
    toggleImportRow,
    commitSelectedImports,
    submitNewRule,
    toggleRule,
    deleteRule,
    applyRulesToRegister,
    triggerReconcile: () => openReconcileModal(state.selectedAccountId),
    openExternalPortal
  };
})();
