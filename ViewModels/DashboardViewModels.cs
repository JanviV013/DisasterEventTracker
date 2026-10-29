using System.ComponentModel.DataAnnotations;
using DisasterEventTracker.Models;

namespace DisasterEventTracker.ViewModels;

/// <summary>The values from the dashboard filter form (all optional).</summary>
public class EarthquakeFilter
{
    [Range(-2.0, 10.0, ErrorMessage = "Minimum magnitude must be between -2 and 10.")]
    public double? MinMagnitude { get; set; }

    [Range(-2.0, 10.0, ErrorMessage = "Maximum magnitude must be between -2 and 10.")]
    public double? MaxMagnitude { get; set; }

    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    [StringLength(100, ErrorMessage = "Location search can be at most 100 characters.")]
    public string? Location { get; set; }
}

/// <summary>A watch list entry together with the earthquakes that currently match it.</summary>
public class WatchMatch
{
    public WatchListItem Item { get; set; } = null!;
    public List<Earthquake> Earthquakes { get; set; } = new();
}

public class DashboardViewModel
{
    public EarthquakeFilter Filter { get; set; } = new();
    public bool FiltersRestored { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime LoadedAt { get; set; } = DateTime.Now;

    // The earthquakes after filtering, newest first
    public List<Earthquake> Earthquakes { get; set; } = new();

    // Statistics (calculated from the filtered earthquakes)
    public int TotalCount { get; set; }
    public Earthquake? Strongest { get; set; }
    public double? AverageMagnitude { get; set; }
    public int Mag5Count { get; set; }
    public int TsunamiCount { get; set; }

    // Chart data
    public int[] MagnitudeBuckets { get; set; } = new int[5];
    public List<string> DayLabels { get; set; } = new();
    public List<int> DayCounts { get; set; } = new();

    // Logged-in user data
    public bool IsLoggedIn { get; set; }
    public HashSet<string> SavedIds { get; set; } = new();
    public List<WatchMatch> WatchMatches { get; set; } = new();
}

public class EarthquakeDetailsViewModel
{
    public Earthquake Earthquake { get; set; } = null!;
    public int? SavedEventId { get; set; }
}
