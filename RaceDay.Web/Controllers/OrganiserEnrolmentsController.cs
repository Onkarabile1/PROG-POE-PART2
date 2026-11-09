using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserEnrolmentsController : OrganiserBaseController
{
    public OrganiserEnrolmentsController(IRaceDayApi api, ICurrentUser current) : base(api, current)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure is not null)
            return failure;

        var page = new EnrolmentsPageViewModel { Event = ev! };

        // The API lists enrolments per category, so we ask for each category of the event.
        foreach (var category in ev!.Categories ?? new List<CategoryResponse>())
        {
            var result = await Api.GetAsync<List<EnrolmentResponse>>($"api/categories/{category.CategoryId}/enrolments");

            if (!result.Success)
                return HandleFailure(result, RedirectToAction("Index", "OrganiserEvents"));

            page.Groups.Add(new CategoryEnrolments
            {
                Category = category,
                Enrolments = result.Data ?? new List<EnrolmentResponse>()
            });
        }

        return View(page);
    }
}