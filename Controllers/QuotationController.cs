using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Controllers;

[Authorize]
public class QuotationController : AppController
{
    /// <summary>How long a generated quote stays valid, when the event is further out than that.</summary>
    private const int QuotationValidityDays = 14;

    /// <summary>Event date used when the inquiry never gave one (still "date TBA" on the inquiry).</summary>
    private const int DefaultEventLeadDays = 30;

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
        await PopulateCreateViewData();
        return View();
    }

    /// <summary>
    /// Phase 27 - prefilled "quote this inquiry" screen. Reuses the normal create form
    /// so the sales team can adjust pax, package and notes before committing. This GET is
    /// side-effect free: nothing is written until the quotation is saved.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CreateFromInquiry(int id, string? returnUrl = null)
    {
        var inquiry = await LoadInquiryAsync(id);
        if (inquiry == null) return NotFound();

        if (!inquiry.IsQuotable)
        {
            TempData["Error"] = $"Inquiry {inquiry.Reference} is marked Lost and cannot be quoted.";
            return RedirectToIndex(returnUrl);
        }

        var existing = await _db.Quotations.FirstOrDefaultAsync(q => q.InquiryId == inquiry.Id);
        if (existing != null)
        {
            TempData["Success"] = $"Quotation {existing.QuotationNumber} was already generated from inquiry {inquiry.Reference}.";
            return RedirectToDetails(returnUrl, nameof(Details), new { id = existing.Id });
        }

        // Only *find* a customer here - creating one is a write, and this is a GET.
        var customer = await FindCustomerForInquiryAsync(inquiry);
        var draft = await BuildFromInquiryAsync(inquiry, customer?.Id ?? 0);
        await PopulateCreateViewData();
        return View("Create", draft);
    }

    /// <summary>
    /// Phase 27 - one-click conversion: creates the draft quotation straight from the
    /// inquiry, links everything together and moves the pipeline forward. An inquiry is
    /// only ever quoted once.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConvertFromInquiry(int id, string? returnUrl = null)
    {
        var inquiry = await LoadInquiryAsync(id);
        if (inquiry == null) return NotFound();

        if (!inquiry.IsQuotable)
        {
            TempData["Error"] = $"Inquiry {inquiry.Reference} is marked Lost and cannot be quoted.";
            return RedirectToIndex(returnUrl);
        }

        var existing = await _db.Quotations.FirstOrDefaultAsync(q => q.InquiryId == inquiry.Id);
        if (existing != null)
        {
            TempData["Success"] = $"Quotation {existing.QuotationNumber} already covers inquiry {inquiry.Reference}.";
            return RedirectToDetails(returnUrl, nameof(Details), new { id = existing.Id });
        }

        var customer = await EnsureCustomerForInquiryAsync(inquiry);
        var quotation = await BuildFromInquiryAsync(inquiry, customer.Id);
        _db.Quotations.Add(quotation);
        await _db.SaveChangesAsync();
        await LinkToInquiryAsync(inquiry, quotation);

        TempData["Success"] = $"Quotation {quotation.QuotationNumber} generated from inquiry {inquiry.Reference}.";
        return RedirectToDetails(returnUrl, nameof(Details), new { id = quotation.Id });
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

        InquiryModel? sourceInquiry = null;
        if (quotation.InquiryId.HasValue)
        {
            sourceInquiry = await _db.Inquiries.Include(i => i.CrmLead).FirstOrDefaultAsync(i => i.Id == quotation.InquiryId.Value);
            if (sourceInquiry == null)
            {
                ModelState.AddModelError(nameof(quotation.InquiryId), "The source inquiry no longer exists.");
            }
            else if (!sourceInquiry.IsQuotable)
            {
                ModelState.AddModelError(nameof(quotation.InquiryId), $"Inquiry {sourceInquiry.Reference} is marked Lost and cannot be quoted.");
            }
        }

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

            if (sourceInquiry != null && quotation.CustomerId <= 0)
            {
                // Nobody picked a customer on the form: fall back to the inquiry's client
                // (created on the spot if the inquiry never had a customer record).
                var customer = await EnsureCustomerForInquiryAsync(sourceInquiry);
                quotation.CustomerId = customer.Id;
            }

            _db.Quotations.Add(quotation);
            await _db.SaveChangesAsync();

            if (sourceInquiry != null)
            {
                await LinkToInquiryAsync(sourceInquiry, quotation);
                TempData["Success"] = $"Quotation {quotation.QuotationNumber} created from inquiry {sourceInquiry.Reference}.";
            }
            else
            {
                TempData["Success"] = $"Quotation {quotation.QuotationNumber} created successfully.";
            }

            return RedirectToIndex(returnUrl);
        }
        await PopulateCreateViewData();
        return View("Create", quotation);
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

    // ================= Phase 27: inquiry -> quotation conversion =================

    private Task<InquiryModel?> LoadInquiryAsync(int id)
        => _db.Inquiries
            .Include(i => i.CrmLead)
            .Include(i => i.Package)
            .FirstOrDefaultAsync(i => i.Id == id);

    /// <summary>
    /// The customer a quotation for this inquiry belongs to: the inquiry's own record,
    /// else the customer its CRM lead was won into, else an existing customer with the
    /// same email. Never writes - see <see cref="EnsureCustomerForInquiryAsync"/>.
    /// </summary>
    private async Task<CustomerModel?> FindCustomerForInquiryAsync(InquiryModel inquiry)
    {
        if (inquiry.CustomerId.HasValue)
        {
            var byInquiry = await _db.Customers.FirstOrDefaultAsync(c => c.Id == inquiry.CustomerId.Value);
            if (byInquiry != null) return byInquiry;
        }

        if (inquiry.CrmLead?.CustomerId != null)
        {
            var byLead = await _db.Customers.FirstOrDefaultAsync(c => c.Id == inquiry.CrmLead!.CustomerId.Value);
            if (byLead != null) return byLead;
        }

        if (!string.IsNullOrWhiteSpace(inquiry.Email))
        {
            var email = inquiry.Email;
            return await _db.Customers.FirstOrDefaultAsync(c => c.Email == email);
        }

        return null;
    }

    /// <summary>
    /// Same lookup as above, but creates the customer record when the inquiry has never
    /// been turned into one - mirroring the "winning a lead creates a customer" rule, so
    /// a quotation always has a real client behind it.
    /// </summary>
    private async Task<CustomerModel> EnsureCustomerForInquiryAsync(InquiryModel inquiry)
    {
        var existing = await FindCustomerForInquiryAsync(inquiry);
        if (existing != null) return existing;

        var customer = new CustomerModel
        {
            FullName = inquiry.FullName,
            Email = inquiry.Email,
            Phone = inquiry.Phone,
            Type = string.IsNullOrWhiteSpace(inquiry.Company) ? "Individual" : "Corporate",
            Status = "Active",
            CreatedAt = DateTime.Now,
            Notes = $"Auto-created from {inquiry.Source} inquiry {inquiry.Reference}."
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        inquiry.CustomerId = customer.Id;
        _db.Inquiries.Update(inquiry);
        await _db.SaveChangesAsync();

        return customer;
    }

    /// <summary>
    /// Maps a live inquiry onto a draft quotation: next number, the client, the event
    /// date (or a placeholder), pax (a quotation needs at least one), the package they
    /// asked about, and the price for pax as the starting total.
    /// </summary>
    private async Task<QuotationModel> BuildFromInquiryAsync(InquiryModel inquiry, int customerId)
    {
        var package = inquiry.PackageId.HasValue
            ? await _db.MenuPackages.FirstOrDefaultAsync(p => p.Id == inquiry.PackageId.Value)
            : null;

        var pax = Math.Max(1, inquiry.PaxCount);
        var eventDate = (inquiry.EventDate ?? DateTime.Today.AddDays(DefaultEventLeadDays)).Date;
        var validFrom = DateTime.Today.AddDays(QuotationValidityDays);

        return new QuotationModel
        {
            QuotationNumber = await NextQuotationNumberAsync(),
            CustomerId = customerId,
            EventDate = eventDate,
            PaxCount = pax,
            PackageId = package?.Id,
            TotalAmount = Math.Round((package?.PricePerPax ?? 0m) * pax, 2),
            Status = "Draft",
            ValidUntil = eventDate > validFrom ? eventDate : validFrom,
            Notes = BuildConversionNotes(inquiry),
            CreatedAt = DateTime.Now,
            InquiryId = inquiry.Id,
            Inquiry = inquiry
        };
    }

    private static string BuildConversionNotes(InquiryModel inquiry)
    {
        var notes = $"Converted from inquiry {inquiry.Reference} ({inquiry.Source})";
        if (!string.IsNullOrWhiteSpace(inquiry.EventType)) notes += $" · {inquiry.EventType}";
        if (!string.IsNullOrWhiteSpace(inquiry.Company)) notes += $" · {inquiry.Company}";
        if (!string.IsNullOrWhiteSpace(inquiry.Venue)) notes += $" · Venue: {inquiry.Venue}";
        return notes;
    }

    /// <summary>
    /// Ties a saved quotation back to the inquiry that produced it: the inquiry moves to
    /// <c>Quoted</c>, its CRM lead advances towards <c>Proposal</c> (never backwards, and
    /// never off a closed lead), the sales team is notified and the audit trail records it.
    /// </summary>
    private async Task LinkToInquiryAsync(InquiryModel inquiry, QuotationModel quotation)
    {
        if (inquiry.Status != InquiryStatuses.Won && inquiry.Status != InquiryStatuses.Lost)
        {
            inquiry.Status = InquiryStatuses.Quoted;
        }
        inquiry.UpdatedAt = DateTime.Now;
        _db.Inquiries.Update(inquiry);

        if (inquiry.CrmLeadId.HasValue)
        {
            var lead = await _db.CrmLeads.FindAsync(inquiry.CrmLeadId.Value);
            var stage = lead == null ? null : CrmBoardViewModel.StageForQuotation(lead.Stage);
            if (lead != null && stage != null)
            {
                lead.Stage = stage;
                lead.LastContact = DateTime.Now;
                _db.CrmLeads.Update(lead);
            }
        }

        _db.Notifications.Add(new NotificationModel
        {
            Title = "Quotation Generated from Inquiry",
            Message = $"{quotation.QuotationNumber} (₱{quotation.TotalAmount:N2}, {quotation.PaxCount} pax) was generated from inquiry {inquiry.Reference} for {inquiry.FullName}.",
            Type = "Info",
            IsRead = false,
            TargetRole = "Sales / CRM Staff",
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
        await ActivityLogger.LogAsync(
            _db,
            "Created",
            "Quotation",
            quotation.Id,
            $"Quotation {quotation.QuotationNumber} (₱{quotation.TotalAmount:N2}) generated from inquiry {inquiry.Reference} for {inquiry.FullName}.",
            User.Identity?.Name);
    }

    private async Task<string> NextQuotationNumberAsync()
    {
        var last = await _db.Quotations.OrderByDescending(q => q.Id).FirstOrDefaultAsync();
        if (last == null) return "QTN-1001";
        return int.TryParse(last.QuotationNumber.Replace("QTN-", ""), out var number)
            ? $"QTN-{number + 1}"
            : $"QTN-{last.Id + 1001}";
    }

    private async Task PopulateCreateViewData()
    {
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Events"] = await _db.Events.OrderByDescending(e => e.EventDate).ToListAsync();
        ViewData["Packages"] = await _db.MenuPackages.Where(p => p.Status == "Active").OrderBy(p => p.PackageName).ToListAsync();
        ViewData["SuggestedNumber"] = await NextQuotationNumberAsync();
    }
}