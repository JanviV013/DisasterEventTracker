using DisasterEventTracker.Data;
using DisasterEventTracker.Extensions;
using DisasterEventTracker.Models;
using DisasterEventTracker.Services;
using DisasterEventTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisasterEventTracker.Controllers;

/// <summary>
/// CRUD for the logged-in user's saved earthquakes.
/// Every query includes "UserId == current user", so nobody can read or change another user's data.
/// </summary>
[Authorize]
public class SavedEventsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IEarthquakeService _earthquakes;

    public SavedEventsController(AppDbContext db, IEarthquakeService earthquakes)
    {
        _db = db;
        _earthquakes = earthquakes;
    }

    // ---------------------------------------------------------------
    // READ (list)
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index(EventStatus? status, PriorityLevel? priority)
    {
        var userId = User.GetUserId();

        var all = await _db.SavedEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .ToListAsync();

        IEnumerable<SavedEvent> filtered = all;
        if (status.HasValue)
        {
            filtered = filtered.Where(e => e.Status == status.Value);
        }
        if (priority.HasValue)
        {
            filtered = filtered.Where(e => e.Priority == priority.Value);
        }

        var vm = new SavedEventsIndexViewModel
        {
            Items = filtered.OrderByDescending(e => e.SavedAt).ToList(),
            StatusFilter = status,
            PriorityFilter = priority,
            TotalSaved = all.Count,
            MonitoringCount = all.Count(e => e.Status == EventStatus.Monitoring),
            ReviewedCount = all.Count(e => e.Status == EventStatus.Reviewed),
            ClosedCount = all.Count(e => e.Status == EventStatus.Closed),
            HighPriorityCount = all.Count(e => e.Priority == PriorityLevel.High),
            StrongestSaved = all
                .Where(e => e.Magnitude.HasValue)
                .OrderByDescending(e => e.Magnitude)
                .FirstOrDefault()
        };

        return View(vm);
    }

    // ---------------------------------------------------------------
    // READ (one item)
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.GetUserId();
        var item = await _db.SavedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }
        return View(item);
    }

    // ---------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Create(string? earthquakeId)
    {
        if (string.IsNullOrWhiteSpace(earthquakeId))
        {
            return RedirectToAction("Index", "Earthquakes");
        }

        var userId = User.GetUserId();

        // Already saved? Then go to the edit page instead of creating a duplicate
        var existing = await _db.SavedEvents
            .FirstOrDefaultAsync(e => e.UserId == userId && e.EarthquakeId == earthquakeId);
        if (existing != null)
        {
            TempData["Info"] = "You have already saved this earthquake. You can edit it here.";
            return RedirectToAction(nameof(Edit), new { id = existing.Id });
        }

        var (earthquake, error) = await LoadEarthquakeAsync(earthquakeId);
        if (earthquake == null)
        {
            TempData["Error"] = error;
            return RedirectToAction("Index", "Earthquakes");
        }

        var vm = new SavedEventFormViewModel
        {
            EarthquakeId = earthquakeId,
            Info = ToInfo(earthquake)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SavedEventFormViewModel vm)
    {
        var userId = User.GetUserId();

        // The earthquake details are loaded again on the server, never trusted from the browser
        var (earthquake, error) = await LoadEarthquakeAsync(vm.EarthquakeId);
        if (earthquake == null)
        {
            ModelState.AddModelError(string.Empty, error ?? "This earthquake could not be loaded, so it cannot be saved.");
            return View(vm);
        }

        // Custom validation: no duplicates
        var alreadySaved = await _db.SavedEvents
            .AnyAsync(e => e.UserId == userId && e.EarthquakeId == vm.EarthquakeId);
        if (alreadySaved)
        {
            ModelState.AddModelError(string.Empty, "You have already saved this earthquake.");
        }

        if (!ModelState.IsValid)
        {
            vm.Info = ToInfo(earthquake);
            return View(vm);
        }

        var entity = new SavedEvent
        {
            UserId = userId,
            EarthquakeId = earthquake.Id,
            Magnitude = earthquake.Magnitude,
            Place = earthquake.Place.Length > 250 ? earthquake.Place[..250] : earthquake.Place,
            EventTime = earthquake.Time,
            Latitude = earthquake.Latitude,
            Longitude = earthquake.Longitude,
            DepthKm = earthquake.DepthKm,
            SourceUrl = earthquake.Url,
            Note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim(),
            Priority = vm.Priority,
            Status = vm.Status
        };

        _db.SavedEvents.Add(entity);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // For example the unique index rejected a duplicate, or the database went away
            ModelState.AddModelError(string.Empty, "The event could not be saved. Please try again.");
            vm.Info = ToInfo(earthquake);
            return View(vm);
        }

        TempData["Success"] = "Earthquake saved to your tracker.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.GetUserId();
        var entity = await _db.SavedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entity == null)
        {
            return NotFound();
        }

        var vm = new SavedEventFormViewModel
        {
            Id = entity.Id,
            EarthquakeId = entity.EarthquakeId,
            Note = entity.Note,
            Priority = entity.Priority,
            Status = entity.Status,
            Info = ToInfo(entity)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SavedEventFormViewModel vm)
    {
        var userId = User.GetUserId();
        var entity = await _db.SavedEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entity == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            vm.Id = id;
            vm.EarthquakeId = entity.EarthquakeId;
            vm.Info = ToInfo(entity);
            return View(vm);
        }

        // Only these three fields can be changed
        entity.Note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim();
        entity.Priority = vm.Priority;
        entity.Status = vm.Status;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The changes could not be saved. Please try again.");
            vm.Id = id;
            vm.EarthquakeId = entity.EarthquakeId;
            vm.Info = ToInfo(entity);
            return View(vm);
        }

        TempData["Success"] = "Saved event updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // DELETE (confirmation page, then the real delete)
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var item = await _db.SavedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }
        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = User.GetUserId();
        var item = await _db.SavedEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (item != null)
        {
            _db.SavedEvents.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Saved event removed.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    private async Task<(Earthquake? Earthquake, string? Error)> LoadEarthquakeAsync(string earthquakeId)
    {
        try
        {
            var earthquake = await _earthquakes.GetByIdAsync(earthquakeId);
            if (earthquake == null)
            {
                return (null, "That earthquake is no longer in the 7-day USGS feed, so it cannot be saved.");
            }
            return (earthquake, null);
        }
        catch (EarthquakeDataException ex)
        {
            return (null, ex.Message);
        }
    }

    private static EventInfo ToInfo(Earthquake e) => new EventInfo
    {
        Magnitude = e.Magnitude,
        Place = e.Place,
        EventTime = e.Time,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        DepthKm = e.DepthKm,
        SourceUrl = e.Url
    };

    private static EventInfo ToInfo(SavedEvent e) => new EventInfo
    {
        Magnitude = e.Magnitude,
        Place = e.Place,
        EventTime = e.EventTime,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        DepthKm = e.DepthKm,
        SourceUrl = e.SourceUrl
    };
}
