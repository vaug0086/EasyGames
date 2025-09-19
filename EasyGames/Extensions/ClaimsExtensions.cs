namespace EasyGames.Extensions
{
    //  adds an extension method getuserid to claimsprincipal so that controllers can call user.getuserid()
    //  it looks for the claimtypes.nameidentifier claim which is how asp.net core identity stores the user’s primary key
    //  returns the string id or null if not present
    using System.Security.Claims;
    public static class ClaimsExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
