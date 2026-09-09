using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using cateringflow.Models;

namespace cateringflow.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View("~/Views/Client/Index.cshtml");
    }

    public IActionResult Privacy()
    {
        return View("~/Views/Client/Index.cshtml");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
