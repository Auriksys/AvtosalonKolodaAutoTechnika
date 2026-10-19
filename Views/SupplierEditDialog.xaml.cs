using System;
using System.Windows;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;

namespace FDNJ_CSharp.Views;

public partial class SupplierEditDialog : Window
{
    private readonly Supplier _supplier;
    private readonly bool _isEdit;

    public SupplierEditDialog(Supplier? existingSupplier = null)
    {
        InitializeComponent();
        _isEdit = existingSupplier != null;
        _supplier = existingSupplier ?? new Supplier();

        if (_isEdit)
        {
            TitleTextBlock.Text = "Редактирование поставщика";
            NameBox.Text = _supplier.Name;
            ContactBox.Text = _supplier.ContactPerson;
            PhoneBox.Text = _supplier.Phone;
            EmailBox.Text = _supplier.Email;
            AddressBox.Text = _supplier.Address;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Введите название компании поставщика.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            NameBox.Focus();
            return;
        }

        _supplier.Name = name;
        _supplier.ContactPerson = ContactBox.Text.Trim();
        _supplier.Phone = PhoneBox.Text.Trim();
        _supplier.Email = EmailBox.Text.Trim();
        _supplier.Address = AddressBox.Text.Trim();

        try
        {
            if (_isEdit)
                DatabaseService.Instance.UpdateSupplier(_supplier);
            else
                DatabaseService.Instance.AddSupplier(_supplier);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения поставщика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
