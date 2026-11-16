using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers;

public class CategoryController
{
    private static int Uid => AuthController.CurrentUser!.Id;

    /// <summary>Default categories plus the current user's own.</summary>
    public List<Category> GetForUser()
    {
        using var db = new AppDbContext();
        return db.Categories.Where(c => c.UserId == null || c.UserId == Uid).OrderBy(c => c.Name).ToList();
    }

    public List<Category> GetDefaults()
    {
        using var db = new AppDbContext();
        return db.Categories.Where(c => c.UserId == null).OrderBy(c => c.Name).ToList();
    }

    /// <summary>FR-12 (user) / FR-19 (admin, asDefault = true).</summary>
    public (bool ok, string message) Add(string name, bool asDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name)) return (false, "Name is required.");
        using var db = new AppDbContext();
        name = name.Trim();
        int? owner = asDefault ? null : Uid;
        if (db.Categories.Any(c => c.Name == name && (c.UserId == null || c.UserId == owner)))
            return (false, "Category already exists.");
        db.Categories.Add(new Category { Name = name, UserId = owner });
        db.SaveChanges();
        return (true, "Category added.");
    }

    public (bool ok, string message) Rename(int id, string name, bool asAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(name)) return (false, "Name is required.");
        using var db = new AppDbContext();
        var c = db.Categories.Find(id);
        if (c == null || !CanEdit(c, asAdmin)) return (false, "You can only edit your own categories.");
        c.Name = name.Trim();
        db.SaveChanges();
        return (true, "Renamed.");
    }

    public (bool ok, string message) Delete(int id, bool asAdmin = false)
    {
        using var db = new AppDbContext();
        var c = db.Categories.Find(id);
        if (c == null || !CanEdit(c, asAdmin)) return (false, "You can only delete your own categories.");
        if (db.Transactions.Any(t => t.CategoryId == id) || db.Budgets.Any(b => b.CategoryId == id))
            return (false, "Category is in use by transactions or budgets.");
        db.Categories.Remove(c);
        db.SaveChanges();
        return (true, "Deleted.");
    }

    private static bool CanEdit(Category c, bool asAdmin) => asAdmin ? c.UserId == null : c.UserId == Uid;
}
