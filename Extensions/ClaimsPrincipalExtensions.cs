using System.Security.Claims;

namespace DisasterEventTracker.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the logged-in user's database Id (0 if nobody is logged in).</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }
}
