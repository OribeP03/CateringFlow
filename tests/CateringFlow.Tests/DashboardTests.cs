using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class DashboardTests
{
    private static SuperAdminController CreateSuperAdmin(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<CustomerModel> SeedCustomer(CateringFlowDbContext db, string name, DateTime? createdAt = null)
    {
        var customer = new CustomerModel
        {
            FullName = name,
            Email = $"{name.ToLower().Replace(" ", string.Empty)}@test.com",
            Type = "Individual",
            Status = "Active",
            CreatedAt = createdAt ?? DateTime.Now
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private static async Task<EventModel> SeedEvent(CateringFlowDbContext db, string name, CustomerModel customer, DateTime date, string status = "Upcoming", decimal amount = 50000m, DateTime? createdAt = null, string eventType = "Wedding")
    {
        var item = new EventModel
        {
            EventName = name,
            CustomerId = customer.Id,
            EventType = eventType,
            EventDate = date,
            PaxCount = 120,
            Status = status,
            TotalAmount = amount,
            CreatedAt = createdAt ?? DateTime.Now
        };
        db.Events.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static async Task<InvoiceModel> SeedInvoice(CateringFlowDbContext db, CustomerModel customer, decimal total, decimal paid, string status, DateTime dueDate)
    {
        var invoice = new InvoiceModel
        {
            InvoiceNumber = $"INV-T-{Guid.NewGuid():N}".Substring(0, 20),
            CustomerId = customer.Id,
            TotalAmount = total,
            AmountPaid = paid,
            Status = status,
            DueDate = dueDate,
            IssueDate = DateTime.Now
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    private static async Task<PaymentModel> SeedPayment(CateringFlowDbContext db, CustomerModel customer, InvoiceModel invoice, decimal amount, DateTime date, string method = "GCash")
    {
        var payment = new PaymentModel
        {
            InvoiceId = invoice.Id,
            CustomerId = customer.Id,
            Amount = amount,
            PaymentDate = date,
            PaymentMethod = method,
            CreatedAt = date
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return payment;
    }

    private static async Task<InventoryModel> SeedInventory(CateringFlowDbContext db, string name, double stock, double reorder)
    {
        var item = new InventoryModel
        {
            ItemCode = $"INV-{Guid.NewGuid():N}".Substring(0, 12).ToUpper(),
            ItemName = name,
            Category = "Meats & Poultry",
            CurrentStock = stock,
            MinReorderLevel = reorder,
            Unit = "kg",
            UnitCost = 100m,
            StockStatus = stock == 0 ? "Out of Stock" : (stock <= reorder ? "Low Stock" : "Adequate")
        };
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    [Fact]
    public async Task DashboardLive_ReturnsDashboardViewModel()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateSuperAdmin(db);

        var result = await controller.DashboardLive();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Dashboard", view.ViewName);
        var model = Assert.IsType<DashboardViewModel>(view.Model);
        Assert.NotNull(model.KpiCards);
        Assert.NotNull(model.MonthlyRevenue);
        Assert.Equal(6, model.MonthlyRevenue.Count);
    }

    [Fact]
    public async Task DashboardLive_KpiCardsAscending_RevenueLargestRight()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Kpi Tester");
        await SeedEvent(db, "Founders Day", customer, DateTime.Today.AddDays(5), amount: 100000m);
        await SeedEvent(db, "Reyes Wedding", customer, DateTime.Today.AddDays(10), amount: 200000m);
        var invoice = await SeedInvoice(db, customer, 300000m, 150000m, "Partially Paid", DateTime.Today.AddDays(20));
        var q1 = new QuotationModel { CustomerId = customer.Id, QuotationNumber = $"QT-{Guid.NewGuid():N}".Substring(0, 12), Status = "Approved", TotalAmount = 50000m };
        var q2 = new QuotationModel { CustomerId = customer.Id, QuotationNumber = $"QT-{Guid.NewGuid():N}".Substring(0, 12), Status = "Sent", TotalAmount = 70000m };
        db.Quotations.AddRange(q1, q2);
        await db.SaveChangesAsync();
        await SeedPayment(db, customer, invoice, 150000m, DateTime.Now.AddDays(-1));

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.DashboardLive());
        var model = Assert.IsType<DashboardViewModel>(view.Model);

        // Stat-card rule: ascending SortValue (largest value last/right).
        for (var i = 1; i < model.KpiCards.Count; i++)
            Assert.True(model.KpiCards[i].SortValue >= model.KpiCards[i - 1].SortValue);

        Assert.Equal("Total Revenue", model.KpiCards[^1].Label);
        Assert.Equal("Pending Quotations", model.KpiCards[0].Label);
        Assert.Contains(model.KpiCards, c => c.Label == "Total Bookings" && c.Value == "2");
        Assert.Contains(model.KpiCards, c => c.Label == "Active Customers" && c.Value == "1");
    }

    [Fact]
    public async Task DashboardLive_UpcomingEvents_OnlyFutureSorted()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Upcoming Tester");
        await SeedEvent(db, "Past Event", customer, DateTime.Today.AddDays(-10));
        var e1 = await SeedEvent(db, "Later Event", customer, DateTime.Today.AddDays(20));
        var e2 = await SeedEvent(db, "Soon Event", customer, DateTime.Today.AddDays(3));
        await SeedEvent(db, "Cancelled", customer, DateTime.Today.AddDays(7), status: "Cancelled");

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.DashboardLive());
        var model = Assert.IsType<DashboardViewModel>(view.Model);

        Assert.Equal(2, model.UpcomingEvents.Count);
        Assert.Equal(e2.Id, model.UpcomingEvents[0].Id);
        Assert.Equal(e1.Id, model.UpcomingEvents[1].Id);
        Assert.All(model.UpcomingEvents, e => Assert.True(e.EventDate >= DateTime.Today));
    }

    [Fact]
    public async Task DashboardLive_RecentPayments_NewestFirst()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Payment Tester");
        var invoice = await SeedInvoice(db, customer, 300000m, 0m, "Unpaid", DateTime.Today.AddDays(20));
        var old = await SeedPayment(db, customer, invoice, 10000m, DateTime.Today.AddDays(-5), "Bank Transfer");
        var mid = await SeedPayment(db, customer, invoice, 20000m, DateTime.Today.AddDays(-2));
        var newest = await SeedPayment(db, customer, invoice, 30000m, DateTime.Today.AddDays(-1));

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.DashboardLive());
        var model = Assert.IsType<DashboardViewModel>(view.Model);

        Assert.Equal(3, model.RecentPayments.Count);
        Assert.Equal(newest.Id, model.RecentPayments[0].Id);
        Assert.Equal(mid.Id, model.RecentPayments[1].Id);
        Assert.Equal(old.Id, model.RecentPayments[2].Id);
    }

    [Fact]
    public async Task DashboardLive_StockAlerts_OnlyBelowReorder()
    {
        using var db = TestControllerSupport.CreateContext();
        await SeedInventory(db, "Pork Belly", 5, 10);
        await SeedInventory(db, "Beef Sirloin", 0, 10);
        await SeedInventory(db, "Onions", 15, 5);
        await SeedInventory(db, "Garlic", 20, 3);

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.DashboardLive());
        var model = Assert.IsType<DashboardViewModel>(view.Model);

        Assert.Equal(2, model.StockAlerts.Count);
        Assert.Contains(model.StockAlerts, i => i.ItemName == "Pork Belly");
        Assert.Contains(model.StockAlerts, i => i.ItemName == "Beef Sirloin");
        Assert.All(model.StockAlerts, i => Assert.True(i.CurrentStock <= i.MinReorderLevel));
    }

    [Fact]
    public async Task DashboardLive_MonthlyRevenue_AggregatedByMonth()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Trend Tester");
        var invoice = await SeedInvoice(db, customer, 500000m, 0m, "Unpaid", DateTime.Today.AddDays(20));

        var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 10);
        var lastMonth = thisMonth.AddMonths(-1).AddDays(3);
        var oldMonth = thisMonth.AddMonths(-4).AddDays(5);
        await SeedPayment(db, customer, invoice, 10000m, thisMonth);
        await SeedPayment(db, customer, invoice, 5000m, thisMonth);
        await SeedPayment(db, customer, invoice, 30000m, lastMonth);
        await SeedPayment(db, customer, invoice, 80000m, oldMonth);

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.DashboardLive());
        var model = Assert.IsType<DashboardViewModel>(view.Model);

        Assert.Equal(6, model.MonthlyRevenue.Count);
        var lastPoint = model.MonthlyRevenue[^1];
        Assert.Equal(thisMonth.ToString("MMM"), lastPoint.Month);
        Assert.Equal(15000m, lastPoint.Amount);
        var lastMonthPoint = model.MonthlyRevenue[^2];
        Assert.Equal(30000m, lastMonthPoint.Amount);
        Assert.Equal(0m, model.MonthlyRevenue[0].Amount);
        Assert.Equal(80000m, model.MonthlyRevenue[1].Amount);
    }

    [Fact]
    public async Task DashboardLive_EventsByType_Grouped()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Type Tester");
        await SeedEvent(db, "Wedding A", customer, DateTime.Today.AddDays(3));
        await SeedEvent(db, "Wedding B", customer, DateTime.Today.AddDays(5));
        await SeedEvent(db, "Birthday C", customer, DateTime.Today.AddDays(8), eventType: "Birthday");
        await SeedEvent(db, "Town Hall", customer, DateTime.Today.AddDays(12), eventType: "Corporate");

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.DashboardLive());
        var model = Assert.IsType<DashboardViewModel>(view.Model);

        Assert.Contains(model.EventsByType, t => t.Type == "Wedding" && t.Count == 2);
        Assert.Contains(model.EventsByType, t => t.Type == "Birthday" && t.Count == 1);
        Assert.Contains(model.EventsByType, t => t.Type == "Corporate" && t.Count == 1);
        Assert.True(model.EventsByType[0].Count >= model.EventsByType[^1].Count);
    }
}