using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class CRMLeadController : AppController
{
    private readonly CateringFlowDbContext _db;

    public CRMLeadController(CateringFlowDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Live pipeline list. Leads are only ever created from a website inquiry
    /// (<see cref="ClientController.Inquiry"/>) — there is no manual "add lead"
    /// endpoint, which is how a real CRM behaves.
    /// </summary>
    public async Task<IActionResult> Index(string? search, string? stage, string? assignee, int? page)
    {
        var query = _db.CrmLeads.Include(l => l.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.LeadName.Contains(search)
                                     || (l.Company != null && l.Company.Contains(search))
                                     || (l.Email != null && l.Email.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(stage) && stage != "All")
        {
            var requested = stage;
            query = query.Where(l => l.Stage == requested);
        }
        if (!string.IsNullOrWhiteSpace(assignee) && assignee != "All")
        {
            var owner = assignee;
            query = query.Where(l => l.AssignedTo == owner);
        }

        ViewData["TotalNew"] = await query.CountAsync(l => l.Stage == "New");
        ViewData["TotalContacted"] = await query.CountAsync(l => l.Stage == "Contacted");
        ViewData["TotalProposal"] = await query.CountAsync(l => l.Stage == "Proposal");
        ViewData["TotalNegotiation"] = await query.CountAsync(l => l.Stage == "Negotiation");
        ViewData["TotalLost"] = await query.CountAsync(l => l.Stage == "Lost" || l.Stage == "Locked/Lost");
        ViewData["TotalWon"] = await query.CountAsync(l => l.Stage == "Won");
        ViewData["TotalValue"] = await query.SumAsync(l => (decimal?)l.EstimatedValue) ?? 0m;
        ViewData["Search"] = search;
        ViewData["StageFilter"] = stage;
        ViewData["AssigneeFilter"] = assignee;

        var leads = await PagedResult<CRMLeadModel>.CreateAsync(
            query.OrderByDescending(l => l.CreatedAt),
            page);
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
    public async Task<IActionResult> Edit(int id, CRMLeadModel lead, string? returnUrl = null)
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
                return RedirectToIndex(returnUrl);
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
    public async Task<IActionResult> UpdateStage(int id, string stage, string? returnUrl = null)
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
            return RedirectToIndex(returnUrl);
        }

        TempData["Success"] = $"Lead stage updated to \"{stage}\".";
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var lead = await _db.CrmLeads.FindAsync(id);
        if (lead == null) return NotFound();
        _db.CrmLeads.Remove(lead);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Lead deleted successfully.";
        return RedirectToIndex(returnUrl);
    }
}