using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class MenuPackageController : AppController
{
    private readonly CateringFlowDbContext _db;

    public MenuPackageController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? page)
    {
        var packages = await PagedResult<MenuPackageModel>.CreateAsync(
            _db.MenuPackages.Include(p => p.Events).OrderBy(p => p.PricePerPax),
            page);
        return View(packages);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var package = await _db.MenuPackages
            .Include(p => p.Events).ThenInclude(e => e.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (package == null) return NotFound();
        return View(package);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuPackageModel package, string? returnUrl = null)
    {
        package.CreatedAt = DateTime.Now;
        ModelState.Remove(nameof(package.Events));
        if (ModelState.IsValid)
        {
            _db.MenuPackages.Add(package);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Package \"{package.PackageName}\" created successfully.";
            return RedirectToIndex(returnUrl);
        }
        return View(package);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var package = await _db.MenuPackages.FindAsync(id);
        if (package == null) return NotFound();
        return View(package);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MenuPackageModel package, string? returnUrl = null)
    {
        if (id != package.Id) return NotFound();
        ModelState.Remove(nameof(package.Events));
        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _db.MenuPackages.FindAsync(id);
                if (existing == null) return NotFound();
                existing.PackageName = package.PackageName;
                existing.Description = package.Description;
                existing.PricePerPax = package.PricePerPax;
                existing.CourseCount = package.CourseCount;
                existing.ServiceHours = package.ServiceHours;
                existing.Status = package.Status;
                _db.MenuPackages.Update(existing);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Package updated successfully.";
                return RedirectToIndex(returnUrl);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.MenuPackages.Any(p => p.Id == id)) return NotFound();
                throw;
            }
        }
        return View(package);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var package = await _db.MenuPackages.FindAsync(id);
        if (package == null) return NotFound();
        _db.MenuPackages.Remove(package);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Package deleted successfully.";
        return RedirectToIndex(returnUrl);
    }
}