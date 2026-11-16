using System.Net.Mail;
using ExpenseTracker.Data;
using ExpenseTracker.Helpers;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers;

public class AuthController
{
    public static User? CurrentUser { get; private set; }
    public static void Logout() => CurrentUser = null;

    /// <summary>FR-01: register with unique e-mail.</summary>
    public (bool ok, string message) Register(string fullName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return (false, "Full name is required.");
        if (!IsValidEmail(email)) return (false, "Enter a valid e-mail address.");
        if (password.Length < 6) return (false, "Password must be at least 6 characters.");
        using var db = new AppDbContext();
        email = email.Trim().ToLowerInvariant();
        if (db.Users.Any(u => u.Email == email)) return (false, "That e-mail is already registered.");
        db.Users.Add(new User { FullName = fullName.Trim(), Email = email, PasswordHash = PasswordHasher.Hash(password) });
        db.SaveChanges();
        return (true, "Account created. You can now log in.");
    }

    /// <summary>FR-02: login; lock for 15 minutes after 5 failed attempts.</summary>
    public (bool ok, string message) Login(string email, string password)
    {
        using var db = new AppDbContext();
        email = (email ?? "").Trim().ToLowerInvariant();
        var user = db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return (false, "Invalid e-mail or password.");
        if (!user.IsActive) return (false, "This account has been deactivated.");
        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.Now)
            return (false, $"Account locked until {user.LockoutEnd:HH:mm}. Try again later.");

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= 5)
            {
                user.LockoutEnd = DateTime.Now.AddMinutes(15);
                user.FailedAttempts = 0;
            }
            db.SaveChanges();
            return (false, "Invalid e-mail or password.");
        }
        user.FailedAttempts = 0;
        user.LockoutEnd = null;
        db.SaveChanges();
        CurrentUser = user;
        return (true, "OK");
    }

    /// <summary>FR-03: update profile / change password.</summary>
    public (bool ok, string message) UpdateProfile(string fullName, string? newPassword)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return (false, "Full name is required.");
        if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < 6)
            return (false, "Password must be at least 6 characters.");
        using var db = new AppDbContext();
        var u = db.Users.Find(CurrentUser!.Id)!;
        u.FullName = fullName.Trim();
        if (!string.IsNullOrEmpty(newPassword)) u.PasswordHash = PasswordHasher.Hash(newPassword);
        db.SaveChanges();
        CurrentUser = u;
        return (true, "Profile updated.");
    }

    private static bool IsValidEmail(string e)
    {
        try { return !string.IsNullOrWhiteSpace(e) && new MailAddress(e).Address == e.Trim(); }
        catch { return false; }
    }
}
