using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Services;

/// <summary>
/// Protects a controller or action. Not logged in: redirected to the login page.
/// Wrong role: redirected to the access denied page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public SessionAuthorizeAttribute(string? role = null)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var current = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

        if (!current.IsAuthenticated)
        {
            var request = context.HttpContext.Request;
            string? returnUrl = HttpMethods.IsGet(request.Method) ? request.Path + request.QueryString : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_role is not null && current.User!.Role != _role)
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}