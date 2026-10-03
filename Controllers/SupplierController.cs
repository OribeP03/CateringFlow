using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Controllers;

[Authorize]
public class SupplierController : AppController
{
    private readonly CateringFlowDbContext _db;

    public SupplierController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? category, int? page)
    {
        var query = _db.Suppliers
            .Include(s => s.InventoryItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.SupplierName.Contains(search) || (s.ContactPerson != null && s.ContactPerson.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(s => s.Category == category);
        }

        var suppliers = await PagedResult<SupplierModel>.CreateAsync(
            query.OrderByDescending(s => s.CreatedAt),
            page);
        ViewData["Search"] = search;
        ViewData["CategoryFilter"] = category;
        return View(suppliers);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierModel supplier, string? returnUrl = null)
    {
        supplier.CreatedAt = DateTime.Now;
        ModelState.Remove(nameof(supplier.InventoryItems));
        if (ModelState.IsValid)
        {
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();
            await ActivityLogger.LogAsync(_db, "Created", "Supplier", supplier.Id, $"Supplier \"{supplier.SupplierName}\" was added.", User.Identity?.Name);
            TempData["Success"] = $"Supplier \"{supplier.SupplierName}\" added successfully.";
            return RedirectToIndex(returnUrl);
        }
        return View(supplier);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        return View(supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierModel supplier, string? returnUrl = null)
    {
        if (id != supplier.Id) return NotFound();
        ModelState.Remove(nameof(supplier.InventoryItems));
        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _db.Suppliers.FindAsync(id);
                if (existing == null) return NotFound();
                existing.SupplierName = supplier.SupplierName;
                existing.ContactPerson = supplier.ContactPerson;
                existing.Email = supplier.Email;
                existing.Phone = supplier.Phone;
                existing.Address = supplier.Address;
                existing.Category = supplier.Category;
                existing.Status = supplier.Status;
                _db.Suppliers.Update(existing);
                await _db.SaveChangesAsync();
                await ActivityLogger.LogAsync(_db, "Updated", "Supplier", existing.Id, $"Supplier \"{existing.SupplierName}\" was updated.", User.Identity?.Name);
                TempData["Success"] = "Supplier updated successfully.";
                return RedirectToIndex(returnUrl);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.Suppliers.Any(s => s.Id == id)) return NotFound();
                throw;
            }
        }
        return View(supplier);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();
        await ActivityLogger.LogAsync(_db, "Deleted", "Supplier", id, $"Supplier \"{supplier.SupplierName}\" was deleted.", User.Identity?.Name);
        TempData["Success"] = "Supplier deleted successfully.";
        return RedirectToIndex(returnUrl);
    }
}