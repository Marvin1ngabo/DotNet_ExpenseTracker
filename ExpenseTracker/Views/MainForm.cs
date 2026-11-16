using System.Windows.Forms.DataVisualization.Charting;
using ExpenseTracker.Controllers;
using ExpenseTracker.Models;

namespace ExpenseTracker.Views;

/// <summary>Main window: Dashboard, Transactions, Categories, Budgets, Reports, Account, Admin.</summary>
public class MainForm : Form
{
    public bool LoggedOut { get; private set; }

    private readonly TransactionController _tx = new();
    private readonly CategoryController _cat = new();
    private readonly BudgetController _bud = new();
    private readonly ReportController _rep = new();
    private readonly AdminController _admin = new();
    private readonly AuthController _auth = new();
    private readonly User _user = AuthController.CurrentUser!;

    // dashboard
    private readonly Label _dIncome = Big(), _dExpense = Big(), _dBalance = Big();
    private readonly DataGridView _dCats = Grid(), _dBudgets = Grid();
    // transactions
    private readonly DataGridView _txGrid = Grid();
    private readonly TextBox _fSearch = new() { Width = 120, PlaceholderText = "search text" };
    private readonly DateTimePicker _fFrom = Dt(), _fTo = Dt();
    private readonly CheckBox _fUseDates = new() { Text = "Dates", AutoSize = true };
    private readonly ComboBox _fCat = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly TextBox _fMin = new() { Width = 60, PlaceholderText = "min" };
    private readonly TextBox _fMax = new() { Width = 60, PlaceholderText = "max" };
    private readonly CheckBox _fDeleted = new() { Text = "Show deleted", AutoSize = true };
    // categories
    private readonly DataGridView _catGrid = Grid();
    private readonly TextBox _catName = new() { Width = 160 };
    // budgets
    private readonly DataGridView _budGrid = Grid();
    private readonly ComboBox _bCat = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly NumericUpDown _bAmount = new() { DecimalPlaces = 2, Maximum = 1_000_000_000, Width = 110, ThousandsSeparator = true };
    private readonly NumericUpDown _bThreshold = new() { Minimum = 1, Maximum = 100, Value = 80, Width = 55 };
    private readonly DateTimePicker _bMonth = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "MMMM yyyy", Width = 130 };
    // reports
    private readonly ComboBox _rPeriod = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
    private readonly DateTimePicker _rFrom = Dt(), _rTo = Dt();
    private readonly Label _rSummary = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly DataGridView _rGrid = Grid();
    // account
    private readonly TextBox _aName = new() { Width = 220 };
    private readonly TextBox _aPass = new() { Width = 220, UseSystemPasswordChar = true };
    // admin
    private readonly DataGridView _uGrid = Grid();
    private readonly DataGridView _dcGrid = Grid();
    private readonly TextBox _dcName = new() { Width = 160 };

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    public MainForm()
    {
        Text = $"ETMS - Expense Tracker  |  {_user.FullName} ({_user.Role})";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1000, 640);
        MinimumSize = new Size(900, 560);

        var top = new Panel { Dock = DockStyle.Top, Height = 34 };
        var logout = new Button { Text = "Log out", Dock = DockStyle.Right, Width = 90 };
        logout.Click += (_, _) => { LoggedOut = true; Close(); };
        top.Controls.Add(logout);
        top.Controls.Add(new Label { Text = $"Welcome, {_user.FullName}", Dock = DockStyle.Left, AutoSize = false, Width = 400, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) });

        if (_user.Role == Role.Admin)
        {
            _tabs.TabPages.Add(BuildUsersTab());
            _tabs.TabPages.Add(BuildDefaultCategoriesTab());
        }
        else
        {
            _tabs.TabPages.Add(BuildDashboardTab());
            _tabs.TabPages.Add(BuildTransactionsTab());
            _tabs.TabPages.Add(BuildCategoriesTab());
            _tabs.TabPages.Add(BuildBudgetsTab());
            _tabs.TabPages.Add(BuildReportsTab());
        }
        _tabs.TabPages.Add(BuildAccountTab());
        _tabs.SelectedIndexChanged += (_, _) => RefreshAll();
        Controls.Add(_tabs);
        Controls.Add(top);
        Shown += (_, _) => RefreshAll();
    }

    // ---------- helpers ----------
    private static Label Big() => new() { AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), Margin = new Padding(0, 0, 40, 0) };
    private static DateTimePicker Dt() => new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 100 };
    private static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = SystemColors.Window
    };
    private static string Money(decimal d) => d.ToString("N2") + " RWF";
    private static FlowLayoutPanel Bar(params Control[] c)
    {
        var p = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(4), WrapContents = false };
        p.Controls.AddRange(c);
        return p;
    }
    private static Button Btn(string text, EventHandler onClick, int w = 90)
    {
        var b = new Button { Text = text, Width = w, Height = 26 };
        b.Click += onClick;
        return b;
    }
    private static Label Lbl(string t) => new() { Text = t, AutoSize = true, Margin = new Padding(6, 6, 2, 0) };
    private static void Info(string m) => MessageBox.Show(m, "ETMS", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private static void Warn(string m) => MessageBox.Show(m, "ETMS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    private static int? SelectedId(DataGridView g) => g.CurrentRow?.Tag as int?;

    // ---------- Dashboard (FR-15) ----------
    private readonly Chart _chartCat = new() { Dock = DockStyle.Fill };
    private readonly Chart _chartMonth = new() { Dock = DockStyle.Fill };

    private TabPage BuildDashboardTab()
    {
        var page = new TabPage("Dashboard");
        var totals = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };
        totals.Controls.AddRange(new Control[] { _dIncome, _dExpense, _dBalance });
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        _chartCat.ChartAreas.Add(new ChartArea("a"));
        _chartCat.Legends.Add(new Legend("l"));
        _chartCat.Titles.Add("Spending by category (this month)");
        _chartMonth.ChartAreas.Add(new ChartArea("a"));
        _chartMonth.Legends.Add(new Legend("l"));
        _chartMonth.Titles.Add("Income vs expenses by month (last 6 months)");
        split.Controls.Add(_chartCat, 0, 0);
        split.Controls.Add(_chartMonth, 1, 0);
        var gb = new GroupBox { Text = "Budget status (this month)", Dock = DockStyle.Fill };
        gb.Controls.Add(_dBudgets);
        split.Controls.Add(gb, 0, 1);
        split.SetColumnSpan(gb, 2);
        page.Controls.Add(split);
        page.Controls.Add(totals);
        return page;
    }

    private void RefreshDashboard()
    {
        var now = DateTime.Today;
        var from = new DateTime(now.Year, now.Month, 1);
        var s = _rep.GetSummary(from, from.AddMonths(1).AddDays(-1));
        _dIncome.Text = "Income: " + Money(s.Income); _dIncome.ForeColor = Color.SeaGreen;
        _dExpense.Text = "Expenses: " + Money(s.Expenses); _dExpense.ForeColor = Color.Firebrick;
        _dBalance.Text = "Balance: " + Money(s.Balance); _dBalance.ForeColor = s.Balance >= 0 ? Color.SteelBlue : Color.Firebrick;

        _chartCat.Series.Clear();
        var pie = new Series("Spending") { ChartType = SeriesChartType.Pie };
        foreach (var (c, t) in _rep.SpendingByCategory(from, from.AddMonths(1).AddDays(-1))) pie.Points.AddXY(c, (double)t);
        _chartCat.Series.Add(pie);

        _chartMonth.Series.Clear();
        var inc = new Series("Income") { ChartType = SeriesChartType.Column, Color = Color.SeaGreen };
        var exp = new Series("Expenses") { ChartType = SeriesChartType.Column, Color = Color.Firebrick };
        foreach (var (m, i, e) in _rep.ByMonth(from.AddMonths(-5), from.AddMonths(1).AddDays(-1)))
        { inc.Points.AddXY(m, (double)i); exp.Points.AddXY(m, (double)e); }
        _chartMonth.Series.Add(inc); _chartMonth.Series.Add(exp);

        FillBudgetGrid(_dBudgets, now.Year, now.Month, true);
    }

    // ---------- Transactions (FR-05..FR-09) ----------
    private TabPage BuildTransactionsTab()
    {
        var page = new TabPage("Transactions");
        var actions = Bar(
            Btn("Add", (_, _) => EditTx(null)), Btn("Edit", (_, _) => EditTx(SelectedTx())),
            Btn("Delete", (_, _) => DeleteTx(true)), Btn("Restore", (_, _) => DeleteTx(false)));
        var filters = Bar(Lbl("Search"), _fSearch, _fUseDates, _fFrom, _fTo, Lbl("Category"), _fCat,
            _fMin, _fMax, _fDeleted, Btn("Filter", (_, _) => RefreshTx(), 70));
        _fFrom.Value = DateTime.Today.AddMonths(-1);
        _txGrid.DoubleClick += (_, _) => EditTx(SelectedTx());
        _txGrid.Sorted += (_, _) => { };
        page.Controls.Add(_txGrid);
        page.Controls.Add(filters);
        page.Controls.Add(actions);
        return page;
    }

    private Transaction? SelectedTx() => _txGrid.CurrentRow?.Tag as Transaction;

    private void RefreshTx()
    {
        var cats = _cat.GetForUser();
        var sel = _fCat.SelectedValue as int?;
        var items = new List<Category> { new() { Id = 0, Name = "(all)" } };
        items.AddRange(cats);
        _fCat.DataSource = items; _fCat.DisplayMember = "Name"; _fCat.ValueMember = "Id";
        if (sel.HasValue) _fCat.SelectedValue = sel.Value;

        decimal.TryParse(_fMin.Text, out var min); decimal.TryParse(_fMax.Text, out var max);
        var catId = _fCat.SelectedValue as int?;
        var list = _tx.Query(_fSearch.Text, _fUseDates.Checked ? _fFrom.Value : null, _fUseDates.Checked ? _fTo.Value : null,
            catId > 0 ? catId : null, string.IsNullOrEmpty(_fMin.Text) ? null : min, string.IsNullOrEmpty(_fMax.Text) ? null : max, _fDeleted.Checked);

        _txGrid.Columns.Clear(); _txGrid.Rows.Clear();
        foreach (var h in new[] { "Date", "Type", "Category", "Amount", "Source / Payment", "Description" }) _txGrid.Columns.Add(h, h);
        _txGrid.Columns[3].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        foreach (var t in list)
        {
            var i = _txGrid.Rows.Add(t.Date.ToString("dd/MM/yyyy"), t.Type, t.Category?.Name ?? "", t.Amount.ToString("N2"),
                t.Type == TransactionType.Income ? t.Source : t.PaymentMethod, t.Description);
            _txGrid.Rows[i].Tag = t;
            _txGrid.Rows[i].DefaultCellStyle.ForeColor = t.Type == TransactionType.Income ? Color.SeaGreen : Color.Black;
        }
    }

    private void EditTx(Transaction? t)
    {
        using var f = new TransactionForm(t);
        if (f.ShowDialog(this) == DialogResult.OK) { RefreshTx(); CheckBudgetAlerts(); }
    }

    private void DeleteTx(bool delete)
    {
        var t = SelectedTx();
        if (t == null) { Warn("Select a transaction first."); return; }
        if (delete && MessageBox.Show("Delete this transaction? (it can be restored later)", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        _tx.SetDeleted(t.Id, delete);
        RefreshTx();
    }

    /// <summary>FR-14: notify when threshold reached / budget exceeded.</summary>
    private void CheckBudgetAlerts()
    {
        var now = DateTime.Today;
        var bad = _bud.GetStatuses(now.Year, now.Month).Where(s => s.State != "OK").ToList();
        if (bad.Count == 0) return;
        var text = string.Join("\n", bad.Select(s => $"{s.Name}: {s.State} - spent {Money(s.Spent)} of {Money(s.Budget)} ({s.Percent}%)"));
        MessageBox.Show(text, "Budget alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // ---------- Categories (FR-12) ----------
    private TabPage BuildCategoriesTab()
    {
        var page = new TabPage("Categories");
        var bar = Bar(Lbl("Name"), _catName, Btn("Add", (_, _) => CatResult(_cat.Add(_catName.Text))),
            Btn("Rename", (_, _) => { if (SelectedId(_catGrid) is int id) CatResult(_cat.Rename(id, _catName.Text)); }),
            Btn("Delete", (_, _) => { if (SelectedId(_catGrid) is int id) CatResult(_cat.Delete(id)); }));
        _catGrid.SelectionChanged += (_, _) => { if (_catGrid.CurrentRow != null) _catName.Text = _catGrid.CurrentRow.Cells[0].Value?.ToString(); };
        page.Controls.Add(_catGrid);
        page.Controls.Add(bar);
        return page;
    }

    private void CatResult((bool ok, string message) r)
    {
        if (!r.ok) Warn(r.message);
        else _catName.Clear();
        RefreshCategories();
    }

    private void RefreshCategories() => FillCats(_catGrid, _cat.GetForUser());

    private static void FillCats(DataGridView g, List<Category> cats)
    {
        g.Columns.Clear(); g.Rows.Clear();
        g.Columns.Add("Name", "Name"); g.Columns.Add("Kind", "Kind");
        foreach (var c in cats)
        {
            var i = g.Rows.Add(c.Name, c.UserId == null ? "Default" : "Personal");
            g.Rows[i].Tag = c.Id;
        }
    }

    // ---------- Budgets (FR-13, FR-14) ----------
    private TabPage BuildBudgetsTab()
    {
        var page = new TabPage("Budgets");
        var bar = Bar(Lbl("Month"), _bMonth, Lbl("Category"), _bCat, Lbl("Amount"), _bAmount, Lbl("Alert %"), _bThreshold,
            Btn("Save", (_, _) => SaveBudget()), Btn("Delete", (_, _) =>
            { if (SelectedId(_budGrid) is int id) { _bud.Delete(id); RefreshBudgets(); } }));
        _bMonth.ValueChanged += (_, _) => RefreshBudgets();
        page.Controls.Add(_budGrid);
        page.Controls.Add(bar);
        return page;
    }

    private void SaveBudget()
    {
        var catId = _bCat.SelectedValue as int?;
        var (ok, msg) = _bud.Set(catId > 0 ? catId : null, _bMonth.Value.Year, _bMonth.Value.Month, _bAmount.Value, (int)_bThreshold.Value);
        if (!ok) Warn(msg);
        RefreshBudgets();
    }

    private void RefreshBudgets()
    {
        var items = new List<Category> { new() { Id = 0, Name = "Overall" } };
        items.AddRange(_cat.GetForUser());
        _bCat.DataSource = items; _bCat.DisplayMember = "Name"; _bCat.ValueMember = "Id";
        FillBudgetGrid(_budGrid, _bMonth.Value.Year, _bMonth.Value.Month, false);
    }

    private void FillBudgetGrid(DataGridView g, int y, int m, bool compact)
    {
        g.Columns.Clear(); g.Rows.Clear();
        foreach (var h in new[] { "Budget for", "Budget", "Spent", "Remaining", "Used %", "Status" }) g.Columns.Add(h, h);
        foreach (var s in _bud.GetStatuses(y, m))
        {
            var i = g.Rows.Add(s.Name, s.Budget.ToString("N2"), s.Spent.ToString("N2"), s.Remaining.ToString("N2"), s.Percent + "%", s.State);
            g.Rows[i].Tag = s.Id;
            g.Rows[i].DefaultCellStyle.BackColor = s.State == "EXCEEDED" ? Color.MistyRose : s.State == "ALERT" ? Color.LemonChiffon : Color.Honeydew;
        }
    }

    // ---------- Reports (FR-16, FR-17) ----------
    private TabPage BuildReportsTab()
    {
        var page = new TabPage("Reports");
        _rPeriod.Items.AddRange(new object[] { "Daily", "Weekly", "Monthly", "Custom" });
        _rPeriod.SelectedIndex = 2;
        _rPeriod.SelectedIndexChanged += (_, _) => RefreshReport();
        _rFrom.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var bar = Bar(Lbl("Period"), _rPeriod, Lbl("From"), _rFrom, Lbl("To"), _rTo,
            Btn("Run", (_, _) => RefreshReport(), 70), Btn("Export CSV", (_, _) => ExportReport(), 100));
        var sum = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(8, 6, 0, 0) };
        sum.Controls.Add(_rSummary);
        page.Controls.Add(_rGrid);
        page.Controls.Add(sum);
        page.Controls.Add(bar);
        return page;
    }

    private (DateTime from, DateTime to) ReportRange()
    {
        var today = DateTime.Today;
        switch (_rPeriod.SelectedItem?.ToString())
        {
            case "Daily": return (today, today);
            case "Weekly":
                var start = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                return (start, start.AddDays(6));
            case "Monthly":
                var f = new DateTime(today.Year, today.Month, 1);
                return (f, f.AddMonths(1).AddDays(-1));
            default: return (_rFrom.Value.Date, _rTo.Value.Date);
        }
    }

    private void RefreshReport()
    {
        var custom = _rPeriod.SelectedItem?.ToString() == "Custom";
        _rFrom.Enabled = _rTo.Enabled = custom;
        var (from, to) = ReportRange();
        var s = _rep.GetSummary(from, to);
        _rSummary.Text = $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}   Income {Money(s.Income)}   Expenses {Money(s.Expenses)}   Balance {Money(s.Balance)}";
        _rGrid.Columns.Clear(); _rGrid.Rows.Clear();
        _rGrid.Columns.Add("c", "Category"); _rGrid.Columns.Add("t", "Total spent");
        foreach (var (c, t) in _rep.SpendingByCategory(from, to)) _rGrid.Rows.Add(c, t.ToString("N2"));
    }

    private void ExportReport()
    {
        var (from, to) = ReportRange();
        using var dlg = new SaveFileDialog { Filter = "CSV (Excel)|*.csv", FileName = $"report_{from:yyyyMMdd}_{to:yyyyMMdd}.csv" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        _rep.ExportCsv(dlg.FileName, from, to);
        Info("Report exported.");
    }

    // ---------- Account (FR-03) ----------
    private TabPage BuildAccountTab()
    {
        var page = new TabPage("Account");
        _aName.Text = _user.FullName;
        var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20) };
        p.Controls.AddRange(new Control[] {
            new Label { Text = "E-mail: " + _user.Email, AutoSize = true },
            Lbl("Full name"), _aName, Lbl("New password (leave blank to keep)"), _aPass,
            Btn("Update profile", (_, _) =>
            {
                var (ok, msg) = _auth.UpdateProfile(_aName.Text, _aPass.Text);
                if (ok) _aPass.Clear();
                Info(msg);
            }, 120) });
        page.Controls.Add(p);
        return page;
    }

    // ---------- Admin (FR-18..FR-20) ----------
    private TabPage BuildUsersTab()
    {
        var page = new TabPage("Users & Backup");
        var bar = Bar(
            Btn("Activate", (_, _) => UserAct(id => _admin.SetActive(id, true))),
            Btn("Deactivate", (_, _) => UserAct(id => _admin.SetActive(id, false)), 100),
            Btn("Delete user", (_, _) =>
            {
                if (MessageBox.Show("Permanently delete this user and all their data?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    UserAct(id => _admin.DeleteUser(id));
            }, 100),
            Btn("Backup DB", (_, _) =>
            {
                using var d = new SaveFileDialog { Filter = "Backup (JSON)|*.json", FileName = $"etms_backup_{DateTime.Now:yyyyMMdd_HHmm}.json" };
                if (d.ShowDialog() == DialogResult.OK) { _admin.Backup(d.FileName); Info("Backup saved."); }
            }, 100),
            Btn("Restore DB", (_, _) =>
            {
                using var d = new OpenFileDialog { Filter = "Backup (JSON)|*.json" };
                if (d.ShowDialog() != DialogResult.OK) return;
                if (MessageBox.Show("Replace ALL current data with this backup?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                _admin.Restore(d.FileName); RefreshUsers(); Info("Database restored.");
            }, 100));
        page.Controls.Add(_uGrid);
        page.Controls.Add(bar);
        return page;
    }

    private void UserAct(Func<int, (bool ok, string message)> act)
    {
        if (SelectedId(_uGrid) is not int id) { Warn("Select a user first."); return; }
        var (ok, msg) = act(id);
        if (!ok) Warn(msg);
        RefreshUsers();
    }

    private void RefreshUsers()
    {
        _uGrid.Columns.Clear(); _uGrid.Rows.Clear();
        foreach (var h in new[] { "Name", "E-mail", "Role", "Active" }) _uGrid.Columns.Add(h, h);
        foreach (var u in _admin.GetUsers())
        {
            var i = _uGrid.Rows.Add(u.FullName, u.Email, u.Role, u.IsActive ? "Yes" : "No");
            _uGrid.Rows[i].Tag = u.Id;
        }
    }

    private TabPage BuildDefaultCategoriesTab()
    {
        var page = new TabPage("Default categories");
        var bar = Bar(Lbl("Name"), _dcName,
            Btn("Add", (_, _) => DcAct(() => _cat.Add(_dcName.Text, true))),
            Btn("Rename", (_, _) => { if (SelectedId(_dcGrid) is int id) DcAct(() => _cat.Rename(id, _dcName.Text, true)); }),
            Btn("Delete", (_, _) => { if (SelectedId(_dcGrid) is int id) DcAct(() => _cat.Delete(id, true)); }));
        _dcGrid.SelectionChanged += (_, _) => { if (_dcGrid.CurrentRow != null) _dcName.Text = _dcGrid.CurrentRow.Cells[0].Value?.ToString(); };
        page.Controls.Add(_dcGrid);
        page.Controls.Add(bar);
        return page;
    }

    private void DcAct(Func<(bool ok, string message)> act)
    {
        var (ok, msg) = act();
        if (!ok) Warn(msg); else _dcName.Clear();
        FillCats(_dcGrid, _cat.GetDefaults());
    }

    // ---------- refresh dispatch ----------
    private void RefreshAll()
    {
        switch (_tabs.SelectedTab?.Text)
        {
            case "Dashboard": RefreshDashboard(); break;
            case "Transactions": RefreshTx(); break;
            case "Categories": RefreshCategories(); break;
            case "Budgets": RefreshBudgets(); break;
            case "Reports": RefreshReport(); break;
            case "Users & Backup": RefreshUsers(); break;
            case "Default categories": FillCats(_dcGrid, _cat.GetDefaults()); break;
        }
    }
}
