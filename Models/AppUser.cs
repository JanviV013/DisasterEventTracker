using System.ComponentModel.DataAnnotations;

namespace DisasterEventTracker.Models;

/// <summary>A registered user. Passwords are never stored, only a salted hash.</summary>
public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(300)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<SavedEvent> SavedEvents { get; set; } = new();
    public List<WatchListItem> WatchListItems { get; set; } = new();
}
