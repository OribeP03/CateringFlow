using System.Security.Claims;
using cateringflow.Controllers;
using cateringflow.Models;
using cateringflow.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A25 - Account / session CRUD.
/// Sign-in, role switching and sign-out must respect RBAC, and the demo login
/// shortcut must stay disabled as soon as Firebase is configured.
/// </summary>
public class AccountAuthTests
{
    private sealed class StubAuthenticationService : IAuthenticationService
    {
        public bool SignedIn { get; private set; }

        public bool SignedOut { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedIn = true;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignedOut = true;
            return Task.CompletedTask;
        }
    }

    private static (AccountController Controller, DefaultHttpContext Context, StubAuthenticationService Auth) CreateController(
        FirebaseSettings? firebase = null)
    {
        var auth = new StubAuthenticationService();
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(auth);
        var http = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        var controller = new AccountController(new RbacService(), firebase ?? new FirebaseSettings())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        controller.Url = new LocalUrlHelper();
        controller.TempData = new TempDataDictionary(http, new NullTempDataStub());

        return (controller, http, auth);
    }

    private sealed class LocalUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext { get; } =
            new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

        public string? Action(UrlActionContext actionContext) => $"/{actionContext.Controller}/{actionContext.Action}";

        public string? Content(string? contentPath) => contentPath;

        public string? Link(string? routeName, object? values) => null;

        public string? RouteUrl(UrlRouteContext routeContext) => null;

        public bool IsLocalUrl(string? url)
            => !string.IsNullOrEmpty(url) && url![0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
    }

    private sealed class NullTempDataStub : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    [Fact]
    public void Login_Get_KeepsTheRequestedReturnUrl()
    {
        var (controller, _, _) = CreateController();

        var view = Assert.IsType<ViewResult>(controller.Login("/SuperAdmin/Inquiries"));

        Assert.Equal("/SuperAdmin/Inquiries", controller.ViewData["ReturnUrl"]);
        Assert.NotNull(controller.ViewData["FirebaseSettings"]);
    }

    [Fact]
    public async Task Login_Post_IsBlockedOnceFirebaseIsConfigured()
    {
        var configured = new FirebaseSettings
        {
            ProjectId = "cateringflow-test",
            ServiceAccountPath = "firebase-service-account.json",
            IsConfigured = true
        };
        var (controller, _, auth) = CreateController(configured);

        Assert.True(configured.IsConfigured);
        var result = await controller.Login("admin@cateringflow.ph", "password");

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
        Assert.False(auth.SignedIn);
    }

    [Fact]
    public async Task Logout_SignsTheUserOutAndClearsTheRoleCookie()
    {
        var (controller, _, auth) = CreateController();

        var result = await controller.Logout();

        Assert.True(auth.SignedOut);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Contains("CateringFlow_Role", controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task SwitchRole_ReSignsTheUserInWithTheNewRole()
    {
        var (controller, _, auth) = CreateController();

        var result = await controller.SwitchRole(UserRoles.InventoryStaff);

        Assert.True(auth.SignedIn);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirect.ActionName);
        Assert.Equal("SuperAdmin", redirect.ControllerName);
        Assert.Contains("CateringFlow_Role", controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task SwitchRole_CanJumpStraightToAnAllowedPage()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.SwitchRole(UserRoles.SalesCrm, "CRM");

        Assert.Equal("/SuperAdmin/CRM", Assert.IsType<RedirectResult>(result).Url);
    }

    [Fact]
    public async Task SwitchRole_IgnoresAPageTheNewRoleCannotSee()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.SwitchRole(UserRoles.StaffCrew, "Invoices");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.NotEqual("Invoices", redirect.ActionName);
        Assert.Equal("Dashboard", redirect.ActionName);
    }

    [Fact]
    public void AccessDenied_ExplainsWhatWasBlocked()
    {
        var (controller, _, _) = CreateController();

        var view = Assert.IsType<ViewResult>(controller.AccessDenied("Inquiries", UserRoles.FinanceStaff));

        Assert.Equal("Inquiries", controller.ViewData["AttemptedPage"]);
        Assert.Equal(UserRoles.FinanceStaff, controller.ViewData["CurrentRole"]);
        Assert.NotNull(view);
    }

    [Fact]
    public void AccessDenied_FallsBackToAnAttemptedPageLabel()
    {
        var (controller, _, _) = CreateController();

        controller.AccessDenied(null, UserRoles.KitchenManager);

        Assert.Equal("Requested Page", controller.ViewData["AttemptedPage"]);
        Assert.Equal(UserRoles.KitchenManager, controller.ViewData["CurrentRole"]);
    }
}