using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cateringflow.Data;
using cateringflow.Models;

namespace cateringflow.Controllers;

public class ClientController : Controller
{
    private readonly CateringFlowDbContext _db;

    public ClientController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        return View("~/Views/Client/Index.cshtml");
    }

    public IActionResult Packages()
    {
        return View("~/Views/Client/Index.cshtml");
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
    public async Task<IActionResult> Book([FromBody] BookingRequest request)
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

        var packageName = request.PackageName?.Split('-')[0]?.Trim();
        var package = string.IsNullOrWhiteSpace(packageName) || packageName.Contains("Custom", StringComparison.OrdinalIgnoreCase)
            ? null
            : await _db.MenuPackages.FirstOrDefaultAsync(p => p.Status == "Active" && p.PackageName == packageName);

        var eventType = string.IsNullOrWhiteSpace(request.EventType) ? "Wedding" : request.EventType;
        var eventDate = request.EventDate?.Date ?? DateTime.Today;

        var booking = new EventModel
        {
            EventName = $"{customer.FullName} - {eventType}",
            CustomerId = customer.Id,
            EventType = eventType,
            EventDate = eventDate,
            Venue = request.Venue,
            PaxCount = request.PaxCount,
            PackageId = package?.Id,
            TotalAmount = package != null ? Math.Round(package.PricePerPax * request.PaxCount, 2) : 0,
            Status = "Upcoming",
            Notes = request.Notes,
            CreatedAt = DateTime.Now
        };
        _db.Events.Add(booking);

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

        return Ok(new { success = true, bookingId = booking.Id, eventName = booking.EventName });
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
    public string? PackageName { get; set; }
    public string? Notes { get; set; }
}