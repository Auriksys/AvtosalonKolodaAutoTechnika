namespace FDNJ_CSharp.Models;

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "manager"; // admin, manager, director

    public string RoleDisplay => Role.ToLower() switch
    {
        "admin" => "Администратор",
        "director" => "Директор",
        "manager" => "Менеджер",
        _ => Role
    };

    public bool IsAdmin => string.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase);
    public bool IsDirector => string.Equals(Role, "director", StringComparison.OrdinalIgnoreCase);
    public bool CanManageUsers => IsAdmin;
    public bool CanViewFinancials => IsAdmin || IsDirector;
}
