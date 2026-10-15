using System.Security.Claims;

namespace RaceDay.API.Services;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // For an Organiser this is their OrganiserId; for a Participant their ParticipantId.
    public static int GetProfileId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue("profileId")!);
}