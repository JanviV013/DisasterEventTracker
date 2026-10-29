using System.ComponentModel.DataAnnotations;
using DisasterEventTracker.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DisasterEventTracker.ViewModels;

/// <summary>Read-only earthquake information shown on the Create / Edit pages (never posted back).</summary>
public class EventInfo
{
    public double? Magnitude { get; set; }
    public string Place { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DepthKm { get; set; }
    public string? SourceUrl { get; set; }
}

/// <summary>The form for saving a new event and editing a saved one. Only these fields can be changed by the user.</summary>
public class SavedEventFormViewModel
{
    public int Id { get; set; }

    [Required]
    public string EarthquakeId { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "The note can be at most 300 characters.")]
    [Display(Name = "Your note")]
    public string? Note { get; set; }

    [EnumDataType(typeof(PriorityLevel), ErrorMessage = "Please choose a valid priority.")]
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    [EnumDataType(typeof(EventStatus), ErrorMessage = "Please choose a valid status.")]
    public EventStatus Status { get; set; } = EventStatus.Monitoring;

    // Display only: filled by the controller, never taken from the browser
    [BindNever, ValidateNever]
    public EventInfo? Info { get; set; }
}

public class SavedEventsIndexViewModel
{
    public List<SavedEvent> Items { get; set; } = new();
    public EventStatus? StatusFilter { get; set; }
    public PriorityLevel? PriorityFilter { get; set; }

    public int TotalSaved { get; set; }
    public int MonitoringCount { get; set; }
    public int ReviewedCount { get; set; }
    public int ClosedCount { get; set; }
    public int HighPriorityCount { get; set; }
    public SavedEvent? StrongestSaved { get; set; }
}
