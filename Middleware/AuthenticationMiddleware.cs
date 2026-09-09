// Middleware/AuthenticationMiddleware.cs
using System.Security.Claims;
using cateringflow.Models;

namespace cateringflow.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for secure authentication cookie
        if (context.Request.Cookies.TryGetValue("CateringFlow_Role", out var roleCookie) && !string.IsNullOrEmpty(roleCookie))
        {
            // Validate that the role is one of the valid system roles
            if (UserRoles.AllRoles.Contains(roleCookie, StringComparer.OrdinalIgnoreCase))
            {
                context.Items["AuthenticatedRole"] = roleCookie;
                
                // If ClaimsPrincipal is empty or unauthenticated, reconstruct valid identity
                if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, $"{roleCookie} User"),
                        new Claim(ClaimTypes.Role, roleCookie)
                    };
                    var identity = new ClaimsIdentity(claims, "CookieAuth");
                    context.User = new ClaimsPrincipal(identity);
                }
            }
            else
            {
                _logger.LogWarning("Invalid or tampered role cookie detected: {Role}", roleCookie);
                context.Response.Cookies.Delete("CateringFlow_Role");
            }
        }

        await _next(context);
    }
}
