using System.Text.Json;
using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers;

public class AdminController
{
    private record BackupData(List<User> Users, List<Category> Categories, List<Transaction> Transactions, List<Budget> Budgets);

    /// <summary>FR-18: manage user accounts.</summary>
    public List<User> GetUsers()
    {
        using var db = new AppDbContext();
        return db.Users.OrderBy(u => u.Email).ToList();
    }

    public (bool ok, string message) SetActive(int id, bool active)
    {
        using var db = new AppDbContext();
        var u = db.Users.Find(id);
        if (u == null) return (false, "User not found.");
        if (u.Role == Role.Admin) return (false, "Administrator accounts cannot be changed here.");
        u.IsActive = active;
        db.SaveChanges();
        return (true, active ? "User activated." : "User deactivated.");
    }

    public (bool ok, string message) DeleteUser(int id)
    {
        using var db = new AppDbContext();
        var u = db.Users.Find(id);
        if (u == null) return (false, "User not found.");
        if (u.Role == Role.Admin) return (false, "Administrator accounts cannot be deleted.");
        db.Transactions.RemoveRange(db.Transactions.Where(t => t.UserId == id));
        db.Budgets.RemoveRange(db.Budgets.Where(b => b.UserId == id));
        db.Categories.RemoveRange(db.Categories.Where(c => c.UserId == id));
        db.Users.Remove(u);
        db.SaveChanges();
        return (true, "User deleted.");
    }

    /// <summary>FR-20: back up all data to a JSON file.</summary>
    public void Backup(string path)
    {
        using var db = new AppDbContext();
        var data = new BackupData(db.Users.ToList(), db.Categories.ToList(),
            db.Transactions.ToList(), db.Budgets.ToList());
        File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>FR-20: restore from a JSON backup (replaces all current data; ids are re-generated).</summary>
    public void Restore(string path)
    {
        var data = JsonSerializer.Deserialize<BackupData>(File.ReadAllText(path))
                   ?? throw new InvalidDataException("Invalid backup file.");
        using var db = new AppDbContext();
        using var tx = db.Database.BeginTransaction();
        db.Transactions.RemoveRange(db.Transactions);
        db.Budgets.RemoveRange(db.Budgets);
        db.Categories.RemoveRange(db.Categories);
        db.Users.RemoveRange(db.Users);
        db.SaveChanges();

        var userMap = new Dictionary<int, User>();
        foreach (var u in data.Users) { var old = u.Id; u.Id = 0; db.Users.Add(u); userMap[old] = u; }
        var catMap = new Dictionary<int, Category>();
        foreach (var c in data.Categories) { var old = c.Id; c.Id = 0; db.Categories.Add(c); catMap[old] = c; }
        db.SaveChanges();   // generates new ids

        foreach (var c in data.Categories.Where(c => c.UserId.HasValue))
            c.UserId = userMap[c.UserId!.Value].Id;
        foreach (var t in data.Transactions)
        {
            t.Id = 0; t.Category = null;
            t.UserId = userMap[t.UserId].Id;
            if (t.CategoryId.HasValue) t.CategoryId = catMap[t.CategoryId.Value].Id;
            db.Transactions.Add(t);
        }
        foreach (var b in data.Budgets)
        {
            b.Id = 0; b.Category = null;
            b.UserId = userMap[b.UserId].Id;
            if (b.CategoryId.HasValue) b.CategoryId = catMap[b.CategoryId.Value].Id;
            db.Budgets.Add(b);
        }
        db.SaveChanges();
        tx.Commit();
    }
}
