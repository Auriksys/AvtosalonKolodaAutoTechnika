using System;
using System.Windows;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;

namespace FDNJ_CSharp.Views;

public partial class UserEditDialog : Window
{
    private readonly User? _user;

    public UserEditDialog(User? existingUser = null)
    {
        InitializeComponent();
        _user = existingUser;

        RoleComboBox.ItemsSource = new[]
        {
            "admin", "manager", "director"
        };
        RoleComboBox.SelectedIndex = 1;

        if (_user != null)
        {
            TitleTextBlock.Text = $"Смена пароля: {_user.Login}";
            LoginBox.Text = _user.Login;
            LoginBox.IsEnabled = false;
            RoleComboBox.SelectedItem = _user.Role;
            RoleComboBox.IsEnabled = false;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string login = LoginBox.Text.Trim();
        string pass = PasswordBox.Text;

        if (string.IsNullOrWhiteSpace(login))
        {
            MessageBox.Show("Введите логин.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(pass))
        {
            MessageBox.Show("Введите пароль.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_user != null)
            {
                DatabaseService.Instance.UpdateUserPassword(_user.Id, pass);
            }
            else
            {
                var newUser = new User
                {
                    Login = login,
                    Password = pass,
                    Role = RoleComboBox.SelectedItem?.ToString() ?? "manager"
                };
                DatabaseService.Instance.AddUser(newUser);
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
