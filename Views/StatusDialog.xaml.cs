using System;
using System.Windows;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;

namespace FDNJ_CSharp.Views;

public partial class StatusDialog : Window
{
    private readonly Car _car;

    public StatusDialog(Car car)
    {
        InitializeComponent();
        _car = car;
        CarInfoTextBlock.Text = $"{_car.Brand} {_car.Model} (VIN: {_car.Vin})";

        StatusComboBox.ItemsSource = new[]
        {
            "В наличии", "Забронирован", "Продан", "На диагностике", "На ремонте", "Ожидает поступления"
        };
        StatusComboBox.SelectedItem = _car.Status;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string? newStatus = StatusComboBox.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(newStatus))
        {
            MessageBox.Show("Выберите статус.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            DatabaseService.Instance.ChangeCarStatus(_car.Id, newStatus, NoteTextBox.Text.Trim());
            _car.Status = newStatus;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось изменить статус: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
