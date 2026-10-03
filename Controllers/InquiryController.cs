using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Controllers;

/// <summary>
/// Manual handling of website inquiries by the events / sales team.
/// Inquiries are never created here — they arrive from
/// <see cref="ClientController.Inquiry"/> and land in the CRM as "New" leads.
/// The team reads, assigns, advances and closes them from the inbox at
/// <c>/SuperAdmin/Inquiries</c>.
/// </summary>
[Authorize]
public class InquiryController : AppController
{
    private const string InboxUrl = "/SuperAdmin/Inquiries";

    private readonly CateringFlowDbContext _db;

    public InquiryController(CateringFlowDbContext db)
    {
        _db = db;
    }

    /// <summary>Moves an inquiry along the workflow and keeps the CRM lead in sync.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl = null)
    {
        if (!InquiryStatuses.All.Contains(status))
        {
            TempData["Error"] = $"\"{status}\" is not a valid inquiry status.";
            return RedirectToInbox(returnUrl);
        }

        var inquiry = await _db.Inquiries
            .Include(i => i.CrmLead)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inquiry == null) return NotFound();

        inquiry.Status = status;
        inquiry.UpdatedAt = DateTime.Now;

        if (inquiry.CrmLead != null)
        {
            inquiry.CrmLead.Stage = InquiryStatuses.ToLeadStage(status) ?? inquiry.CrmLead.Stage;
            inquiry.CrmLead.LastContact = DateTime.Now;
        }

        await _db.SaveChangesAsync();

        _db.Notifications.Add(new NotificationModel
        {
            Title = $"Inquiry {inquiry.Reference} — {status}",
            Message = $"{inquiry.FullName}'s inquiry moved to {status}.",
            Type = status == InquiryStatuses.Lost ? "Warning" : "Info",
            TargetRole = "Sales / CRM Staff",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();

        await ActivityLogger.LogAsync(
            _db,
            "Updated",
            "Inquiry",
            inquiry.Id,
            $"Inquiry {inquiry.Reference} ({inquiry.FullName}) moved to {status}.",
            CurrentUserName());

        TempData["Success"] = $"Inquiry {inquiry.Reference} marked as {status}.";
        return RedirectToInbox(returnUrl);
    }

    /// <summary>Assigns an inquiry (and its CRM lead) to a member of the team.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, string? assignee, string? returnUrl = null)
    {
        var inquiry = await _db.Inquiries
            .Include(i => i.CrmLead)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inquiry == null) return NotFound();

        var owner = string.IsNullOrWhiteSpace(assignee) ? null : assignee.Trim();
        inquiry.AssignedTo = owner;
        inquiry.UpdatedAt = DateTime.Now;

        if (inquiry.CrmLead != null)
        {
            inquiry.CrmLead.AssignedTo = owner;
        }

        await _db.SaveChangesAsync();

        await ActivityLogger.LogAsync(
            _db,
            "Updated",
            "Inquiry",
            inquiry.Id,
            owner == null
                ? $"Inquiry {inquiry.Reference} was unassigned."
                : $"Inquiry {inquiry.Reference} assigned to {owner}.",
            CurrentUserName());

        TempData["Success"] = owner == null
            ? $"Inquiry {inquiry.Reference} is now unassigned."
            : $"Inquiry {inquiry.Reference} assigned to {owner}.";
        return RedirectToInbox(returnUrl);
    }

    /// <summary>
    /// Removes a spam / duplicate inquiry. The CRM lead stays in the pipeline so
    /// sales history is never lost by deleting an inquiry record.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var inquiry = await _db.Inquiries
            .Include(i => i.CrmLead)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inquiry == null) return NotFound();

        var reference = inquiry.Reference;
        var leadId = inquiry.CrmLeadId;

        if (inquiry.CrmLead != null)
        {
            inquiry.CrmLeadId = null;
            _db.CrmLeads.Update(inquiry.CrmLead);
        }

        _db.Inquiries.Remove(inquiry);
        await _db.SaveChangesAsync();

        await ActivityLogger.LogAsync(
            _db,
            "Deleted",
            "Inquiry",
            leadId,
            $"Inquiry {reference} deleted from the inbox"
                + (leadId.HasValue ? $"; CRM lead #{leadId} kept in the pipeline." : "."),
            CurrentUserName());

        TempData["Success"] = $"Inquiry {reference} deleted.";
        return RedirectToInbox(returnUrl);
    }

    private IActionResult RedirectToInbox(string? returnUrl)
    {
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : Redirect(InboxUrl);
    }

    private string? CurrentUserName()
    {
        return User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
    }
}