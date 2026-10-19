using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;
using FDNJ_CSharp.Views;

namespace FDNJ_CSharp;

public partial class MainWindow : Window
{
    private User _currentUser = null!;
    private Car? _selectedCar;

    private List<Car> _allCars = new();
    private List<Client> _allClients = new();
    private List<Sale> _allSales = new();
    private List<Booking> _allBookings = new();
    private List<Supplier> _allSuppliers = new();
    private List<User> _allUsers = new();
    private List<HistoryItem> _allHistory = new();

    public MainWindow() : this(new User { Login = "admin", Role = "admin" })
    {
    }

    public MainWindow(User currentUser)
    {
        InitializeComponent();
        SetCurrentUser(currentUser);
        InitializeFilters();
        RefreshAll();
        RecalculateCredit();
        SwitchView(NavDashboard);
    }

    private void SetCurrentUser(User user)
    {
        _currentUser = user;
        UserNameTextBlock.Text = user.Login;
        UserRoleTextBlock.Text = user.RoleDisplay;
        UserAvatarLetter.Text = string.IsNullOrEmpty(user.Login) ? "U" : user.Login[..1].ToUpperInvariant();

        // Admin-only features
        NavUsers.Visibility = user.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        Hide();
        var loginWnd = new LoginWindow();
        if (loginWnd.ShowDialog() == true && loginWnd.AuthenticatedUser != null)
        {
            SetCurrentUser(loginWnd.AuthenticatedUser);
            RefreshAll();
            SwitchView(NavDashboard);
            Show();
        }
        else
        {
            Close();
        }
    }

    private void InitializeFilters()
    {
        CarStatusFilter.ItemsSource = new[]
        {
            "Все статусы", "В наличии", "Забронирован", "Продан", "На диагностике", "На ремонте", "Ожидает поступления"
        };
        CarStatusFilter.SelectedIndex = 0;

        CarSortFilter.ItemsSource = new[]
        {
            "Сортировка: По умолчанию",
            "Цена: по возрастанию",
            "Цена: по убыванию",
            "Год: сначала новые",
            "Пробег: минимальный"
        };
        CarSortFilter.SelectedIndex = 0;

        SalesPaymentFilter.ItemsSource = new[]
        {
            "Все способы оплаты", "Наличные", "Банковская карта", "Банковский перевод", "Автокредит"
        };
        SalesPaymentFilter.SelectedIndex = 0;
    }

    public void RefreshAll()
    {
        try
        {
            _allCars = DatabaseService.Instance.GetCars();
            _allClients = DatabaseService.Instance.GetClients();
            _allSales = DatabaseService.Instance.GetSales();
            _allBookings = DatabaseService.Instance.GetBookings();
            _allSuppliers = DatabaseService.Instance.GetSuppliers();
            _allUsers = DatabaseService.Instance.GetUsers();
            _allHistory = DatabaseService.Instance.GetHistory(250);

            var brands = new List<string> { "Все марки" };
            brands.AddRange(_allCars.Select(c => c.Brand).Where(b => !string.IsNullOrWhiteSpace(b)).Distinct().OrderBy(b => b));
            string? prevBrand = CarBrandFilter.SelectedItem?.ToString();
            CarBrandFilter.ItemsSource = brands;
            CarBrandFilter.SelectedItem = (prevBrand != null && brands.Contains(prevBrand)) ? prevBrand : "Все марки";

            UpdateDashboard();
            ApplyCarFilters();
            ApplyClientFilter();
            ApplySalesFilter();
            ApplyBookingsFilter();
            ApplySupplierFilter();
            ApplyHistoryFilter();

            if (_currentUser.IsAdmin)
                UsersDataGrid.ItemsSource = _allUsers;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка обновления данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #region Navigation & View Switching
    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb)
            SwitchView(rb);
    }

    private void OpenCatalogFromDashboard_Click(object sender, RoutedEventArgs e)
    {
        SwitchView(NavCars);
    }

    private void OpenHistoryFromDashboard_Click(object sender, RoutedEventArgs e)
    {
        SwitchView(NavHistory);
    }

    private void SwitchView(RadioButton activeNav)
    {
        NavDashboard.IsChecked = activeNav == NavDashboard;
        NavCars.IsChecked = activeNav == NavCars;
        NavClients.IsChecked = activeNav == NavClients;
        NavSales.IsChecked = activeNav == NavSales;
        NavBookings.IsChecked = activeNav == NavBookings;
        NavSuppliers.IsChecked = activeNav == NavSuppliers;
        NavReports.IsChecked = activeNav == NavReports;
        NavUsers.IsChecked = activeNav == NavUsers;
        NavHistory.IsChecked = activeNav == NavHistory;

        ViewDashboard.Visibility = activeNav == NavDashboard ? Visibility.Visible : Visibility.Collapsed;
        ViewCars.Visibility = activeNav == NavCars ? Visibility.Visible : Visibility.Collapsed;
        ViewClients.Visibility = activeNav == NavClients ? Visibility.Visible : Visibility.Collapsed;
        ViewSales.Visibility = activeNav == NavSales ? Visibility.Visible : Visibility.Collapsed;
        ViewBookings.Visibility = activeNav == NavBookings ? Visibility.Visible : Visibility.Collapsed;
        ViewSuppliers.Visibility = activeNav == NavSuppliers ? Visibility.Visible : Visibility.Collapsed;
        ViewReports.Visibility = activeNav == NavReports ? Visibility.Visible : Visibility.Collapsed;
        ViewUsers.Visibility = activeNav == NavUsers ? Visibility.Visible : Visibility.Collapsed;
        ViewHistory.Visibility = activeNav == NavHistory ? Visibility.Visible : Visibility.Collapsed;

        if (activeNav == NavDashboard)
        {
            PageHeaderTitle.Text = "Панель управления";
            PageHeaderSubtitle.Text = "Сводная аналитика, витрина шоурума, кредитный калькулятор и живой аудит";
            TopAddButton.Content = "+  Оформить продажу";
            UpdateDashboard();
        }
        else if (activeNav == NavCars)
        {
            PageHeaderTitle.Text = "Каталог автомобилей";
            PageHeaderSubtitle.Text = "Управление автопарком, фотографиями, карточками и техническими паспортами";
            TopAddButton.Content = "+  Добавить авто";
            ApplyCarFilters();
        }
        else if (activeNav == NavClients)
        {
            PageHeaderTitle.Text = "База клиентов";
            PageHeaderSubtitle.Text = "Покупатели, контактные номера, история обращений и предпочтения";
            TopAddButton.Content = "+  Добавить клиента";
            ApplyClientFilter();
        }
        else if (activeNav == NavSales)
        {
            PageHeaderTitle.Text = "Журнал продаж";
            PageHeaderSubtitle.Text = "Оформленные сделки купли-продажи автомобилей, способы расчетов и выручка";
            TopAddButton.Content = "+  Оформить сделку";
            ApplySalesFilter();
        }
        else if (activeNav == NavBookings)
        {
            PageHeaderTitle.Text = "Резервы и бронирования";
            PageHeaderSubtitle.Text = "Автомобили, закрепленные за покупателями до определенной даты";
            TopAddButton.Content = "+  Новая бронь";
            ApplyBookingsFilter();
        }
        else if (activeNav == NavSuppliers)
        {
            PageHeaderTitle.Text = "Поставщики и партнеры";
            PageHeaderSubtitle.Text = "Источники поступления автомобилей, контактные лица и реквизиты";
            TopAddButton.Content = "+  Добавить поставщика";
            ApplySupplierFilter();
        }
        else if (activeNav == NavReports)
        {
            PageHeaderTitle.Text = "Отчёты и аналитика";
            PageHeaderSubtitle.Text = "Формирование ведомостей, экспорт в Excel (.xlsx) и печатные отчеты (PDF)";
            TopAddButton.Content = "📊  Сводный Excel";
        }
        else if (activeNav == NavUsers)
        {
            PageHeaderTitle.Text = "Управление персоналом";
            PageHeaderSubtitle.Text = "Учетные записи сотрудников автосалона, роли доступа и смена паролей";
            TopAddButton.Content = "+  Новый сотрудник";
        }
        else if (activeNav == NavHistory)
        {
            PageHeaderTitle.Text = "Журнал операций (Аудит)";
            PageHeaderSubtitle.Text = "Хронологическая история всех действий сотрудников и изменений базы данных";
            TopAddButton.Content = "↻  Обновить журнал";
            ApplyHistoryFilter();
        }
    }

    private void TopAddButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewCars.Visibility == Visibility.Visible)
            AddCar_Click(sender, e);
        else if (ViewClients.Visibility == Visibility.Visible)
            AddClient_Click(sender, e);
        else if (ViewSales.Visibility == Visibility.Visible || ViewDashboard.Visibility == Visibility.Visible)
            AddSale_Click(sender, e);
        else if (ViewBookings.Visibility == Visibility.Visible)
            AddBooking_Click(sender, e);
        else if (ViewSuppliers.Visibility == Visibility.Visible)
            AddSupplier_Click(sender, e);
        else if (ViewUsers.Visibility == Visibility.Visible)
            AddUser_Click(sender, e);
        else if (ViewReports.Visibility == Visibility.Visible)
            ExportFullFinancialReport_Click(sender, e);
        else if (ViewHistory.Visibility == Visibility.Visible)
            RefreshHistory_Click(sender, e);
    }
    #endregion

    #region Dashboard & Credit Calculator
    private void UpdateDashboard()
    {
        var stats = DatabaseService.Instance.GetDashboardStats();

        // Sidebar pulse
        if (SidebarStockValue != null) SidebarStockValue.Text = stats.FormattedTotalStockValue;
        if (SidebarAvgSale != null) SidebarAvgSale.Text = stats.FormattedAverageSalePrice;
        if (SidebarClientsCount != null) SidebarClientsCount.Text = $"{stats.TotalClients} чел.";

        // Top executive banner
        if (BannerStockValue != null) BannerStockValue.Text = stats.FormattedTotalStockValue;
        if (BannerAvgCheck != null) BannerAvgCheck.Text = stats.FormattedAverageSalePrice;
        if (BannerPartners != null) BannerPartners.Text = $"{stats.TotalClients} клиентов • {stats.TotalSuppliers} поставщиков";

        // 5 KPI cards + sub-labels
        KpiTotalCars.Text = stats.TotalCars.ToString();
        if (KpiTotalCarsSub != null) KpiTotalCarsSub.Text = $"{stats.TopBrands.Count} марок в каталоге";

        KpiInStock.Text = stats.InStockCars.ToString();
        if (KpiInStockSub != null) KpiInStockSub.Text = $"На сумму {stats.FormattedTotalStockValue}";

        KpiBooked.Text = stats.BookedCars.ToString();
        if (KpiBookedSub != null) KpiBookedSub.Text = stats.BookedCars > 0 ? "Закреплены за клиентами" : "Нет активных резервов";

        KpiSold.Text = stats.SoldCars.ToString();
        if (KpiSoldSub != null) KpiSoldSub.Text = $"Оформлено сделок: {stats.TotalSalesCount}";

        KpiRevenue.Text = stats.FormattedTotalRevenue;
        if (KpiRevenueSub != null) KpiRevenueSub.Text = $"Ср. чек: {stats.FormattedAverageSalePrice}";

        // Showroom list on dashboard (in-stock and booked cars)
        if (DashboardShowroomList != null)
        {
            var showroomCars = _allCars
                .Where(c => c.Status == "В наличии" || c.Status == "Забронирован")
                .OrderByDescending(c => c.Price)
                .Take(8)
                .ToList();
            DashboardShowroomList.ItemsSource = showroomCars;
        }

        if (DashboardStatusesList != null)
            DashboardStatusesList.ItemsSource = stats.CarStatuses;

        TopBrandsList.ItemsSource = stats.TopBrands;
        PaymentMethodsList.ItemsSource = stats.PaymentMethods;
        RecentHistoryList.ItemsSource = stats.RecentHistory;
        ReportsStatusesList.ItemsSource = stats.CarStatuses;

        LoadCustomKpiIcons();
    }

    private void CreditCalc_Changed(object sender, TextChangedEventArgs e)
    {
        RecalculateCredit();
    }

    private void RecalculateCredit()
    {
        if (CalcMonthlyResult == null || CalcLoanSumResult == null || CalcInitialSumResult == null)
            return;

        decimal price = 3500000m;
        if (CalcCarPriceBox != null && decimal.TryParse(CalcCarPriceBox.Text.Replace(" ", ""), out decimal parsedPrice) && parsedPrice > 0)
            price = parsedPrice;

        decimal downPct = 20m;
        if (CalcDownPaymentBox != null && decimal.TryParse(CalcDownPaymentBox.Text.Replace(" ", ""), out decimal parsedDown) && parsedDown >= 0 && parsedDown <= 95)
            downPct = parsedDown;

        int months = 60;
        if (CalcMonthsBox != null && int.TryParse(CalcMonthsBox.Text.Trim(), out int parsedMonths) && parsedMonths >= 6 && parsedMonths <= 120)
            months = parsedMonths;

        decimal initialPayment = Math.Round(price * (downPct / 100m), 0);
        decimal loanAmount = Math.Max(0m, price - initialPayment);

        // Annuity formula at 14.9% annual rate
        double annualRate = 0.149;
        double monthlyRate = annualRate / 12.0;
        decimal monthlyPayment = 0m;
        if (loanAmount > 0 && months > 0)
        {
            double factor = (monthlyRate * Math.Pow(1 + monthlyRate, months)) / (Math.Pow(1 + monthlyRate, months) - 1);
            monthlyPayment = Math.Round(loanAmount * (decimal)factor, 0);
        }

        CalcMonthlyResult.Text = $"{monthlyPayment:N0} ₽ / мес.";
        CalcLoanSumResult.Text = $"Кредит: {loanAmount:N0} ₽";
        CalcInitialSumResult.Text = $"Взнос: {initialPayment:N0} ₽";
    }

    private void LoadCustomKpiIcons()
    {
        TryApplyKpiIcon("kpi_cars", KpiPath_Cars, KpiImg_Cars);
        TryApplyKpiIcon("kpi_instock", KpiPath_InStock, KpiImg_InStock);
        TryApplyKpiIcon("kpi_booked", KpiPath_Booked, KpiImg_Booked);
        TryApplyKpiIcon("kpi_sold", KpiPath_Sold, KpiImg_Sold);
        TryApplyKpiIcon("kpi_revenue", KpiPath_Revenue, KpiImg_Revenue);
    }

    private void TryApplyKpiIcon(string key, System.Windows.Shapes.Path defaultPath, Image customImg)
    {
        try
        {
            string iconsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons");
            if (!Directory.Exists(iconsDir)) return;

            var match = Directory.GetFiles(iconsDir, $"{key}.*").FirstOrDefault();
            if (match != null && File.Exists(match))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(match, UriKind.Absolute);
                bmp.EndInit();
                customImg.Source = bmp;
                customImg.Visibility = Visibility.Visible;
                defaultPath.Visibility = Visibility.Collapsed;
            }
            else
            {
                customImg.Visibility = Visibility.Collapsed;
                defaultPath.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }

    private void KpiIcon_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string key })
        {
            var ofd = new OpenFileDialog
            {
                Title = "Выберите свою иконку для карточки показателя",
                Filter = "Иконки и изображения (*.png;*.ico;*.jpg;*.jpeg;*.webp)|*.png;*.ico;*.jpg;*.jpeg;*.webp"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string iconsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Icons");
                    Directory.CreateDirectory(iconsDir);

                    foreach (var old in Directory.GetFiles(iconsDir, $"{key}.*"))
                    {
                        try { File.Delete(old); } catch { }
                    }

                    string ext = Path.GetExtension(ofd.FileName);
                    string dest = Path.Combine(iconsDir, $"{key}{ext}");
                    File.Copy(ofd.FileName, dest, true);

                    LoadCustomKpiIcons();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось установить иконку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    #endregion

    #region Cars Catalog, Filters & Vehicle Dossier Inspector
    private void CarFilter_Changed(object sender, RoutedEventArgs e)
    {
        ApplyCarFilters();
    }

    private void CarViewMode_Changed(object sender, RoutedEventArgs e)
    {
        bool isCards = ToggleCardsView.IsChecked == true;
        CarsCardsScrollViewer.Visibility = isCards ? Visibility.Visible : Visibility.Collapsed;
        CarsDataGrid.Visibility = isCards ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ResetCarFilters_Click(object sender, RoutedEventArgs e)
    {
        CarSearchBox.Text = string.Empty;
        CarStatusFilter.SelectedIndex = 0;
        CarBrandFilter.SelectedIndex = 0;
        CarSortFilter.SelectedIndex = 0;
        ApplyCarFilters();
    }

    private void ApplyCarFilters()
    {
        string query = CarSearchBox?.Text?.Trim().ToLowerInvariant() ?? "";
        string? status = CarStatusFilter?.SelectedItem?.ToString();
        string? brand = CarBrandFilter?.SelectedItem?.ToString();
        int sortIndex = CarSortFilter?.SelectedIndex ?? 0;

        var filtered = _allCars.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(c =>
                c.Brand.ToLowerInvariant().Contains(query) ||
                c.Model.ToLowerInvariant().Contains(query) ||
                c.Vin.ToLowerInvariant().Contains(query) ||
                c.BodyType.ToLowerInvariant().Contains(query) ||
                c.Transmission.ToLowerInvariant().Contains(query) ||
                c.Engine.ToLowerInvariant().Contains(query));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "Все статусы")
        {
            filtered = filtered.Where(c => string.Equals(c.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(brand) && brand != "Все марки")
        {
            filtered = filtered.Where(c => string.Equals(c.Brand, brand, StringComparison.OrdinalIgnoreCase));
        }

        filtered = sortIndex switch
        {
            1 => filtered.OrderBy(c => c.Price),
            2 => filtered.OrderByDescending(c => c.Price),
            3 => filtered.OrderByDescending(c => c.Year),
            4 => filtered.OrderBy(c => c.Mileage),
            _ => filtered.OrderByDescending(c => c.Id)
        };

        var list = filtered.ToList();
        if (CarsCardsItemsControl != null)
            CarsCardsItemsControl.ItemsSource = list;
        if (CarsDataGrid != null)
            CarsDataGrid.ItemsSource = list;

        if (CarsCountSummary != null)
        {
            decimal totalFilteredSum = list.Sum(c => c.Price);
            CarsCountSummary.Text = $"Показано: {list.Count} из {_allCars.Count} авто • На сумму: {totalFilteredSum:N0} ₽";
        }

        // Keep inspector synced
        if (_selectedCar != null)
        {
            var updatedSelected = list.FirstOrDefault(c => c.Id == _selectedCar.Id) ?? list.FirstOrDefault();
            UpdateCarInspector(updatedSelected);
        }
        else
        {
            UpdateCarInspector(list.FirstOrDefault());
        }
    }

    private void CarCard_Select(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Car car })
        {
            UpdateCarInspector(car);
        }
    }

    private void CarsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CarsDataGrid.SelectedItem is Car car)
        {
            UpdateCarInspector(car);
        }
    }

    private void UpdateCarInspector(Car? car)
    {
        _selectedCar = car;
        if (InspectorCarTitle == null) return;

        if (car == null)
        {
            InspectorCarTitle.Text = "Автомобиль не выбран";
            InspectorCarVin.Text = "VIN: —";
            InspectorCarPrice.Text = "0 ₽";
            InspectorCarMonthly.Text = "—";
            InspectorStatusText.Text = "—";
            InspectorYear.Text = "—";
            InspectorBody.Text = "—";
            InspectorColor.Text = "—";
            InspectorEngine.Text = "—";
            InspectorTransmission.Text = "—";
            InspectorDrive.Text = "—";
            InspectorMileage.Text = "—";
            InspectorSupplier.Text = "—";
            InspectorArrival.Text = "—";
            InspectorCarImage.Source = null;
            return;
        }

        InspectorCarTitle.Text = car.Title;
        InspectorCarVin.Text = $"VIN: {car.Vin}";
        InspectorCarPrice.Text = car.FormattedPrice;
        InspectorCarMonthly.Text = $"Автокредит: {car.MonthlyPaymentFormatted}";
        InspectorStatusText.Text = car.Status;

        try
        {
            InspectorStatusBadge.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(car.StatusBackground)!;
            InspectorStatusText.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString(car.StatusForeground)!;
        }
        catch { }

        InspectorYear.Text = $"{car.Year} г.";
        InspectorBody.Text = string.IsNullOrWhiteSpace(car.BodyType) ? "—" : car.BodyType;
        InspectorColor.Text = string.IsNullOrWhiteSpace(car.Color) ? "—" : car.Color;
        InspectorEngine.Text = car.EngineAndPower;
        InspectorTransmission.Text = string.IsNullOrWhiteSpace(car.Transmission) ? "—" : car.Transmission;
        InspectorDrive.Text = string.IsNullOrWhiteSpace(car.Drive) ? "—" : car.Drive;
        InspectorMileage.Text = car.FormattedMileage;
        InspectorSupplier.Text = string.IsNullOrWhiteSpace(car.SupplierName) ? "—" : car.SupplierName;
        InspectorArrival.Text = string.IsNullOrWhiteSpace(car.ArrivalDate) ? "—" : car.ArrivalDate;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(car.DisplayImagePath, UriKind.Absolute);
            bmp.EndInit();
            InspectorCarImage.Source = bmp;
        }
        catch
        {
            InspectorCarImage.Source = null;
        }
    }

    private void InspectorImage_Click(object sender, MouseButtonEventArgs e)
    {
        if (_selectedCar != null)
        {
            var dlg = new CarPhotoDialog(_selectedCar) { Owner = this };
            if (dlg.ShowDialog() == true)
                RefreshAll();
        }
    }

    private Car? GetSelectedCar()
    {
        if (CarsDataGrid.Visibility == Visibility.Visible && CarsDataGrid.SelectedItem is Car gridCar)
            return gridCar;

        return _selectedCar;
    }

    private void AddCar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CarEditDialog { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void EditCar_Click(object sender, RoutedEventArgs e)
    {
        var car = GetSelectedCar();
        if (car == null)
        {
            MessageBox.Show("Выберите автомобиль (кликните по карточке или строке таблицы).", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new CarEditDialog(car) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void ChangeCarStatus_Click(object sender, RoutedEventArgs e)
    {
        var car = GetSelectedCar();
        if (car == null)
        {
            MessageBox.Show("Выберите автомобиль для смены статуса.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new StatusDialog(car) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void SellSelectedCar_Click(object sender, RoutedEventArgs e)
    {
        var car = GetSelectedCar();
        if (car == null)
        {
            MessageBox.Show("Выберите автомобиль для продажи.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaleDialog(_currentUser, car) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void DeleteCar_Click(object sender, RoutedEventArgs e)
    {
        var car = GetSelectedCar();
        if (car == null)
        {
            MessageBox.Show("Выберите автомобиль для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Вы уверены, что хотите удалить автомобиль {car.Brand} {car.Model} (VIN: {car.Vin})?",
            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            try
            {
                DatabaseService.Instance.DeleteCar(car.Id);
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CarsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        EditCar_Click(sender, e);
    }

    private void CardAction_Sell(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Car car })
        {
            UpdateCarInspector(car);
            var dlg = new SaleDialog(_currentUser, car) { Owner = this };
            if (dlg.ShowDialog() == true)
                RefreshAll();
        }
    }

    private void CardAction_Book(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Car car })
        {
            UpdateCarInspector(car);
            var dlg = new BookingDialog(_currentUser, car) { Owner = this };
            if (dlg.ShowDialog() == true)
                RefreshAll();
        }
    }

    private void CardAction_Edit(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Car car })
        {
            UpdateCarInspector(car);
            var dlg = new CarEditDialog(car) { Owner = this };
            if (dlg.ShowDialog() == true)
                RefreshAll();
        }
    }

    private void ChangeCarPhoto_Click(object sender, RoutedEventArgs e)
    {
        var car = GetSelectedCar();
        if (car == null)
        {
            MessageBox.Show("Выберите автомобиль или нажмите кнопку «📷 Фото» на карточке автомобиля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new CarPhotoDialog(car) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void CardAction_ChangePhoto(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Car car })
        {
            UpdateCarInspector(car);
            var dlg = new CarPhotoDialog(car) { Owner = this };
            if (dlg.ShowDialog() == true)
                RefreshAll();
        }
    }

    private void CardImage_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Car car })
        {
            UpdateCarInspector(car);
            var dlg = new CarPhotoDialog(car) { Owner = this };
            if (dlg.ShowDialog() == true)
                RefreshAll();
        }
    }

    private void CardImage_Drop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Car car } && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                string droppedFile = files[0];
                string ext = Path.GetExtension(droppedFile).ToLowerInvariant();
                string[] allowed = { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif" };
                if (allowed.Contains(ext))
                {
                    try
                    {
                        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cars");
                        Directory.CreateDirectory(dir);
                        string newFileName = $"car_{car.Id}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                        string dest = Path.Combine(dir, newFileName);
                        File.Copy(droppedFile, dest, true);

                        DatabaseService.Instance.UpdateCarImage(car.Id, newFileName);
                        RefreshAll();
                        MessageBox.Show($"Фотография для автомобиля {car.Brand} {car.Model} успешно обновлена!", "Фото загружено", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения перетащенного файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    private void ExportCarsExcel_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Сохранить каталог автомобилей в Excel",
            Filter = "Книга Excel (*.xlsx)|*.xlsx",
            FileName = $"DRIVE_Cars_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                ReportExportService.Instance.ExportCarsToExcel(_allCars, sfd.FileName);
                PromptOpenFile(sfd.FileName, "Каталог автомобилей успешно экспортирован в Excel!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region Clients
    private void ClientSearch_Changed(object sender, TextChangedEventArgs e)
    {
        ApplyClientFilter();
    }

    private void ApplyClientFilter()
    {
        string q = ClientSearchBox?.Text?.Trim().ToLowerInvariant() ?? "";
        var filtered = _allClients.Where(c =>
            string.IsNullOrWhiteSpace(q) ||
            c.FullName.ToLowerInvariant().Contains(q) ||
            c.Phone.ToLowerInvariant().Contains(q) ||
            c.Email.ToLowerInvariant().Contains(q) ||
            c.Comment.ToLowerInvariant().Contains(q)).ToList();

        if (ClientsDataGrid != null)
            ClientsDataGrid.ItemsSource = filtered;

        if (ClientsTotalCountText != null)
            ClientsTotalCountText.Text = $"{_allClients.Count} чел.";
        if (ClientsWithSalesCountText != null)
            ClientsWithSalesCountText.Text = $"{_allSales.Count} сделок";
        if (ClientsActiveBookingsText != null)
            ClientsActiveBookingsText.Text = $"{_allBookings.Count} в резерве";
    }

    private void AddClient_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ClientEditDialog { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void EditClient_Click(object sender, RoutedEventArgs e)
    {
        if (ClientsDataGrid.SelectedItem is not Client client)
        {
            MessageBox.Show("Выберите клиента для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new ClientEditDialog(client) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void DeleteClient_Click(object sender, RoutedEventArgs e)
    {
        if (ClientsDataGrid.SelectedItem is not Client client)
        {
            MessageBox.Show("Выберите клиента для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Удалить клиента {client.FullName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            try
            {
                DatabaseService.Instance.DeleteClient(client.Id);
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExportClientsExcel_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Сохранить базу клиентов в Excel",
            Filter = "Книга Excel (*.xlsx)|*.xlsx",
            FileName = $"DRIVE_Clients_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                ReportExportService.Instance.ExportClientsToExcel(_allClients, sfd.FileName);
                PromptOpenFile(sfd.FileName, "База клиентов успешно выгружена в Excel!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region Sales
    private void SalesFilter_Changed(object sender, RoutedEventArgs e)
    {
        ApplySalesFilter();
    }

    private void ApplySalesFilter()
    {
        string q = SalesSearchBox?.Text?.Trim().ToLowerInvariant() ?? "";
        string? pay = SalesPaymentFilter?.SelectedItem?.ToString();

        var filtered = _allSales.Where(s =>
            (string.IsNullOrWhiteSpace(q) ||
             s.CarTitle.ToLowerInvariant().Contains(q) ||
             s.CarVin.ToLowerInvariant().Contains(q) ||
             s.ClientName.ToLowerInvariant().Contains(q) ||
             s.ClientPhone.ToLowerInvariant().Contains(q) ||
             s.ManagerLogin.ToLowerInvariant().Contains(q)) &&
            (string.IsNullOrWhiteSpace(pay) || pay == "Все способы оплаты" || string.Equals(s.PaymentMethod, pay, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        if (SalesDataGrid != null)
            SalesDataGrid.ItemsSource = filtered;

        decimal sum = filtered.Sum(s => s.Price);
        decimal avg = filtered.Count > 0 ? sum / filtered.Count : 0m;

        if (SalesSummaryText != null)
            SalesSummaryText.Text = $"Найдено: {filtered.Count} сдел.";
        if (SalesCountCardText != null)
            SalesCountCardText.Text = $"{filtered.Count} шт.";
        if (SalesSumCardText != null)
            SalesSumCardText.Text = $"{sum:N0} ₽";
        if (SalesAvgCardText != null)
            SalesAvgCardText.Text = $"{avg:N0} ₽";
    }

    private void AddSale_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaleDialog(_currentUser) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void PrintSaleReceipt_Click(object sender, RoutedEventArgs e)
    {
        if (SalesDataGrid.SelectedItem is not Sale sale)
        {
            MessageBox.Show("Выберите сделку из таблицы для печати договора/чека.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var car = _allCars.FirstOrDefault(c => c.Id == sale.CarId);
        string html = ReportExportService.Instance.GenerateSaleReceiptHtml(sale, car);
        ReportExportService.Instance.OpenInBrowserOrPrint(html);
    }

    private void ExportSalesExcel_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Сохранить реестр продаж в Excel",
            Filter = "Книга Excel (*.xlsx)|*.xlsx",
            FileName = $"DRIVE_Sales_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                ReportExportService.Instance.ExportSalesToExcel(_allSales, sfd.FileName);
                PromptOpenFile(sfd.FileName, "Реестр продаж успешно выгружен в Excel!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region Bookings
    private void BookingsSearch_Changed(object sender, TextChangedEventArgs e)
    {
        ApplyBookingsFilter();
    }

    private void ApplyBookingsFilter()
    {
        string q = BookingsSearchBox?.Text?.Trim().ToLowerInvariant() ?? "";
        var filtered = _allBookings.Where(b =>
            string.IsNullOrWhiteSpace(q) ||
            b.CarTitle.ToLowerInvariant().Contains(q) ||
            b.CarVin.ToLowerInvariant().Contains(q) ||
            b.ClientName.ToLowerInvariant().Contains(q) ||
            b.ClientPhone.ToLowerInvariant().Contains(q) ||
            b.ManagerLogin.ToLowerInvariant().Contains(q)).ToList();

        if (BookingsDataGrid != null)
            BookingsDataGrid.ItemsSource = filtered;
    }

    private void AddBooking_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new BookingDialog(_currentUser) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void ConvertBookingToSale_Click(object sender, RoutedEventArgs e)
    {
        if (BookingsDataGrid.SelectedItem is not Booking booking)
        {
            MessageBox.Show("Выберите бронирование для перевода в сделку.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var car = _allCars.FirstOrDefault(c => c.Id == booking.CarId);
        var client = _allClients.FirstOrDefault(c => c.Id == booking.ClientId);

        var dlg = new SaleDialog(_currentUser, car, client) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void CancelBooking_Click(object sender, RoutedEventArgs e)
    {
        if (BookingsDataGrid.SelectedItem is not Booking booking)
        {
            MessageBox.Show("Выберите бронирование для отмены.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Снять бронь на автомобиль {booking.CarTitle} за клиентом {booking.ClientName}?",
            "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            try
            {
                DatabaseService.Instance.CancelBooking(booking.Id, booking.CarId);
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отмены брони: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region Suppliers
    private void SupplierSearch_Changed(object sender, TextChangedEventArgs e)
    {
        ApplySupplierFilter();
    }

    private void ApplySupplierFilter()
    {
        string q = SupplierSearchBox?.Text?.Trim().ToLowerInvariant() ?? "";
        var filtered = _allSuppliers.Where(s =>
            string.IsNullOrWhiteSpace(q) ||
            s.Name.ToLowerInvariant().Contains(q) ||
            s.ContactPerson.ToLowerInvariant().Contains(q) ||
            s.Phone.ToLowerInvariant().Contains(q) ||
            s.Email.ToLowerInvariant().Contains(q) ||
            s.Address.ToLowerInvariant().Contains(q)).ToList();

        if (SuppliersDataGrid != null)
            SuppliersDataGrid.ItemsSource = filtered;
    }

    private void AddSupplier_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SupplierEditDialog { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void EditSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (SuppliersDataGrid.SelectedItem is not Supplier supplier)
        {
            MessageBox.Show("Выберите поставщика для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SupplierEditDialog(supplier) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void DeleteSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (SuppliersDataGrid.SelectedItem is not Supplier supplier)
        {
            MessageBox.Show("Выберите поставщика для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Удалить поставщика {supplier.Name}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            try
            {
                DatabaseService.Instance.DeleteSupplier(supplier.Id);
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления поставщика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region Reports & Analytics
    private void ExportFullFinancialReport_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Сохранить полный финансовый отчёт в Excel",
            Filter = "Книга Excel (*.xlsx)|*.xlsx",
            FileName = $"DRIVE_Financial_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                var stats = DatabaseService.Instance.GetDashboardStats();
                ReportExportService.Instance.ExportFullFinancialReportToExcel(stats, _allCars, _allSales, sfd.FileName);
                PromptOpenFile(sfd.FileName, "Генеральный финансовый отчёт успешно сформирован!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта финансового отчета: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PrintSummaryReport_Click(object sender, RoutedEventArgs e)
    {
        var stats = DatabaseService.Instance.GetDashboardStats();

        string brandsRows = string.Join("", stats.TopBrands.Select(b => $"<tr><td>{b.Key}</td><td>{b.Count} шт.</td><td>{b.Percentage:F1}%</td></tr>"));
        string paymentRows = string.Join("", stats.PaymentMethods.Select(p => $"<tr><td>{p.Key}</td><td>{p.Count} сделок</td><td>{p.Value:N0} ₽</td></tr>"));

        string html = $@"<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <title>Сводный отчёт автосалона «DRIVE»</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 40px; color: #0f172a; }}
        .header {{ display: flex; justify-content: space-between; border-bottom: 3px solid #3b82f6; padding-bottom: 15px; margin-bottom: 25px; }}
        .title {{ font-size: 26px; font-weight: 800; color: #1d4ed8; }}
        .kpi-grid {{ display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; margin-bottom: 30px; }}
        .kpi-card {{ background: #f8fafc; border: 1px solid #cbd5e1; border-radius: 8px; padding: 15px; text-align: center; }}
        .kpi-val {{ font-size: 24px; font-weight: 800; color: #0f172a; margin-top: 5px; }}
        .kpi-lbl {{ font-size: 11px; font-weight: 700; color: #64748b; text-transform: uppercase; }}
        table {{ width: 100%; border-collapse: collapse; margin-bottom: 25px; }}
        th, td {{ border: 1px solid #e2e8f0; padding: 10px 14px; text-align: left; font-size: 13px; }}
        th {{ background: #f1f5f9; font-weight: 700; }}
        .print-btn {{ position: fixed; top: 20px; right: 20px; background: #3b82f6; color: white; border: none; padding: 10px 20px; font-weight: bold; border-radius: 6px; cursor: pointer; }}
        @media print {{ .print-btn {{ display: none; }} body {{ margin: 0; }} }}
    </style>
</head>
<body>
    <button class=""print-btn"" onclick=""window.print()"">🖨 Печать / Сохранить в PDF</button>
    <div class=""header"">
        <div>
            <div class=""title"">АВТОСАЛОН «DRIVE»</div>
            <div style=""color:#64748b; font-size:13px;"">Сводный управленческий отчёт &bull; Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}</div>
        </div>
        <div style=""text-align:right; font-size:12px; color:#64748b;"">
            Пользователь: <strong>{_currentUser.Login} ({_currentUser.RoleDisplay})</strong><br>
            ООО «ДРАЙВ МОТОРС»
        </div>
    </div>

    <div class=""kpi-grid"">
        <div class=""kpi-card""><div class=""kpi-lbl"">Автомобилей на складе</div><div class=""kpi-val"">{stats.TotalCars}</div></div>
        <div class=""kpi-card""><div class=""kpi-lbl"">В наличии к продаже</div><div class=""kpi-val"" style=""color:#10b981;"">{stats.InStockCars}</div></div>
        <div class=""kpi-card""><div class=""kpi-lbl"">Продано автомобилей</div><div class=""kpi-val"" style=""color:#ef4444;"">{stats.SoldCars}</div></div>
        <div class=""kpi-card""><div class=""kpi-lbl"">Итоговая выручка</div><div class=""kpi-val"" style=""color:#3b82f6;"">{stats.FormattedTotalRevenue}</div></div>
    </div>

    <h3>Топ брендов на складе</h3>
    <table>
        <tr><th>Марка</th><th>Количество</th><th>Доля склада</th></tr>
        {brandsRows}
    </table>

    <h3>Структура оплат по сделкам</h3>
    <table>
        <tr><th>Способ расчета</th><th>Количество сделок</th><th>Сумма</th></tr>
        {paymentRows}
    </table>
</body>
</html>";

        ReportExportService.Instance.OpenInBrowserOrPrint(html);
    }
    #endregion

    #region Users (Admin only)
    private void AddUser_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new UserEditDialog { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void ChangeUserPassword_Click(object sender, RoutedEventArgs e)
    {
        if (UsersDataGrid.SelectedItem is not User user)
        {
            MessageBox.Show("Выберите пользователя для смены пароля.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new UserEditDialog(user) { Owner = this };
        if (dlg.ShowDialog() == true)
            RefreshAll();
    }

    private void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        if (UsersDataGrid.SelectedItem is not User user)
        {
            MessageBox.Show("Выберите пользователя для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (user.Id == _currentUser.Id)
        {
            MessageBox.Show("Вы не можете удалить свою текущую учетную запись.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show($"Удалить пользователя {user.Login}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            try
            {
                DatabaseService.Instance.DeleteUser(user.Id);
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    #endregion

    #region History & Audit
    private void HistorySearch_Changed(object sender, TextChangedEventArgs e)
    {
        ApplyHistoryFilter();
    }

    private void RefreshHistory_Click(object sender, RoutedEventArgs e)
    {
        RefreshAll();
    }

    private void ApplyHistoryFilter()
    {
        string q = HistorySearchBox?.Text?.Trim().ToLowerInvariant() ?? "";
        var filtered = _allHistory.Where(h =>
            string.IsNullOrWhiteSpace(q) ||
            h.Action.ToLowerInvariant().Contains(q) ||
            h.CarTitle.ToLowerInvariant().Contains(q) ||
            h.CreatedAt.ToLowerInvariant().Contains(q)).ToList();

        if (HistoryDataGrid != null)
            HistoryDataGrid.ItemsSource = filtered;
    }
    #endregion

    #region Database Backup & Restore
    private void BackupDb_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Создать резервную копию базы данных",
            Filter = "База данных SQLite (*.db)|*.db",
            FileName = $"avtosalon_backup_{DateTime.Now:yyyyMMdd_HHmm}.db"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                DatabaseService.Instance.BackupDatabase(sfd.FileName);
                MessageBox.Show($"Резервная копия успешно создана в файле:\n{sfd.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания бэкапа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RestoreDb_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Выберите файл резервной копии базы данных",
            Filter = "База данных SQLite (*.db)|*.db"
        };

        if (ofd.ShowDialog() == true)
        {
            if (MessageBox.Show("Текущие данные будут заменены данными из выбранного файла резервной копии. Продолжить?",
                "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseService.Instance.RestoreDatabase(ofd.FileName);
                    RefreshAll();
                    MessageBox.Show("База данных успешно восстановлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка восстановления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void PromptOpenFile(string filePath, string message)
    {
        if (MessageBox.Show($"{message}\n\nОткрыть сформированный файл прямо сейчас?", "Экспорт завершён", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }
    #endregion
}