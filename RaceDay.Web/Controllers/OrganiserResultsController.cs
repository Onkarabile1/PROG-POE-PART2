using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserResultsController : OrganiserBaseController
{
    public OrganiserResultsController(IRaceDayApi api, ICurrentUser current) : base(api, current)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(int eventId, int? categoryId)
    {
        var (ev, failure) = await LoadOwnEventAsync(eventId);
        if (failure is not null)
            return failure;

        var categories = ev!.Categories ?? new List<CategoryResponse>();
        var selected = categories.FirstOrDefault(c => c.CategoryId == categoryId) ?? categories.FirstOrDefault();

        var page = new ResultsPageViewModel
        {
            Event = ev,
            Categories = categories,
            Selected = selected,
            // Results can be entered once the event day has arrived.
            CanEnterResults = ev.EventDate <= DateOnly.FromDateTime(DateTime.Today)
        };

        if (selected is null)
            return View(page);

        var enrolments = await Api.GetAsync<List<EnrolmentResponse>>($"api/categories/{selected.CategoryId}/enrolments");
        if (!enrolments.Success)
            return HandleFailure(enrolments, RedirectToAction("Index", "OrganiserEvents"));

        var results = await Api.GetAsync<List<ResultResponse>>($"api/categories/{selected.CategoryId}/results");
        if (!results.Success)
            return HandleFailure(results, RedirectToAction("Index", "OrganiserEvents"));

        var byEnrolment = (results.Data ?? new List<ResultResponse>()).ToDictionary(r => r.EnrolmentId);

        page.Rows = (enrolments.Data ?? new List<EnrolmentResponse>())
            .Where(e => e.Status == "Active")
            .OrderBy(e => e.BibNumber)
            .Select(e =>
            {
                byEnrolment.TryGetValue(e.EnrolmentId, out var r);

                return new ResultRowViewModel
                {
                    EnrolmentId = e.EnrolmentId,
                    ResultId = r?.ResultId,
                    BibNumber = e.BibNumber,
                    ParticipantName = e.ParticipantName,
                    Status = r?.Status ?? "Finished",
                    FinishTimeText = r?.FinishTime?.ToString(@"hh\:mm\:ss") ?? string.Empty,
                    Position = r?.Position
                };
            })
            .ToList();

        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SaveResultViewModel model)
    {
        var back = RedirectToAction(nameof(Index), new { eventId = model.EventId, categoryId = model.CategoryId });

        if (!FormOptions.ResultStatuses.Contains(model.Status))
        {
            ShowError("Please choose Finished, DNF or DNS.");
            return back;
        }

        TimeSpan? finishTime = null;
        int? position = null;

        if (model.Status == "Finished")
        {
            if (!TimeSpan.TryParseExact(model.FinishTime?.Trim(), @"h\:mm\:ss", CultureInfo.InvariantCulture, out var parsed)
                || parsed <= TimeSpan.Zero)
            {
                ShowError("Enter the finish time as hours:minutes:seconds, for example 00:27:45.");
                return back;
            }

            if (model.Position is null or < 1)
            {
                ShowError("Enter the finishing position (1 or higher).");
                return back;
            }

            finishTime = parsed;
            position = model.Position;
        }

        var body = new { finishTime, position, status = model.Status };

        var result = model.ResultId is null
            ? await Api.PostAsync<ResultResponse>($"api/enrolments/{model.EnrolmentId}/result", body)
            : await Api.PutAsync<ResultResponse>($"api/results/{model.ResultId}", body);

        if (!result.Success)
            return HandleFailure(result, back);

        ShowSuccess($"Result saved for {result.Data?.ParticipantName ?? "the participant"}.");
        return back;
    }
}