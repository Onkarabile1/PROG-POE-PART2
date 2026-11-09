using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Base class for all controllers: shared helpers for messages and expired logins.</summary>
public abstract class AppController : Controller
{
    protected void ShowSuccess(string message) => TempData["Success"] = message;

    protected void ShowError(string message) => TempData["Error"] = message;

    /// <summary>Call when the API answers 401 on a page the user was already logged in on (the token has expired).</summary>
    protected IActionResult ExpiredSession()
    {
        HttpContext.RequestServices.GetRequiredService<ICurrentUser>().SignOut();
        ShowError("Your session has expired. Please log in again.");
        return RedirectToAction("Login", "Account");
    }

    /// <summary>A failed API call: an expired login goes to the login page, anything else shows the message.</summary>
    protected IActionResult HandleFailure(ApiResult result, IActionResult fallback)
    {
        if (result.IsUnauthorized)
            return ExpiredSession();

        ShowError(result.Error);
        return fallback;
    }
}