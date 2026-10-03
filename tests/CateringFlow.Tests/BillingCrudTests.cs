using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A25 - Quotation, invoice and payment CRUD.
/// The billing chain must hold together: approving a quotation generates one
/// invoice, payments settle it, and deleting a payment re-opens the balance.
/// </summary>
public class BillingCrudTests
{
    private static QuotationController CreateQuotationController(CateringFlowDbContext db)
    {
        var controller = new QuotationController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static InvoiceController CreateInvoiceController(CateringFlowDbContext db)
    {
        var controller = new InvoiceController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static PaymentController CreatePaymentController(CateringFlowDbContext db)
    {
        var controller = new PaymentController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static void AssertIndexRedirect(IActionResult result)
    {
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    private static async Task<(CustomerModel Customer, EventModel Event, MenuPackageModel Package)> SeedAsync(
        CateringFlowDbContext db)
    {
        var customer = new CustomerModel
        {
            FullName = "Ana Dela Cruz",
            Email = "ana@example.com",
            Type = "Individual",
            Status = "Active"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var package = new MenuPackageModel
        {
            PackageName = "Grand Fiesta",
            PricePerPax = 1500m,
            Status = "Active"
        };
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        var ev = new EventModel
        {
            EventName = "Dela Cruz Wedding",
            CustomerId = customer.Id,
            EventType = "Wedding",
            EventDate = new DateTime(2026, 12, 12),
            PaxCount = 200,
            Status = "Upcoming",
            PackageId = package.Id,
            TotalAmount = 300000m
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return (customer, ev, package);
    }

    // ---------- quotations ----------

    [Fact]
    public async Task Quotation_Index_FiltersAndCountsStatuses()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, _) = await SeedAsync(db);
        db.Quotations.AddRange(
            new QuotationModel { QuotationNumber = "QTN-1001", CustomerId = customer.Id, EventId = ev.Id, EventDate = ev.EventDate, PaxCount = 200, Status = "Draft" },
            new QuotationModel { QuotationNumber = "QTN-1002", CustomerId = customer.Id, EventId = ev.Id, EventDate = ev.EventDate, PaxCount = 200, Status = "Approved" },
            new QuotationModel { QuotationNumber = "QTN-1003", CustomerId = customer.Id, EventId = ev.Id, EventDate = ev.EventDate, PaxCount = 200, Status = "Approved" });
        await db.SaveChangesAsync();

        var controller = CreateQuotationController(db);

        var view = Assert.IsType<ViewResult>(await controller.Index(null, null, null));
        Assert.Equal(3, Assert.IsType<PagedResult<QuotationModel>>(view.Model).TotalItems);
        Assert.Equal(3, Convert.ToInt32(controller.ViewData["TotalQuotations"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalDraft"]));
        Assert.Equal(2, Convert.ToInt32(controller.ViewData["TotalApproved"]));

        var byStatus = Assert.IsType<ViewResult>(await controller.Index(null, "Approved", null));
        Assert.Equal(2, Assert.IsType<PagedResult<QuotationModel>>(byStatus.Model).TotalItems);

        var bySearch = Assert.IsType<ViewResult>(await controller.Index("1001", null, null));
        Assert.Equal("QTN-1001", Assert.IsType<PagedResult<QuotationModel>>(bySearch.Model).Items.Single().QuotationNumber);

        var byCustomer = Assert.IsType<ViewResult>(await controller.Index("Ana", null, null));
        Assert.Equal(3, Assert.IsType<PagedResult<QuotationModel>>(byCustomer.Model).TotalItems);
    }

    [Fact]
    public async Task Quotation_Create_PricesFromTheSelectedPackage()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, package) = await SeedAsync(db);

        var quotation = new QuotationModel
        {
            QuotationNumber = "QTN-1001",
            CustomerId = customer.Id,
            EventId = ev.Id,
            PackageId = package.Id,
            EventDate = ev.EventDate,
            PaxCount = 200,
            Status = "Draft"
        };

        AssertIndexRedirect(await CreateQuotationController(db).Create(quotation));

        var stored = await db.Quotations.AsNoTracking().SingleAsync();
        Assert.Equal(300000m, stored.TotalAmount);
        Assert.NotEqual(default, stored.CreatedAt);
    }

    [Fact]
    public async Task Quotation_Create_WithoutAPackage_HasNoTotal()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, _) = await SeedAsync(db);

        await CreateQuotationController(db).Create(new QuotationModel
        {
            QuotationNumber = "QTN-1001",
            CustomerId = customer.Id,
            EventId = ev.Id,
            EventDate = ev.EventDate,
            PaxCount = 50
        });

        Assert.Equal(0m, (await db.Quotations.AsNoTracking().SingleAsync()).TotalAmount);
    }

    [Fact]
    public async Task Quotation_Create_WithInvalidModel_RedisplaystheForm()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateQuotationController(db);
        var quotation = new QuotationModel { QuotationNumber = "", PaxCount = 200 };
        controller.ModelState.AddModelError(nameof(quotation.QuotationNumber), "Quotation number is required.");

        var view = Assert.IsType<ViewResult>(await controller.Create(quotation));

        Assert.Same(quotation, view.Model);
        Assert.Empty(await db.Quotations.ToListAsync());
    }

    [Fact]
    public async Task Quotation_Create_SuggestsTheNextNumber()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, _) = await SeedAsync(db);
        db.Quotations.Add(new QuotationModel
        {
            QuotationNumber = "QTN-1007",
            CustomerId = customer.Id,
            EventId = ev.Id,
            EventDate = ev.EventDate,
            PaxCount = 100
        });
        await db.SaveChangesAsync();

        var controller = CreateQuotationController(db);
        await controller.Create();

        Assert.Equal("QTN-1008", controller.ViewData["SuggestedNumber"]);
    }

    [Fact]
    public async Task Quotation_UpdateStatus_ApprovingGeneratesExactlyOneInvoice()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, package) = await SeedAsync(db);
        var quotation = new QuotationModel
        {
            QuotationNumber = "QTN-1001",
            CustomerId = customer.Id,
            EventId = ev.Id,
            PackageId = package.Id,
            EventDate = ev.EventDate,
            PaxCount = 200,
            TotalAmount = 300000m,
            Status = "Draft"
        };
        db.Quotations.Add(quotation);
        await db.SaveChangesAsync();

        var controller = CreateQuotationController(db);

        var result = await controller.UpdateStatus(quotation.Id, "Approved");
        var detailsRedirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", detailsRedirect.ActionName);

        var invoice = await db.Invoices.AsNoTracking().SingleAsync();
        Assert.Equal(quotation.Id, invoice.QuotationId);
        Assert.Equal(300000m, invoice.TotalAmount);
        Assert.Equal("Unpaid", invoice.Status);

        await controller.UpdateStatus(quotation.Id, "Approved");
        Assert.Single(await db.Invoices.ToListAsync());
    }

    [Fact]
    public async Task Quotation_UpdateStatus_WithoutApproval_DoesNotInvoice()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, _) = await SeedAsync(db);
        var quotation = new QuotationModel
        {
            QuotationNumber = "QTN-1001",
            CustomerId = customer.Id,
            EventId = ev.Id,
            EventDate = ev.EventDate,
            PaxCount = 200,
            Status = "Draft"
        };
        db.Quotations.Add(quotation);
        await db.SaveChangesAsync();

        await CreateQuotationController(db).UpdateStatus(quotation.Id, "Sent");

        Assert.Equal("Sent", (await db.Quotations.AsNoTracking().SingleAsync()).Status);
        Assert.Empty(await db.Invoices.ToListAsync());
    }

    [Fact]
    public async Task Quotation_UpdateStatus_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateQuotationController(db).UpdateStatus(55, "Approved"));
    }

    [Fact]
    public async Task Quotation_Delete_RemovesTheQuotation()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, _) = await SeedAsync(db);
        var quotation = new QuotationModel
        {
            QuotationNumber = "QTN-1001",
            CustomerId = customer.Id,
            EventId = ev.Id,
            EventDate = ev.EventDate,
            PaxCount = 100
        };
        db.Quotations.Add(quotation);
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreateQuotationController(db).Delete(quotation.Id));
        Assert.Empty(await db.Quotations.ToListAsync());
    }

    [Fact]
    public async Task Quotation_Delete_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateQuotationController(db).Delete(56));
    }

    // ---------- invoices ----------

    [Fact]
    public async Task Invoice_Index_FiltersAndCountsStatuses()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, _) = await SeedAsync(db);
        db.Invoices.AddRange(
            NewInvoice(customer.Id, "INV-1", "Unpaid"),
            NewInvoice(customer.Id, "INV-2", "Paid"),
            NewInvoice(customer.Id, "INV-3", "Partial"));
        await db.SaveChangesAsync();

        var controller = CreateInvoiceController(db);

        var view = Assert.IsType<ViewResult>(await controller.Index(null, null, null));
        Assert.Equal(3, Assert.IsType<PagedResult<InvoiceModel>>(view.Model).TotalItems);
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalUnpaid"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalPaid"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalPartial"]));

        var paid = Assert.IsType<ViewResult>(await controller.Index(null, "Paid", null));
        Assert.Equal("INV-2", Assert.IsType<PagedResult<InvoiceModel>>(paid.Model).Items.Single().InvoiceNumber);
    }

    [Fact]
    public async Task Invoice_Create_ComputesTheStatusFromThePayment()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, _) = await SeedAsync(db);

        AssertIndexRedirect(await CreateInvoiceController(db).Create(NewInvoice(customer.Id, "INV-2026-001", "Unpaid", 100000m, 0m)));

        var stored = await db.Invoices.AsNoTracking().SingleAsync();
        Assert.Equal("Unpaid", stored.Status);

        AssertIndexRedirect(await CreateInvoiceController(db).Create(NewInvoice(customer.Id, "INV-2026-002", "Unpaid", 100000m, 40000m)));
        Assert.Equal("Partial", (await db.Invoices.AsNoTracking().SingleAsync(i => i.InvoiceNumber == "INV-2026-002")).Status);
    }

    [Fact]
    public async Task Invoice_Create_MarksAnOverdueInvoice()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);

        var invoice = NewInvoice(customer.Id, "INV-2026-003", "Unpaid", 100000m, 0m);
        invoice.DueDate = DateTime.Now.AddDays(-5);

        await CreateInvoiceController(db).Create(invoice);

        Assert.Equal("Overdue", (await db.Invoices.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task Invoice_Create_InheritsTheApprovedQuotation()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, ev, package) = await SeedAsync(db);
        var quotation = new QuotationModel
        {
            QuotationNumber = "QTN-1001",
            CustomerId = customer.Id,
            EventId = ev.Id,
            PackageId = package.Id,
            EventDate = ev.EventDate,
            PaxCount = 200,
            TotalAmount = 300000m,
            Status = "Approved"
        };
        db.Quotations.Add(quotation);
        await db.SaveChangesAsync();

        var invoice = NewInvoice(0, "INV-2026-004", "Unpaid", 0m, 0m);
        invoice.QuotationId = quotation.Id;
        invoice.DueDate = DateTime.Now.AddDays(30);

        await CreateInvoiceController(db).Create(invoice);

        var stored = await db.Invoices.AsNoTracking().SingleAsync();
        Assert.Equal(customer.Id, stored.CustomerId);
        Assert.Equal(ev.Id, stored.EventId);
        Assert.Equal(300000m, stored.TotalAmount);
    }

    [Fact]
    public async Task Invoice_Create_WithInvalidModel_RedisplaystheForm()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateInvoiceController(db);
        var invoice = NewInvoice(0, "", "Unpaid", 100m, 0m);
        controller.ModelState.AddModelError(nameof(invoice.InvoiceNumber), "Invoice number is required.");

        var view = Assert.IsType<ViewResult>(await controller.Create(invoice));

        Assert.Same(invoice, view.Model);
        Assert.Empty(await db.Invoices.ToListAsync());
    }

    [Fact]
    public async Task Invoice_UpdateStatus_MarkingPaidClearsTheBalance()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);
        var invoice = NewInvoice(customer.Id, "INV-2026-005", "Partial", 100000m, 40000m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        await CreateInvoiceController(db).UpdateStatus(invoice.Id, "Paid");

        var stored = await db.Invoices.AsNoTracking().SingleAsync();
        Assert.Equal("Paid", stored.Status);
        Assert.Equal(100000m, stored.AmountPaid);
    }

    [Fact]
    public async Task Invoice_UpdateStatus_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateInvoiceController(db).UpdateStatus(77, "Paid"));
    }

    [Fact]
    public async Task Invoice_Print_RendersThePrintableDocument()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);
        var invoice = NewInvoice(customer.Id, "INV-2026-006", "Unpaid", 100000m, 0m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var result = await CreateInvoiceController(db).Print(invoice.Id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Print", view.ViewName);
        Assert.Equal(invoice.Id, Assert.IsType<InvoiceModel>(view.Model).Id);
    }

    [Fact]
    public async Task Invoice_Print_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateInvoiceController(db).Print(78));
    }

    [Fact]
    public async Task Invoice_Delete_RemovesTheInvoice()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);
        var invoice = NewInvoice(customer.Id, "INV-2026-007", "Unpaid", 100000m, 0m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreateInvoiceController(db).Delete(invoice.Id));
        Assert.Empty(await db.Invoices.ToListAsync());
    }

    [Fact]
    public async Task Invoice_Delete_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateInvoiceController(db).Delete(79));
    }

    // ---------- payments ----------

    [Fact]
    public async Task Payment_Index_SummarizesCollections()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);
        var invoice = NewInvoice(customer.Id, "INV-2026-008", "Unpaid", 100000m, 0m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        db.Payments.Add(new PaymentModel
        {
            InvoiceId = invoice.Id,
            Amount = 60000m,
            PaymentMethod = "GCash",
            PaymentDate = DateTime.Now
        });
        await db.SaveChangesAsync();

        var controller = CreatePaymentController(db);
        var view = Assert.IsType<ViewResult>(await controller.Index(null));

        Assert.Single(Assert.IsType<PagedResult<PaymentModel>>(view.Model).Items);
        Assert.Equal(60000m, Convert.ToDecimal(controller.ViewData["TotalCollected"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["PaymentCount"]));
        Assert.Equal(60000m, Convert.ToDecimal(controller.ViewData["ThisMonth"]));
        Assert.Equal("GCash", controller.ViewData["MostUsedMethod"]);
    }

    [Fact]
    public async Task Payment_Create_SettlesTheInvoiceAndLogsIt()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);
        var invoice = NewInvoice(customer.Id, "INV-2026-009", "Unpaid", 100000m, 0m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var result = await CreatePaymentController(db).Create(new PaymentModel
        {
            InvoiceId = invoice.Id,
            Amount = 60000m,
            PaymentMethod = "Bank Transfer",
            ReferenceNumber = "TRX-1",
            PaymentDate = DateTime.Now
        });

        AssertIndexRedirect(result);

        var storedInvoice = await db.Invoices.AsNoTracking().SingleAsync();
        Assert.Equal(60000m, storedInvoice.AmountPaid);
        Assert.Equal("Partial", storedInvoice.Status);

        var payment = await db.Payments.AsNoTracking().SingleAsync();
        Assert.Equal(customer.Id, payment.CustomerId);

        var log = await db.ActivityLogs.AsNoTracking().SingleAsync(l => l.EntityType == "Payment");
        Assert.Equal("Created", log.Action);
    }

    [Fact]
    public async Task Payment_Create_FullyClearsTheInvoice()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);
        var invoice = NewInvoice(customer.Id, "INV-2026-010", "Partial", 100000m, 40000m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        await CreatePaymentController(db).Create(new PaymentModel
        {
            InvoiceId = invoice.Id,
            Amount = 60000m,
            PaymentMethod = "Cash"
        });

        var stored = await db.Invoices.AsNoTracking().SingleAsync();
        Assert.Equal("Paid", stored.Status);
        Assert.Equal(0m, stored.Balance);
    }

    [Fact]
    public async Task Payment_Create_WithInvalidAmount_IsRejected()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);
        var invoice = NewInvoice(customer.Id, "INV-2026-011", "Unpaid", 100000m, 0m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var controller = CreatePaymentController(db);
        var payment = new PaymentModel { InvoiceId = invoice.Id, Amount = 0m, PaymentMethod = "Cash" };
        controller.ModelState.AddModelError(nameof(payment.Amount), "Amount must be greater than zero.");

        Assert.IsType<ViewResult>(await controller.Create(payment));
        Assert.Empty(await db.Payments.ToListAsync());
        Assert.Equal(0m, (await db.Invoices.AsNoTracking().SingleAsync()).AmountPaid);
    }

    [Fact]
    public async Task Payment_Create_OnlyOffersUnpaidInvoices()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);
        db.Invoices.AddRange(
            NewInvoice(customer.Id, "INV-OPEN", "Unpaid"),
            NewInvoice(customer.Id, "INV-CLOSED", "Paid"));
        await db.SaveChangesAsync();

        var controller = CreatePaymentController(db);
        await controller.Create(null);

        var invoices = Assert.IsType<List<InvoiceModel>>(controller.ViewData["Invoices"]);
        Assert.Equal("INV-OPEN", invoices.Single().InvoiceNumber);
    }

    [Fact]
    public async Task Payment_Delete_ReopensTheInvoiceBalance()
    {
        using var db = TestControllerSupport.CreateContext();
        var (customer, _, _) = await SeedAsync(db);
        var invoice = NewInvoice(customer.Id, "INV-2026-012", "Partial", 100000m, 40000m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var payment = new PaymentModel
        {
            InvoiceId = invoice.Id,
            Amount = 40000m,
            PaymentMethod = "Cash",
            PaymentDate = DateTime.Now
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreatePaymentController(db).Delete(payment.Id));

        Assert.Empty(await db.Payments.ToListAsync());
        var stored = await db.Invoices.AsNoTracking().SingleAsync();
        Assert.Equal(0m, stored.AmountPaid);
        Assert.Equal("Unpaid", stored.Status);
    }

    [Fact]
    public async Task Payment_Delete_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreatePaymentController(db).Delete(99));
    }

    private static InvoiceModel NewInvoice(int customerId, string number, string status, decimal total = 100000m, decimal paid = 0m) => new()
    {
        InvoiceNumber = number,
        CustomerId = customerId,
        TotalAmount = total,
        AmountPaid = paid,
        IssueDate = DateTime.Now,
        DueDate = DateTime.Now.AddDays(30),
        Status = status
    };
}