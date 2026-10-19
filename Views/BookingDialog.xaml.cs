using System;
using System.Linq;
using System.Windows;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;

namespace FDNJ_CSharp.Views;

public partial class BookingDialog : Window
{
    private readonly User _currentUser;

    public BookingDialog(User currentUser, Car? preselectedCar = null, Client? preselectedClient = null)
    {
        InitializeComponent();
        _currentUser = currentUser;

        ExpirationDatePicker.SelectedDate = DateTime.Today.AddDays(7); // Default 7 days hold

        var availableCars = DatabaseService.Instance.GetCars()
            .Where(c => c.Status == "В наличии" || (preselectedCar != null && c.Id == preselectedCar.Id))
            .ToList();

        CarComboBox.ItemsSource = availableCars;
        CarComboBox.DisplayMemberPath = "FullTitle";
        CarComboBox.SelectedValuePath = "Id";

        var clients = DatabaseService.Instance.GetClients();
        ClientComboBox.ItemsSource = clients;
        ClientComboBox.DisplayMemberPath = "DisplayTitle";
        ClientComboBox.SelectedValuePath = "Id";

        if (preselectedCar != null)
            CarComboBox.SelectedValue = preselectedCar.Id;

        if (preselectedClient != null)
            ClientComboBox.SelectedValue = preselectedClient.Id;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (CarComboBox.SelectedItem is not Car car)
        {
            MessageBox.Show("Выберите автомобиль.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ClientComboBox.SelectedItem is not Client client)
        {
            MessageBox.Show("Выберите клиента.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ExpirationDatePicker.SelectedDate.HasValue || ExpirationDatePicker.SelectedDate.Value.Date < DateTime.Today)
        {
            MessageBox.Show("Дата окончания брони должна быть сегодня или в будущем.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string expDate = ExpirationDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd");

        try
        {
            DatabaseService.Instance.AddBooking(car.Id, client.Id, _currentUser.Id, expDate);
            MessageBox.Show($"Автомобиль {car.Brand} {car.Model} успешно забронирован за клиентом {client.FullName} до {expDate}!", "Бронь оформлена", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка бронирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
