using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly CateringFlowDbContext _db;

    public SettingsController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var settings = await _db.Settings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SettingsModel();
        }
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsModel model)
    {
        model.UpdatedAt = DateTime.Now;
        var existing = await _db.Settings.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (existing == null)
        {
            _db.Settings.Add(model);
        }
        else
        {
            existing.CompanyName = model.CompanyName;
            existing.CompanyEmail = model.CompanyEmail;
            existing.CompanyPhone = model.CompanyPhone;
            existing.CompanyAddress = model.CompanyAddress;
            existing.Tagline = model.Tagline;
            existing.UpdatedAt = DateTime.Now;
            _db.Settings.Update(existing);
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = "Company settings updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}