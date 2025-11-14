using System.Security.Claims;

namespace ABCRetailers_POE3_.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(idValue, out var userId))
        {
            return userId;
        }

        return null;
    }

    public static string? GetUserRole(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Role);
}

