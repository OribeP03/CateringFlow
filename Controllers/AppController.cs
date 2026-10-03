using Microsoft.AspNetCore.Mvc;

namespace cateringflow.Controllers;

/// <summary>
/// Base controller for list/CRUD pages.
/// Provides safe redirect-back-to-Index handling for modal form posts
/// that carry a hidden <c>returnUrl</c>.
/// </summary>
public abstract class AppController : Controller
{
    protected IActionResult RedirectToIndex(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    protected IActionResult RedirectToDetails(string? returnUrl, string defaultAction, object routeValues)
    {
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(defaultAction, routeValues);
    }
}