using Microsoft.AspNetCore.Mvc;

namespace DisasterEventTracker.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Earthquakes");
    }

    /// <summary>State management with a cookie: remembers the light / dark theme for one year.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetTheme(string theme, string? returnUrl)
    {
        var value = theme == "dark" ? "dark" : "light";

        Response.Cookies.Append("theme", value, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax
        });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Earthquakes");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
