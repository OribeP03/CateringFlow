// Middleware/RoleAccessControlMiddleware.cs
using System.Security.Claims;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Middleware;

public class RoleAccessControlMiddleware
{
    private readonly RequestDelegate _next;

    public RoleAccessControlMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRbacService rbacService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Check if path is targeting SuperAdmin portal
        if (path.StartsWith("/SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            // 1. Check if user is authenticated (has a valid role cookie or claim)
            string? currentRole = context.Request.Cookies["CateringFlow_Role"] ??
                                  context.User?.FindFirst(ClaimTypes.Role)?.Value;

            // If no role found at all, user is not logged in — redirect to Login
            if (string.IsNullOrEmpty(currentRole) || !UserRoles.AllRoles.Contains(currentRole, StringComparer.OrdinalIgnoreCase))
            {
                var returnUrl = Uri.EscapeDataString(path);
                context.Response.Redirect($"/Account/Login?returnUrl={returnUrl}");
                return;
            }

            context.Items["CurrentRole"] = currentRole;

            // 2. Check RBAC permissions for the specific page
            var segments = path.Trim('/').Split('/');
            string action = segments.Length > 1 ? segments[1] : "Dashboard";

            if (string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase))
            {
                action = "Dashboard";
            }

            if (rbacService.HasAccess(currentRole, action, out var permLevel))
            {
                context.Items["PermissionLevel"] = permLevel;
            }
            else
            {
                // Access Denied for this role
                context.Response.Redirect($"/Account/AccessDenied?attemptedPage={action}&currentRole={Uri.EscapeDataString(currentRole)}");
                return;
            }
        }
        else
        {
            // For non-SuperAdmin routes, still set the role context if available
            string? currentRole = context.Request.Cookies["CateringFlow_Role"] ??
                                  context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(currentRole))
            {
                context.Items["CurrentRole"] = currentRole;
            }
        }

        await _next(context);
    }
}
