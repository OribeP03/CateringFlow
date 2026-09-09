using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using cateringflow.Data;
using cateringflow.Middleware;
using cateringflow.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register EF Core DbContext with SQL Server
builder.Services.AddDbContext<CateringFlowDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register RBAC Service
builder.Services.AddSingleton<IRbacService, RbacService>();

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

// Firebase Authentication settings + graceful init (only when the service account file exists)
var firebaseSettings = builder.Configuration.GetSection("Firebase").Get<FirebaseSettings>() ?? new FirebaseSettings();
if (!string.IsNullOrWhiteSpace(firebaseSettings.ServiceAccountPath))
{
    var credentialPath = Path.GetFullPath(firebaseSettings.ServiceAccountPath, builder.Environment.ContentRootPath);
    firebaseSettings.IsConfigured = File.Exists(credentialPath);
    if (firebaseSettings.IsConfigured)
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = CredentialFactory.FromFile<ServiceAccountCredential>(credentialPath).ToGoogleCredential(),
            ProjectId = string.IsNullOrWhiteSpace(firebaseSettings.ProjectId) ? null : firebaseSettings.ProjectId
        });
    }
}
builder.Services.AddSingleton(firebaseSettings);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 1. Security Headers Middleware
app.UseMiddleware<SecurityHeadersMiddleware>();

// 2. Rate Limiting Middleware
app.UseMiddleware<RateLimitingMiddleware>();

app.UseRouting();

// 3. Core Authentication & Identity Verification Middleware
app.UseAuthentication();
app.UseMiddleware<AuthenticationMiddleware>();

app.UseAuthorization();

// 4. Role-Based Access Control Middleware
app.UseMiddleware<RoleAccessControlMiddleware>();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Seed the database with sample data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CateringFlowDbContext>();
    db.Database.Migrate();
    SeedData.Initialize(db);
}

// Seed a permanent admin account into Firebase Authentication (runs only when
// the service account file is present, so Firebase can verify login tokens).
if (firebaseSettings.IsConfigured)
{
    try
    {
        await FirebaseSeeder.EnsureSeedAdminsAsync(firebaseSettings, m => app.Logger.LogInformation("Firebase: {Message}", m));
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Firebase seed admin setup failed: {Message}", ex.Message);
    }
}

app.Run();
