namespace ExpenseTracker.Models;

public enum Role { User = 0, Admin = 1 }
public enum TransactionType { Expense = 0, Income = 1 }

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; } = Role.User;
    public bool IsActive { get; set; } = true;
    public int FailedAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>null = default category available to everyone (managed by admin).</summary>
    public int? UserId { get; set; }
    public override string ToString() => Name;
}

public class Transaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string? Source { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
}

public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public int AlertThresholdPercent { get; set; } = 80;
}
