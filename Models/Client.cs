namespace FDNJ_CSharp.Models;

public class Client
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;

    public string DisplayTitle => $"{FullName} ({Phone})".Trim();
}
