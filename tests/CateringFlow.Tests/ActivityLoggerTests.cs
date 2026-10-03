using System.Security.Claims;
using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class ActivityLoggerTests
{
    private static HttpContext CreateContextWithUser(string name)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, "Test");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    [Fact]
    public async Task LogAsync_WritesActivityEntry()
    {
        using var db = TestControllerSupport.CreateContext();

        await ActivityLogger.LogAsync(db, "Created", "Event", 42, "Event \"Gala\" was created.", "Admin Rivera");

        var log = await db.ActivityLogs.SingleAsync();
        Assert.Equal("Created", log.Action);
        Assert.Equal("Event", log.EntityType);
        Assert.Equal(42, log.EntityId);
        Assert.Equal("Event \"Gala\" was created.", log.Description);
        Assert.Equal("Admin Rivera", log.PerformedBy);
    }

    [Fact]
    public async Task CustomerCreate_RecordsActivityWithPerformer()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = new CustomerController(db);
        TestControllerSupport.InitController(controller, CreateContextWithUser("Admin Rivera"));

        var result = await controller.Create(new CustomerModel
        {
            FullName = "Ana Reyes",
            Email = "ana.r@test.com",
            Type = "Individual",
            Status = "Active"
        });

        Assert.IsType<RedirectToActionResult>(result);
        var log = await db.ActivityLogs.SingleAsync();
        Assert.Equal("Created", log.Action);
        Assert.Equal("Customer", log.EntityType);
        Assert.Equal("Admin Rivera", log.PerformedBy);
        Assert.Contains("Ana Reyes", log.Description);
    }

    [Fact]
    public async Task ActivityLogAction_ReturnsNewestFirst_PaginatedTen()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 12; i++)
        {
            db.ActivityLogs.Add(new ActivityLogModel
            {
                Action = "Created",
                EntityType = "Event",
                Description = $"Log entry {i}",
                CreatedAt = DateTime.Now.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync();

        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);

        var result = await controller.ActivityLog(null, null, null, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<ActivityLogModel>>(view.Model);
        Assert.Equal(12, model.TotalItems);
        Assert.Equal(2, model.TotalPages);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal("Log entry 12", model.Items[0].Description);
        Assert.Equal("Log entry 3", model.Items[^1].Description);
    }

    [Fact]
    public async Task ActivityLogAction_AppliesEntityTypeFilter()
    {
        using var db = TestControllerSupport.CreateContext();
        db.ActivityLogs.AddRange(
            new ActivityLogModel { Action = "Created", EntityType = "Customer", Description = "Customer added", CreatedAt = DateTime.Now },
            new ActivityLogModel { Action = "Updated", EntityType = "Inventory", Description = "Stock adjusted", CreatedAt = DateTime.Now },
            new ActivityLogModel { Action = "Created", EntityType = "Customer", Description = "Another customer", CreatedAt = DateTime.Now });
        await db.SaveChangesAsync();

        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);

        var view = Assert.IsType<ViewResult>(await controller.ActivityLog(null, "Customer", "All", null));
        var model = Assert.IsType<PagedResult<ActivityLogModel>>(view.Model);

        Assert.Equal(2, model.TotalItems);
        Assert.All(model.Items, l => Assert.Equal("Customer", l.EntityType));
    }

    [Fact]
    public async Task ActivityLogApproveProof_RecordsApprovalEntry()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = new CustomerModel { FullName = "Proof Client", Email = "proof@test.com", Type = "Individual", Status = "Active" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var evt = new EventModel
        {
            EventName = "Proof Event",
            CustomerId = customer.Id,
            EventType = "Wedding",
            EventDate = DateTime.Today.AddDays(20),
            PaxCount = 60,
            Status = "Upcoming",
            TotalAmount = 25000m
        };
        db.Events.Add(evt);
        await db.SaveChangesAsync();

        var proof = new PaymentProofModel
        {
            EventId = evt.Id,
            CustomerId = customer.Id,
            Amount = 25000m,
            PaymentMethod = "GCash",
            ReferenceNumber = "REF-999",
            Status = "Pending"
        };
        db.PaymentProofs.Add(proof);
        await db.SaveChangesAsync();
        Assert.True(proof.Id > 0, "proof.Id should be auto-generated");
        Assert.Equal(1, await db.PaymentProofs.CountAsync());

        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller, CreateContextWithUser("Finance Admin"));

        var result = await controller.ApprovePaymentProof(proof.Id);

        Assert.IsType<RedirectToActionResult>(result);
        var log = await db.ActivityLogs.SingleAsync(l => l.EntityType == "PaymentProof");
        Assert.Equal("Approved", log.Action);
        Assert.Equal("Finance Admin", log.PerformedBy);
        Assert.Contains("25,000.00", log.Description);
    }
}