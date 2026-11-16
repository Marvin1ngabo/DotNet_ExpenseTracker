using ExpenseTracker.Controllers;

namespace ExpenseTracker.Views;

/// <summary>Login / Register screen.</summary>
public class LoginForm : Form
{
    private readonly AuthController _auth = new();
    private readonly TextBox _loginEmail = new() { Width = 240 };
    private readonly TextBox _loginPass = new() { Width = 240, UseSystemPasswordChar = true };
    private readonly TextBox _regName = new() { Width = 240 };
    private readonly TextBox _regEmail = new() { Width = 240 };
    private readonly TextBox _regPass = new() { Width = 240, UseSystemPasswordChar = true };
    private readonly Label _msg = new() { AutoSize = false, Width = 380, Height = 36, ForeColor = Color.Firebrick };

    public LoginForm()
    {
        Text = "ETMS - Expense Tracker";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        ClientSize = new Size(420, 360);

        var tabs = new TabControl { Left = 10, Top = 10, Width = 400, Height = 270 };
        var tLogin = new TabPage("Login");
        var tReg = new TabPage("Register");

        var btnLogin = new Button { Text = "Log in", Width = 100 };
        btnLogin.Click += (_, _) => DoLogin();
        tLogin.Controls.Add(Stack(new Control[] {
            new Label { Text = "E-mail", AutoSize = true }, _loginEmail,
            new Label { Text = "Password", AutoSize = true }, _loginPass, btnLogin }));

        var btnReg = new Button { Text = "Create account", Width = 120 };
        btnReg.Click += (_, _) => DoRegister();
        tReg.Controls.Add(Stack(new Control[] {
            new Label { Text = "Full name", AutoSize = true }, _regName,
            new Label { Text = "E-mail", AutoSize = true }, _regEmail,
            new Label { Text = "Password (min 6)", AutoSize = true }, _regPass, btnReg }));

        tabs.TabPages.Add(tLogin);
        tabs.TabPages.Add(tReg);
        _msg.Left = 20; _msg.Top = 290;
        Controls.Add(tabs);
        Controls.Add(_msg);
        AcceptButton = btnLogin;
    }

    private static FlowLayoutPanel Stack(Control[] items)
    {
        var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20, 10, 0, 0) };
        p.Controls.AddRange(items);
        return p;
    }

    private void DoLogin()
    {
        var (ok, message) = _auth.Login(_loginEmail.Text, _loginPass.Text);
        if (ok) { DialogResult = DialogResult.OK; Close(); return; }
        _msg.ForeColor = Color.Firebrick; _msg.Text = message;
    }

    private void DoRegister()
    {
        var (ok, message) = _auth.Register(_regName.Text, _regEmail.Text, _regPass.Text);
        _msg.ForeColor = ok ? Color.SeaGreen : Color.Firebrick;
        _msg.Text = message;
        if (ok) { _loginEmail.Text = _regEmail.Text; _regName.Clear(); _regEmail.Clear(); _regPass.Clear(); }
    }
}
