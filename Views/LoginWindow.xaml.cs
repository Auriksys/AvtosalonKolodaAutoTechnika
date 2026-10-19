using System.Windows;
using System.Windows.Input;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;

namespace FDNJ_CSharp.Views;

public partial class LoginWindow : Window
{
    public User? AuthenticatedUser { get; private set; }

    public LoginWindow()
    {
        InitializeComponent();
        LoginTextBox.Focus();
        KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
                PerformLogin();
        };
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        PerformLogin();
    }

    private void PerformLogin()
    {
        string login = LoginTextBox.Text.Trim();
        string pass = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(login))
        {
            ShowError("Введите логин");
            LoginTextBox.Focus();
            return;
        }

        var user = DatabaseService.Instance.Authenticate(login, pass);
        if (user == null)
        {
            ShowError("Неверный логин или пароль");
            return;
        }

        AuthenticatedUser = user;
        DialogResult = true;
    }

    private void ShowError(string msg)
    {
        ErrorTextBlock.Text = msg;
        ErrorTextBlock.Visibility = Visibility.Visible;
    }

    private void QuickLogin_Admin(object sender, RoutedEventArgs e)
    {
        LoginTextBox.Text = "admin";
        PasswordBox.Password = "admin";
        PerformLogin();
    }

    private void QuickLogin_Manager(object sender, RoutedEventArgs e)
    {
        LoginTextBox.Text = "manager";
        PasswordBox.Password = "manager";
        PerformLogin();
    }

    private void QuickLogin_Director(object sender, RoutedEventArgs e)
    {
        LoginTextBox.Text = "director";
        PasswordBox.Password = "director";
        PerformLogin();
    }
}
