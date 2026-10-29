using System.Text.Json;
using DisasterEventTracker.Models;
using Microsoft.Extensions.Caching.Memory;

namespace DisasterEventTracker.Services;

/// <summary>Thrown when the earthquake data cannot be loaded. The message is safe to show to users.</summary>
public class EarthquakeDataException : Exception
{
    public EarthquakeDataException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public interface IEarthquakeService
{
    Task<IReadOnlyList<Earthquake>> GetRecentAsync(CancellationToken cancellationToken = default);
    Task<Earthquake?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    void ClearCache();
}

/// <summary>Downloads the last 7 days of earthquakes from USGS (async) and caches them for 2 minutes.</summary>
public class EarthquakeService : IEarthquakeService
{
    private const string CacheKey = "usgs_earthquakes_week";
    private const string DefaultFeedUrl = "https://earthquake.usgs.gov/earthquakes/feed/v1.0/summary/all_week.geojson";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;

    public EarthquakeService(HttpClient http, IMemoryCache cache, IConfiguration config)
    {
        _http = http;
        _cache = cache;
        _config = config;
    }

    public async Task<IReadOnlyList<Earthquake>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out List<Earthquake>? cached) && cached != null)
        {
            return cached;
        }

        var url = _config["Usgs:FeedUrl"] ?? DefaultFeedUrl;

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var earthquakes = new List<Earthquake>();
            foreach (var feature in document.RootElement.GetProperty("features").EnumerateArray())
            {
                var earthquake = ParseFeature(feature);
                if (earthquake != null)
                {
                    earthquakes.Add(earthquake);
                }
            }

            _cache.Set(CacheKey, earthquakes, CacheDuration);
            return earthquakes;
        }
        catch (TaskCanceledException ex)
        {
            throw new EarthquakeDataException("The USGS server took too long to respond. Please try again.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new EarthquakeDataException("Could not reach the USGS server. Please check your internet connection.", ex);
        }
        catch (Exception ex) when (ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException)
        {
            throw new EarthquakeDataException("USGS returned earthquake data in an unexpected format.", ex);
        }
    }

    public async Task<Earthquake?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var all = await GetRecentAsync(cancellationToken);
        return all.FirstOrDefault(e => e.Id == id);
    }

    public void ClearCache()
    {
        _cache.Remove(CacheKey);
    }

    // ---------------------------------------------------------------
    // Reading one GeoJSON "feature" (one earthquake)
    // ---------------------------------------------------------------
    private static Earthquake? ParseFeature(JsonElement feature)
    {
        if (!feature.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        if (!feature.TryGetProperty("geometry", out var geometry) || geometry.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        if (!geometry.TryGetProperty("coordinates", out var coords)
            || coords.ValueKind != JsonValueKind.Array
            || coords.GetArrayLength() < 2)
        {
            return null;
        }

        var id = ReadString(feature, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        // GeoJSON order is [longitude, latitude, depth]
        if (coords[0].ValueKind != JsonValueKind.Number || coords[1].ValueKind != JsonValueKind.Number)
        {
            return null;
        }
        var longitude = coords[0].GetDouble();
        var latitude = coords[1].GetDouble();
        var depth = coords.GetArrayLength() > 2 && coords[2].ValueKind == JsonValueKind.Number
            ? coords[2].GetDouble()
            : 0.0;

        long timeMs = 0;
        if (props.TryGetProperty("time", out var timeElement) && timeElement.ValueKind == JsonValueKind.Number)
        {
            timeMs = timeElement.GetInt64();
        }

        var tsunami = props.TryGetProperty("tsunami", out var tsunamiElement)
            && tsunamiElement.ValueKind == JsonValueKind.Number
            && tsunamiElement.GetInt32() == 1;

        return new Earthquake
        {
            Id = id,
            Magnitude = ReadDouble(props, "mag"),            // can be null in the USGS data
            Place = ReadString(props, "place") ?? "Unknown location",
            Time = DateTimeOffset.FromUnixTimeMilliseconds(timeMs).UtcDateTime,
            Latitude = latitude,
            Longitude = longitude,
            DepthKm = depth,
            TsunamiFlag = tsunami,
            Url = ReadString(props, "url"),
            EventType = ReadString(props, "type"),
            Status = ReadString(props, "status")
        };
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }
        return null;
    }

    private static double? ReadDouble(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.GetDouble();
        }
        return null;
    }
}
