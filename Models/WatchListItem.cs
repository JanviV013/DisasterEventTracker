using System.ComponentModel.DataAnnotations;

namespace DisasterEventTracker.Models;

/// <summary>A region the user wants to watch, for example "Japan, magnitude 5 and above".</summary>
public class WatchListItem
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Text searched for inside the USGS place name, e.g. "Japan" or "Alaska".</summary>
    [Required, StringLength(100)]
    public string RegionKeyword { get; set; } = string.Empty;

    public double MinMagnitude { get; set; } = 5.0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
