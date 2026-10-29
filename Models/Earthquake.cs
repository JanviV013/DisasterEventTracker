namespace DisasterEventTracker.Models;

/// <summary>One earthquake read from the live USGS feed. This is NOT stored in the database.</summary>
public class Earthquake
{
    public string Id { get; set; } = string.Empty;
    public double? Magnitude { get; set; }
    public string Place { get; set; } = string.Empty;
    public DateTime Time { get; set; }          // UTC
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DepthKm { get; set; }
    public bool TsunamiFlag { get; set; }
    public string? Url { get; set; }
    public string? EventType { get; set; }
    public string? Status { get; set; }
}
