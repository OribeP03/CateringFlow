using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class CRMLeadController : Controller
{
    private readonly CateringFlowDbContext _db;

    public CRMLeadController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var leads = await _db.CrmLeads
            .Include(l => l.Customer)
            .OrderByDescending(l => l.EstimatedValue)
            .ToListAsync();

        ViewData["TotalValue"] = leads.Sum(l => l.EstimatedValue);
        return View(leads);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var lead = await _db.CrmLeads
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lead == null) return NotFound();
        return View(lead);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CRMLeadModel lead)
    {
        lead.CreatedAt = DateTime.Now;
        ModelState.Remove(nameof(lead.Customer));
        ModelState.Remove(nameof(lead.CustomerName));
        if (ModelState.IsValid)
        {
            _db.CrmLeads.Add(lead);
            await _db.SaveChangesAsync();

            _db.Notifications.Add(new NotificationModel
            {
                Title = "New Lead Added",
                Message = $"Lead \"{lead.LeadName}\" worth ₱{lead.EstimatedValue:N0} was added to the pipeline.",
                Type = "Info",
                IsRead = false,
                TargetRole = "Sales / CRM Staff",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Lead \"{lead.LeadName}\" created successfully.";
            return RedirectToAction(nameof(Index));
        }
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        return View(lead);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var lead = await _db.CrmLeads.FindAsync(id);
        if (lead == null) return NotFound();
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CRMLeadModel lead)
    {
        if (id != lead.Id) return NotFound();
        ModelState.Remove(nameof(lead.Customer));
        ModelState.Remove(nameof(lead.CustomerName));
        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _db.CrmLeads.FindAsync(id);
                if (existing == null) return NotFound();
                existing.CustomerId = lead.CustomerId;
                existing.LeadName = lead.LeadName;
                existing.Company = lead.Company;
                existing.Email = lead.Email;
                existing.Phone = lead.Phone;
                existing.EstimatedValue = lead.EstimatedValue;
                existing.Stage = lead.Stage;
                existing.AssignedTo = lead.AssignedTo;
                existing.Notes = lead.Notes;
                existing.LastContact = lead.LastContact;
                _db.CrmLeads.Update(existing);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Lead updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.CrmLeads.Any(l => l.Id == id)) return NotFound();
                throw;
            }
        }
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStage(int id, string stage)
    {
        var lead = await _db.CrmLeads.FindAsync(id);
        if (lead == null) return NotFound();
        lead.Stage = stage;
        lead.LastContact = DateTime.Now;
        _db.CrmLeads.Update(lead);
        await _db.SaveChangesAsync();

        if (stage == "Won" && !lead.CustomerId.HasValue)
        {
            var customer = new CustomerModel
            {
                FullName = lead.LeadName,
                Email = lead.Email ?? $"{lead.LeadName.Replace(" ", "")}@example.com",
                Phone = lead.Phone,
                Type = "Individual",
                Status = "Active",
                CreatedAt = DateTime.Now,
                Notes = "Auto-converted from won CRM lead."
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
            lead.CustomerId = customer.Id;
            _db.CrmLeads.Update(lead);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Lead marked as Won. Customer \"{customer.FullName}\" was created automatically.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = $"Lead stage updated to \"{stage}\".";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await _db.CrmLeads.FindAsync(id);
        if (lead == null) return NotFound();
        _db.CrmLeads.Remove(lead);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Lead deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}