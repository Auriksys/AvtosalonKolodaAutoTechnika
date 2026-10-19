using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;

namespace FDNJ_CSharp.Views;

public partial class CarEditDialog : Window
{
    private readonly Car _car;
    private readonly bool _isEdit;
    private string? _selectedImagePath;

    public Car SavedCar => _car;

    public CarEditDialog(Car? existingCar = null)
    {
        InitializeComponent();

        _isEdit = existingCar != null;
        _car = existingCar ?? new Car
        {
            Year = DateTime.Today.Year,
            Status = "В наличии",
            ArrivalDate = DateTime.Today.ToString("yyyy-MM-dd")
        };

        _selectedImagePath = _car.ImagePath;

        PopulateComboboxes();
        LoadCarData();
    }

    private void PopulateComboboxes()
    {
        BodyTypeBox.ItemsSource = new[]
        {
            "Седан", "Хэтчбек", "Внедорожник", "Кроссовер", "Купе",
            "Универсал", "Лифтбек", "Минивэн", "Пикап", "Кабриолет"
        };

        TransmissionBox.ItemsSource = new[]
        {
            "АКПП (Автомат)", "МКПП (Механика)", "CVT (Вариатор)", "РКПП (Робот)"
        };

        DriveBox.ItemsSource = new[]
        {
            "Передний (FWD)", "Задний (RWD)", "Полный (AWD / 4WD)"
        };

        StatusBox.ItemsSource = new[]
        {
            "В наличии", "Забронирован", "Продан", "На диагностике", "На ремонте", "Ожидает поступления"
        };

        try
        {
            var suppliers = DatabaseService.Instance.GetSuppliers();
            SupplierBox.ItemsSource = suppliers;
        }
        catch { }
    }

    private void LoadCarData()
    {
        if (_isEdit)
        {
            TitleTextBlock.Text = $"Редактирование: {_car.Brand} {_car.Model}";
            VinBox.Text = _car.Vin;
            YearBox.Text = _car.Year.ToString();
            BrandBox.Text = _car.Brand;
            ModelBox.Text = _car.Model;
            BodyTypeBox.Text = _car.BodyType;
            ColorBox.Text = _car.Color;
            EngineBox.Text = _car.Engine;
            PowerBox.Text = _car.Power > 0 ? _car.Power.ToString() : "";
            TransmissionBox.Text = _car.Transmission;
            DriveBox.Text = _car.Drive;
            MileageBox.Text = _car.Mileage.ToString();
            PriceBox.Text = _car.Price.ToString("0");
            StatusBox.SelectedItem = _car.Status;
            if (_car.SupplierId.HasValue)
                SupplierBox.SelectedValue = _car.SupplierId.Value;
        }
        else
        {
            TitleTextBlock.Text = "Добавление нового автомобиля";
            YearBox.Text = DateTime.Today.Year.ToString();
            StatusBox.SelectedItem = "В наличии";
            MileageBox.Text = "0";
        }

        UpdateImagePreview();
    }

    private void UpdateImagePreview()
    {
        string? pathToLoad = null;

        if (!string.IsNullOrWhiteSpace(_selectedImagePath))
        {
            if (File.Exists(_selectedImagePath))
            {
                pathToLoad = _selectedImagePath;
            }
            else
            {
                string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cars", _selectedImagePath);
                if (File.Exists(local))
                    pathToLoad = local;
            }
        }

        if (string.IsNullOrWhiteSpace(pathToLoad))
        {
            string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cars", "default_car.png");
            if (File.Exists(fallback))
                pathToLoad = fallback;
        }

        if (!string.IsNullOrWhiteSpace(pathToLoad) && File.Exists(pathToLoad))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(pathToLoad, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                CarImagePreview.Source = bmp;
                return;
            }
            catch { }
        }

        CarImagePreview.Source = null;
    }

    private void ChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Выберите фотографию автомобиля",
            Filter = "Изображения (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Все файлы (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            try
            {
                string carsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cars");
                Directory.CreateDirectory(carsDir);

                string ext = Path.GetExtension(ofd.FileName);
                string newFileName = $"car_{Guid.NewGuid():N}{ext}";
                string destPath = Path.Combine(carsDir, newFileName);

                File.Copy(ofd.FileName, destPath, true);
                _selectedImagePath = newFileName;
                UpdateImagePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось скопировать изображение: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PasteImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Clipboard.ContainsImage())
            {
                var bitmapSource = Clipboard.GetImage();
                if (bitmapSource != null)
                {
                    string carsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cars");
                    Directory.CreateDirectory(carsDir);

                    string newFileName = $"car_clip_{Guid.NewGuid():N}.png";
                    string destPath = Path.Combine(carsDir, newFileName);

                    using (var fs = new FileStream(destPath, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(fs);
                    }

                    _selectedImagePath = newFileName;
                    UpdateImagePreview();
                    return;
                }
            }

            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                if (files.Count > 0 && files[0] is string firstFile && File.Exists(firstFile))
                {
                    string carsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cars");
                    Directory.CreateDirectory(carsDir);
                    string ext = Path.GetExtension(firstFile);
                    string newFileName = $"car_{Guid.NewGuid():N}{ext}";
                    string destPath = Path.Combine(carsDir, newFileName);
                    File.Copy(firstFile, destPath, true);
                    _selectedImagePath = newFileName;
                    UpdateImagePreview();
                    return;
                }
            }

            MessageBox.Show("В буфере обмена нет скопированной картинки.\nСкопируйте изображение в браузере или сделайте скриншот (Win+Shift+S).",
                "Буфер обмена", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка вставки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetImage_Click(object sender, RoutedEventArgs e)
    {
        _selectedImagePath = null;
        UpdateImagePreview();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string vin = VinBox.Text.Trim().ToUpperInvariant();
        string brand = BrandBox.Text.Trim();
        string model = ModelBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(vin) || vin.Length < 5)
        {
            MessageBox.Show("VIN номер должен содержать не менее 5 символов.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            VinBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            MessageBox.Show("Укажите марку автомобиля.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            BrandBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            MessageBox.Show("Укажите модель автомобиля.", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            ModelBox.Focus();
            return;
        }

        if (!int.TryParse(YearBox.Text.Trim(), out int year) || year < 1886 || year > DateTime.Today.Year + 2)
        {
            MessageBox.Show($"Введите корректный год выпуска (от 1886 до {DateTime.Today.Year + 1}).", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            YearBox.Focus();
            return;
        }

        if (!decimal.TryParse(PriceBox.Text.Trim().Replace(" ", ""), out decimal price) || price <= 0)
        {
            MessageBox.Show("Введите корректную цену (больше 0).", "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
            PriceBox.Focus();
            return;
        }

        int.TryParse(PowerBox.Text.Trim(), out int power);
        int.TryParse(MileageBox.Text.Trim().Replace(" ", ""), out int mileage);

        _car.Vin = vin;
        _car.Brand = brand;
        _car.Model = model;
        _car.Year = year;
        _car.BodyType = BodyTypeBox.Text.Trim();
        _car.Color = ColorBox.Text.Trim();
        _car.Engine = EngineBox.Text.Trim();
        _car.Power = Math.Max(0, power);
        _car.Transmission = TransmissionBox.Text.Trim();
        _car.Drive = DriveBox.Text.Trim();
        _car.Mileage = Math.Max(0, mileage);
        _car.Price = price;
        _car.Status = StatusBox.SelectedItem?.ToString() ?? "В наличии";
        _car.SupplierId = SupplierBox.SelectedValue is int sid ? sid : null;
        _car.ImagePath = _selectedImagePath;

        try
        {
            if (_isEdit)
            {
                DatabaseService.Instance.UpdateCar(_car);
            }
            else
            {
                _car.Id = DatabaseService.Instance.AddCar(_car);
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения автомобиля: {ex.Message}", "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
