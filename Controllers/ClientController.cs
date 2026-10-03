using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

public class ClientController : Controller
{
    /// <summary>
    /// Fallback per-pax estimate used when an inquiry has no package attached yet,
    /// so the CRM lead still carries a sensible pipeline value from day one.
    /// </summary>
    public const decimal DefaultPricePerPax = 750m;

    private readonly CateringFlowDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ClientController(CateringFlowDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Packages"] = await _db.MenuPackages
            .Where(p => p.Status == "Active")
            .OrderBy(p => p.PricePerPax)
            .ToListAsync();
        ViewData["CompanySettings"] = await _db.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
        return View("~/Views/Client/Index.cshtml");
    }

    // Client package exploration page
    public async Task<IActionResult> Packages()
    {
        var packages = await _db.MenuPackages
            .Where(p => p.Status == "Active")
            .OrderBy(p => p.PricePerPax)
            .ToListAsync();
        ViewData["Packages"] = packages;
        ViewData["CompanySettings"] = await _db.Settings.OrderBy(s => s.Id).FirstOrDefaultAsync();
        return View(packages);
    }

    // Returns the signed-in user's details so the booking wizard can pre-fill fields.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> CurrentUser()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "";
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());

        return Json(new
        {
            email,
            name = User.Identity?.Name ?? email.Split('@')[0],
            phone = customer?.Phone ?? ""
        });
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var customer = await EnsureCustomerAsync(email);

        ViewData["RoleLabel"] = User.FindFirstValue(ClaimTypes.Role) ??
                                Request.Cookies["CateringFlow_Role"] ??
                                UserRoles.Customer;
        ViewData["BookingCount"] = customer == null ? 0 :
            await _db.Events.CountAsync(e => e.CustomerId == customer.Id);

        return View(customer);
    }

    [Authorize]
    public async Task<IActionResult> MyBookings()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var events = new List<EventModel>();

        if (!string.IsNullOrWhiteSpace(email))
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());
            if (customer != null)
            {
                events = await _db.Events
                    .Include(e => e.Customer)
                    .Include(e => e.Package)
                    .Include(e => e.Invoices)
                    .Include(e => e.PaymentProofs).ThenInclude(p => p.Messages)
                    .Where(e => e.CustomerId == customer.Id)
                    .OrderByDescending(e => e.EventDate)
                    .ToListAsync();
            }
        }

        return View(events);
    }

    // Customer-facing booking details — only the owner of the event can view it.
    [Authorize]
    public async Task<IActionResult> BookingDetails(int? id)
    {
        if (id == null) return NotFound();

        var email = User.FindFirstValue(ClaimTypes.Email);
        var item = await _db.Events
            .Include(e => e.Customer)
            .Include(e => e.Package)
            .Include(e => e.Invoices)
            .Include(e => e.PaymentProofs).ThenInclude(p => p.Messages)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (item == null || item.Customer == null ||
            string.IsNullOrWhiteSpace(email) ||
            !string.Equals(item.Customer.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        return View(item);
    }

    // Public-facing booking form (must be signed in — the customer is taken
    // from the current session, never trusted from the client).
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(BookingRequest request)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized(new { error = "Please sign in before booking." });
        }

        if (request == null || request.PaxCount <= 0)
        {
            return BadRequest(new { error = "Please provide a guest count." });
        }

        var customer = await EnsureCustomerAsync(email);
        if (customer == null)
        {
            return Unauthorized(new { error = "Please sign in before booking." });
        }

        if (!string.IsNullOrWhiteSpace(request.Phone) && string.IsNullOrWhiteSpace(customer.Phone))
        {
            customer.Phone = request.Phone;
            _db.Customers.Update(customer);
        }

        var package = request.PackageId.HasValue
            ? await _db.MenuPackages.FirstOrDefaultAsync(p => p.Status == "Active" && p.Id == request.PackageId)
            : null;

        var eventType = string.IsNullOrWhiteSpace(request.EventType) ? "Wedding" : request.EventType;
        var eventDate = request.EventDate?.Date ?? DateTime.Today;

        var customerName = request.FullName ?? customer.FullName ?? email.Split('@')[0];
        customer.FullName = customerName;
        _db.Customers.Update(customer);

        var totalAmount = package != null ? Math.Round(package.PricePerPax * request.PaxCount, 2) : request.Amount ?? 0m;

        var booking = new EventModel
        {
            EventName = $"{customerName} - {eventType}",
            CustomerId = customer.Id,
            EventType = eventType,
            EventDate = eventDate,
            Venue = request.Venue,
            PaxCount = request.PaxCount,
            PackageId = package?.Id,
            TotalAmount = totalAmount,
            Status = "Upcoming",
            Notes = request.Notes,
            CreatedAt = DateTime.Now
        };
        _db.Events.Add(booking);

        // Persist the booking first so it gets an Id the invoice and payment
        // proof can reference (SQL FK otherwise rejects EventId = 0).
        await _db.SaveChangesAsync();

        // Generate an invoice so the payment proof can be settled against it.
        var invoice = new InvoiceModel
        {
            InvoiceNumber = $"INV-{DateTime.Now.Year}-{DateTime.Now:HHmmssfff}",
            CustomerId = customer.Id,
            EventId = booking.Id,
            TotalAmount = totalAmount,
            AmountPaid = 0m,
            IssueDate = DateTime.Now,
            DueDate = eventDate,
            Status = "Unpaid",
            CreatedAt = DateTime.Now,
            Notes = $"Created from online booking of {customerName}."
        };
        _db.Invoices.Add(invoice);

        // Persist the payment proof (GCash / PayMaya / Credit Card) submitted with
        // the booking request so the admin can verify it and mark the booking paid.
        string? imagePath = null;
        if (request.ProofImage != null && request.ProofImage.Length > 0)
        {
            imagePath = await SaveProofImageAsync(request.ProofImage);
        }

        PaymentProofModel? paymentProof = null;
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            paymentProof = new PaymentProofModel
            {
                EventId = booking.Id,
                CustomerId = customer.Id,
                PaymentMethod = request.PaymentMethod,
                ReferenceNumber = request.ReferenceNumber,
                ProofImagePath = imagePath,
                Amount = invoice.TotalAmount,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            _db.PaymentProofs.Add(paymentProof);
        }

        if (!string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            if (paymentProof == null)
            {
                paymentProof = new PaymentProofModel
                {
                    EventId = booking.Id,
                    CustomerId = customer.Id,
                    PaymentMethod = request.PaymentMethod ?? "Cash",
                    ReferenceNumber = request.ReferenceNumber,
                    ProofImagePath = imagePath,
                    Amount = invoice.TotalAmount,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                _db.PaymentProofs.Add(paymentProof);
            }
        }

        _db.Notifications.Add(new NotificationModel
        {
            Title = "New Client Booking",
            Message = $"New booking \"{booking.EventName}\" on {booking.EventDate:MMM dd, yyyy} for {booking.PaxCount} pax.",
            Type = "Info",
            IsRead = false,
            TargetRole = "Super Admin",
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();

        if (paymentProof != null && paymentProof.Id > 0 && !string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            _db.PaymentMessages.Add(new PaymentMessageModel
            {
                PaymentProofId = paymentProof.Id,
                SenderRole = "Customer",
                SenderName = customerName,
                Message = request.InitialMessage,
                CreatedAt = DateTime.Now
            });

            _db.Notifications.Add(new NotificationModel
            {
                Title = "Payment Proof Submitted",
                Message = $"{customerName} submitted a {request.PaymentMethod} proof (₱{invoice.TotalAmount:N2}) pending verification.",
                Type = "Warning",
                IsRead = false,
                TargetRole = "Finance Staff",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
        }

        return Ok(new { success = true, bookingId = booking.Id, eventName = booking.EventName });
    }

    // Public contact / request-a-quote form. This is the ONLY way a lead enters the
    // CRM: staff never type a lead in, the website does it for them.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inquiry(InquiryRequest request)
    {
        if (request == null || !ModelState.IsValid)
        {
            var error = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault();

            return BadRequest(new { error = string.IsNullOrWhiteSpace(error) ? "Please check the form and try again." : error });
        }

        var source = string.IsNullOrWhiteSpace(request.Source) ? InquirySources.Website : request.Source.Trim();
        var email = request.Email.Trim();

        // Only link the inquiry to a customer row when that package actually exists.
        var package = request.PackageId.HasValue
            ? await _db.MenuPackages.FirstOrDefaultAsync(p => p.Id == request.PackageId.Value)
            : null;

        var signedInEmail = User.FindFirstValue(ClaimTypes.Email);
        var customer = string.IsNullOrWhiteSpace(signedInEmail)
            ? null
            : await _db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == signedInEmail.ToLower());

        var inquiry = new InquiryModel
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            EventType = string.IsNullOrWhiteSpace(request.EventType) ? null : request.EventType.Trim(),
            Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim(),
            EventDate = request.EventDate?.Date,
            PaxCount = request.PaxCount,
            Venue = string.IsNullOrWhiteSpace(request.Venue) ? null : request.Venue.Trim(),
            PackageId = package?.Id,
            Message = request.Message.Trim(),
            Source = source,
            Status = InquiryStatuses.New,
            CustomerId = customer?.Id,
            CreatedAt = DateTime.Now
        };
        _db.Inquiries.Add(inquiry);
        await _db.SaveChangesAsync();

        // Rough pipeline estimate so the CRM board shows a value on day one:
        // package price per pax when a package was picked, otherwise a per-pax default.
        var pricePerPax = package?.PricePerPax ?? DefaultPricePerPax;
        var estimatedValue = Math.Round(pricePerPax * Math.Max(1, inquiry.PaxCount), 2);

        var lead = new CRMLeadModel
        {
            CustomerId = customer?.Id,
            LeadName = inquiry.FullName,
            Company = inquiry.Company,
            Email = inquiry.Email,
            Phone = inquiry.Phone,
            EstimatedValue = estimatedValue,
            Stage = CRMLeadModelStages.New,
            AssignedTo = null,
            Notes = $"Auto-created from {source} inquiry {inquiry.Reference} on {inquiry.CreatedAt:MMM dd, yyyy}. {inquiry.Message}",
            CreatedAt = inquiry.CreatedAt
        };
        _db.CrmLeads.Add(lead);
        await _db.SaveChangesAsync();

        inquiry.CrmLeadId = lead.Id;
        _db.Inquiries.Update(inquiry);

        _db.Notifications.Add(new NotificationModel
        {
            Title = "New Website Inquiry",
            Message = $"{inquiry.FullName} ({inquiry.Email}) inquired about a {(inquiry.EventType ?? "catering")} event for ~{inquiry.PaxCount} pax. Reference {inquiry.Reference}.",
            Type = "Info",
            IsRead = false,
            TargetRole = "Sales / CRM Staff",
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
        await Services.ActivityLogger.LogAsync(
            _db,
            "Created",
            "Inquiry",
            inquiry.Id,
            $"Website inquiry {inquiry.Reference} from {inquiry.FullName} ({inquiry.Email}) — {inquiry.EventType ?? "Event"}, ~{inquiry.PaxCount} pax. Routed to CRM lead #{lead.Id}.",
            User.Identity?.Name ?? inquiry.FullName);

        return Ok(new
        {
            success = true,
            inquiryId = inquiry.Id,
            reference = inquiry.Reference
        });
    }

    // Client-side chat tied to a payment proof.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ProofMessages(int proofId)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var proof = await _db.PaymentProofs
            .Include(p => p.Customer)
            .Include(p => p.Messages)
            .FirstOrDefaultAsync(p => p.Id == proofId);

        if (proof == null || proof.Customer == null ||
            string.IsNullOrWhiteSpace(email) ||
            !string.Equals(proof.Customer.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var messages = proof.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.SenderRole,
                m.SenderName,
                m.Message,
                m.ImagePath,
                createdAt = m.CreatedAt.ToString("MMMM d, yyyy 'at' h:mm tt")
            });

        return Json(messages);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendProofMessage([FromForm] int proofId, [FromForm] string message)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var proof = await _db.PaymentProofs
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == proofId);

        if (proof == null || proof.Customer == null ||
            string.IsNullOrWhiteSpace(email) ||
            !string.Equals(proof.Customer.Email, email, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(message))
        {
            return BadRequest(new { error = "Unable to send message." });
        }

        var msg = new PaymentMessageModel
        {
            PaymentProofId = proof.Id,
            SenderRole = "Customer",
            SenderName = proof.Customer.FullName,
            Message = message.Trim(),
            CreatedAt = DateTime.Now
        };
        _db.PaymentMessages.Add(msg);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    private async Task<string?> SaveProofImageAsync(IFormFile file)
    {
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "proofs");
        Directory.CreateDirectory(uploadDir);

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
        var fileName = $"proof_{DateTime.Now:yyyyMMdd_HHmmss_fff}{ext}";
        var fullPath = Path.Combine(uploadDir, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/proofs/{fileName}";
    }

    // Finds an existing customer row by the signed-in email, or creates one
    // so the user can see their own profile and bookings immediately.
    private async Task<CustomerModel?> EnsureCustomerAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());
        if (customer != null) return customer;

        var name = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(name)) name = email.Split('@')[0];

        customer = new CustomerModel
        {
            FullName = name,
            Email = email,
            Phone = null,
            Type = "Individual",
            Status = "Active",
            CreatedAt = DateTime.Now
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }
}

public class BookingRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? EventType { get; set; }
    public DateTime? EventDate { get; set; }
    public int PaxCount { get; set; }
    public string? Venue { get; set; }
    public int? PackageId { get; set; }
    public string? PackageName { get; set; }
    public string? Notes { get; set; }

    public string? PaymentMethod { get; set; }
    public string? ReferenceNumber { get; set; }
    public IFormFile? ProofImage { get; set; }
    public string? InitialMessage { get; set; }
    public decimal? Amount { get; set; }
}

/// <summary>
/// Payload of the public Contact / Request-a-Quote form. Validated by the
/// <see cref="Inquiry"/> action, which turns it into an Inquiry + CRM lead.
/// </summary>
public class InquiryRequest
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(40)]
    public string? Phone { get; set; }

    [StringLength(60)]
    public string? EventType { get; set; }

    [StringLength(160)]
    public string? Company { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EventDate { get; set; }

    [Range(0, 100000, ErrorMessage = "Guest count looks invalid.")]
    public int PaxCount { get; set; }

    [StringLength(200)]
    public string? Venue { get; set; }

    public int? PackageId { get; set; }

    [Required(ErrorMessage = "Please tell us about your event.")]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Source { get; set; }
}