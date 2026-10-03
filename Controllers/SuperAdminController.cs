using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;

namespace cateringflow.Controllers;

public class SuperAdminController : Controller
{
    private readonly CateringFlowDbContext _db;

    public SuperAdminController(CateringFlowDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    public IActionResult Dashboard()
    {
        return RedirectToAction(nameof(DashboardLive));
    }

    public async Task<IActionResult> DashboardLive()
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var today = DateTime.Today;

        var totalBookings = await _db.Events.CountAsync();
        var bookingsThisMonth = await _db.Events.CountAsync(e => e.CreatedAt >= monthStart && e.CreatedAt < monthEnd);
        var activeCustomers = await _db.Customers.CountAsync(c => c.Status == "Active");
        var customersThisMonth = await _db.Customers.CountAsync(c => c.Status == "Active" && c.CreatedAt >= monthStart && c.CreatedAt < monthEnd);
        var pendingQuotations = await _db.Quotations.CountAsync(q => q.Status != "Approved");
        var expiringQuotations = await _db.Quotations.CountAsync(q => q.ValidUntil != null && q.ValidUntil < monthEnd && q.ValidUntil >= today);

        var totalRevenue = await _db.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0m;
        var outstanding = await _db.Invoices
            .Where(i => i.Status != "Paid" && i.Status != "Cancelled")
            .SumAsync(i => (decimal?)(i.TotalAmount - i.AmountPaid)) ?? 0m;
        var overdueCount = await _db.Invoices.CountAsync(i => i.Status == "Overdue");

        var cards = new List<KpiCardModel>
        {
            new() { Label = "Pending Quotations", Value = pendingQuotations.ToString("N0"), SortValue = pendingQuotations, Icon = "bi bi-file-earmark-text", IconPill = "amber", SubNote = expiringQuotations > 0 ? $"{expiringQuotations} expiring this month" : "None expiring this month", SubNoteClass = expiringQuotations > 0 ? "warning" : "positive" },
            new() { Label = "Total Bookings", Value = totalBookings.ToString("N0"), SortValue = totalBookings, Icon = "bi bi-calendar-check", IconPill = "blue", SubNote = bookingsThisMonth > 0 ? $"<i class=\"bi bi-arrow-up-short\"></i> {bookingsThisMonth} new this month" : "No new bookings this month", SubNoteClass = bookingsThisMonth > 0 ? "positive" : "muted" },
            new() { Label = "Active Customers", Value = activeCustomers.ToString("N0"), SortValue = activeCustomers, Icon = "bi bi-people", IconPill = "purple", SubNote = customersThisMonth > 0 ? $"<i class=\"bi bi-arrow-up-short\"></i> {customersThisMonth} this month" : "No new customers this month", SubNoteClass = customersThisMonth > 0 ? "positive" : "muted" },
            new() { Label = "Outstanding", Value = $"₱{outstanding:N0}", SortValue = (double)outstanding, Icon = "bi bi-currency-dollar", IconPill = "red", SubNote = overdueCount > 0 ? $"{overdueCount} overdue invoice{(overdueCount == 1 ? "" : "s")}" : "No overdue invoices", SubNoteClass = overdueCount > 0 ? "danger" : "positive" },
            new() { Label = "Total Revenue", Value = $"₱{totalRevenue:N0}", SortValue = (double)totalRevenue, Icon = "bi bi-graph-up-arrow", IconPill = "gold", SubNote = "Collected from verified payments", SubNoteClass = "positive" }
        };

        var upcoming = await _db.Events
            .Where(e => e.EventDate >= today && (e.Status == "Upcoming" || e.Status == "In Progress"))
            .OrderBy(e => e.EventDate)
            .Take(5)
            .ToListAsync();

        var recentPayments = await _db.Payments
            .Include(p => p.Customer)
            .OrderByDescending(p => p.PaymentDate)
            .Take(5)
            .ToListAsync();

        var stockAlerts = await _db.InventoryItems
            .Where(i => i.CurrentStock <= i.MinReorderLevel)
            .OrderBy(i => i.CurrentStock)
            .Take(5)
            .ToListAsync();

        var recentActivity = await _db.Events
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .ToListAsync();

        var monthlyRevenue = new List<MonthlyRevenuePoint>();
        var monthCursor = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        for (var i = 0; i < 6; i++)
        {
            var start = monthCursor;
            var end = start.AddMonths(1);
            var amount = Math.Round(await _db.Payments
                .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m, 2);
            monthlyRevenue.Add(new MonthlyRevenuePoint { Month = start.ToString("MMM"), Amount = amount });
            monthCursor = monthCursor.AddMonths(1);
        }

        var eventsByType = await _db.Events
            .GroupBy(e => e.EventType)
            .Select(g => new EventTypeCount { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var model = new DashboardViewModel
        {
            KpiCards = Services.StatCardOrdering.OrderIncremental(cards),
            UpcomingEvents = upcoming,
            RecentPayments = recentPayments,
            StockAlerts = stockAlerts,
            RecentActivity = recentActivity,
            MonthlyRevenue = monthlyRevenue,
            EventsByType = eventsByType.OrderByDescending(t => t.Count).ToList()
        };

        return View("Dashboard", model);
    }

    public async Task<IActionResult> Reports()
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var prevMonthStart = monthStart.AddMonths(-1);

        var currentMonthRevenue = Math.Round(await _db.Payments
            .Where(p => p.PaymentDate >= monthStart && p.PaymentDate < monthEnd)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m, 2);
        var previousMonthRevenue = Math.Round(await _db.Payments
            .Where(p => p.PaymentDate >= prevMonthStart && p.PaymentDate < monthStart)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m, 2);
        var revenueChangePercent = previousMonthRevenue > 0
            ? Math.Round((currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue * 100m, 1)
            : (currentMonthRevenue > 0 ? 100m : 0m);

        var totalCollected = Math.Round(await _db.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0m, 2);
        var totalBookings = await _db.Events.CountAsync();
        var avgBookingValue = totalBookings > 0
            ? Math.Round(await _db.Events.AverageAsync(e => e.TotalAmount), 2)
            : 0m;
        var pendingQuotations = await _db.Quotations.CountAsync(q => q.Status != "Approved");
        var lowStockItems = await _db.InventoryItems.CountAsync(i => i.CurrentStock <= i.MinReorderLevel);
        var completedEvents = await _db.Events.CountAsync(e => e.Status == "Completed");
        var cancelledEvents = await _db.Events.CountAsync(e => e.Status == "Cancelled");

        var cards = new List<KpiCardModel>
        {
            new() { Label = "Low Stock Items", Value = lowStockItems.ToString("N0"), SortValue = lowStockItems, Icon = "bi bi-exclamation-triangle", IconPill = "amber", SubNoteClass = "positive" },
            new() { Label = "Pending Quotations", Value = pendingQuotations.ToString("N0"), SortValue = pendingQuotations, Icon = "bi bi-file-earmark-text", IconPill = "sky" },
            new() { Label = "Completed Events", Value = completedEvents.ToString("N0"), SortValue = completedEvents, Icon = "bi bi-check2-circle", IconPill = "green", SubNote = cancelledEvents > 0 ? $"{cancelledEvents} cancelled" : "No cancellations", SubNoteClass = cancelledEvents > 0 ? "danger" : "positive" },
            new() { Label = "Total Bookings", Value = totalBookings.ToString("N0"), SortValue = totalBookings, Icon = "bi bi-calendar-check", IconPill = "blue", SubNote = $"Avg. ₱{avgBookingValue:N0} per booking", SubNoteClass = "muted" },
            new() { Label = "Total Collected", Value = $"₱{totalCollected:N0}", SortValue = (double)totalCollected, Icon = "bi bi-piggy-bank", IconPill = "gold", SubNote = "Across all verified payments", SubNoteClass = "positive" }
        };

        var revenueTrend = new List<MonthlyRevenuePoint>();
        var monthCursor = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        for (var i = 0; i < 6; i++)
        {
            var start = monthCursor;
            var end = start.AddMonths(1);
            var amount = Math.Round(await _db.Payments
                .Where(p => p.PaymentDate >= start && p.PaymentDate < end)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m, 2);
            revenueTrend.Add(new MonthlyRevenuePoint { Month = start.ToString("MMM"), Amount = amount });
            monthCursor = monthCursor.AddMonths(1);
        }

        var eventsByType = await _db.Events
            .GroupBy(e => e.EventType)
            .Select(g => new EventTypeCount { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var topPackages = await _db.Events
            .Where(e => e.PackageId != null && e.Package != null)
            .GroupBy(e => e.Package!.PackageName)
            .Select(g => new PackageStat { PackageName = g.Key, Count = g.Count(), Revenue = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync();

        var invoiceBuckets = await _db.Invoices
            .GroupBy(i => i.Status)
            .Select(g => new StatusBreakdown { Status = g.Key, Count = g.Count(), TotalAmount = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.TotalAmount)
            .ToListAsync();

        var paymentMethods = await _db.Payments
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new MethodStat { Method = g.Key, Count = g.Count(), TotalAmount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.TotalAmount)
            .ToListAsync();

        var model = new ReportsViewModel
        {
            ReportKpis = Services.StatCardOrdering.OrderIncremental(cards),
            RevenueTrend = revenueTrend,
            EventsByType = eventsByType.OrderByDescending(t => t.Count).ToList(),
            TopPackages = topPackages,
            InvoiceBuckets = invoiceBuckets,
            PaymentMethods = paymentMethods,
            TotalEvents = totalBookings,
            CompletedEvents = completedEvents,
            CancelledEvents = cancelledEvents,
            PendingQuotations = pendingQuotations,
            LowStockItems = lowStockItems,
            CurrentMonthRevenue = currentMonthRevenue,
            PreviousMonthRevenue = previousMonthRevenue,
            RevenueChangePercent = revenueChangePercent
        };

        return View("Reports", model);
    }

    public async Task<IActionResult> Calendar(int? year, int? month)
    {
        var now = DateTime.Now;
        var y = year ?? now.Year;
        var m = month ?? now.Month;
        if (y < 2000 || y > 2100) y = now.Year;
        if (m < 1 || m > 12) m = now.Month;

        var first = new DateTime(y, m, 1);
        var monthStart = first;
        var monthEnd = first.AddMonths(1);
        var offset = (int)first.DayOfWeek;

        var monthEvents = await _db.Events
            .Where(e => e.EventDate >= monthStart && e.EventDate < monthEnd)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        var cells = new List<CalendarDay>();
        var gridStart = first.AddDays(-offset);
        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var inMonth = date.Year == y && date.Month == m;
            cells.Add(new CalendarDay
            {
                Date = date,
                InMonth = inMonth,
                IsToday = date.Date == DateTime.Today,
                Events = inMonth
                    ? monthEvents.Where(e => e.EventDate.Date == date.Date).OrderBy(e => e.EventDate).ToList()
                    : new List<EventModel>()
            });
        }

        var model = new CalendarViewModel
        {
            Year = y,
            Month = m,
            TotalDaysInMonth = DateTime.DaysInMonth(y, m),
            WeekdayOffset = offset,
            Days = cells,
            PrevMonthDate = first.AddMonths(-1),
            NextMonthDate = first.AddMonths(1),
            MonthName = first.ToString("MMMM yyyy"),
            UpcomingCount = await _db.Events.CountAsync(e => e.EventDate >= monthStart && e.EventDate < monthEnd && (e.Status == "Upcoming" || e.Status == "In Progress")),
            MonthRevenue = Math.Round(await _db.Events
                .Where(e => e.EventDate >= monthStart && e.EventDate < monthEnd)
                .SumAsync(e => (decimal?)e.TotalAmount) ?? 0m, 2)
        };

        return View("Calendar", model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCustomers(string? search, string? type, string? status)
    {
        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.FullName.Contains(search) || c.Email.Contains(search) || (c.Phone != null && c.Phone.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(type) && type != "All")
        {
            query = query.Where(c => c.Type == type);
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(c => c.Status == status);
        }

        var customers = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        var bytes = BuildCsv(
            new[] { "Id", "FullName", "Email", "Phone", "Address", "Type", "Status", "CreatedAt" },
            customers.Select(c => new[] { c.Id.ToString(), c.FullName, c.Email, c.Phone ?? string.Empty, c.Address ?? string.Empty, c.Type, c.Status, c.CreatedAt.ToString("yyyy-MM-dd HH:mm") }));

        return File(bytes, "text/csv; charset=utf-8", $"customers-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportPayments(string? search, string? method)
    {
        var query = _db.Payments
            .Include(p => p.Customer)
            .Include(p => p.Invoice)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => (p.Customer != null && p.Customer.FullName.Contains(search)) || (p.Invoice != null && p.Invoice.InvoiceNumber.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(method) && method != "All")
        {
            query = query.Where(p => p.PaymentMethod == method);
        }

        var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
        var bytes = BuildCsv(
            new[] { "Id", "InvoiceNumber", "CustomerName", "Amount", "PaymentMethod", "ReferenceNumber", "PaymentDate" },
            payments.Select(p => new[]
            {
                p.Id.ToString(),
                p.Invoice?.InvoiceNumber ?? string.Empty,
                p.Customer?.FullName ?? string.Empty,
                p.Amount.ToString("F2"),
                p.PaymentMethod,
                p.ReferenceNumber ?? string.Empty,
                p.PaymentDate.ToString("yyyy-MM-dd")
            }));

        return File(bytes, "text/csv; charset=utf-8", $"payments-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportInventory(string? category, string? stockStatus)
    {
        var query = _db.InventoryItems
            .Include(i => i.Supplier)
            .AsQueryable();

        if (string.IsNullOrWhiteSpace(category)) category = null;
        if (string.IsNullOrWhiteSpace(stockStatus)) stockStatus = null;
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(i => i.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(stockStatus) && stockStatus != "All")
        {
            query = query.Where(i => i.StockStatus == stockStatus);
        }

        var items = await query.OrderBy(i => i.ItemName).ToListAsync();
        var bytes = BuildCsv(
            new[] { "Id", "ItemCode", "ItemName", "Category", "StockStatus", "CurrentStock", "MinReorderLevel", "Unit", "UnitCost", "Supplier" },
            items.Select(i => new[]
            {
                i.Id.ToString(),
                i.ItemCode,
                i.ItemName,
                i.Category,
                i.StockStatus,
                i.CurrentStock.ToString("0.##"),
                i.MinReorderLevel.ToString("0.##"),
                i.Unit ?? string.Empty,
                i.UnitCost.ToString("F2"),
                i.Supplier?.SupplierName ?? string.Empty
            }));

        return File(bytes, "text/csv; charset=utf-8", $"inventory-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ActivityLog(string? search, string? entityType, string? action, int? page)
    {
        var query = _db.ActivityLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.Description.Contains(search) || (l.PerformedBy != null && l.PerformedBy.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(entityType) && entityType != "All")
        {
            query = query.Where(l => l.EntityType == entityType);
        }
        if (!string.IsNullOrWhiteSpace(action) && action != "All")
        {
            query = query.Where(l => l.Action == action);
        }

        var logs = await PagedResult<ActivityLogModel>.CreateAsync(
            query.OrderByDescending(l => l.CreatedAt),
            page);

        ViewData["Search"] = search;
        ViewData["EntityTypeFilter"] = entityType;
        ViewData["ActionFilter"] = action;
        ViewData["TotalEntries"] = await _db.ActivityLogs.CountAsync();
        return View("ActivityLog", logs);
    }

    private static byte[] BuildCsv(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var sb = new StringBuilder();

        void AppendRow(IEnumerable<string> cells)
        {
            sb.Append(string.Join(",", cells.Select(EscapeCsv))).Append("\r\n");
        }

        AppendRow(headers);
        foreach (var row in rows)
        {
            AppendRow(row);
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    public async Task<IActionResult> Customers(string? search, string? type, string? status, int? page)
    {
        var query = _db.Customers
            .Include(c => c.Events)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.FullName.Contains(search) || c.Email.Contains(search) || (c.Phone != null && c.Phone.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(type) && type != "All")
        {
            query = query.Where(c => c.Type == type);
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(c => c.Status == status);
        }

        var customers = await PagedResult<CustomerModel>.CreateAsync(
            query.OrderByDescending(c => c.CreatedAt),
            page);

        ViewData["Search"] = search;
        ViewData["TypeFilter"] = type;
        ViewData["StatusFilter"] = status;
        return View(customers);
    }

    /// <summary>
    /// Live CRM pipeline. Leads are never typed in by hand — they are created
    /// automatically from website inquiries (see <see cref="ClientController.Inquiry"/>) —
    /// so this page is read + qualify + advance only, with no "add lead" control.
    /// </summary>
    public async Task<IActionResult> CRM(string? search, string? stage, string? assignee, int? page)
    {
        var query = _db.CrmLeads
            .Include(l => l.Customer)
            .AsQueryable();

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

        var filtered = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

        // The board always shows the full picture (every lead, unfiltered) so the
        // pipeline columns stay meaningful while the table below is filtered.
        var allLeads = await _db.CrmLeads
            .Include(l => l.Customer)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        var paged = await PagedResult<CRMLeadModel>.CreateAsync(
            query.OrderByDescending(l => l.CreatedAt),
            page);

        ViewData["Search"] = search;
        ViewData["StageFilter"] = stage;
        ViewData["AssigneeFilter"] = assignee;

        return View(CrmBoardViewModel.Build(allLeads, paged, search, stage, assignee));
    }

    /// <summary>
    /// Inquiry inbox — where website inquiries land and the events team works them
    /// by hand (assign, quote, win/lose). Read-only here; all writes go through
    /// <see cref="InquiryController"/> so there is no manual "add inquiry" action.
    /// </summary>
    public async Task<IActionResult> Inquiries(string? search, string? status, string? assignee, int? page)
    {
        var query = _db.Inquiries
            .Include(i => i.Package)
            .Include(i => i.CrmLead)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.FullName.Contains(search)
                                     || (i.Email != null && i.Email.Contains(search))
                                     || (i.Company != null && i.Company.Contains(search))
                                     || (i.Venue != null && i.Venue.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            var requested = status;
            query = query.Where(i => i.Status == requested);
        }
        if (!string.IsNullOrWhiteSpace(assignee) && assignee != "All")
        {
            var owner = assignee;
            query = query.Where(i => i.AssignedTo == owner);
        }

        ViewData["TotalInquiries"] = await _db.Inquiries.CountAsync();
        ViewData["TotalNew"] = await _db.Inquiries.CountAsync(i => i.Status == InquiryStatuses.New);
        ViewData["TotalContacted"] = await _db.Inquiries.CountAsync(i => i.Status == InquiryStatuses.Contacted);
        ViewData["TotalQuoted"] = await _db.Inquiries.CountAsync(i => i.Status == InquiryStatuses.Quoted);
        ViewData["TotalWon"] = await _db.Inquiries.CountAsync(i => i.Status == InquiryStatuses.Won);
        ViewData["TotalUnassigned"] = await _db.Inquiries.CountAsync(i => i.AssignedTo == null || i.AssignedTo == "");
        ViewData["Search"] = search;
        ViewData["StatusFilter"] = status;
        ViewData["AssigneeFilter"] = assignee;

        var inquiries = await PagedResult<InquiryModel>.CreateAsync(
            query.OrderByDescending(i => i.CreatedAt),
            page);

        return View("Inquiries", inquiries);
    }

    public async Task<IActionResult> Events(string? search, string? type, string? status, int? page)
    {
        var query = _db.Events
            .Include(e => e.Customer)
            .Include(e => e.Package)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.EventName.Contains(search) || (e.Venue != null && e.Venue.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(type) && type != "All")
        {
            query = query.Where(e => e.EventType == type);
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(e => e.Status == status);
        }

        var events = await PagedResult<EventModel>.CreateAsync(
            query.OrderBy(e => e.EventDate),
            page);

        ViewData["Search"] = search;
        ViewData["TypeFilter"] = type;
        ViewData["StatusFilter"] = status;
        ViewData["Customers"] = await _db.Customers.Where(c => c.Status == "Active").OrderBy(c => c.FullName).ToListAsync();
        ViewData["Packages"] = await _db.MenuPackages.Where(p => p.Status == "Active").OrderBy(p => p.PackageName).ToListAsync();
        return View(events);
    }

    public IActionResult MenuPackages()
    {
        return View();
    }

    public async Task<IActionResult> Inventory(string? search, string? category, string? stockStatus, int? page)
    {
        var query = _db.InventoryItems
            .Include(i => i.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.ItemName.Contains(search) || i.ItemCode.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(i => i.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(stockStatus) && stockStatus != "All")
        {
            query = query.Where(i => i.StockStatus == stockStatus);
        }

        var items = await PagedResult<InventoryModel>.CreateAsync(
            query.OrderBy(i => i.ItemCode),
            page);

        ViewData["Search"] = search;
        ViewData["CategoryFilter"] = category;
        ViewData["StockStatusFilter"] = stockStatus;
        var lastItem = await _db.InventoryItems.OrderByDescending(i => i.Id).FirstOrDefaultAsync();
        var nextNumber = lastItem != null ? int.Parse(lastItem.ItemCode.Replace("INV-", "")) + 1 : 1;
        ViewData["SuggestedCode"] = $"INV-{nextNumber:D3}";
        ViewData["Suppliers"] = await _db.Suppliers.Where(s => s.Status == "Active").OrderBy(s => s.SupplierName).ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> Suppliers(string? search, string? category, int? page)
    {
        var query = _db.Suppliers
            .Include(s => s.InventoryItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.SupplierName.Contains(search) || (s.ContactPerson != null && s.ContactPerson.Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(s => s.Category == category);
        }

        var suppliers = await PagedResult<SupplierModel>.CreateAsync(
            query.OrderByDescending(s => s.CreatedAt),
            page);

        ViewData["Search"] = search;
        ViewData["CategoryFilter"] = category;
        return View(suppliers);
    }

    public IActionResult Staff()
    {
        return View();
    }

    public IActionResult Quotations()
    {
        return View();
    }

    public IActionResult Invoices()
    {
        return View();
    }

    public IActionResult Payments()
    {
        return View();
    }

    public IActionResult Notifications()
    {
        return View();
    }

    public IActionResult Settings()
    {
        return View();
    }

    // ---- Payment Proofs (client-submitted pending payments + chat) ----

    public async Task<IActionResult> PaymentProofs()
    {
        var proofs = await _db.PaymentProofs
            .Include(p => p.Customer)
            .Include(p => p.Event)
            .Include(p => p.Messages)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(proofs);
    }

    [HttpGet]
    public async Task<IActionResult> ProofAdminChat(int id)
    {
        var proof = await _db.PaymentProofs
            .Include(p => p.Messages)
            .Include(p => p.Event)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (proof == null)
        {
            return Json(new { error = "Not found" });
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

        return Json(new
        {
            proof = new
            {
                proof.Id,
                paymentMethod = proof.PaymentMethod,
                referenceNumber = proof.ReferenceNumber,
                amount = proof.Amount,
                status = proof.Status,
                createdAt = proof.CreatedAt.ToString("MMMM d, yyyy 'at' h:mm tt"),
                proofImagePath = proof.ProofImagePath,
                eventName = proof.Event?.EventName ?? "Unknown booking",
                customerName = proof.Customer?.FullName ?? "Unknown customer"
            },
            messages
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendProofAdminMessage([FromForm] int proofId, [FromForm] string message)
    {
        var proof = await _db.PaymentProofs.FindAsync(proofId);
        if (proof == null || string.IsNullOrWhiteSpace(message))
        {
            return BadRequest(new { error = "Unable to send message." });
        }

        var name = User.Identity?.Name ?? "Admin";
        _db.PaymentMessages.Add(new PaymentMessageModel
        {
            PaymentProofId = proof.Id,
            SenderRole = "Admin",
            SenderName = name,
            Message = message.Trim(),
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePaymentProof(int id)
    {
        var proof = await _db.PaymentProofs
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (proof == null) return NotFound();

        // Settlement target: the single invoice for this customer's event.
        // Online bookings create one in ClientController.Book (customer + event linked).
        var invoice = proof.Event != null
            ? await _db.Invoices.FirstOrDefaultAsync(i => i.CustomerId == proof.CustomerId && i.EventId == proof.Event.Id)
            : null;

        // No invoice yet — guarantee the chain is connected by creating one for the event.
        if (invoice == null && proof.Event != null)
        {
            var lastInvoice = await _db.Invoices.OrderByDescending(i => i.Id).FirstOrDefaultAsync();
            invoice = new InvoiceModel
            {
                InvoiceNumber = NextInvoiceNumber(lastInvoice),
                CustomerId = proof.CustomerId,
                EventId = proof.Event.Id,
                TotalAmount = proof.Event.TotalAmount > 0 ? proof.Event.TotalAmount : proof.Amount,
                AmountPaid = 0m,
                IssueDate = DateTime.Now,
                DueDate = proof.Event.EventDate,
                Status = "Unpaid",
                CreatedAt = DateTime.Now,
                Notes = $"Invoice auto-created from approved payment proof #{proof.Id}."
            };
            _db.Invoices.Add(invoice);
            // Persist the invoice first so the official Payment can reference its Id.
            await _db.SaveChangesAsync();
        }

        proof.Status = "Approved";

        if (invoice != null)
        {
            var remainingBefore = invoice.TotalAmount - invoice.AmountPaid;
            var applied = Math.Min(proof.Amount, Math.Max(remainingBefore, 0m));

            invoice.AmountPaid += applied;
            invoice.Status = invoice.AmountPaid >= invoice.TotalAmount ? "Paid" : "Partial";
            _db.Invoices.Update(invoice);

            // Record the official payment entry on every approval.
            _db.Payments.Add(new PaymentModel
            {
                InvoiceId = invoice.Id,
                CustomerId = proof.CustomerId,
                Amount = applied,
                PaymentMethod = proof.PaymentMethod,
                ReferenceNumber = proof.ReferenceNumber,
                PaymentDate = DateTime.Now,
                CreatedAt = DateTime.Now,
                Notes = $"Verified from client proof #{proof.Id}."
            });
        }

        _db.PaymentProofs.Update(proof);
        _db.Notifications.Add(new NotificationModel
        {
            Title = "Payment Verified",
            Message = $"Payment proof #{proof.Id} ({proof.PaymentMethod}) for ₱{proof.Amount:N2} was approved.",
            Type = "Success",
            IsRead = false,
            TargetRole = "Finance Staff",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();

        await ActivityLogger.LogAsync(_db, "Approved", "PaymentProof", proof.Id, $"Payment proof #{proof.Id} approved — ₱{proof.Amount:N2} recorded against invoice {(invoice != null ? invoice.InvoiceNumber : "none")}.", User.Identity?.Name);

        TempData["Success"] = $"Payment proof #{proof.Id} approved and \u20b1{proof.Amount:N2} recorded against invoice {(invoice != null ? invoice.InvoiceNumber : "none")}.";
        return RedirectToAction(nameof(PaymentProofs));
    }

    private static string NextInvoiceNumber(InvoiceModel? last)
    {
        if (last != null && last.InvoiceNumber.Split('-').Length > 1
            && int.TryParse(last.InvoiceNumber.Split('-')[^1], out var n))
        {
            return $"INV-{DateTime.Now.Year}-{(n + 1):D3}";
        }
        return $"INV-{DateTime.Now.Year}-{DateTime.Now:MMddHHmmssfff}";
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPaymentProof(int id)
    {
        var proof = await _db.PaymentProofs.FindAsync(id);
        if (proof == null) return NotFound();

        proof.Status = "Rejected";
        _db.PaymentProofs.Update(proof);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Payment proof #{proof.Id} marked as rejected.";
        return RedirectToAction(nameof(PaymentProofs));
    }
}