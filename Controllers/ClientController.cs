using Microsoft.AspNetCore.Mvc;

namespace cateringflow.Controllers;

public class ClientController : Controller
{
    public IActionResult Index()
    {
        return View("~/Views/Client/Index.cshtml");
    }

    public IActionResult Packages()
    {
        return View("~/Views/Client/Index.cshtml");
    }
}
