using System.Globalization;
using System.Text;
using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Controllers;

public record Summary(decimal Income, decimal Expenses, decimal Balance);

public class ReportController
{
    private static int Uid => AuthController.CurrentUser!.Id;

    private List<Transaction> Load(DateTime from, DateTime to)
    {
        using var db = new AppDbContext();
        return db.Transactions.Include(t => t.Category)
            .Where(t => t.UserId == Uid && !t.IsDeleted && t.Date >= from.Date && t.Date <= to.Date)
            .ToList().OrderBy(t => t.Date).ToList();
    }

    /// <summary>FR-15/16: totals for any period (daily, weekly, monthly, custom).</summary>
    public Summary GetSummary(DateTime from, DateTime to)
    {
        var l = Load(from, to);
        var inc = l.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var exp = l.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        return new Summary(inc, exp, inc - exp);
    }

    public List<(string Category, decimal Total)> SpendingByCategory(DateTime from, DateTime to) =>
        Load(from, to).Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category?.Name ?? "Uncategorized")
            .Select(g => (g.Key, g.Sum(t => t.Amount))).OrderByDescending(x => x.Item2).ToList();

    public List<(string Month, decimal Income, decimal Expenses)> ByMonth(DateTime from, DateTime to) =>
        Load(from, to).GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => ($"{g.Key.Year}-{g.Key.Month:00}",
                g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount))).ToList();

    /// <summary>FR-17: export the period's report as CSV (opens in Excel).</summary>
    public void ExportCsv(string path, DateTime from, DateTime to)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder("Date,Type,Category,Amount,Source/Payment,Description\n");
        foreach (var t in Load(from, to))
            sb.AppendLine(string.Join(",", t.Date.ToString("dd/MM/yyyy"), t.Type, Csv(t.Category?.Name ?? ""),
                t.Amount.ToString("0.00", inv),
                Csv(t.Type == TransactionType.Income ? t.Source : t.PaymentMethod), Csv(t.Description)));
        var s = GetSummary(from, to);
        sb.AppendLine()
          .AppendLine($"Total income,,,{s.Income.ToString("0.00", inv)}")
          .AppendLine($"Total expenses,,,{s.Expenses.ToString("0.00", inv)}")
          .AppendLine($"Balance,,,{s.Balance.ToString("0.00", inv)}");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    private static string Csv(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";
}
