# Expense Tracker Management System (ETMS) - WinForms / .NET

Desktop implementation of the ETMS SRS (C#, WinForms, **MVC**, EF Core + MySQL).

## Architecture (MVC)
| Layer | Folder | Role |
|---|---|---|
| Model | `Models/`, `Data/` | Entities (User, Category, Transaction, Budget) and EF Core `AppDbContext` |
| View | `Views/` | WinForms screens: `LoginForm`, `MainForm`, `TransactionForm` (no DB code) |
| Controller | `Controllers/` | Business rules and DB access: Auth, Transaction, Category, Budget, Report, Admin |

## Features (SRS requirement IDs)
- Register / login with PBKDF2-hashed passwords, 15-min lockout after 5 failed attempts, profile & password change (FR-01..03)
- Add/edit income and expenses with validation, search/filter/sort, soft delete + restore (FR-05..09)
- Default + personal categories (FR-12); monthly budgets with alert threshold, remaining budget, alerts (FR-13, 14)
- Dashboard: totals and charts by category and month (FR-15); daily/weekly/monthly/custom reports, CSV export for Excel (FR-16, 17)
- Admin: activate/deactivate/delete users, manage default categories, backup/restore database (FR-18..20)

## Run
Requires the .NET SDK 10 on Windows.

    cd ExpenseTracker
    dotnet run

Database setup (MySQL, e.g. XAMPP): start MySQL, open phpMyAdmin (`http://localhost/phpmyadmin`) and create an empty database named `Expense_Tracker`. The app creates its tables (`users`, `categories`, `transactions`, `budgets`) and the default data on first run.
The connection string is in `Data/AppDbContext.cs` (default: `localhost:3306`, database `Expense_Tracker`, user `root`, empty password). To use other credentials, create `ExpenseTracker/connection.local.txt` (git-ignored) containing, for example `Server=localhost;Port=3306;Database=Expense_Tracker;User=root;Password=yourpassword;` or set the `ETMS_CONNECTION` environment variable.
Default admin: `admin@etms.local` / `Admin@123` (change after first login). Register a normal user from the Register tab.

## Notes
- The SRS proposes SQL Server; MySQL is used here as required by the lecturer. Only the provider in `AppDbContext` would change for SQL Server.
- Backup/restore (admin) uses a JSON file.
- Amounts are `decimal` in C# and `DECIMAL(18,2)` in the database.
- PDF export, receipts, recurring transactions and password reset e-mail (low/medium priority) are not implemented.
