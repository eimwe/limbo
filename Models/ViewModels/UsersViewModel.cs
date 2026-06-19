namespace limbo.Models.ViewModels;

public class UsersViewModel
{
    public List<User> Users { get; set; } = new();
    public string? StatusMessage { get; set; }
}