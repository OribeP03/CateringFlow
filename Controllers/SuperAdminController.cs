using Microsoft.AspNetCore.Mvc;

namespace cateringflow.Controllers;

public class SuperAdminController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult Customers()
    {
        return View();
    }

    public IActionResult CRM()
    {
        return View();
    }

    public IActionResult Events()
    {
        return View();
    }

    public IActionResult MenuPackages()
    {
        return View();
    }

    public IActionResult Inventory()
    {
        return View();
    }

    public IActionResult Suppliers()
    {
        return View();
    }

    public IActionResult Staff()
    {
        return View();
    }

    public IActionResult Quotations()
    {
        return View();
    }

    public IActionResult Invoices()
    {
        return View();
    }

    public IActionResult Payments()
    {
        return View();
    }

    public IActionResult Reports()
    {
        return View();
    }

    public IActionResult Notifications()
    {
        return View();
    }

    public IActionResult Settings()
    {
        return View();
    }
}
