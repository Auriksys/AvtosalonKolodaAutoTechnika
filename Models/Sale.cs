namespace FDNJ_CSharp.Models;

public class Sale
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public int ClientId { get; set; }
    public int? ManagerId { get; set; }
    public string SaleDate { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string PaymentMethod { get; set; } = "Наличные";

    // Joined details
    public string CarTitle { get; set; } = string.Empty;
    public string CarVin { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public string ManagerLogin { get; set; } = string.Empty;

    public string FormattedPrice => $"{Price:N0} ₽";
}
