using System;
using System.Windows;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;

namespace FDNJ_CSharp.Views;

public partial class ClientEditDialog : Window
{
    private readonly Client _client;
    private readonly bool _isEdit;

    public ClientEditDialog(Client? existingClient = null)
    {
        InitializeComponent();
        _isEdit = existingClient != null;
        _client = existingClient ?? new Client();

        if (_isEdit)
        {
            TitleTextBlock.Text = "Редактирование клиента";
            FullNameBox.Text = _client.FullName;
            PhoneBox.Text = _client.Phone;
            EmailBox.Text = _client.Email;
            CommentBox.Text = _client.Comment;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string name = FullNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Введите ФИО клиента.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            FullNameBox.Focus();
            return;
        }

        _client.FullName = name;
        _client.Phone = PhoneBox.Text.Trim();
        _client.Email = EmailBox.Text.Trim();
        _client.Comment = CommentBox.Text.Trim();

        try
        {
            if (_isEdit)
                DatabaseService.Instance.UpdateClient(_client);
            else
                DatabaseService.Instance.AddClient(_client);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
