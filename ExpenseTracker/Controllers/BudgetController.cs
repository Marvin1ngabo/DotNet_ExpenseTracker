using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Controllers;

public record BudgetStatus(int Id, string Name, decimal Budget, decimal Spent, decimal Remaining, int Percent, string State);

public class BudgetController
{
    private static int Uid => AuthController.CurrentUser!.Id;

    /// <summary>FR-13: set a monthly budget (overall when categoryId is null); updates if one exists.</summary>
    public (bool ok, string message) Set(int? categoryId, int year, int month, decimal amount, int threshold)
    {
        if (amount <= 0) return (false, "Budget must be greater than 0.");
        if (threshold < 1 || threshold > 100) return (false, "Threshold must be between 1 and 100.");
        using var db = new AppDbContext();
        var b = db.Budgets.FirstOrDefault(x => x.UserId == Uid && x.CategoryId == categoryId && x.Year == year && x.Month == month);
        if (b == null)
            db.Budgets.Add(new Budget { UserId = Uid, CategoryId = categoryId, Year = year, Month = month, Amount = Math.Round(amount, 2), AlertThresholdPercent = threshold });
        else { b.Amount = Math.Round(amount, 2); b.AlertThresholdPercent = threshold; }
        db.SaveChanges();
        return (true, "Budget saved.");
    }

    public void Delete(int id)
    {
        using var db = new AppDbContext();
        var b = db.Budgets.FirstOrDefault(x => x.Id == id && x.UserId == Uid);
        if (b == null) return;
        db.Budgets.Remove(b);
        db.SaveChanges();
    }

    /// <summary>FR-14: remaining budget and alert state (OK / ALERT / EXCEEDED).</summary>
    public List<BudgetStatus> GetStatuses(int year, int month)
    {
        using var db = new AppDbContext();
        var budgets = db.Budgets.Include(b => b.Category).Where(b => b.UserId == Uid && b.Year == year && b.Month == month).ToList();
        var expenses = db.Transactions.Where(t => t.UserId == Uid && !t.IsDeleted && t.Type == TransactionType.Expense
                        && t.Date.Year == year && t.Date.Month == month).ToList();
        var result = new List<BudgetStatus>();
        foreach (var b in budgets)
        {
            var spent = expenses.Where(e => b.CategoryId == null || e.CategoryId == b.CategoryId).Sum(e => e.Amount);
            var pct = (int)Math.Round(spent / b.Amount * 100, MidpointRounding.AwayFromZero);
            var state = spent > b.Amount ? "EXCEEDED" : pct >= b.AlertThresholdPercent ? "ALERT" : "OK";
            result.Add(new BudgetStatus(b.Id, b.Category?.Name ?? "Overall", b.Amount, spent, b.Amount - spent, pct, state));
        }
        return result;
    }
}
