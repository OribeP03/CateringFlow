using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class QuotationController : AppController
{
    private readonly CateringFlowDbContext _db;

    public QuotationController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? status, int? page)
    {
        var query = _db.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Event)
            .Include(q => q.Package)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(q => q.QuotationNumber.Contains(search) || q.Customer != null && q.Customer.FullName.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(q => q.Status == status);
        }

        ViewData["TotalQuotations"] = await query.CountAsync();
        ViewData["TotalDraft"] = await query.CountAsync(q => q.Status == "Draft");
        ViewData["TotalRejected"] = await query.CountAsync(q => q.Status == "Rejected");
        ViewData["TotalSent"] = await query.CountAsync(q => q.Status == "Sent");
        ViewData["TotalApproved"] = await query.CountAsync(q => q.Status == "Approved");

        var quotations = await PagedResult<QuotationModel>.CreateAsync(
            query.OrderByDescending(q => q.CreatedAt),
            page);

        ViewData["Search"] = search;
        ViewData["StatusFilter"] = status;
        return View(quotations);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var quotation = await _db.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Event)
            .Include(q => q.Package)
            .Include(q => q.Invoice)
            .FirstOrDefaultAsync(q => q.Id == id);
        if (quotation == null) return NotFound();
        return View(quotation);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Events"] = await _db.Events.OrderByDescending(e => e.EventDate).ToListAsync();
        ViewData["Packages"] = await _db.MenuPackages.Where(p => p.Status == "Active").OrderBy(p => p.PackageName).ToListAsync();
        var lastQuote = await _db.Quotations.OrderByDescending(q => q.Id).FirstOrDefaultAsync();
        ViewData["SuggestedNumber"] = lastQuote != null ? $"QTN-{int.Parse(lastQuote.QuotationNumber.Replace("QTN-", "")) + 1}" : "QTN-1001";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuotationModel quotation, string? returnUrl = null)
    {
        quotation.CreatedAt = DateTime.Now;
        ModelState.Remove(nameof(quotation.Customer));
        ModelState.Remove(nameof(quotation.Event));
        ModelState.Remove(nameof(quotation.Package));
        ModelState.Remove(nameof(quotation.CustomerName));
        ModelState.Remove(nameof(quotation.PackageName));
        if (ModelState.IsValid)
        {
            if (quotation.PackageId.HasValue)
            {
                var package = await _db.MenuPackages.FindAsync(quotation.PackageId.Value);
                quotation.TotalAmount = package != null ? package.PricePerPax * quotation.PaxCount : 0;
            }
            else
            {
                quotation.TotalAmount = 0;
            }
            _db.Quotations.Add(quotation);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Quotation {quotation.QuotationNumber} created successfully.";
            return RedirectToIndex(returnUrl);
        }
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Events"] = await _db.Events.OrderByDescending(e => e.EventDate).ToListAsync();
        ViewData["Packages"] = await _db.MenuPackages.Where(p => p.Status == "Active").OrderBy(p => p.PackageName).ToListAsync();
        return View(quotation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl = null)
    {
        var quotation = await _db.Quotations.FindAsync(id);
        if (quotation == null) return NotFound();
        quotation.Status = status;
        _db.Quotations.Update(quotation);
        await _db.SaveChangesAsync();

        if (status == "Approved" && quotation.Invoice == null)
        {
            var count = await _db.Invoices.CountAsync();
            var invoice = new InvoiceModel
            {
                InvoiceNumber = $"INV-{DateTime.Now.Year}-{(count + 1):D3}",
                QuotationId = quotation.Id,
                CustomerId = quotation.CustomerId,
                EventId = quotation.EventId,
                TotalAmount = quotation.TotalAmount,
                AmountPaid = 0,
                DueDate = quotation.EventDate > DateTime.Now ? quotation.EventDate : DateTime.Now.AddDays(30),
                Status = "Unpaid",
                CreatedAt = DateTime.Now
            };
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Quotation approved. Invoice {invoice.InvoiceNumber} auto-generated.";
            return RedirectToDetails(returnUrl, nameof(Details), new { id });
        }

        TempData["Success"] = $"Quotation status updated to \"{status}\".";
        return RedirectToDetails(returnUrl, nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var quotation = await _db.Quotations.FindAsync(id);
        if (quotation == null) return NotFound();
        _db.Quotations.Remove(quotation);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Quotation deleted successfully.";
        return RedirectToIndex(returnUrl);
    }
}