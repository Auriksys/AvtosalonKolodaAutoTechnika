using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FDNJ_CSharp.Models;
using FDNJ_CSharp.Services;

namespace FDNJ_CSharp.Views;

public partial class CarPhotoDialog : Window
{
    private readonly Car _car;
    private string? _selectedImagePath;

    public CarPhotoDialog(Car car)
    {
        InitializeComponent();
        _car = car;
        _selectedImagePath = car.ImagePath;

        CarTitleText.Text = $"Фото: {car.Brand} {car.Model} ({car.Year} г.)";
        if (!string.IsNullOrWhiteSpace(_selectedImagePath) &&
            (_selectedImagePath.StartsWith("http://") || _selectedImagePath.StartsWith("https://")))
        {
            UrlTextBox.Text = _selectedImagePath;
        }

        UpdatePreview();
    }

    private string GetAssetsCarsDir()
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cars");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private void MirrorToProjectAssets(string sourceFilePath, string fileName)
    {
        try
        {
            DirectoryInfo? parent = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 4 && parent != null; i++)
            {
                string projFile = Path.Combine(parent.FullName, "FDNJ_CSharp.csproj");
                if (File.Exists(projFile))
                {
                    string projAssets = Path.Combine(parent.FullName, "Assets", "Cars");
                    Directory.CreateDirectory(projAssets);
                    string target = Path.Combine(projAssets, fileName);
                    if (!string.Equals(Path.GetFullPath(sourceFilePath), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(sourceFilePath, target, true);
                    }
                    break;
                }
                parent = parent.Parent;
            }
        }
        catch { }
    }

    private void UpdatePreview()
    {
        try
        {
            var tempCar = new Car { ImagePath = _selectedImagePath };
            string resolved = tempCar.DisplayImagePath;

            if (!string.IsNullOrWhiteSpace(resolved))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(resolved, UriKind.Absolute);
                bmp.EndInit();
                PreviewImage.Source = bmp;
                return;
            }
        }
        catch { }

        PreviewImage.Source = null;
    }

    private void ImportLocalFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Файл не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string[] allowed = { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".gif" };
            if (!allowed.Contains(ext))
            {
                MessageBox.Show("Поддерживаются форматы изображений: PNG, JPG, JPEG, WEBP, BMP.", "Формат файла", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newFileName = $"car_{_car.Id}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            string destPath = Path.Combine(GetAssetsCarsDir(), newFileName);

            File.Copy(filePath, destPath, true);
            MirrorToProjectAssets(destPath, newFileName);

            _selectedImagePath = newFileName;
            StatusHintText.Text = $"✅ Загружен файл: {Path.GetFileName(filePath)}";
            UpdatePreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка импорта файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChooseFile_Click(object sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Выберите картинку автомобиля",
            Filter = "Изображения (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|Все файлы (*.*)|*.*"
        };

        if (ofd.ShowDialog() == true)
        {
            ImportLocalFile(ofd.FileName);
        }
    }

    private void PasteClipboard_Click(object sender, RoutedEventArgs e)
    {
        TryPasteFromClipboard();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (!UrlTextBox.IsFocused)
            {
                TryPasteFromClipboard();
                e.Handled = true;
            }
        }
    }

    private void TryPasteFromClipboard()
    {
        try
        {
            // 1. Direct bitmap in clipboard (e.g. "Copy Image" from browser or Screenshot)
            if (Clipboard.ContainsImage())
            {
                var bitmapSource = Clipboard.GetImage();
                if (bitmapSource != null)
                {
                    string newFileName = $"car_clip_{_car.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    string destPath = Path.Combine(GetAssetsCarsDir(), newFileName);

                    using (var fileStream = new FileStream(destPath, FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                        encoder.Save(fileStream);
                    }

                    MirrorToProjectAssets(destPath, newFileName);
                    _selectedImagePath = newFileName;
                    StatusHintText.Text = "✅ Картинка успешно вставлена из буфера обмена!";
                    UpdatePreview();
                    return;
                }
            }

            // 2. Copied file in Windows Explorer
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                if (files.Count > 0 && files[0] is string firstFile)
                {
                    ImportLocalFile(firstFile);
                    return;
                }
            }

            // 3. Copied URL or file path text
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText().Trim().Trim('"');
                if (File.Exists(text))
                {
                    ImportLocalFile(text);
                    return;
                }
                if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    UrlTextBox.Text = text;
                    LoadFromUrl(text);
                    return;
                }
            }

            MessageBox.Show("В буфере обмена нет скопированной картинки или файла.\n\nВы можете скопировать любую картинку в браузере (ПКМ -> Копировать картинку) или сделать скриншот (Win+Shift+S) и нажать эту кнопку.",
                "Буфер обмена", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось вставить из буфера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                ImportLocalFile(files[0]);
            }
        }
    }

    private void LoadUrl_Click(object sender, RoutedEventArgs e)
    {
        string url = UrlTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            MessageBox.Show("Введите ссылку (URL) на изображение.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        LoadFromUrl(url);
    }

    private async void LoadFromUrl(string url)
    {
        try
        {
            StatusHintText.Text = "⏳ Загрузка изображения по ссылке...";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            byte[] bytes = await client.GetByteArrayAsync(url);

            string newFileName = $"car_web_{_car.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            string destPath = Path.Combine(GetAssetsCarsDir(), newFileName);
            await File.WriteAllBytesAsync(destPath, bytes);
            MirrorToProjectAssets(destPath, newFileName);

            _selectedImagePath = newFileName;
            StatusHintText.Text = "✅ Картинка скачана по ссылке и сохранена локально!";
            UpdatePreview();
        }
        catch
        {
            // Fallback to direct URI
            _selectedImagePath = url;
            StatusHintText.Text = "✅ Ссылка привязана!";
            UpdatePreview();
        }
    }

    private void ResetDefault_Click(object sender, RoutedEventArgs e)
    {
        _selectedImagePath = "default_car.png";
        StatusHintText.Text = "Установлена стандартная заставка DRIVE";
        UpdatePreview();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DatabaseService.Instance.UpdateCarImage(_car.Id, _selectedImagePath);
            _car.ImagePath = _selectedImagePath;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения фотографии: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
