using ExpenseTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ExpenseTracker.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();

    /// <summary>
    /// Oracle XE connection string. Order: ETMS_CONNECTION env var, then connection.local.txt
    /// next to the exe (git-ignored, keeps the password out of source control), then a default.
    /// </summary>
    public static string ConnectionString
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("ETMS_CONNECTION");
            if (!string.IsNullOrWhiteSpace(env)) return env;
            var file = System.IO.Path.Combine(AppContext.BaseDirectory, "connection.local.txt");
            if (File.Exists(file)) return File.ReadAllText(file).Trim();
            return "User Id=ETMS;Password=etms123;Data Source=localhost:1521/XEPDB1;";
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseOracle(ConnectionString, b => b.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion21));

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
        // EnsureCreated() does nothing when the Oracle schema already holds other tables,
        // so create the ETMS tables explicitly if they are missing.
        var exists = db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS \"Value\" FROM user_tables WHERE UPPER(table_name) IN ('USERS','CATEGORIES','TRANSACTIONS','BUDGETS')")
            .AsEnumerable().First();
        if (exists > 0 && exists < 4)
            throw new InvalidOperationException(
                "Some ETMS tables exist but not all (a previous setup was interrupted). In SQL Developer run: " +
                "DROP TABLE \"Budgets\" PURGE; DROP TABLE \"Transactions\" PURGE; DROP TABLE \"Categories\" PURGE; DROP TABLE \"Users\" PURGE; " +
                "(ignore 'table does not exist' errors) and start the app again.");
        if (exists == 0)
            db.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>().CreateTables();
        else
        {
            // Upgrade tables created by an earlier version: these columns must allow NULL
            // (Oracle stores an empty string as NULL).
            foreach (var col in new[] { "Source", "PaymentMethod", "Description" })
            {
                try { db.Database.ExecuteSqlRaw("ALTER TABLE \"Transactions\" MODIFY (\"" + col + "\" NULL)"); }
                catch { /* ORA-01451: already nullable */ }
            }
        }
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
