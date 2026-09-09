using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class EventController : Controller
{
    private readonly CateringFlowDbContext _db;

    public EventController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? type, string? status)
    {
        var query = _db.Events
            .Include(e => e.Customer)
            .Include(e => e.Package)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.EventName.Contains(search) || e.Venue != null && e.Venue.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(type) && type != "All")
        {
            query = query.Where(e => e.EventType == type);
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(e => e.Status == status);
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        ViewData["Search"] = search;
        ViewData["TypeFilter"] = type;
        ViewData["StatusFilter"] = status;
        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var item = await _db.Events
            .Include(e => e.Customer)
            .Include(e => e.Package)
            .Include(e => e.StaffAssignments).ThenInclude(sa => sa.Staff)
            .Include(e => e.Invoices)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Packages"] = await _db.MenuPackages.Where(p => p.Status == "Active").OrderBy(p => p.PackageName).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventModel item)
    {
        item.CreatedAt = DateTime.Now;
        ModelState.Remove(nameof(item.Customer));
        ModelState.Remove(nameof(item.Package));
        ModelState.Remove(nameof(item.CustomerName));
        if (ModelState.IsValid)
        {
            if (item.PackageId.HasValue)
            {
                var package = await _db.MenuPackages.FindAsync(item.PackageId.Value);
                item.TotalAmount = package != null ? package.PricePerPax * item.PaxCount : 0;
            }
            else
            {
                item.TotalAmount = 0;
            }
            _db.Events.Add(item);
            await _db.SaveChangesAsync();

            _db.Notifications.Add(new NotificationModel
            {
                Title = "New Event Booked",
                Message = $"Event \"{item.EventName}\" for {item.PaxCount} pax was created.",
                Type = "Info",
                IsRead = false,
                TargetRole = "Super Admin",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Event \"{item.EventName}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Packages"] = await _db.MenuPackages.Where(p => p.Status == "Active").OrderBy(p => p.PackageName).ToListAsync();
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var item = await _db.Events.FindAsync(id);
        if (item == null) return NotFound();
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Packages"] = await _db.MenuPackages.Where(p => p.Status == "Active").OrderBy(p => p.PackageName).ToListAsync();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventModel item)
    {
        if (id != item.Id) return NotFound();
        ModelState.Remove(nameof(item.Customer));
        ModelState.Remove(nameof(item.Package));
        ModelState.Remove(nameof(item.CustomerName));
        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _db.Events.FindAsync(id);
                if (existing == null) return NotFound();
                existing.EventName = item.EventName;
                existing.CustomerId = item.CustomerId;
                existing.EventType = item.EventType;
                existing.EventDate = item.EventDate;
                existing.Venue = item.Venue;
                existing.PaxCount = item.PaxCount;
                existing.Status = item.Status;
                existing.PackageId = item.PackageId;
                existing.Notes = item.Notes;
                if (item.PackageId.HasValue)
                {
                    var package = await _db.MenuPackages.FindAsync(item.PackageId.Value);
                    existing.TotalAmount = package != null ? package.PricePerPax * item.PaxCount : 0;
                }
                else
                {
                    existing.TotalAmount = 0;
                }
                _db.Events.Update(existing);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Event updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.Events.Any(e => e.Id == id)) return NotFound();
                throw;
            }
        }
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Packages"] = await _db.MenuPackages.Where(p => p.Status == "Active").OrderBy(p => p.PackageName).ToListAsync();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Events.FindAsync(id);
        if (item == null) return NotFound();
        _db.Events.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Event deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var item = await _db.Events.FindAsync(id);
        if (item == null) return NotFound();
        item.Status = status;
        _db.Events.Update(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Event status updated to \"{status}\".";
        return RedirectToAction(nameof(Details), new { id });
    }
}