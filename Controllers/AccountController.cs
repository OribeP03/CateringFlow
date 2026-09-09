// Controllers/AccountController.cs
using System.Security.Claims;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Controllers;

public class AccountController : Controller
{
    private readonly IRbacService _rbacService;
    private readonly FirebaseSettings _firebaseSettings;

    public AccountController(IRbacService rbacService, FirebaseSettings firebaseSettings)
    {
        _rbacService = rbacService;
        _firebaseSettings = firebaseSettings;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["FirebaseSettings"] = _firebaseSettings;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string role = UserRoles.SuperAdmin, string? returnUrl = null)
    {
        // Once real Firebase auth is configured, this demo "type anything to log in"
        // endpoint is disabled so Firestore/Admin accounts are the only way in.
        if (_firebaseSettings.IsConfigured)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Firebase authentication is enabled. Use a verified sign-in method.");
        }

        // Issue Claims and Auth Cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, email ?? "Admin User"),
            new Claim(ClaimTypes.Email, email ?? "admin@cateringflow.ph"),
            new Claim(ClaimTypes.Role, role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
        Response.Cookies.Append("CateringFlow_Role", role, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Direct to first allowed page
        var allowedPages = _rbacService.GetAllowedPages(role);
        if (allowedPages.ContainsKey("Dashboard"))
        {
            return RedirectToAction("Dashboard", "SuperAdmin");
        }
        else if (allowedPages.Count > 0)
        {
            var firstPage = allowedPages.Keys.First();
            return Redirect($"/SuperAdmin/{firstPage}");
        }

        return RedirectToAction("Index", "Home");
    }

    // Firebase sign-in: the client sends the Firebase ID token after a Google popup
    // or an email/password sign-in. The token is verified with the Admin SDK, then a
    // cookie session is issued with an RBAC role.
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseLoginRequest request)
    {
        if (!_firebaseSettings.IsConfigured)
        {
            return StatusCode(503, new { error = "Firebase is not configured yet. Add your Firebase settings in appsettings.json and the service account file." });
        }

        if (string.IsNullOrWhiteSpace(request?.IdToken))
        {
            return BadRequest(new { error = "Missing Firebase ID token." });
        }

        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(request.IdToken);
            var email = decoded.Claims.TryGetValue("email", out var emailClaim) ? emailClaim?.ToString() : null;
            var fullName = decoded.Claims.TryGetValue("name", out var nameClaim) ? nameClaim?.ToString() : null;

            if (string.IsNullOrWhiteSpace(email))
            {
                return Unauthorized(new { error = "Firebase account has no email address." });
            }

            // Map the email to an RBAC role: seeded account roles take priority,
            // then the SuperAdmin allowlist, then the default role.
            var role = _firebaseSettings.ResolveRole(email);
            var displayName = _firebaseSettings.ResolveDisplayName(email);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(fullName) ? displayName ?? email.Split('@')[0] : fullName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("FirebaseUid", decoded.Uid)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
            Response.Cookies.Append("CateringFlow_Role", role, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });

            // Prefer the requested local page; otherwise send admins to the dashboard
            // and everyone else to the public home page.
            string? redirectUrl = null;
            if (!string.IsNullOrWhiteSpace(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl))
            {
                redirectUrl = request.ReturnUrl;
            }
            else if (_rbacService.GetAllowedPages(role).ContainsKey("Dashboard"))
            {
                redirectUrl = Url.Action("Dashboard", "SuperAdmin");
            }
            else
            {
                redirectUrl = Url.Action("Index", "Home");
            }

            return Ok(new { success = true, role, redirectUrl });
        }
        catch (FirebaseAuthException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> SwitchRole(string role, string? redirectPage = null)
    {
        if (string.IsNullOrEmpty(role)) role = UserRoles.SuperAdmin;

        // Update cookie
        Response.Cookies.Append("CateringFlow_Role", role, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(7) });

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, $"{role} User"),
            new Claim(ClaimTypes.Role, role)
        };
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

        // If currently requested page is allowed for the new role, go there
        if (!string.IsNullOrEmpty(redirectPage) && _rbacService.CanAccessPage(role, redirectPage))
        {
            return Redirect($"/SuperAdmin/{redirectPage}");
        }

        var allowed = _rbacService.GetAllowedPages(role);
        if (allowed.ContainsKey("Dashboard"))
        {
            return RedirectToAction("Dashboard", "SuperAdmin");
        }
        else if (allowed.Count > 0)
        {
            return Redirect($"/SuperAdmin/{allowed.Keys.First()}");
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("CateringFlow_Role");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied(string? attemptedPage, string? currentRole)
    {
        ViewData["AttemptedPage"] = attemptedPage ?? "Requested Page";
        ViewData["CurrentRole"] = currentRole ?? (Request.Cookies["CateringFlow_Role"] ?? UserRoles.SuperAdmin);
        return View();
    }
}

public class FirebaseLoginRequest
{
    public string? IdToken { get; set; }
    public string? ReturnUrl { get; set; }
}
