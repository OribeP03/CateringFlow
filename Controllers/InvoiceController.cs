using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Controllers;

[Authorize]
public class InvoiceController : AppController
{
    private readonly CateringFlowDbContext _db;

    public InvoiceController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? status, int? page)
    {
        var query = _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Quotation)
            .Include(i => i.Event)
            .Include(i => i.Payments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.InvoiceNumber.Contains(search) || i.Customer != null && i.Customer.FullName.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(i => i.Status == status);
        }

        ViewData["TotalInvoices"] = await query.CountAsync();
        ViewData["TotalPartial"] = await query.CountAsync(i => i.Status == "Partial");
        ViewData["TotalUnpaid"] = await query.CountAsync(i => i.Status == "Unpaid");
        ViewData["TotalOverdue"] = await query.CountAsync(i => i.Status == "Overdue");
        ViewData["TotalPaid"] = await query.CountAsync(i => i.Status == "Paid");

        var invoices = await PagedResult<InvoiceModel>.CreateAsync(
            query.OrderByDescending(i => i.IssueDate),
            page);

        ViewData["Search"] = search;
        ViewData["StatusFilter"] = status;
        return View(invoices);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Quotation)
            .Include(i => i.Event)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return NotFound();
        return View(invoice);
    }

    [HttpGet]
    public async Task<IActionResult> Print(int? id)
    {
        if (id == null) return NotFound();
        var invoice = await _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Quotation)
            .Include(i => i.Event)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return NotFound();
        return View("Print", invoice);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Quotations"] = await _db.Quotations.Where(q => q.Status == "Approved").OrderByDescending(q => q.CreatedAt).ToListAsync();
        ViewData["Events"] = await _db.Events.OrderByDescending(e => e.EventDate).ToListAsync();
        var lastInvoice = await _db.Invoices.OrderByDescending(i => i.Id).FirstOrDefaultAsync();
        var nextNumber = lastInvoice != null ? int.Parse(lastInvoice.InvoiceNumber.Split('-')[^1]) + 1 : 1;
        ViewData["SuggestedNumber"] = $"INV-{DateTime.Now.Year}-{nextNumber:D3}";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceModel invoice, string? returnUrl = null)
    {
        invoice.CreatedAt = DateTime.Now;
        invoice.Status = ComputeInvoiceStatus(invoice);
        ModelState.Remove(nameof(invoice.Customer));
        ModelState.Remove(nameof(invoice.Quotation));
        ModelState.Remove(nameof(invoice.Event));
        ModelState.Remove(nameof(invoice.CustomerName));
        ModelState.Remove(nameof(invoice.Balance));

        if (invoice.QuotationId.HasValue)
        {
            var quotation = await _db.Quotations.FindAsync(invoice.QuotationId.Value);
            if (quotation != null)
            {
                if (invoice.CustomerId == 0) invoice.CustomerId = quotation.CustomerId;
                if (!invoice.EventId.HasValue) invoice.EventId = quotation.EventId;
                if (invoice.TotalAmount == 0) invoice.TotalAmount = quotation.TotalAmount;
            }
        }

        if (ModelState.IsValid)
        {
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            await ActivityLogger.LogAsync(_db, "Created", "Invoice", invoice.Id, $"Invoice {invoice.InvoiceNumber} created for ₱{invoice.TotalAmount:N2} ({invoice.Status}).", User.Identity?.Name);
            TempData["Success"] = $"Invoice {invoice.InvoiceNumber} created successfully.";
            return RedirectToIndex(returnUrl);
        }
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Quotations"] = await _db.Quotations.Where(q => q.Status == "Approved").OrderByDescending(q => q.CreatedAt).ToListAsync();
        ViewData["Events"] = await _db.Events.OrderByDescending(e => e.EventDate).ToListAsync();
        return View(invoice);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl = null)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice == null) return NotFound();
        invoice.Status = status;
        if (status == "Paid")
        {
            invoice.AmountPaid = invoice.TotalAmount;
        }
        _db.Invoices.Update(invoice);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Invoice status updated to \"{status}\".";
        return RedirectToDetails(returnUrl, nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice == null) return NotFound();
        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Invoice deleted successfully.";
        return RedirectToIndex(returnUrl);
    }

    private static string ComputeInvoiceStatus(InvoiceModel invoice)
    {
        if (invoice.DueDate < DateTime.Now && invoice.AmountPaid < invoice.TotalAmount) return "Overdue";
        if (invoice.AmountPaid <= 0) return "Unpaid";
        if (invoice.AmountPaid < invoice.TotalAmount) return "Partial";
        return "Paid";
    }
}