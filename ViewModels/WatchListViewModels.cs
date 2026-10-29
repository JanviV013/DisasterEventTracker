using System.ComponentModel.DataAnnotations;
using DisasterEventTracker.Models;

namespace DisasterEventTracker.ViewModels;

public class WatchListFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please give this watch a name.")]
    [StringLength(100, ErrorMessage = "The name can be at most 100 characters.")]
    [Display(Name = "Watch name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a region keyword, for example Japan.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "The region keyword must be 2 to 100 characters.")]
    [Display(Name = "Region keyword")]
    public string RegionKeyword { get; set; } = string.Empty;

    [Range(0.0, 10.0, ErrorMessage = "Minimum magnitude must be between 0 and 10.")]
    [Display(Name = "Minimum magnitude")]
    public double MinMagnitude { get; set; } = 5.0;

    [Display(Name = "Alerts on")]
    public bool IsActive { get; set; } = true;
}

public class WatchListRow
{
    public WatchListItem Item { get; set; } = null!;
    public int? MatchCount { get; set; }   // null when the live data could not be loaded
}

public class WatchListIndexViewModel
{
    public List<WatchListRow> Rows { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
