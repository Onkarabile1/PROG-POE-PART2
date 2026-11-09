using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserController : OrganiserBaseController
{
    public OrganiserController(IRaceDayApi api, ICurrentUser current) : base(api, current)
    {
    }

    public async Task<IActionResult> Dashboard()
    {
        var result = await Api.GetAsync<List<EventResponse>>("api/events/mine");

        if (!result.Success || result.Data is null)
            return HandleFailure(result, View(new DashboardViewModel()));

        return View(new DashboardViewModel { Events = result.Data });
    }
}