using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class InvoicePrintTests
{
    private static InvoiceController CreateInvoiceController(CateringFlowDbContext db)
    {
        var controller = new InvoiceController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<CustomerModel> SeedCustomer(CateringFlowDbContext db, string name = "Print Customer")
    {
        var customer = new CustomerModel
        {
            FullName = name,
            Email = $"{name.ToLower().Replace(" ", string.Empty)}@test.com",
            Type = "Company",
            Status = "Active",
            Address = "123 Main St",
            Phone = "0917-000-1111"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private static async Task<InvoiceModel> SeedInvoice(CateringFlowDbContext db, CustomerModel customer, bool withEvent = true, bool withPayment = true)
    {
        var cEvent = new EventModel
        {
            EventName = "Gala Night",
            CustomerId = customer.Id,
            EventType = "Corporate",
            EventDate = DateTime.Today.AddDays(7),
            PaxCount = 90,
            Status = "Upcoming",
            TotalAmount = 150000m
        };
        db.Events.Add(cEvent);
        await db.SaveChangesAsync();

        var invoice = new InvoiceModel
        {
            InvoiceNumber = $"INV-P-{Guid.NewGuid():N}".Substring(0, 18).ToUpper(),
            CustomerId = customer.Id,
            EventId = withEvent ? cEvent.Id : null,
            TotalAmount = 150000m,
            AmountPaid = withPayment ? 50000m : 0m,
            Status = withPayment ? "Partial" : "Unpaid",
            DueDate = DateTime.Today.AddDays(15),
            IssueDate = DateTime.Today,
            Notes = "Balance due on or before due date."
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        if (withPayment)
        {
            db.Payments.Add(new PaymentModel
            {
                InvoiceId = invoice.Id,
                CustomerId = customer.Id,
                Amount = 50000m,
                PaymentMethod = "GCash",
                PaymentDate = DateTime.Today.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        return invoice;
    }

    [Fact]
    public async Task Print_WithValidId_ReturnsPrintView()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var invoice = await SeedInvoice(db, customer);

        var controller = CreateInvoiceController(db);
        var result = await controller.Print(invoice.Id);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Print", view.ViewName);
        var model = Assert.IsType<InvoiceModel>(view.Model);
        Assert.Equal(invoice.Id, model.Id);
    }

    [Fact]
    public async Task Print_LoadsCustomerEventAndPayments()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var invoice = await SeedInvoice(db, customer, withEvent: true, withPayment: true);

        var controller = CreateInvoiceController(db);
        var view = Assert.IsType<ViewResult>(await controller.Print(invoice.Id));
        var model = Assert.IsType<InvoiceModel>(view.Model);

        Assert.NotNull(model.Customer);
        Assert.Equal("Print Customer", model.Customer!.FullName);
        Assert.NotNull(model.Event);
        Assert.Equal("Gala Night", model.Event!.EventName);
        Assert.Single(model.Payments!);
        Assert.Equal(150000m - 50000m, model.Balance);
    }

    [Fact]
    public async Task Print_WithoutPayments_StillRenders()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var invoice = await SeedInvoice(db, customer, withPayment: false);

        var controller = CreateInvoiceController(db);
        var view = Assert.IsType<ViewResult>(await controller.Print(invoice.Id));
        var model = Assert.IsType<InvoiceModel>(view.Model);

        Assert.NotNull(model.Payments);
        Assert.Equal(150000m, model.Balance);
    }

    [Fact]
    public async Task Print_WithNullId_ReturnsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateInvoiceController(db);

        var result = await controller.Print(null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Print_WithUnknownId_ReturnsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var invoice = await SeedInvoice(db, customer);

        var controller = CreateInvoiceController(db);
        var result = await controller.Print(invoice.Id + 999);

        Assert.IsType<NotFoundResult>(result);
    }
}