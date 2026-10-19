using System;
using System.IO;

namespace FDNJ_CSharp.Models;

public class Car
{
    public int Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string BodyType { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public int Power { get; set; }
    public string Transmission { get; set; } = string.Empty;
    public string Drive { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = "В наличии";
    public string ArrivalDate { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? ImagePath { get; set; }

    public string Title => $"{Brand} {Model}".Trim();
    public string FullTitle => $"{Brand} {Model} ({Year})".Trim();
    public string FormattedPrice => $"{Price:N0} ₽";
    public string FormattedMileage => Mileage == 0 ? "Без пробега" : $"{Mileage:N0} км";
    public string FormattedPower => Power > 0 ? $"{Power} л.с." : "—";
    public string EngineAndPower => string.IsNullOrWhiteSpace(Engine) ? FormattedPower : $"{Engine} ({FormattedPower})";
    public string MonthlyPaymentFormatted => $"В кредит: от {(Price * 0.85m / 60m):N0} ₽/мес";

    public string SpecsBrief => $"{Year} г. • {FormattedPower} • {Transmission} • {Drive}".Replace(" •  • ", " • ").Trim(' ', '•');

    public string StatusBackground => Status switch
    {
        "В наличии" => "#10B981",          // Emerald Green
        "Забронирован" => "#F59E0B",       // Amber
        "Продан" => "#EF4444",             // Red
        "На диагностике" => "#3B82F6",     // Blue
        "На ремонте" => "#8B5CF6",         // Purple
        "Ожидает поступления" => "#64748B",// Slate
        _ => "#475569"
    };

    public string StatusForeground => "#FFFFFF";

    public string DisplayImagePath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ImagePath))
            {
                if (ImagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    ImagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return ImagePath;
                }

                if (File.Exists(ImagePath))
                    return ImagePath;

                // Check relative to AppDomain or working dir
                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImagePath);
                if (File.Exists(localPath))
                    return localPath;

                string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cars", ImagePath);
                if (File.Exists(assetsPath))
                    return assetsPath;
            }

            // Fallback default
            string defaultAsset = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cars", "default_car.png");
            if (File.Exists(defaultAsset))
                return defaultAsset;

            return string.Empty;
        }
    }
}
