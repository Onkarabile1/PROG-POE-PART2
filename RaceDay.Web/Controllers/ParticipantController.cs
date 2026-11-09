using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Roles.Participant)]
public class ParticipantController : AppController
{
    private readonly IRaceDayApi _api;
    private readonly Dictionary<int, CategoryResponse?> _categories = new();
    private readonly Dictionary<int, EventResponse?> _events = new();

    public ParticipantController(IRaceDayApi api) { _api = api; }

    // ---------- Home: upcoming events with filters ----------
    public async Task<IActionResult> Events(string? province, string? eventType, DateOnly? date)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(province)) qs.Add($"province={Uri.EscapeDataString(province)}");
        if (!string.IsNullOrWhiteSpace(eventType)) qs.Add($"eventType={Uri.EscapeDataString(eventType)}");
        if (date.HasValue) qs.Add($"date={date.Value:yyyy-MM-dd}");

        var vm = new BrowseViewModel { Province = province, EventType = eventType, Date = date };
        var r = await _api.GetAsync<List<EventResponse>>("api/events" + (qs.Count > 0 ? "?" + string.Join("&", qs) : ""));
        if (!r.Success) return HandleFailure(r, View(vm));

        vm.Events = (r.Data ?? new()).OrderBy(e => e.EventDate).ToList();
        return View(vm);
    }

    // ---------- Event details with categories ----------
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventResponse>($"api/events/{id}");
        if (!ev.Success) return HandleFailure(ev, RedirectToAction(nameof(Events)));

        var mine = await _api.GetAsync<List<EnrolmentResponse>>("api/users/me/enrolments");
        var enrolled = (mine.Data ?? new())
            .Where(e => e.CanCancel).Select(e => e.CategoryId).ToHashSet();

        return View(new EventDetailsViewModel { Event = ev.Data!, EnrolledCategoryIds = enrolled });
    }

    // ---------- Enrol (recorded immediately by the API) ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var r = await _api.PostAsync<EnrolmentResponse>($"api/categories/{categoryId}/enrolments", new { });
        if (!r.Success) return HandleFailure(r, RedirectToAction(nameof(Details), new { id = eventId }));

        ShowSuccess("You're enrolled! Pay your entry fee to move from Pending to Confirmed.");
        return RedirectToAction(nameof(MyEnrolments));
    }

    // ---------- My enrolments ----------
    public async Task<IActionResult> MyEnrolments()
    {
        var r = await _api.GetAsync<List<EnrolmentResponse>>("api/users/me/enrolments");
        if (!r.Success) return HandleFailure(r, View(new List<EnrolmentRow>()));

        var rows = new List<EnrolmentRow>();
        foreach (var e in (r.Data ?? new()).OrderByDescending(x => x.EnrolmentId))
        {
            var cat = await GetCategoryAsync(e.CategoryId);
            var ev = cat is null ? null : await GetEventAsync(cat.EventId);
            rows.Add(new EnrolmentRow
            {
                Enrolment = e,
                EventId = cat?.EventId ?? 0,
                EventName = ev?.EventName ?? e.EventName ?? "Event",
                EventDate = ev?.EventDate,
                CategoryName = cat?.CategoryName ?? e.CategoryName ?? "—",
                Fee = cat?.EntryFee ?? 0
            });
        }
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(int enrolmentId, decimal amount, string method)
    {
        // The API checks that the amount equals the category fee.
        var r = await _api.PostAsync<PaymentResponse>($"api/enrolments/{enrolmentId}/payment",
            new { amount, paymentMethod = method });
        if (!r.Success) return HandleFailure(r, RedirectToAction(nameof(MyEnrolments)));

        ShowSuccess("Payment received. Your enrolment is now confirmed.");
        return RedirectToAction(nameof(MyEnrolments));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int enrolmentId)
    {
        var r = await _api.DeleteAsync($"api/enrolments/{enrolmentId}");
        if (!r.Success) return HandleFailure(r, RedirectToAction(nameof(MyEnrolments)));

        ShowSuccess("Your enrolment has been cancelled.");
        return RedirectToAction(nameof(MyEnrolments));
    }

    // ---------- My results ----------
    public async Task<IActionResult> MyResults()
    {
        var r = await _api.GetAsync<List<ResultResponse>>("api/users/me/results");
        if (!r.Success) return HandleFailure(r, View(new List<ResultRow>()));

        var mine = await _api.GetAsync<List<EnrolmentResponse>>("api/users/me/enrolments");
        var catByEnrolment = (mine.Data ?? new()).ToDictionary(e => e.EnrolmentId, e => e.CategoryId);
        var finishers = new Dictionary<int, int>();
        var rows = new List<ResultRow>();

        foreach (var res in r.Data ?? new())
        {
            var catId = res.CategoryId != 0 ? res.CategoryId : catByEnrolment.GetValueOrDefault(res.EnrolmentId);
            var cat = catId == 0 ? null : await GetCategoryAsync(catId);
            var ev = cat is null ? null : await GetEventAsync(cat.EventId);

            if (catId != 0 && !finishers.ContainsKey(catId))
            {
                var all = await _api.GetAsync<List<ResultResponse>>($"api/categories/{catId}/results");
                finishers[catId] = (all.Data ?? new()).Count(x => x.Position.HasValue);
            }

            rows.Add(new ResultRow
            {
                Result = res,
                EventName = ev?.EventName ?? "Event",
                EventDate = ev?.EventDate,
                CategoryName = cat?.CategoryName ?? "—",
                Finishers = catId != 0 ? finishers[catId] : 0
            });
        }
        return View(rows.OrderByDescending(x => x.EventDate).ToList());
    }

    // ---------- Profile ----------
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var r = await _api.GetAsync<Dictionary<string, JsonElement>>("api/users/me");
        if (!r.Success) return HandleFailure(r, RedirectToAction(nameof(Events)));
        return View(ProfileViewModel.From(r.Data ?? new()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var body = model.Fields.ToDictionary(
            kv => kv.Key,
            kv => string.IsNullOrWhiteSpace(kv.Value) ? null : kv.Value.Trim());

        var r = await _api.PutAsync<UserResponse>("api/users/me", body);
        if (!r.Success) return HandleFailure(r, View(model));

        ShowSuccess("Your profile has been updated.");
        return RedirectToAction(nameof(Profile));
    }

    // ---------- helpers (cached per request) ----------
    private async Task<CategoryResponse?> GetCategoryAsync(int id)
    {
        if (!_categories.TryGetValue(id, out var c))
        {
            c = (await _api.GetAsync<CategoryResponse>($"api/categories/{id}")).Data;
            _categories[id] = c;
        }
        return c;
    }

    private async Task<EventResponse?> GetEventAsync(int id)
    {
        if (!_events.TryGetValue(id, out var e))
        {
            e = (await _api.GetAsync<EventResponse>($"api/events/{id}")).Data;
            _events[id] = e;
        }
        return e;
    }
}