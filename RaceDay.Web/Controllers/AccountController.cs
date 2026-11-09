using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    private readonly IRaceDayApi _api;
    private readonly ICurrentUser _currentUser;

    public AccountController(IRaceDayApi api, ICurrentUser currentUser)
    {
        _api = api;
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_currentUser.IsAuthenticated)
            return RedirectToHome();

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PostAsync<AuthResponse>("api/auth/login",
            new { email = model.Email, password = model.Password });

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty,
                result.IsUnauthorized ? "The email or password is incorrect." : result.Error);
            return View(model);
        }

        _currentUser.SignIn(result.Data);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToHome();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (_currentUser.IsAuthenticated)
            return RedirectToHome();

        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == Roles.Participant && model.DateOfBirth is null)
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth is required for participants.");

        if (!ModelState.IsValid)
            return View(model);

        var register = await _api.PostAsync<object>("api/auth/register", new
        {
            email = model.Email,
            password = model.Password,
            role = model.Role,
            firstName = model.FirstName,
            lastName = model.LastName,
            phone = model.Phone,
            dateOfBirth = model.Role == Roles.Participant ? model.DateOfBirth : null
        });

        if (!register.Success)
        {
            ModelState.AddModelError(string.Empty, register.Error);
            return View(model);
        }

        // Log the new user straight in.
        var login = await _api.PostAsync<AuthResponse>("api/auth/login",
            new { email = model.Email, password = model.Password });

        if (!login.Success || login.Data is null)
        {
            ShowSuccess("Your account was created. Please log in.");
            return RedirectToAction(nameof(Login));
        }

        _currentUser.SignIn(login.Data);
        ShowSuccess($"Welcome to RaceDay, {login.Data.User.FirstName}!");
        return RedirectToHome();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _currentUser.SignOut();
        ShowSuccess("You have been logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private IActionResult RedirectToHome() =>
        _currentUser.IsOrganiser
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");
}