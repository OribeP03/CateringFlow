using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Controllers;

[Authorize]
public class InventoryController : AppController
{
    private readonly CateringFlowDbContext _db;

    public InventoryController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? category, string? stockStatus, int? page)
    {
        var query = _db.InventoryItems
            .Include(i => i.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.ItemName.Contains(search) || i.ItemCode.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(i => i.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(stockStatus) && stockStatus != "All")
        {
            query = query.Where(i => i.StockStatus == stockStatus);
        }

        var items = await PagedResult<InventoryModel>.CreateAsync(
            query.OrderBy(i => i.ItemCode),
            page);
        ViewData["Search"] = search;
        ViewData["CategoryFilter"] = category;
        ViewData["StockStatusFilter"] = stockStatus;
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var lastItem = await _db.InventoryItems.OrderByDescending(i => i.Id).FirstOrDefaultAsync();
        var nextNumber = lastItem != null ? int.Parse(lastItem.ItemCode.Replace("INV-", "")) + 1 : 1;
        ViewData["SuggestedCode"] = $"INV-{nextNumber:D3}";
        ViewData["Suppliers"] = await _db.Suppliers.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryModel item, string? returnUrl = null)
    {
        item.LastUpdated = DateTime.Now;
        item.StockStatus = ComputeStockStatus(item);
        ModelState.Remove(nameof(item.Supplier));
        ModelState.Remove(nameof(item.SupplierName));
        if (ModelState.IsValid)
        {
            _db.InventoryItems.Add(item);
            await _db.SaveChangesAsync();
            await ActivityLogger.LogAsync(_db, "Created", "Inventory", item.Id, $"Inventory item \"{item.ItemName}\" added ({item.CurrentStock:0.##} {item.Unit}).", User.Identity?.Name);
            TempData["Success"] = $"Item \"{item.ItemName}\" added to inventory.";
            return RedirectToIndex(returnUrl);
        }
        ViewData["Suppliers"] = await _db.Suppliers.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToListAsync();
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var item = await _db.InventoryItems.FindAsync(id);
        if (item == null) return NotFound();
        ViewData["Suppliers"] = await _db.Suppliers.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToListAsync();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InventoryModel item, string? returnUrl = null)
    {
        if (id != item.Id) return NotFound();
        ModelState.Remove(nameof(item.Supplier));
        ModelState.Remove(nameof(item.SupplierName));
        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _db.InventoryItems.FindAsync(id);
                if (existing == null) return NotFound();
                existing.ItemCode = item.ItemCode;
                existing.ItemName = item.ItemName;
                existing.Category = item.Category;
                existing.CurrentStock = item.CurrentStock;
                existing.MinReorderLevel = item.MinReorderLevel;
                existing.Unit = item.Unit;
                existing.UnitCost = item.UnitCost;
                existing.SupplierId = item.SupplierId;
                existing.StockStatus = ComputeStockStatus(item);
                existing.LastUpdated = DateTime.Now;
                _db.InventoryItems.Update(existing);
                await _db.SaveChangesAsync();
                await ActivityLogger.LogAsync(_db, "Updated", "Inventory", existing.Id, $"Inventory item \"{existing.ItemName}\" updated to {existing.CurrentStock:0.##} {existing.Unit} ({existing.StockStatus}).", User.Identity?.Name);
                TempData["Success"] = "Inventory item updated successfully.";
                return RedirectToIndex(returnUrl);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.InventoryItems.Any(i => i.Id == id)) return NotFound();
                throw;
            }
        }
        ViewData["Suppliers"] = await _db.Suppliers.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToListAsync();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var item = await _db.InventoryItems.FindAsync(id);
        if (item == null) return NotFound();
        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync();
        await ActivityLogger.LogAsync(_db, "Deleted", "Inventory", id, $"Inventory item \"{item.ItemName}\" was deleted.", User.Identity?.Name);
        TempData["Success"] = "Inventory item deleted successfully.";
        return RedirectToIndex(returnUrl);
    }

    private static string ComputeStockStatus(InventoryModel item)
    {
        if (item.CurrentStock <= 0) return "Out of Stock";
        if (item.CurrentStock < item.MinReorderLevel) return "Low Stock";
        return "Adequate";
    }
}