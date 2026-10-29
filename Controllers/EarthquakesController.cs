using System.Text.Json;
using DisasterEventTracker.Data;
using DisasterEventTracker.Extensions;
using DisasterEventTracker.Models;
using DisasterEventTracker.Services;
using DisasterEventTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisasterEventTracker.Controllers;

public class EarthquakesController : Controller
{
    private const string FilterSessionKey = "LastEarthquakeFilter";

    private readonly IEarthquakeService _earthquakes;
    private readonly AppDbContext _db;

    public EarthquakesController(IEarthquakeService earthquakes, AppDbContext db)
    {
        _earthquakes = earthquakes;
        _db = db;
    }

    // ---------------------------------------------------------------
    // Dashboard: filters + statistics + map + charts + table
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EarthquakeFilter filter)
    {
        filter ??= new EarthquakeFilter();
        var restored = false;

        if (Request.Query.Count == 0)
        {
            // State management (Session): bring back the filters used on the previous visit
            var json = HttpContext.Session.GetString(FilterSessionKey);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    filter = JsonSerializer.Deserialize<EarthquakeFilter>(json) ?? new EarthquakeFilter();
                    restored = HasAnyFilter(filter);
                }
                catch (JsonException)
                {
                    filter = new EarthquakeFilter();
                }
            }
        }
        else
        {
            // Validation that needs two fields at once
            if (filter.MinMagnitude.HasValue && filter.MaxMagnitude.HasValue
                && filter.MinMagnitude.Value > filter.MaxMagnitude.Value)
            {
                ModelState.AddModelError(string.Empty, "Minimum magnitude cannot be greater than maximum magnitude.");
            }
            if (filter.FromDate.HasValue && filter.ToDate.HasValue
                && filter.FromDate.Value.Date > filter.ToDate.Value.Date)
            {
                ModelState.AddModelError(string.Empty, "The 'From' date cannot be after the 'To' date.");
            }

            if (ModelState.IsValid)
            {
                HttpContext.Session.SetString(FilterSessionKey, JsonSerializer.Serialize(filter));
            }
        }

        var vm = new DashboardViewModel
        {
            Filter = filter,
            FiltersRestored = restored,
            IsLoggedIn = User.Identity?.IsAuthenticated == true
        };

        // Load the live USGS data
        List<Earthquake> all;
        try
        {
            all = (await _earthquakes.GetRecentAsync()).ToList();
        }
        catch (EarthquakeDataException ex)
        {
            vm.ErrorMessage = ex.Message;
            return View(vm);
        }

        // Apply the filters (skipped if the filter form had errors)
        var filtered = ModelState.IsValid ? ApplyFilter(all, filter) : all;
        vm.Earthquakes = filtered.OrderByDescending(e => e.Time).ToList();

        FillStatistics(vm);

        // Extra data for logged-in users
        if (vm.IsLoggedIn)
        {
            var userId = User.GetUserId();

            vm.SavedIds = (await _db.SavedEvents
                    .Where(s => s.UserId == userId)
                    .Select(s => s.EarthquakeId)
                    .ToListAsync())
                .ToHashSet();

            var watchItems = await _db.WatchListItems
                .Where(w => w.UserId == userId && w.IsActive)
                .OrderBy(w => w.Name)
                .ToListAsync();

            foreach (var item in watchItems)
            {
                var matches = FindWatchMatches(all, item);
                if (matches.Count > 0)
                {
                    vm.WatchMatches.Add(new WatchMatch { Item = item, Earthquakes = matches });
                }
            }
        }

        return View(vm);
    }

    // ---------------------------------------------------------------
    // Details of one earthquake
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        Earthquake? earthquake;
        try
        {
            earthquake = await _earthquakes.GetByIdAsync(id);
        }
        catch (EarthquakeDataException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        if (earthquake == null)
        {
            TempData["Error"] = "That earthquake is no longer in the 7-day USGS feed.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new EarthquakeDetailsViewModel { Earthquake = earthquake };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.GetUserId();
            var saved = await _db.SavedEvents
                .FirstOrDefaultAsync(s => s.UserId == userId && s.EarthquakeId == id);
            vm.SavedEventId = saved?.Id;
        }

        return View(vm);
    }

    // Forget the remembered filters
    [HttpGet]
    public IActionResult Reset()
    {
        HttpContext.Session.Remove(FilterSessionKey);
        return RedirectToAction(nameof(Index));
    }

    // Throw away the cached USGS data so the next page load downloads fresh data
    [HttpGet]
    public IActionResult Refresh()
    {
        _earthquakes.ClearCache();
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // Helper methods
    // ---------------------------------------------------------------
    private static bool HasAnyFilter(EarthquakeFilter f)
    {
        return f.MinMagnitude.HasValue || f.MaxMagnitude.HasValue
            || f.FromDate.HasValue || f.ToDate.HasValue
            || !string.IsNullOrWhiteSpace(f.Location);
    }

    private static List<Earthquake> ApplyFilter(List<Earthquake> all, EarthquakeFilter f)
    {
        IEnumerable<Earthquake> query = all;

        if (f.MinMagnitude.HasValue)
        {
            query = query.Where(e => e.Magnitude.HasValue && e.Magnitude.Value >= f.MinMagnitude.Value);
        }
        if (f.MaxMagnitude.HasValue)
        {
            query = query.Where(e => e.Magnitude.HasValue && e.Magnitude.Value <= f.MaxMagnitude.Value);
        }
        if (f.FromDate.HasValue)
        {
            query = query.Where(e => e.Time.Date >= f.FromDate.Value.Date);
        }
        if (f.ToDate.HasValue)
        {
            query = query.Where(e => e.Time.Date <= f.ToDate.Value.Date);
        }
        if (!string.IsNullOrWhiteSpace(f.Location))
        {
            var text = f.Location.Trim();
            query = query.Where(e => e.Place.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        return query.ToList();
    }

    private static List<Earthquake> FindWatchMatches(List<Earthquake> all, WatchListItem item)
    {
        return all
            .Where(e => e.Magnitude.HasValue
                        && e.Magnitude.Value >= item.MinMagnitude
                        && e.Place.Contains(item.RegionKeyword, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Magnitude)
            .ToList();
    }

    private static void FillStatistics(DashboardViewModel vm)
    {
        var list = vm.Earthquakes;
        var withMagnitude = list.Where(e => e.Magnitude.HasValue).ToList();

        vm.TotalCount = list.Count;
        vm.Strongest = withMagnitude.OrderByDescending(e => e.Magnitude!.Value).FirstOrDefault();
        vm.AverageMagnitude = withMagnitude.Count > 0
            ? (double?)withMagnitude.Average(e => e.Magnitude!.Value)
            : null;
        vm.Mag5Count = withMagnitude.Count(e => e.Magnitude!.Value >= 5);
        vm.TsunamiCount = list.Count(e => e.TsunamiFlag);

        // Chart 1: earthquakes per magnitude range
        var buckets = new int[5];   // below 2 | 2-3.9 | 4-4.9 | 5-5.9 | 6+
        foreach (var e in withMagnitude)
        {
            var m = e.Magnitude!.Value;
            if (m < 2) buckets[0]++;
            else if (m < 4) buckets[1]++;
            else if (m < 5) buckets[2]++;
            else if (m < 6) buckets[3]++;
            else buckets[4]++;
        }
        vm.MagnitudeBuckets = buckets;

        // Chart 2: earthquakes per day (UTC)
        var perDay = list
            .GroupBy(e => e.Time.Date)
            .OrderBy(g => g.Key)
            .ToList();
        vm.DayLabels = perDay.Select(g => g.Key.ToString("yyyy-MM-dd")).ToList();
        vm.DayCounts = perDay.Select(g => g.Count()).ToList();
    }
}
