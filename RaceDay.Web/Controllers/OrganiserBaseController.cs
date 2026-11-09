using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Base class for every Organiser page. Only logged-in Organisers can reach controllers that inherit it.</summary>
[SessionAuthorize(Roles.Organiser)]
public abstract class OrganiserBaseController : AppController
{
    protected IRaceDayApi Api { get; }
    protected ICurrentUser Current { get; }

    protected OrganiserBaseController(IRaceDayApi api, ICurrentUser current)
    {
        Api = api;
        Current = current;
    }

    /// <summary>Loads an event with its categories and checks it belongs to the logged-in Organiser.</summary>
    protected async Task<(EventResponse? Event, IActionResult? Failure)> LoadOwnEventAsync(int eventId)
    {
        var result = await Api.GetAsync<EventResponse>($"api/events/{eventId}");

        if (!result.Success || result.Data is null)
            return (null, HandleFailure(result, RedirectToAction("Index", "OrganiserEvents")));

        if (result.Data.OrganiserId != Current.User!.ProfileId)
        {
            ShowError("You can only manage your own events.");
            return (null, RedirectToAction("Index", "OrganiserEvents"));
        }

        return (result.Data, null);
    }
}