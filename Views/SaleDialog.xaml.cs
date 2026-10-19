using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;

namespace FDNJ_CSharp.Views;

public partial class SaleDialog : Window
{
    private readonly User _currentUser;
    private Car? _preselectedCar;

    public SaleDialog(User currentUser, Car? preselectedCar = null, Client? preselectedClient = null)
    {
        InitializeComponent();
        _currentUser = currentUser;
        _preselectedCar = preselectedCar;

        PaymentComboBox.ItemsSource = new[]
        {
            "Наличные", "Банковская карта", "Банковский перевод", "Автокредит"
        };
        PaymentComboBox.SelectedIndex = 0;

        LoadData(preselectedClient);
    }

    private void LoadData(Client? preselectedClient)
    {
        var allCars = DatabaseService.Instance.GetCars()
            .Where(c => c.Status == "В наличии" || c.Status == "Забронирован" || (_preselectedCar != null && c.Id == _preselectedCar.Id))
            .ToList();

        CarComboBox.ItemsSource = allCars;
        CarComboBox.DisplayMemberPath = "FullTitle";
        CarComboBox.SelectedValuePath = "Id";

        var allClients = DatabaseService.Instance.GetClients();
        ClientComboBox.ItemsSource = allClients;
        ClientComboBox.DisplayMemberPath = "DisplayTitle";
        ClientComboBox.SelectedValuePath = "Id";

        if (_preselectedCar != null)
        {
            CarComboBox.SelectedValue = _preselectedCar.Id;
        }

        if (preselectedClient != null)
        {
            ClientComboBox.SelectedValue = preselectedClient.Id;
        }
    }

    private void CarComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CarComboBox.SelectedItem is Car car)
        {
            PriceBox.Text = car.Price.ToString("0");
            CarDetailsText.Text = $"{car.Brand} {car.Model} {car.Year} г. • VIN: {car.Vin} • {car.FormattedMileage} • Текущий статус: {car.Status}";
        }
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
            MessageBox.Show("Выберите покупателя.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(PriceBox.Text.Trim().Replace(" ", ""), out decimal price) || price <= 0)
        {
            MessageBox.Show("Введите корректную сумму сделки.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            PriceBox.Focus();
            return;
        }

        string payment = PaymentComboBox.SelectedItem?.ToString() ?? "Наличные";

        try
        {
            DatabaseService.Instance.AddSale(car.Id, client.Id, _currentUser.Id, price, payment);

            var saleObj = new Sale
            {
                CarId = car.Id,
                ClientId = client.Id,
                ManagerId = _currentUser.Id,
                SaleDate = DateTime.Today.ToString("yyyy-MM-dd"),
                Price = price,
                PaymentMethod = payment,
                CarTitle = car.Title,
                CarVin = car.Vin,
                ClientName = client.FullName,
                ClientPhone = client.Phone,
                ManagerLogin = _currentUser.Login
            };

            var printRes = MessageBox.Show(
                "Сделка успешно оформлена и сохранена в базе!\n\nРаспечатать / сохранить договор купли-продажи (PDF/HTML)?",
                "Успех", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (printRes == MessageBoxResult.Yes)
            {
                string html = ReportExportService.Instance.GenerateSaleReceiptHtml(saleObj, car);
                ReportExportService.Instance.OpenInBrowserOrPrint(html);
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка оформления сделки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
