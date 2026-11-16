using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Controllers;

public class TransactionController
{
    private static int Uid => AuthController.CurrentUser!.Id;

    /// <summary>FR-05/06/07: create or update with validation.</summary>
    public (bool ok, string message) Save(Transaction t)
    {
        if (t.Amount <= 0) return (false, "Amount must be greater than 0.");
        if (t.Date == default) return (false, "Date is required.");
        if (t.Type == TransactionType.Expense && t.CategoryId == null) return (false, "Select a category.");
        t.Amount = Math.Round(t.Amount, 2);
        using var db = new AppDbContext();
        if (t.Id == 0)
        {
            db.Transactions.Add(new Transaction
            {
                UserId = Uid, Type = t.Type, Amount = t.Amount, Date = t.Date.Date, CategoryId = t.CategoryId,
                Source = t.Source, PaymentMethod = t.PaymentMethod, Description = t.Description
            });
        }
        else
        {
            var e = db.Transactions.FirstOrDefault(x => x.Id == t.Id && x.UserId == Uid);
            if (e == null) return (false, "Transaction not found.");
            e.Type = t.Type; e.Amount = t.Amount; e.Date = t.Date.Date; e.CategoryId = t.CategoryId;
            e.Source = t.Source; e.PaymentMethod = t.PaymentMethod; e.Description = t.Description;
        }
        db.SaveChanges();
        return (true, "Saved.");
    }

    /// <summary>FR-08: search / filter / sort own transactions.</summary>
    public List<Transaction> Query(string? search, DateTime? from, DateTime? to, int? categoryId,
        decimal? min, decimal? max, bool showDeleted = false)
    {
        using var db = new AppDbContext();
        var q = db.Transactions.Include(t => t.Category)
            .Where(t => t.UserId == Uid && t.IsDeleted == showDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(t => (t.Description != null && t.Description.Contains(search)) || (t.Source != null && t.Source.Contains(search)));
        if (from.HasValue) q = q.Where(t => t.Date >= from.Value.Date);
        if (to.HasValue) q = q.Where(t => t.Date <= to.Value.Date);
        if (categoryId.HasValue) q = q.Where(t => t.CategoryId == categoryId);
        IEnumerable<Transaction> list = q.ToList();
        if (min.HasValue) list = list.Where(t => t.Amount >= min.Value);
        if (max.HasValue) list = list.Where(t => t.Amount <= max.Value);
        return list.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToList();
    }

    /// <summary>FR-09: soft delete and restore.</summary>
    public void SetDeleted(int id, bool deleted)
    {
        using var db = new AppDbContext();
        var t = db.Transactions.FirstOrDefault(x => x.Id == id && x.UserId == Uid);
        if (t == null) return;
        t.IsDeleted = deleted;
        db.SaveChanges();
    }
}
