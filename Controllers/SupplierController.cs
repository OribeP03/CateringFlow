using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class SupplierController : Controller
{
    private readonly CateringFlowDbContext _db;

    public SupplierController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? category)
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

        var suppliers = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
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
    public async Task<IActionResult> Create(SupplierModel supplier)
    {
        supplier.CreatedAt = DateTime.Now;
        ModelState.Remove(nameof(supplier.InventoryItems));
        if (ModelState.IsValid)
        {
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Supplier \"{supplier.SupplierName}\" added successfully.";
            return RedirectToAction(nameof(Index));
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
    public async Task<IActionResult> Edit(int id, SupplierModel supplier)
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
                TempData["Success"] = "Supplier updated successfully.";
                return RedirectToAction(nameof(Index));
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
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Supplier deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}