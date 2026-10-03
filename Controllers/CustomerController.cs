using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Controllers;

[Authorize]
public class CustomerController : AppController
{
    private readonly CateringFlowDbContext _db;

    public CustomerController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? type, string? status, int? page)
    {
        var query = _db.Customers
            .Include(c => c.Events)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.FullName.Contains(search) || c.Email.Contains(search) || (c.Phone != null && c.Phone.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(type) && type != "All")
        {
            query = query.Where(c => c.Type == type);
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(c => c.Status == status);
        }

        var customers = await PagedResult<CustomerModel>.CreateAsync(
            query.OrderByDescending(c => c.CreatedAt),
            page);

        ViewData["Search"] = search;
        ViewData["TypeFilter"] = type;
        ViewData["StatusFilter"] = status;
        return View(customers);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var customer = await _db.Customers
            .Include(c => c.Events)
            .Include(c => c.Quotations)
            .Include(c => c.Invoices)
            .Include(c => c.Payments)
            .Include(c => c.CrmLeads)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        return View(customer);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerModel customer, string? returnUrl = null)
    {
        customer.CreatedAt = DateTime.Now;
        if (string.IsNullOrWhiteSpace(customer.Status)) customer.Status = "Active";
        if (string.IsNullOrWhiteSpace(customer.Type)) customer.Type = "Individual";
        ModelState.Remove(nameof(customer.Events));
        if (ModelState.IsValid)
        {
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
            await ActivityLogger.LogAsync(_db, "Created", "Customer", customer.Id, $"Customer \"{customer.FullName}\" was created.", User.Identity?.Name);
            TempData["Success"] = $"Customer \"{customer.FullName}\" created successfully.";
            return RedirectToIndex(returnUrl);
        }
        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerModel customer, string? returnUrl = null)
    {
        if (id != customer.Id) return NotFound();
        ModelState.Remove(nameof(customer.Events));
        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _db.Customers.FindAsync(id);
                if (existing == null) return NotFound();
                existing.FullName = customer.FullName;
                existing.Email = customer.Email;
                existing.Phone = customer.Phone;
                existing.Address = customer.Address;
                existing.Type = customer.Type;
                existing.Status = customer.Status;
                existing.Notes = customer.Notes;
                _db.Customers.Update(existing);
                await _db.SaveChangesAsync();
                await ActivityLogger.LogAsync(_db, "Updated", "Customer", existing.Id, $"Customer \"{existing.FullName}\" was updated.", User.Identity?.Name);
                TempData["Success"] = "Customer updated successfully.";
                return RedirectToIndex(returnUrl);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.Customers.Any(c => c.Id == id)) return NotFound();
                throw;
            }
        }
        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        await ActivityLogger.LogAsync(_db, "Deleted", "Customer", id, $"Customer \"{customer.FullName}\" was deleted.", User.Identity?.Name);
        TempData["Success"] = "Customer deleted successfully.";
        return RedirectToIndex(returnUrl);
    }
}