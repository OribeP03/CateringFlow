# 4. Security Features - Screenshots & Description

> Each security feature is documented with its source code location, screenshot placeholder, and detailed process explanation.

---

## 4.0 Security Middleware

| Middleware | Short Description |
|---|---|
| **Security Headers Middleware** | Adds protective HTTP headers (`X-Frame-Options`, `X-Content-Type-Options`, `X-XSS-Protection`, `Referrer-Policy`, `X-Permitted-Cross-Domain-Policies`) to every response, blocking clickjacking, MIME sniffing, cross-domain data theft, and XSS |
| **Rate Limiting Middleware** | Throttles login attempts to 30 requests per minute per IP address using a sliding-window in-memory store, preventing brute-force and credential-stuffing attacks |
| **Authentication Middleware** | Reads and validates the `CateringFlow_Role` cookie against the 8-role allowlist, reconstructs the user identity for downstream authorization, and deletes tampered/invalid cookies |
| **RBAC Middleware** | Enforces Role-Based Access Control on `/SuperAdmin/*` routes using the RbacService permission matrix, redirecting unauthorized roles to the Access Denied page and attaching the permission level to the request context |

---

## 4.1 Security Headers Middleware

**Label**: `Security-HeadersMiddleware`  
**File**: `Middleware/SecurityHeadersMiddleware.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/security-01-headers-middleware.png]]`

**Source Code**:
```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");

        await _next(context);
    }
}
```

**Headers Explained**:

| Header | Value | Protection |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents browsers from MIME-type sniffing responses, stopping drive-by downloads of malicious content disguised as harmless files |
| `X-Frame-Options` | `SAMEORIGIN` | Prevents the site from being embedded in iframes on other domains, blocking clickjacking attacks |
| `X-XSS-Protection` | `1; mode=block` | Enables the browser's built-in XSS filter and blocks rendering if an attack is detected |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer information sent on cross-origin requests to only the origin (not full path), preventing information leakage |
| `X-Permitted-Cross-Domain-Policies` | `none` | Prevents Flash, PDF, and other cross-domain policy files from loading, blocking cross-domain data theft |

**How It Works**:
1. Middleware is registered early in the pipeline (before routing)
2. Intercepts every HTTP response
3. Appends security headers before the response is sent
4. Runs on every request regardless of route or authentication status
5. Provides defense-in-depth against XSS, clickjacking, MIME sniffing, and data exfiltration

**Middleware Registration** (in `Program.cs`):
```csharp
app.UseMiddleware<SecurityHeadersMiddleware>();
```

---

## 4.2 Rate Limiting Middleware

**Label**: `Security-RateLimiting`  
**File**: `Middleware/RateLimitingMiddleware.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/security-02-rate-limiting.png]]`

**Source Code**:
```csharp
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, List<DateTime>> _loginAttempts = new();
    private const int MaxAttempts = 30;
    private const int WindowMinutes = 1;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/Account/Login" && 
            context.Request.Method == "POST")
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-WindowMinutes);

            var attempts = _loginAttempts.AddOrUpdate(clientIp,
                _ => new List<DateTime> { now },
                (_, list) => {
                    list.RemoveAll(t => t < windowStart);
                    list.Add(now);
                    return list;
                });

            if (attempts.Count > MaxAttempts)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Too Many Requests");
                return;
            }
        }

        await _next(context);
    }
}
```

**Configuration**:
- **Target**: Only `POST /Account/Login` endpoint
- **Limit**: 30 requests per minute per IP address
- **Window**: Sliding window (last 60 seconds)
- **Storage**: In-memory `ConcurrentDictionary` (thread-safe)
- **Response**: HTTP 429 "Too Many Requests" when exceeded

**How It Works**:
1. Intercepts all incoming requests
2. Checks if request matches the login endpoint (POST /Account/Login)
3. Extracts client IP address from `RemoteIpAddress`
4. Looks up IP in the ConcurrentDictionary
5. Removes timestamps older than 1 minute (sliding window cleanup)
6. Adds current timestamp to the list
7. If count exceeds 30, returns HTTP 429 immediately
8. Otherwise, passes request to next middleware

**Protection Against**:
- Brute force password attacks
- Credential stuffing attempts
- Login form spam/bots
- Denial-of-service through login endpoint abuse

**Memory Management**:
- Old entries are cleaned up on each request (sliding window)
- ConcurrentDictionary provides thread-safe access for concurrent requests
- Memory footprint is minimal (only stores timestamps per IP)

---

## 4.3 Authentication Middleware

**Label**: `Security-AuthMiddleware`  
**File**: `Middleware/AuthenticationMiddleware.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/security-03-auth-middleware.png]]`

**Source Code**:
```csharp
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context)
    {
        var roleCookie = context.Request.Cookies["CateringFlow_Role"];

        if (!string.IsNullOrEmpty(roleCookie))
        {
            var validRoles = new[] {
                "Super Admin", "Sales / CRM Staff", "Event Coordinator",
                "Inventory Staff", "Kitchen Manager", "Finance Staff",
                "Staff / Crew", "Customer"
            };

            if (validRoles.Contains(roleCookie))
            {
                context.Items["AuthenticatedRole"] = roleCookie;

                if (context.User?.Identity?.IsAuthenticated != true)
                {
                    var claims = new List<Claim> {
                        new Claim(ClaimTypes.Name, context.User?.Identity?.Name ?? "User"),
                        new Claim(ClaimTypes.Role, roleCookie)
                    };
                    var identity = new ClaimsIdentity(claims, "Cookie");
                    context.User = new ClaimsPrincipal(identity);
                }
            }
            else
            {
                // Invalid/tampered role cookie — delete it
                context.Response.Cookies.Delete("CateringFlow_Role");
            }
        }

        await _next(context);
    }
}
```

**How It Works**:
1. Reads the `CateringFlow_Role` cookie from every request
2. If the cookie exists:
   a. Validates the role value against the 8 allowed system roles
   b. If valid: stores role in `context.Items["AuthenticatedRole"]` for downstream use
   c. If the user's ClaimsPrincipal is not authenticated, reconstructs one with Name and Role claims
   d. If the role value is invalid/tampered: deletes the cookie (security measure)
3. Passes control to next middleware

**Key Security Features**:
- **Role Validation**: Only accepts known system roles; any tampered value is rejected
- **Cookie Cleanup**: Invalid cookies are immediately deleted
- **Claims Reconstruction**: Ensures `context.User` has proper identity even if the auth cookie expired
- **Defense Against Cookie Tampering**: Server-side validation prevents privilege escalation via cookie modification

**Valid System Roles**:
```
Super Admin | Sales / CRM Staff | Event Coordinator |
Inventory Staff | Kitchen Manager | Finance Staff |
Staff / Crew | Customer
```

---

## 4.4 Role-Based Access Control (RBAC) Middleware

**Label**: `Security-RBACMiddleware`  
**File**: `Middleware/RoleAccessControlMiddleware.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/security-04-rbac-middleware.png]]`

**Source Code**:
```csharp
public class RoleAccessControlMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRbacService _rbacService;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Only enforce RBAC on /SuperAdmin/* routes
        if (path.StartsWithSegments("/SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            var role = context.Items["AuthenticatedRole"]?.ToString()
                       ?? context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(role))
            {
                context.Response.Redirect("/Account/Login");
                return;
            }

            // Extract page name from URL path
            var page = path.Split('/').LastOrDefault() ?? "Dashboard";

            if (!_rbacService.HasAccess(role, page, out string permissionLevel))
            {
                context.Response.Redirect($"/Account/AccessDenied?attemptedPage={page}&currentRole={role}");
                return;
            }

            context.Items["PermissionLevel"] = permissionLevel;
        }

        // Set CurrentRole for all routes (used by views)
        var currentRole = context.Items["AuthenticatedRole"]?.ToString()
                          ?? context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.IsNullOrEmpty(currentRole))
        {
            context.Items["CurrentRole"] = currentRole;
        }

        await _next(context);
    }
}
```

**How It Works**:
1. **Route Detection**: Only enforces RBAC on `/SuperAdmin/*` routes
2. **Role Extraction**: Gets role from middleware-provided context item or ClaimsPrincipal
3. **Unauthenticated Check**: If no role found, redirects to Login
4. **Page Extraction**: Parses the page name from URL path (e.g., `/SuperAdmin/Customers` → `Customers`)
5. **Permission Check**: Calls `IRbacService.HasAccess(role, page)` against the permission matrix
6. **Access Denied**: If denied, redirects to AccessDenied page with context (attempted page, current role)
7. **Permission Level**: Stores the permission level (Manage, View, Submit, etc.) in context for view-level rendering
8. **Non-SuperAdmin Routes**: Still sets `CurrentRole` in context for sidebar/view rendering

**RBAC Permission Matrix**:

| Role | Dashboard | Customers | Events | CRM | Inventory | Suppliers | Staff | Quotations | Invoices | Payments | PaymentProofs | Reports | Settings |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Super Admin | Manage | Manage | Manage | Manage | Manage | Manage | Manage | Manage | Manage | Manage | Manage | View/Generate | Manage |
| Sales/CRM | View | Manage | Submit | Manage | - | - | - | Quotations | - | - | - | - | - |
| Event Coordinator | View | - | Submit/Manage | - | - | - | Assign/View | - | - | - | - | - | - |
| Inventory Staff | View | - | - | - | Manage | Manage | - | - | - | - | - | View | - |
| Kitchen Manager | View | - | - | - | Manage | - | - | - | - | - | - | View | - |
| Finance Staff | View | - | - | - | - | - | - | Manage | Manage | Manage | Manage | View/Generate | - |
| Staff/Crew | View | - | - | - | - | - | View | - | - | - | - | - | - |
| Customer | - | Profile | Submit | Interactions | - | - | - | - | - | - | - | - | - |

**Protection Against**:
- Privilege escalation (accessing admin pages as lower-privilege role)
- Horizontal privilege access (one admin role accessing another's pages)
- Direct URL manipulation (typing admin URLs in browser)

---

## 4.5 Cookie Authentication Configuration

**Label**: `Security-CookieConfig`  
**File**: `Program.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/security-05-cookie-config.png]]`

**Configuration Source**:
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "CateringFlow_Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
```

**Security Settings Explained**:

| Setting | Value | Purpose |
|---|---|---|
| `ExpireTimeSpan` | 7 days | Session expires after 7 days of inactivity |
| `SlidingExpiration` | true | Expiry resets on each request (extends active sessions) |
| `Cookie.HttpOnly` | true | Prevents JavaScript access to the auth cookie (XSS protection) |
| `Cookie.SameSite` | Lax | Cookies sent on top-level navigations but not cross-site POST (CSRF mitigation) |
| `Cookie.SecurePolicy` | SameAsRequest | Cookie only sent over HTTPS in production |
| `LoginPath` | /Account/Login | Redirect target for unauthorized access attempts |
| `AccessDeniedPath` | /Account/AccessDenied | Redirect target for RBAC-denied access |

**How It Works**:
1. On successful authentication, ASP.NET Core issues an encrypted cookie
2. Subsequent requests include the cookie automatically
3. The cookie contains encrypted ClaimsPrincipal data (name, email, role, Firebase UID)
4. Middleware decrypts and reconstructs `context.User` on each request
5. Sliding expiration resets the 7-day timer on each active request
6. HttpOnly prevents XSS attacks from stealing the session cookie
7. SameSite=Lax prevents CSRF attacks from cross-origin form submissions

---

## 4.6 Firebase Token Verification

**Label**: `Security-FirebaseAuth`  
**File**: `Controllers/AccountController.cs` → `FirebaseLogin()` action

> [!screenshot] Place screenshot here
> `![[screenshots/security-06-firebase-token.png]]`

**Process Flow**:
```
Client Browser                    CateringFlow Server                 Firebase
     |                                  |                                |
     |--- [1] User signs in ----------->|                                |
     |    (Google OAuth / Email+PW)     |                                |
     |                                  |                                |
     |<-- [2] Firebase ID Token --------|  (client-side SDK)            |
     |                                  |                                |
     |--- [3] POST /Account/FirebaseLogin (IdToken) -->|                |
     |                                  |                                |
     |                          [4] VerifyIdTokenAsync(idToken) -------->|
     |                                  |    (cryptographic verify)     |
     |                                  |<--- [5] Decoded token --------|
     |                                  |    { email, uid, aud, ... }   |
     |                                  |                                |
     |                          [6] ResolveRole(email)                   |
     |                          [7] Create ClaimsPrincipal               |
     |                          [8] Issue auth cookie                    |
     |                                  |                                |
     |<-- [9] Redirect + Set-Cookie -----|                               |
```

**Security Properties**:
- **Asymmetric Cryptography**: Firebase tokens are RSA-signed; server verifies with Google's public keys
- **Token Expiry**: Firebase tokens have short lifetimes (typically 1 hour)
- **Audience Validation**: Token must match this Firebase project's audience
- **No Password Storage**: Server never handles raw passwords; Firebase manages credentials
- **Server-Side Verification**: Token is verified on the server, not just decoded on the client

---

## 4.7 Owner-Only Data Access Validation

**Label**: `Security-OwnerValidation`  
**File**: `Controllers/ClientController.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/security-07-owner-validation.png]]`

**Example — Booking Details**:
```csharp
[Authorize]
public async Task<IActionResult> BookingDetails(int id)
{
    var currentUser = await _userManager.GetUserAsync(User);
    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.FirebaseUid == currentUser.Uid);

    var ev = await _context.Events
        .Include(e => e.Customer)
        .Include(e => e.Package)
        .FirstOrDefaultAsync(e => e.Id == id);

    if (ev == null || ev.CustomerId != customer.Id)
        return NotFound();

    return View(ev);
}
```

**How It Works**:
1. User must be authenticated (`[Authorize]` attribute)
2. System finds the Customer record linked to the current user's Firebase UID
3. Loads the requested Event by ID
4. **Ownership Check**: Compares Event.CustomerId with current user's Customer.Id
5. If IDs don't match → returns 404 (Not Found) — doesn't reveal the resource exists
6. This prevents users from accessing other customers' bookings via URL manipulation

**Applied To**:
- `BookingDetails(id)` — Only show bookings belonging to current customer
- `ProofMessages(proofId)` — Only show messages for own payment proofs
- `SendProofMessage` — Only allow messaging on own proofs

**Protection Against**:
- Insecure Direct Object Reference (IDOR) attacks
- Horizontal privilege escalation
- Data leakage through URL manipulation

---

## 4.8 Anti-Forgery Token (CSRF Protection)

**Label**: `Security-CSRF`  
**Files**: All Razor Forms + Controllers

> [!screenshot] Place screenshot here
> `![[screenshots/security-08-csrf.png]]`

**Razor Form Implementation**:
```html
<form asp-action="Create" method="post">
    @Html.AntiForgeryToken()
    <!-- form fields -->
</form>
```

**JavaScript AJAX Implementation**:
```javascript
const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
fetch('/SuperAdmin/SendProofAdminMessage', {
    method: 'POST',
    headers: {
        'RequestVerificationToken': token
    },
    body: formData
});
```

**How It Works**:
1. ASP.NET Core generates a unique anti-forgery token per session
2. Token is embedded in forms as a hidden field (`__RequestVerificationToken`)
3. On form POST, the server validates the token matches the session
4. If token is missing or invalid → 400 Bad Request
5. Prevents cross-site request forgery where an attacker tricks a logged-in user into submitting a malicious form

---

## 4.9 Middleware Pipeline Order

**Label**: `Security-MiddlewarePipeline`  
**File**: `Program.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/security-09-middleware-pipeline.png]]`

**Pipeline Order** (defense-in-depth):
```
[1] HTTPS Redirection          → Forces all HTTP to HTTPS
[2] SecurityHeadersMiddleware   → Adds protective HTTP headers
[3] RateLimitingMiddleware       → Throttles login attempts
[4] Routing                     → URL matching
[5] Cookie Authentication       → Validates session cookie
[6] AuthenticationMiddleware    → Reconstructs role from cookie
[7] Authorization               → [Authorize] attribute checks
[8] RoleAccessControlMiddleware → RBAC enforcement for /SuperAdmin
[9] Static Files                → Serves CSS, JS, images
[10] Controller Routes          → MVC action execution
```

**Why Order Matters**:
- Security headers are added to ALL responses (including error pages)
- Rate limiting runs before authentication (prevents login brute force)
- Authentication reconstructs identity before RBAC checks
- RBAC runs after authentication but before controller execution
- Each layer provides independent protection (defense-in-depth)

---

## 4.10 Role Cookie Validation

**Label**: `Security-RoleCookieValidation`  
**File**: `Middleware/AuthenticationMiddleware.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/security-10-role-cookie.png]]`

**Validation Process**:
```
1. Read "CateringFlow_Role" cookie value
2. Check if value is empty → skip (no role set)
3. Compare against valid roles whitelist:
   ["Super Admin", "Sales / CRM Staff", "Event Coordinator",
    "Inventory Staff", "Kitchen Manager", "Finance Staff",
    "Staff / Crew", "Customer"]
4. If VALID:
   → Store in context.Items["AuthenticatedRole"]
   → Rebuild ClaimsPrincipal if not authenticated
5. If INVALID (tampered):
   → Delete the cookie immediately
   → Log warning
   → Continue without role (will be caught by RBAC middleware)
```

**Protection Against**:
- Cookie tampering (manually editing the role cookie to escalate privileges)
- Injection attacks on cookie values
- Session fixation through role manipulation

---

## 4.11 HTTPS Redirection

**Label**: `Security-HTTPS`  
**File**: `Program.cs`

> [!screenshot] Place screenshot here
> `![[screenshots/security-11-https.png]]`

**Configuration**:
```csharp
app.UseHttpsRedirection();
```

**How It Works**:
- Intercepts all HTTP requests
- Returns HTTP 301/307 redirect to the HTTPS equivalent URL
- Ensures all data in transit is encrypted via TLS
- Prevents man-in-the-middle attacks and session hijacking
- Applied before any other middleware (first line of defense)

---

## 4.12 File Upload Security

**Label**: `Security-FileUpload`  
**File**: `Controllers/ClientController.cs` → `Book()` action

> [!screenshot] Place screenshot here
> `![[screenshots/security-12-file-upload.png]]`

**Security Measures**:
```csharp
if (model.ProofImage != null && model.ProofImage.Length > 0)
{
    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
    Directory.CreateDirectory(uploadsDir);
    var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ProofImage.FileName)}";
    var filePath = Path.Combine(uploadsDir, uniqueName);
    
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await model.ProofImage.CopyToAsync(stream);
    }
    proof.ProofImagePath = $"/uploads/{uniqueName}";
}
```

**Security Properties**:
- **Unique Filenames**: `Guid.NewGuid()` prevents filename collision and path traversal
- **Original Name Preserved**: For display purposes only; not used for storage path
- **WebRootPath Storage**: Files stored in wwwroot/uploads (not in application root)
- **No Execution**: Uploaded images are served as static files, never executed
- **File Size Limit**: Implicitly limited by Kestrel/server configuration

---

## 4.13 Data Validation (Model Validation)

**Label**: `Security-DataValidation`  
**File**: All Model files in `Models/`

> [!screenshot] Place screenshot here
> `![[screenshots/security-13-data-validation.png]]`

**Validation Attributes Used**:
```csharp
[Required(ErrorMessage = "Full name is required")]
[EmailAddress(ErrorMessage = "Invalid email address")]
[Phone(ErrorMessage = "Invalid phone number")]
[Range(1, int.MaxValue, ErrorMessage = "Pax count must be at least 1")]
[StringLength(50)]
[RegularExpression(@"^[A-Z]{3}-\d{4}$", ErrorMessage = "Invalid item code format")]
```

**How It Works**:
1. Server-side validation via DataAnnotations on model properties
2. `ModelState.IsValid` checked in every POST action
3. Invalid models return the form with error messages
4. Client-side validation via jQuery Unobtrusive Validation (parses data-val attributes)
5. Defense against: SQL injection (EF parameterized queries), XSS (Razor HTML encoding), invalid data entry

---

## Summary - Security Layers

```
Layer 1: HTTPS Redirection           → Encryption in transit
Layer 2: Security Headers            → Browser-level protections
Layer 3: Rate Limiting               → Brute force prevention
Layer 4: Cookie Authentication       → Session management
Layer 5: Auth Middleware             → Identity reconstruction + tamper detection
Layer 6: RBAC Middleware             → Authorization enforcement
Layer 7: Owner Validation            → Data access control
Layer 8: Anti-Forgery Tokens         → CSRF protection
Layer 9: Model Validation            → Input sanitization
Layer 10: File Upload Security       → Safe file handling
```
