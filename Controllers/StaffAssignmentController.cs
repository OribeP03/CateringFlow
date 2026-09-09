using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class StaffAssignmentController : Controller
{
    private readonly CateringFlowDbContext _db;

    public StaffAssignmentController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? eventId)
    {
        var query = _db.StaffAssignments
            .Include(a => a.Staff)
            .Include(a => a.Event)
            .AsQueryable();

        if (eventId.HasValue)
        {
            query = query.Where(a => a.EventId == eventId.Value);
        }

        var assignments = await query
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();

        ViewData["EventFilter"] = eventId;
        ViewData["Events"] = await _db.Events.OrderByDescending(e => e.EventDate).ToListAsync();
        return View(assignments);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Staff"] = await _db.Staff.Where(s => s.Availability == "Available").OrderBy(s => s.FullName).ToListAsync();
        ViewData["Events"] = await _db.Events.Where(e => e.Status != "Completed").OrderBy(e => e.EventDate).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffAssignmentModel assignment)
    {
        assignment.AssignedAt = DateTime.Now;
        ModelState.Remove(nameof(assignment.Staff));
        ModelState.Remove(nameof(assignment.Event));
        ModelState.Remove(nameof(assignment.StaffName));
        if (ModelState.IsValid)
        {
            _db.StaffAssignments.Add(assignment);
            await _db.SaveChangesAsync();

            var staffMember = await _db.Staff.FindAsync(assignment.StaffId);
            if (staffMember != null)
            {
                staffMember.Availability = "Busy";
                _db.Staff.Update(staffMember);
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Staff assigned to event successfully.";
            return RedirectToAction(nameof(Index));
        }
        ViewData["Staff"] = await _db.Staff.Where(s => s.Availability == "Available").OrderBy(s => s.FullName).ToListAsync();
        ViewData["Events"] = await _db.Events.Where(e => e.Status != "Completed").OrderBy(e => e.EventDate).ToListAsync();
        return View(assignment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var assignment = await _db.StaffAssignments.FindAsync(id);
        if (assignment == null) return NotFound();

        var staffMember = await _db.Staff.FindAsync(assignment.StaffId);
        if (staffMember != null && staffMember.Availability == "Busy")
        {
            staffMember.Availability = "Available";
            _db.Staff.Update(staffMember);
        }

        _db.StaffAssignments.Remove(assignment);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Staff assignment removed. Staff availability updated to Available.";
        return RedirectToAction(nameof(Index));
    }
}