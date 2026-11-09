using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserCategoriesController : OrganiserBaseController
{
    public OrganiserCategoriesController(IRaceDayApi api, ICurrentUser current) : base(api, current)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure is not null)
            return failure;

        return View(new CategoriesPageViewModel
        {
            Event = ev!,
            NewCategory = new CategoryFormViewModel { EventId = eventId }
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        var (ev, failure) = await LoadOwnEventAsync(model.EventId);
        if (failure is not null)
            return failure;

        var page = new CategoriesPageViewModel { Event = ev!, NewCategory = model };

        if (!ModelState.IsValid)
            return View("Index", page);

        var result = await Api.PostAsync<CategoryResponse>($"api/events/{model.EventId}/categories", ToRequest(model));

        if (!result.Success)
        {
            if (result.IsUnauthorized)
                return ExpiredSession();

            ModelState.AddModelError(string.Empty, result.Error);
            return View("Index", page);
        }

        ShowSuccess($"Category \"{model.CategoryName}\" was added.");
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetAsync<CategoryResponse>($"api/categories/{id}");

        if (!result.Success || result.Data is null)
            return HandleFailure(result, RedirectToAction("Index", "OrganiserEvents"));

        var (ev, failure) = await LoadOwnEventAsync(result.Data.EventId);
        if (failure is not null)
            return failure;

        var c = result.Data;
        ViewData["EventName"] = ev!.EventName;

        return View(new CategoryFormViewModel
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            CategoryName = c.CategoryName,
            DistanceKm = c.DistanceKm,
            EntryFee = c.EntryFee,
            MaxParticipants = c.MaxParticipants,
            StartPoint = c.StartPoint,
            EndPoint = c.EndPoint,
            ElevationGainM = c.ElevationGainM,
            MapUrl = c.MapUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        model.CategoryId = id;

        var (ev, failure) = await LoadOwnEventAsync(model.EventId);
        if (failure is not null)
            return failure;

        ViewData["EventName"] = ev!.EventName;

        if (!ModelState.IsValid)
            return View(model);

        var result = await Api.PutAsync<CategoryResponse>($"api/categories/{id}", ToRequest(model));

        if (!result.Success)
        {
            if (result.IsUnauthorized)
                return ExpiredSession();

            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        ShowSuccess($"Category \"{model.CategoryName}\" was updated.");
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await Api.DeleteAsync($"api/categories/{id}");
        var back = RedirectToAction(nameof(Index), new { eventId });

        if (!result.Success)
            return HandleFailure(result, back);

        ShowSuccess("The category was deleted.");
        return back;
    }

    private static object ToRequest(CategoryFormViewModel m) => new
    {
        categoryName = m.CategoryName.Trim(),
        distanceKm = m.DistanceKm,
        entryFee = m.EntryFee,
        maxParticipants = m.MaxParticipants,
        startPoint = m.StartPoint,
        endPoint = m.EndPoint,
        elevationGainM = m.ElevationGainM,
        mapUrl = m.MapUrl
    };
}