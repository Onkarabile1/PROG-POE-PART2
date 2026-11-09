using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserEventsController : OrganiserBaseController
{
    public OrganiserEventsController(IRaceDayApi api, ICurrentUser current) : base(api, current)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<List<EventResponse>>("api/events/mine");

        if (!result.Success || result.Data is null)
            return HandleFailure(result, RedirectToAction("Dashboard", "Organiser"));

        return View(result.Data.OrderBy(e => e.EventDate).ToList());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new EventFormViewModel { EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (model.EventDate is not null && model.EventDate < DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(model.EventDate), "The event date must be today or later.");

        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await Api.PostAsync<EventResponse>("api/events", ToRequest(model));

        if (!result.Success || result.Data is null)
        {
            if (result.IsUnauthorized)
                return ExpiredSession();

            ModelState.AddModelError(string.Empty, result.Error);
            return View("Form", model);
        }

        ShowSuccess($"\"{result.Data.EventName}\" was created. Now add its categories.");
        return RedirectToAction("Index", "OrganiserCategories", new { eventId = result.Data.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (ev, failure) = await LoadOwnEventAsync(id);
        if (failure is not null)
            return failure;

        return View("Form", new EventFormViewModel
        {
            EventId = ev!.EventId,
            EventName = ev.EventName,
            EventType = ev.EventType,
            EventDate = ev.EventDate,
            Description = ev.Description,
            City = ev.City,
            Province = ev.Province,
            Venue = ev.Venue
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;

        if (!ModelState.IsValid)
            return View("Form", model);

        var result = await Api.PutAsync<EventResponse>($"api/events/{id}", ToRequest(model));

        if (!result.Success)
        {
            if (result.IsUnauthorized)
                return ExpiredSession();

            ModelState.AddModelError(string.Empty, result.Error);
            return View("Form", model);
        }

        ShowSuccess($"\"{model.EventName}\" was updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteAsync($"api/events/{id}");

        if (!result.Success)
            return HandleFailure(result, RedirectToAction(nameof(Index)));

        ShowSuccess("The event was deleted.");
        return RedirectToAction(nameof(Index));
    }

    private static object ToRequest(EventFormViewModel m) => new
    {
        eventName = m.EventName.Trim(),
        eventType = m.EventType,
        eventDate = m.EventDate,
        city = m.City,
        province = m.Province,
        venue = m.Venue,
        description = m.Description
    };
}