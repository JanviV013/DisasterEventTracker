using System.ComponentModel.DataAnnotations;

namespace DisasterEventTracker.Models;

/// <summary>An earthquake that a user saved to their tracker, with their own note, priority and status.</summary>
public class SavedEvent
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    // ----- Copied from USGS at the moment of saving (USGS only keeps 7 days of data) -----
    [Required, StringLength(50)]
    public string EarthquakeId { get; set; } = string.Empty;

    public double? Magnitude { get; set; }

    [Required, StringLength(250)]
    public string Place { get; set; } = string.Empty;

    public DateTime EventTime { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DepthKm { get; set; }

    [StringLength(500)]
    public string? SourceUrl { get; set; }

    // ----- The user's own review information (editable) -----
    [StringLength(300)]
    public string? Note { get; set; }

    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
    public EventStatus Status { get; set; } = EventStatus.Monitoring;

    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
