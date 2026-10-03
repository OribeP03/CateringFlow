using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Controllers;

[Authorize]
public class PaymentController : AppController
{
    private readonly CateringFlowDbContext _db;

    public PaymentController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? page)
    {
        var query = _db.Payments
            .Include(p => p.Invoice)
            .Include(p => p.Customer)
            .AsQueryable();

        var now = DateTime.Now;
        var thisMonthQuery = _db.Payments.Where(p => p.PaymentDate.Month == now.Month && p.PaymentDate.Year == now.Year);
        var methodStat = await _db.Payments
            .GroupBy(p => p.PaymentMethod)
            .OrderByDescending(g => g.Count())
            .Select(g => new { g.Key, Count = g.Count() })
            .FirstOrDefaultAsync();

        ViewData["TotalCollected"] = await _db.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0m;
        ViewData["PaymentCount"] = await _db.Payments.CountAsync();
        ViewData["ThisMonth"] = await thisMonthQuery.SumAsync(p => (decimal?)p.Amount) ?? 0m;
        ViewData["ThisMonthCount"] = await thisMonthQuery.CountAsync();
        ViewData["MostUsedMethod"] = methodStat?.Key ?? "N/A";
        ViewData["MostUsedMethodCount"] = methodStat?.Count;

        var payments = await PagedResult<PaymentModel>.CreateAsync(
            query.OrderByDescending(p => p.PaymentDate),
            page);

        return View(payments);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? invoiceId)
    {
        ViewData["Invoices"] = await _db.Invoices
            .Where(i => i.Status != "Paid")
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        ViewData["SelectedInvoiceId"] = invoiceId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentModel payment, string? returnUrl = null)
    {
        payment.CreatedAt = DateTime.Now;
        ModelState.Remove(nameof(payment.Invoice));
        ModelState.Remove(nameof(payment.Customer));
        ModelState.Remove(nameof(payment.InvoiceNumber));

        var invoice = payment.InvoiceId > 0 ? await _db.Invoices.FindAsync(payment.InvoiceId) : null;
        if (invoice != null)
        {
            payment.CustomerId = invoice.CustomerId;
        }

        if (ModelState.IsValid)
        {
            if (invoice != null)
            {
                invoice.AmountPaid += payment.Amount;
                invoice.Status = invoice.AmountPaid >= invoice.TotalAmount ? "Paid" : "Partial";
                _db.Invoices.Update(invoice);
            }

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            await ActivityLogger.LogAsync(_db, "Created", "Payment", payment.Id, $"Payment of ₱{payment.Amount:N2} via {payment.PaymentMethod} recorded{(invoice != null ? $" against {invoice.InvoiceNumber}" : "")}.", User.Identity?.Name);
            TempData["Success"] = $"Payment of ₱{payment.Amount:N2} recorded successfully.";
            return RedirectToIndex(returnUrl);
        }

        ViewData["Invoices"] = await _db.Invoices
            .Where(i => i.Status != "Paid")
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        ViewData["SelectedInvoiceId"] = payment.InvoiceId;
        return View(payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl = null)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment == null) return NotFound();

        var invoice = await _db.Invoices.FindAsync(payment.InvoiceId);
        if (invoice != null)
        {
            invoice.AmountPaid = Math.Max(0, invoice.AmountPaid - payment.Amount);
            invoice.Status = invoice.AmountPaid <= 0 ? "Unpaid"
                : invoice.AmountPaid < invoice.TotalAmount ? "Partial" : "Paid";
            _db.Invoices.Update(invoice);
        }

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Payment deleted and invoice balance updated.";
        return RedirectToIndex(returnUrl);
    }
}