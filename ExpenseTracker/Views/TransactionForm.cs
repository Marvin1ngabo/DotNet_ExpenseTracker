using ExpenseTracker.Controllers;
using ExpenseTracker.Models;

namespace ExpenseTracker.Views;

/// <summary>Add / edit transaction dialog (FR-05, FR-06, FR-07).</summary>
public class TransactionForm : Form
{
    private readonly TransactionController _controller = new();
    private readonly Transaction _t;
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly NumericUpDown _amount = new() { DecimalPlaces = 2, Maximum = 1_000_000_000, Width = 220, ThousandsSeparator = true };
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 220 };
    private readonly ComboBox _category = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly Label _extraLabel = new() { AutoSize = true };
    private readonly TextBox _extra = new() { Width = 220 };
    private readonly TextBox _desc = new() { Width = 220, Height = 60, Multiline = true };

    public TransactionForm(Transaction? existing = null)
    {
        _t = existing ?? new Transaction();
        Text = existing == null ? "Add transaction" : "Edit transaction";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ClientSize = new Size(300, 420);

        _type.Items.AddRange(new object[] { "Expense", "Income" });
        _category.DisplayMember = "Name";
        _category.ValueMember = "Id";
        _category.DataSource = new CategoryController().GetForUser();
        _category.SelectedIndex = -1;
        _type.SelectedIndexChanged += (_, _) => UpdateLabels();

        var ok = new Button { Text = "Save", Width = 100 };
        ok.Click += (_, _) => Save();
        var cancel = new Button { Text = "Cancel", Width = 100, DialogResult = DialogResult.Cancel };

        var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20, 10, 0, 0) };
        p.Controls.AddRange(new Control[] {
            L("Type"), _type, L("Amount"), _amount, L("Date"), _date, L("Category"), _category,
            _extraLabel, _extra, L("Description"), _desc, ok, cancel });
        Controls.Add(p);
        CancelButton = cancel;

        _type.SelectedIndex = (int)_t.Type;
        if (existing != null)
        {
            _amount.Value = existing.Amount;
            _date.Value = existing.Date;
            if (existing.CategoryId != null) _category.SelectedValue = existing.CategoryId;
            _extra.Text = existing.Type == TransactionType.Income ? existing.Source : existing.PaymentMethod;
            _desc.Text = existing.Description;
        }
        UpdateLabels();
    }

    private static Label L(string s) => new() { Text = s, AutoSize = true };

    private void UpdateLabels()
    {
        _extraLabel.Text = _type.SelectedIndex == 1 ? "Source" : "Payment method";
        _category.Enabled = _type.SelectedIndex == 0;
        if (!_category.Enabled) _category.SelectedIndex = -1;
    }

    private void Save()
    {
        var isIncome = _type.SelectedIndex == 1;
        _t.Type = isIncome ? TransactionType.Income : TransactionType.Expense;
        _t.Amount = _amount.Value;
        _t.Date = _date.Value.Date;
        _t.CategoryId = isIncome ? null : (_category.SelectedItem as Category)?.Id;
        _t.Source = isIncome ? _extra.Text.Trim() : "";
        _t.PaymentMethod = isIncome ? "" : _extra.Text.Trim();
        _t.Description = _desc.Text.Trim();
        var (ok, message) = _controller.Save(_t);
        if (!ok) { MessageBox.Show(message, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        DialogResult = DialogResult.OK;
        Close();
    }
}
