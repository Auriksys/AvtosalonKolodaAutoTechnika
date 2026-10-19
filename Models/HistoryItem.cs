namespace FDNJ_CSharp.Models;

public class HistoryItem
{
    public int Id { get; set; }
    public int? CarId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string CarTitle { get; set; } = "—";

    public string FormattedDate
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CreatedAt)) return "—";
            if (CreatedAt.Length >= 16) return CreatedAt.Substring(0, 16).Replace("T", " ");
            return CreatedAt;
        }
    }
}
