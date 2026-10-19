using System;

namespace FDNJ_CSharp.Models;

public class Booking
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public int ClientId { get; set; }
    public int? ManagerId { get; set; }
    public string BookingDate { get; set; } = string.Empty;
    public string ExpirationDate { get; set; } = string.Empty;

    // Joined details
    public string CarTitle { get; set; } = string.Empty;
    public string CarVin { get; set; } = string.Empty;
    public decimal CarPrice { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string ManagerLogin { get; set; } = string.Empty;

    public bool IsExpired
    {
        get
        {
            if (DateTime.TryParse(ExpirationDate, out var exp))
                return exp.Date < DateTime.Today;
            return false;
        }
    }

    public string StatusText => IsExpired ? "Истекла" : "Активна";
    public string StatusBackground => IsExpired ? "#EF4444" : "#F59E0B";
}
