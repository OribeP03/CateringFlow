using cateringflow.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace CateringFlow.Tests;

public static class TestControllerSupport
{
    private sealed class NullUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

        public string? Action(UrlActionContext actionContext)
            => $"/{actionContext.Controller}/{actionContext.Action}";

        public string? Content(string? contentPath) => contentPath;

        public string? Link(string? routeName, object? values) => null;

        public string? RouteUrl(UrlRouteContext routeContext) => null;

        public bool IsLocalUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return url![0] == '/' && (url!.Length == 1 || (url[1] != '/' && url[1] != '\\'));
        }
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
            => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    public static CateringFlowDbContext CreateContext()
        => new CateringFlowDbContext(
            new DbContextOptionsBuilder<CateringFlowDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    /// <summary>
    /// Absolute path of the repository root (the folder holding <c>cateringflow.csproj</c>),
    /// found by walking up from the test binaries. Used by view-content assertions.
    /// </summary>
    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "cateringflow.csproj")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new InvalidOperationException("Could not locate the repository root (cateringflow.csproj).");
        }

        return dir.FullName;
    }

    /// <summary>
    /// Reads a repository file (relative to the project root) as text. Used by tests
    /// that assert what a view actually renders, not just what a controller returns.
    /// </summary>
    public static string RepoFile(string relativePath)
    {
        var full = Path.Combine(RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full))
        {
            throw new FileNotFoundException($"Repo file not found: {relativePath}", full);
        }

        return File.ReadAllText(full);
    }

    public static bool RepoFileExists(string relativePath)
        => File.Exists(Path.Combine(RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    public static void InitController(Controller controller, HttpContext? httpContext = null)
    {
        var http = httpContext ?? new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.Url = new NullUrlHelper();
        controller.TempData = new TempDataDictionary(http, new NullTempDataProvider());
    }
}

public sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "CateringFlow";
    public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "cf-wwwroot-test");
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}