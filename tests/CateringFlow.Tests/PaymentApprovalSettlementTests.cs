using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class PaymentApprovalSettlementTests
{
    private static SuperAdminController CreateSuperAdmin(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<CustomerModel> SeedCustomer(CateringFlowDbContext db, string name = "Payer")
    {
        var customer = new CustomerModel
        {
            FullName = name,
            Email = $"{name.ToLower()}@test.com",
            Type = "Individual",
            Status = "Active",
            CreatedAt = DateTime.Now
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private static async Task<EventModel> SeedEvent(CateringFlowDbContext db, CustomerModel customer, decimal total = 10000m)
    {
        var evt = new EventModel
        {
            EventName = "Settlement Event",
            CustomerId = customer.Id,
            EventType = "Wedding",
            EventDate = DateTime.Today.AddDays(30),
            PaxCount = 50,
            Status = "Upcoming",
            TotalAmount = total,
            CreatedAt = DateTime.Now
        };
        db.Events.Add(evt);
        await db.SaveChangesAsync();
        return evt;
    }

    private static async Task<PaymentProofModel> SeedProof(CateringFlowDbContext db, CustomerModel customer, EventModel evt, decimal amount)
    {
        var proof = new PaymentProofModel
        {
            EventId = evt.Id,
            CustomerId = customer.Id,
            PaymentMethod = "GCash",
            ReferenceNumber = "GC-777",
            Amount = amount,
            Status = "Pending",
            CreatedAt = DateTime.Now
        };
        db.PaymentProofs.Add(proof);
        await db.SaveChangesAsync();
        return proof;
    }

    [Fact]
    public async Task Approve_WithExistingBookingInvoice_RecordsPaymentAgainstIt()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var evt = await SeedEvent(db, customer, 10000m);
        db.Invoices.Add(new InvoiceModel
        {
            InvoiceNumber = "INV-2026-0001",
            CustomerId = customer.Id,
            EventId = evt.Id,
            TotalAmount = 10000m,
            AmountPaid = 0m,
            IssueDate = DateTime.Now,
            DueDate = evt.EventDate,
            Status = "Unpaid",
            CreatedAt = DateTime.Now
        });
        var proof = await SeedProof(db, customer, evt, 5000m);
        var controller = CreateSuperAdmin(db);

        var result = await controller.ApprovePaymentProof(proof.Id);

        Assert.IsType<RedirectToActionResult>(result);
        var invoice = await db.Invoices.SingleAsync();
        Assert.Equal("INV-2026-0001", invoice.InvoiceNumber);
        Assert.Equal(5000m, invoice.AmountPaid);
        Assert.Equal("Partial", invoice.Status);
        Assert.Equal(1, await db.Invoices.CountAsync());

        var payment = await db.Payments.SingleAsync();
        Assert.Equal(invoice.Id, payment.InvoiceId);
        Assert.Equal(5000m, payment.Amount);
        Assert.Equal("GCash", payment.PaymentMethod);

        Assert.Equal("Approved", (await db.PaymentProofs.FindAsync(proof.Id))!.Status);
    }

    [Fact]
    public async Task Approve_WhenNoInvoiceExists_AutoCreatesInvoiceLinkedToEventAndRecordsPayment()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var evt = await SeedEvent(db, customer, 12000m);
        var proof = await SeedProof(db, customer, evt, 12000m);
        var controller = CreateSuperAdmin(db);

        var result = await controller.ApprovePaymentProof(proof.Id);

        Assert.IsType<RedirectToActionResult>(result);

        var invoice = await db.Invoices.SingleAsync();
        Assert.Equal(customer.Id, invoice.CustomerId);
        Assert.Equal(evt.Id, invoice.EventId);
        Assert.Equal(12000m, invoice.TotalAmount);
        Assert.Equal(12000m, invoice.AmountPaid);
        Assert.Equal("Paid", invoice.Status);
        Assert.StartsWith("INV-", invoice.InvoiceNumber);

        var payment = await db.Payments.SingleAsync();
        Assert.Equal(invoice.Id, payment.InvoiceId);
        Assert.Equal(customer.Id, payment.CustomerId);
        Assert.Equal(12000m, payment.Amount);
    }

    [Fact]
    public async Task Approve_Overpayment_CappedAtRemainingInvoiceBalance()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var evt = await SeedEvent(db, customer, 5000m);
        db.Invoices.Add(new InvoiceModel
        {
            InvoiceNumber = "INV-2026-0002",
            CustomerId = customer.Id,
            EventId = evt.Id,
            TotalAmount = 5000m,
            AmountPaid = 0m,
            IssueDate = DateTime.Now,
            DueDate = evt.EventDate,
            Status = "Unpaid",
            CreatedAt = DateTime.Now
        });
        var proof = await SeedProof(db, customer, evt, 8000m);
        var controller = CreateSuperAdmin(db);

        await controller.ApprovePaymentProof(proof.Id);

        var invoice = await db.Invoices.SingleAsync();
        Assert.Equal(5000m, invoice.AmountPaid);
        Assert.Equal("Paid", invoice.Status);
        Assert.Equal(5000m, (await db.Payments.SingleAsync()).Amount);
    }

    [Fact]
    public async Task Approve_WhenEventHasNoInvoice_StillRecordsPayment_WithProperLinkedAmount()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var evt = await SeedEvent(db, customer, 0m); // event with no amount (no package booking)
        var proof = await SeedProof(db, customer, evt, 3000m);
        var controller = CreateSuperAdmin(db);

        await controller.ApprovePaymentProof(proof.Id);

        var invoice = await db.Invoices.SingleAsync();
        Assert.Equal(3000m, invoice.TotalAmount); // falls back to the proof amount
        Assert.Equal(3000m, invoice.AmountPaid);
        Assert.Equal("Paid", invoice.Status);
    }

    [Fact]
    public async Task Reject_OnlyFlipsProofStatus_NoPaymentOrInvoiceCreated()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var evt = await SeedEvent(db, customer, 10000m);
        var proof = await SeedProof(db, customer, evt, 4000m);
        var controller = CreateSuperAdmin(db);

        var result = await controller.RejectPaymentProof(proof.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Rejected", (await db.PaymentProofs.FindAsync(proof.Id))!.Status);
        Assert.Empty(await db.Payments.ToListAsync());
        Assert.Empty(await db.Invoices.ToListAsync());
    }
}