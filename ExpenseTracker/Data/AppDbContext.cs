using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();

    /// <summary>
    /// MySQL (XAMPP) connection string. Order: ETMS_CONNECTION env var, then connection.local.txt
    /// next to the exe (git-ignored), then the XAMPP default (root, no password).
    /// </summary>
    public static string ConnectionString
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("ETMS_CONNECTION");
            if (!string.IsNullOrWhiteSpace(env)) return env;
            var file = System.IO.Path.Combine(AppContext.BaseDirectory, "connection.local.txt");
            if (File.Exists(file)) return File.ReadAllText(file).Trim();
            return "Server=localhost;Port=3306;Database=Expense_Tracker;User=root;Password=;";
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseMySQL(ConnectionString);

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<User>().Property(u => u.Email).HasMaxLength(200);
        b.Entity<User>().Property(u => u.FullName).HasMaxLength(200);
        b.Entity<User>().Property(u => u.PasswordHash).HasMaxLength(200);
        b.Entity<Category>().Property(c => c.Name).HasMaxLength(100);
        b.Entity<Transaction>().Property(t => t.Amount).HasPrecision(18, 2);
        b.Entity<Transaction>().Property(t => t.Description).HasMaxLength(500);
        b.Entity<Transaction>().Property(t => t.Source).HasMaxLength(200);
        b.Entity<Transaction>().Property(t => t.PaymentMethod).HasMaxLength(100);
        b.Entity<Budget>().Property(t => t.Amount).HasPrecision(18, 2);
    }

    public static void Initialize()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();   // creates the tables in the Expense_Tracker database if missing
        if (!db.Categories.Any())
        {
            foreach (var n in new[] { "Food", "Transport", "Rent", "Health", "Education", "Airtime", "Other" })
                db.Categories.Add(new Category { Name = n });
        }
        if (!db.Users.Any(u => u.Role == Role.Admin))
        {
            db.Users.Add(new User
            {
                FullName = "Administrator", Email = "admin@etms.local",
                PasswordHash = Helpers.PasswordHasher.Hash("Admin@123"), Role = Role.Admin
            });
        }
        db.SaveChanges();
    }
}
