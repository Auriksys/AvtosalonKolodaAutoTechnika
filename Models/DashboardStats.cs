using System.Collections.Generic;

namespace FDNJ_CSharp.Models;

public class KeyValueStat
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
    public double Percentage { get; set; }
    public string SubText { get; set; } = string.Empty;
    public string FormattedValue => Value > 0 ? $"{Value:N0} ₽" : $"{Count} шт.";
}

public class DashboardStats
{
    public int TotalCars { get; set; }
    public int InStockCars { get; set; }
    public int BookedCars { get; set; }
    public int SoldCars { get; set; }
    public int InDiagnosticsOrRepairCars { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSalePrice { get; set; }
    public decimal TotalStockValue { get; set; }
    public int TotalClients { get; set; }
    public int TotalSuppliers { get; set; }
    public int TotalSalesCount { get; set; }

    public string FormattedTotalRevenue => $"{TotalRevenue:N0} ₽";
    public string FormattedAverageSalePrice => $"{AverageSalePrice:N0} ₽";
    public string FormattedTotalStockValue => $"{TotalStockValue:N0} ₽";

    public List<KeyValueStat> TopBrands { get; set; } = new();
    public List<KeyValueStat> PaymentMethods { get; set; } = new();
    public List<KeyValueStat> CarStatuses { get; set; } = new();
    public List<HistoryItem> RecentHistory { get; set; } = new();
}
