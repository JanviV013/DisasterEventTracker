using DisasterEventTracker.Data;
using DisasterEventTracker.Extensions;
using DisasterEventTracker.Models;
using DisasterEventTracker.Services;
using DisasterEventTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisasterEventTracker.Controllers;

/// <summary>CRUD for the logged-in user's watch list (regions and minimum magnitudes they care about).</summary>
[Authorize]
public class WatchListController : Controller
{
    private readonly AppDbContext _db;
    private readonly IEarthquakeService _earthquakes;

    public WatchListController(AppDbContext db, IEarthquakeService earthquakes)
    {
        _db = db;
        _earthquakes = earthquakes;
    }

    // ---------------------------------------------------------------
    // READ (list), including how many current earthquakes match each watch
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();

        var items = await _db.WatchListItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderBy(w => w.Name)
            .ToListAsync();

        var vm = new WatchListIndexViewModel();

        IReadOnlyList<Earthquake>? current = null;
        try
        {
            current = await _earthquakes.GetRecentAsync();
        }
        catch (EarthquakeDataException ex)
        {
            vm.ErrorMessage = "Live data could not be loaded, so match counts are unavailable. " + ex.Message;
        }

        foreach (var item in items)
        {
            int? matches = null;
            if (current != null)
            {
                matches = current.Count(e => e.Magnitude.HasValue
                    && e.Magnitude.Value >= item.MinMagnitude
                    && e.Place.Contains(item.RegionKeyword, StringComparison.OrdinalIgnoreCase));
            }
            vm.Rows.Add(new WatchListRow { Item = item, MatchCount = matches });
        }

        return View(vm);
    }

    // ---------------------------------------------------------------
    // CREATE
    // ---------------------------------------------------------------
    [HttpGet]
    public IActionResult Create()
    {
        return View(new WatchListFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WatchListFormViewModel vm)
    {
        var userId = User.GetUserId();

        // Custom validation: the same region only once per user
        var keyword = vm.RegionKeyword?.Trim() ?? string.Empty;
        if (keyword.Length > 0
            && await _db.WatchListItems.AnyAsync(w => w.UserId == userId && w.RegionKeyword == keyword))
        {
            ModelState.AddModelError(nameof(vm.RegionKeyword), "You already have a watch for this region keyword.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var item = new WatchListItem
        {
            UserId = userId,
            Name = vm.Name.Trim(),
            RegionKeyword = keyword,
            MinMagnitude = vm.MinMagnitude,
            IsActive = vm.IsActive
        };

        _db.WatchListItems.Add(item);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Watch added.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // UPDATE
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.GetUserId();
        var item = await _db.WatchListItems
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }

        return View(new WatchListFormViewModel
        {
            Id = item.Id,
            Name = item.Name,
            RegionKeyword = item.RegionKeyword,
            MinMagnitude = item.MinMagnitude,
            IsActive = item.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WatchListFormViewModel vm)
    {
        var userId = User.GetUserId();
        var item = await _db.WatchListItems
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }

        var keyword = vm.RegionKeyword?.Trim() ?? string.Empty;
        if (keyword.Length > 0
            && await _db.WatchListItems.AnyAsync(w => w.UserId == userId && w.Id != id && w.RegionKeyword == keyword))
        {
            ModelState.AddModelError(nameof(vm.RegionKeyword), "You already have a watch for this region keyword.");
        }

        if (!ModelState.IsValid)
        {
            vm.Id = id;
            return View(vm);
        }

        item.Name = vm.Name.Trim();
        item.RegionKeyword = keyword;
        item.MinMagnitude = vm.MinMagnitude;
        item.IsActive = vm.IsActive;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Watch updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------
    // DELETE
    // ---------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        var item = await _db.WatchListItems
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

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
        var item = await _db.WatchListItems
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (item != null)
        {
            _db.WatchListItems.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Watch removed.";
        }
        return RedirectToAction(nameof(Index));
    }
}
