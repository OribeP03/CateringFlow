using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

[Authorize]
public class PaymentController : Controller
{
    private readonly CateringFlowDbContext _db;

    public PaymentController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var payments = await _db.Payments
            .Include(p => p.Invoice)
            .Include(p => p.Customer)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        ViewData["TotalCollected"] = payments.Sum(p => p.Amount);
        ViewData["ThisMonth"] = payments.Where(p => p.PaymentDate.Month == DateTime.Now.Month && p.PaymentDate.Year == DateTime.Now.Year).Sum(p => p.Amount);
        ViewData["MostUsedMethod"] = payments.GroupBy(p => p.PaymentMethod)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "N/A";
        ViewData["MostUsedMethodCount"] = payments.GroupBy(p => p.PaymentMethod)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Count())
            .FirstOrDefault();

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
    public async Task<IActionResult> Create(PaymentModel payment)
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
            TempData["Success"] = $"Payment of ₱{payment.Amount:N2} recorded successfully.";
            return RedirectToAction(nameof(Index));
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
    public async Task<IActionResult> Delete(int id)
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
        return RedirectToAction(nameof(Index));
    }
}