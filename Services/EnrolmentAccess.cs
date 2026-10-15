using RaceDay.Api.Models;
using RaceDay.API.Models;
using System.Security.Claims;

namespace RaceDay.API.Services;

public static class EnrolmentAccess
{
    // The participant who owns the enrolment.
    public static bool OwnsEnrolment(this ClaimsPrincipal user, Enrolment enrolment) =>
        user.IsInRole(Roles.Participant) && enrolment.ParticipantId == user.GetProfileId();

    // The organiser who owns the event. Needs Category and Category.Event to be loaded.
    public static bool OrganisesEnrolment(this ClaimsPrincipal user, Enrolment enrolment) =>
        user.IsInRole(Roles.Organiser) && enrolment.Category.Event.OrganiserId == user.GetProfileId();

    public static bool CanAccess(this ClaimsPrincipal user, Enrolment enrolment) =>
        user.OwnsEnrolment(enrolment) || user.OrganisesEnrolment(enrolment);
}