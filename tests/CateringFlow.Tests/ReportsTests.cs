using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class ReportsTests
{
    private static SuperAdminController CreateSuperAdmin(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<CustomerModel> SeedCustomer(CateringFlowDbContext db, string name)
    {
        var customer = new CustomerModel
        {
            FullName = name,
            Email = $"{name.ToLower().Replace(" ", string.Empty)}@test.com",
            Type = "Individual",
            Status = "Active"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private static async Task<MenuPackageModel> SeedPackage(CateringFlowDbContext db, string name, decimal price)
    {
        var package = new MenuPackageModel { PackageName = name, PricePerPax = price, Status = "Active" };
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();
        return package;
    }

    private static async Task<EventModel> SeedEvent(CateringFlowDbContext db, CustomerModel customer, string name, int? packageId, decimal amount, string status = "Upcoming", string eventType = "Wedding")
    {
        var item = new EventModel
        {
            EventName = name,
            CustomerId = customer.Id,
            EventType = eventType,
            EventDate = DateTime.Today.AddDays(5),
            PaxCount = 100,
            Status = status,
            TotalAmount = amount,
            PackageId = packageId
        };
        db.Events.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static async Task<InvoiceModel> SeedInvoice(CateringFlowDbContext db, CustomerModel customer, decimal total, string status)
    {
        var invoice = new InvoiceModel
        {
            InvoiceNumber = $"INV-R-{Guid.NewGuid():N}".Substring(0, 20),
            CustomerId = customer.Id,
            TotalAmount = total,
            AmountPaid = status == "Paid" ? total : 0m,
            Status = status,
            DueDate = DateTime.Today.AddDays(10),
            IssueDate = DateTime.Now
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    private static async Task<PaymentModel> SeedPayment(CateringFlowDbContext db, CustomerModel customer, InvoiceModel invoice, decimal amount, DateTime date, string method)
    {
        var payment = new PaymentModel
        {
            InvoiceId = invoice.Id,
            CustomerId = customer.Id,
            Amount = amount,
            PaymentDate = date,
            PaymentMethod = method
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return payment;
    }

    [Fact]
    public async Task Reports_ReturnsViewModel_WithSixMonthTrend()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateSuperAdmin(db);

        var result = await controller.Reports();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Reports", view.ViewName);
        var model = Assert.IsType<ReportsViewModel>(view.Model);
        Assert.Equal(6, model.RevenueTrend.Count);
        Assert.NotNull(model.ReportKpis);
    }

    [Fact]
    public async Task Reports_KpisAscending_TotalCollectedLargest()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Kpi Reporter");
        var inv = await SeedInvoice(db, customer, 100000m, "Paid");
        await SeedPayment(db, customer, inv, 100000m, DateTime.Now.AddDays(-2), "GCash");
        await SeedEvent(db, customer, "Event A", null, 60000m, status: "Completed");
        await SeedEvent(db, customer, "Quoted", null, 50000m);

        // Pending quotation (Sent).
        db.Quotations.Add(new QuotationModel { CustomerId = customer.Id, QuotationNumber = "QT-R-1", Status = "Sent", TotalAmount = 80000m });
        await db.SaveChangesAsync();

        // Low stock item.
        db.InventoryItems.Add(new InventoryModel { ItemCode = "INV-R-1", ItemName = "Beef", Category = "Meats", CurrentStock = 2, MinReorderLevel = 10, Unit = "kg", StockStatus = "Low Stock" });
        await db.SaveChangesAsync();

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.Reports());
        var model = Assert.IsType<ReportsViewModel>(view.Model);

        for (var i = 1; i < model.ReportKpis.Count; i++)
            Assert.True(model.ReportKpis[i].SortValue >= model.ReportKpis[i - 1].SortValue);

        Assert.Equal("Total Collected", model.ReportKpis[^1].Label);
        Assert.Equal("₱100,000", model.ReportKpis[^1].Value);
        Assert.Contains(model.ReportKpis, c => c.Label == "Low Stock Items" && c.Value == "1");
    }

    [Fact]
    public async Task Reports_RevenueChangePercent_Computed()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Delta");
        var invoice = await SeedInvoice(db, customer, 400000m, "Paid");

        var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 10);
        var prevMonth = thisMonth.AddMonths(-1).AddDays(2);
        await SeedPayment(db, customer, invoice, 30000m, thisMonth, "GCash");
        await SeedPayment(db, customer, invoice, 100000m, prevMonth, "Bank Transfer");

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.Reports());
        var model = Assert.IsType<ReportsViewModel>(view.Model);

        Assert.Equal(30000m, model.CurrentMonthRevenue);
        Assert.Equal(100000m, model.PreviousMonthRevenue);
        Assert.Equal(-70m, model.RevenueChangePercent);
    }

    [Fact]
    public async Task Reports_TopPackages_OrderedByRevenue()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Package Reporter");
        var premium = await SeedPackage(db, "Premium Feast", 1000m);
        var compact = await SeedPackage(db, "Compact Set", 600m);

        await SeedEvent(db, customer, "Big Wedding", premium.Id, 150000m);
        await SeedEvent(db, customer, "Second Wedding", premium.Id, 120000m);
        await SeedEvent(db, customer, "Small Party", compact.Id, 30000m);

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.Reports());
        var model = Assert.IsType<ReportsViewModel>(view.Model);

        Assert.Equal(2, model.TopPackages.Count);
        Assert.Equal("Premium Feast", model.TopPackages[0].PackageName);
        Assert.Equal(2, model.TopPackages[0].Count);
        Assert.Equal(270000m, model.TopPackages[0].Revenue);
        Assert.Equal("Compact Set", model.TopPackages[1].PackageName);
    }

    [Fact]
    public async Task Reports_InvoiceBucketsAndPaymentMethods_Grouped()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Bucket Reporter");
        var paidInv = await SeedInvoice(db, customer, 50000m, "Paid");
        var overdueInv = await SeedInvoice(db, customer, 80000m, "Overdue");
        var unpaidInv = await SeedInvoice(db, customer, 20000m, "Unpaid");

        await SeedPayment(db, customer, paidInv, 50000m, DateTime.Now.AddDays(-1), "GCash");
        await SeedPayment(db, customer, overdueInv, 20000m, DateTime.Now.AddDays(-1), "Bank Transfer");
        await SeedPayment(db, customer, unpaidInv, 5000m, DateTime.Now.AddDays(-1), "Cash");

        var controller = CreateSuperAdmin(db);
        var view = Assert.IsType<ViewResult>(await controller.Reports());
        var model = Assert.IsType<ReportsViewModel>(view.Model);

        Assert.Equal(3, model.InvoiceBuckets.Count);
        Assert.Contains(model.InvoiceBuckets, b => b.Status == "Paid" && b.Count == 1 && b.TotalAmount == 50000m);
        Assert.Contains(model.InvoiceBuckets, b => b.Status == "Overdue" && b.Count == 1 && b.TotalAmount == 80000m);

        Assert.Equal(3, model.PaymentMethods.Count);
        Assert.Contains(model.PaymentMethods, m => m.Method == "GCash" && m.Count == 1 && m.TotalAmount == 50000m);
        Assert.Contains(model.PaymentMethods, m => m.Method == "Cash" && m.Count == 1 && m.TotalAmount == 5000m);
    }
}