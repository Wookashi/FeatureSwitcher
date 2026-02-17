using System.Security.Claims;

namespace Wookashi.FeatureSwitcher.Manager.Api.Extensions;

public static class ClaimsExtensions
{
    extension(ClaimsPrincipal user)
    {
        public int GetUserId()
            => int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        public string GetUserName()
            => user.FindFirst(ClaimTypes.Name)?.Value ?? "";

        public string GetUserRole()
            => user.FindFirst(ClaimTypes.Role)?.Value ?? "";
    }
}