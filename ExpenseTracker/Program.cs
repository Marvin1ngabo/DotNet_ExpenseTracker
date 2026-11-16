using ExpenseTracker.Controllers;
using ExpenseTracker.Data;
using ExpenseTracker.Views;

namespace ExpenseTracker;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        try { AppDbContext.Initialize(); }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "etms_error.log"), ex.ToString());
            MessageBox.Show("Could not initialise the Oracle database:" + Environment.NewLine + Environment.NewLine + ex.GetBaseException().Message,
                "ETMS - database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        while (true)
        {
            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK) return;
            }
            using var main = new MainForm();
            Application.Run(main);
            if (!main.LoggedOut) return;   // window closed -> exit; Logout -> back to login
            AuthController.Logout();
        }
    }
}
