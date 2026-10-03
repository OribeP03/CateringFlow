using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class StaffController : AppController
{
    private readonly CateringFlowDbContext _db;

    public StaffController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? availability, string? employmentType, int? page)
    {
        var query = _db.Staff
            .Include(s => s.Assignments).ThenInclude(a => a.Event)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.FullName.Contains(search) || s.Position.Contains(search) || s.Email.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(availability) && availability != "All")
        {
            query = query.Where(s => s.Availability == availability);
        }
        if (!string.IsNullOrWhiteSpace(employmentType) && employmentType != "All")
        {
            query = query.Where(s => s.EmploymentType == employmentType);
        }

        ViewData["TotalStaff"] = await query.CountAsync();
        ViewData["TotalAvailable"] = await query.CountAsync(s => s.Availability == "Available");
        ViewData["TotalBusy"] = await query.CountAsync(s => s.Availability == "Busy");
        ViewData["TotalOnLeave"] = await query.CountAsync(s => s.Availability == "On Leave");
        ViewData["TotalFullTime"] = await query.CountAsync(s => s.EmploymentType == "Full-time");

        var staff = await PagedResult<StaffModel>.CreateAsync(
            query.OrderByDescending(s => s.CreatedAt),
            page);
        ViewData["Search"] = search;
        ViewData["AvailabilityFilter"] = availability;
        ViewData["EmploymentFilter"] = employmentType;
        return View(staff);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var staff = await _db.Staff
            .Include(s => s.Assignments).ThenInclude(a => a.Event)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (staff == null) return NotFound();
        return View(staff);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffModel staff, string? returnUrl = null)
    {
        staff.CreatedAt = DateTime.Now;
        ModelState.Remove(nameof(staff.Assignments));
        if (ModelState.IsValid)
        {
            _db.Staff.Add(staff);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Staff member \"{staff.FullName}\" added successfully.";
            return RedirectToIndex(returnUrl);
        }
        return View(staff);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var staff = await _db.Staff.FindAsync(id);
        if (staff == null) return NotFound();
        return View(staff);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StaffModel staff, string? returnUrl = null)
    {
        if (id != staff.Id) return NotFound();
        ModelState.Remove(nameof(staff.Assignments));
        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _db.Staff.FindAsync(id);
                if (existing == null) return NotFound();
                existing.FullName = staff.FullName;
                existing.Email = staff.Email;
                existing.Phone = staff.Phone;
                existing.Position = staff.Position;
                existing.Availability = staff.Availability;
                existing.EmploymentType = staff.EmploymentType;
                existing.Specialty = staff.Specialty;
                existing.DateHired = staff.DateHired;
                _db.Staff.Update(existing);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Staff member updated successfully.";
                return RedirectToIndex(returnUrl);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.Staff.Any(s => s.Id == id)) return NotFound();
                throw;
            }
        }
        return View(staff);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var staff = await _db.Staff.FindAsync(id);
        if (staff == null) return NotFound();
        _db.Staff.Remove(staff);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Staff member deleted successfully.";
        return RedirectToIndex(returnUrl);
    }
}