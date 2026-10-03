using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class EventControllerTests
{
    private static EventController CreateEventController(CateringFlowDbContext db)
    {
        var controller = new EventController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static SuperAdminController CreateSuperAdmin(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<CustomerModel> SeedCustomer(CateringFlowDbContext db, string name = "Tester")
    {
        var customer = new CustomerModel
        {
            FullName = name,
            Email = $"{name.ToLower().Replace(" ", string.Empty)}@test.com",
            Type = "Individual",
            Status = "Active",
            CreatedAt = DateTime.Now
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private static async Task<MenuPackageModel> SeedPackage(CateringFlowDbContext db, decimal price = 450m)
    {
        var package = new MenuPackageModel { PackageName = "Premium Feast", PricePerPax = price, Status = "Active" };
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();
        return package;
    }

    private static async Task<EventModel> SeedEvent(CateringFlowDbContext db, string name, DateTime date, int customerId, int pax = 50)
    {
        var item = new EventModel
        {
            EventName = name,
            CustomerId = customerId,
            EventType = "Wedding",
            EventDate = date,
            PaxCount = pax,
            Status = "Upcoming",
            TotalAmount = 0m,
            CreatedAt = DateTime.Now
        };
        db.Events.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static EventModel NewEvent(int customerId, int pax = 100)
        => new()
        {
            EventName = "Ayala Dinner",
            CustomerId = customerId,
            EventType = "Corporate",
            EventDate = new DateTime(2026, 11, 20),
            PaxCount = pax,
            Status = "Upcoming",
            TotalAmount = 0m
        };

    [Fact]
    public async Task Create_WithPackage_ComputesTotalAmountAndNotifies()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var package = await SeedPackage(db, 450m);
        var controller = CreateEventController(db);

        var item = NewEvent(customer.Id, 100);
        item.PackageId = package.Id;

        var result = await controller.Create(item, returnUrl: null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        var saved = await db.Events.FirstAsync();
        Assert.Equal(45000m, saved.TotalAmount);
        Assert.Equal(package.Id, saved.PackageId);
        Assert.NotNull(await db.Notifications.FirstOrDefaultAsync(n => n.Title == "New Event Booked" && n.TargetRole == "Super Admin"));
    }

    [Fact]
    public async Task Create_WithoutPackage_AmountIsZero()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var controller = CreateEventController(db);

        var result = await controller.Create(NewEvent(customer.Id, 50), returnUrl: null);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(0m, (await db.Events.FirstAsync()).TotalAmount);
    }

    [Fact]
    public async Task Create_InvalidModel_ReturnsViewWithoutSaving()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var controller = CreateEventController(db);

        var invalid = NewEvent(customer.Id);
        invalid.EventName = string.Empty;
        controller.ModelState.AddModelError("EventName", "Event name is required.");

        var result = await controller.Create(invalid, returnUrl: null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<EventModel>(view.Model);
        Assert.Empty(await db.Events.ToListAsync());
    }

    [Fact]
    public async Task Edit_RecomputesAmountAndPersistsChanges()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var package = await SeedPackage(db, 450m);
        var evt = await SeedEvent(db, "Orig Dinner", new DateTime(2026, 10, 1), customer.Id, 50);

        var updated = NewEvent(customer.Id, 80);
        updated.Id = evt.Id;
        updated.PackageId = package.Id;
        updated.Status = "In Progress";

        var controller = CreateEventController(db);
        var result = await controller.Edit(evt.Id, updated, returnUrl: null);

        Assert.IsType<RedirectToActionResult>(result);
        var persisted = await db.Events.FindAsync(evt.Id);
        Assert.Equal(36000m, persisted!.TotalAmount);
        Assert.Equal("In Progress", persisted.Status);
    }

    [Fact]
    public async Task Edit_InvalidModel_ReturnsViewWithoutChanges()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var evt = await SeedEvent(db, "Stable Dinner", new DateTime(2026, 10, 1), customer.Id, 50);
        var controller = CreateEventController(db);

        var bad = NewEvent(customer.Id);
        bad.Id = evt.Id;
        bad.EventName = string.Empty;
        controller.ModelState.AddModelError("EventName", "Event name is required.");

        var result = await controller.Edit(evt.Id, bad, returnUrl: null);

        Assert.IsType<ViewResult>(result);
        Assert.Equal("Stable Dinner", (await db.Events.FindAsync(evt.Id))!.EventName);
    }

    [Fact]
    public async Task Delete_RemovesEventAndRedirectsToReturnUrl()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var evt = await SeedEvent(db, "Gone Dinner", new DateTime(2026, 10, 1), customer.Id);
        var controller = CreateEventController(db);

        var result = await controller.Delete(evt.Id, returnUrl: "/SuperAdmin/Events");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/SuperAdmin/Events", redirect.Url);
        Assert.Null(await db.Events.FindAsync(evt.Id));
    }

    [Fact]
    public async Task Delete_EventWithPaymentProof_RemovesProofThenEvent()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var evt = await SeedEvent(db, "Booked Dinner", new DateTime(2026, 10, 1), customer.Id);
        db.PaymentProofs.Add(new PaymentProofModel
        {
            EventId = evt.Id,
            CustomerId = customer.Id,
            PaymentMethod = "GCash",
            ReferenceNumber = "GC-12345",
            Amount = 10000m,
            Status = "Pending",
            CreatedAt = DateTime.Now
        });
        await db.SaveChangesAsync();
        var controller = CreateEventController(db);

        var result = await controller.Delete(evt.Id, returnUrl: "/SuperAdmin/Events");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/SuperAdmin/Events", redirect.Url);
        Assert.Null(await db.Events.FindAsync(evt.Id));
        Assert.Empty(await db.PaymentProofs.Where(p => p.EventId == evt.Id).ToListAsync());
    }

    [Fact]
    public async Task UpdateStatus_ChangesStatusAndRedirectsToDetails()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        var evt = await SeedEvent(db, "Status Dinner", new DateTime(2026, 10, 1), customer.Id);
        var controller = CreateEventController(db);

        var result = await controller.UpdateStatus(evt.Id, "Completed", returnUrl: null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal("Completed", (await db.Events.FindAsync(evt.Id))!.Status);
    }

    // ---- SuperAdmin.Events page (Phase 6) ----

    [Fact]
    public async Task SuperAdmin_Events_PagesAtTen_WithTotalItems()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        for (var i = 1; i <= 25; i++)
        {
            await SeedEvent(db, $"Event {i}", DateTime.Today.AddDays(i), customer.Id, 10 + i);
        }
        var controller = CreateSuperAdmin(db);

        var result = await controller.Events(null, null, null, 1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<EventModel>>(view.Model);
        Assert.Equal(25, model.TotalItems);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(3, model.TotalPages);
    }

    [Fact]
    public async Task SuperAdmin_Events_SearchAndTypeFiltersApply()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db);
        for (var i = 1; i <= 30; i++)
        {
            await SeedEvent(db, i % 3 == 0 ? $"Special {i}" : $"Event {i}", DateTime.Today.AddDays(i), customer.Id, 10);
            var evt = await db.Events.FirstAsync(e => e.EventName == (i % 3 == 0 ? $"Special {i}" : $"Event {i}"));
            evt.EventType = i % 2 == 0 ? "Corporate" : "Wedding";
        }
        await db.SaveChangesAsync();
        var controller = CreateSuperAdmin(db);

        var searchResult = await controller.Events("Special", null, null, null);
        var searchModel = Assert.IsType<PagedResult<EventModel>>(Assert.IsType<ViewResult>(searchResult).Model);
        Assert.Equal(10, searchModel.TotalItems);

        var typeResult = await controller.Events(null, "Corporate", null, null);
        var typeModel = Assert.IsType<PagedResult<EventModel>>(Assert.IsType<ViewResult>(typeResult).Model);
        Assert.Equal(15, typeModel.TotalItems);

        var statusResult = await controller.Events(null, null, "Upcoming", null);
        var statusModel = Assert.IsType<PagedResult<EventModel>>(Assert.IsType<ViewResult>(statusResult).Model);
        Assert.Equal(30, statusModel.TotalItems);
    }
}