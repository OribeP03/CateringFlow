using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A25 - Menu package CRUD.
/// The kitchen team owns the catalog the client site and quotations read from.
/// </summary>
public class MenuPackageCrudTests
{
    private static MenuPackageController CreateController(CateringFlowDbContext db)
    {
        var controller = new MenuPackageController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static void AssertIndexRedirect(IActionResult result)
    {
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    private static MenuPackageModel NewPackage(string name, string status = "Active", decimal price = 1500m) => new()
    {
        PackageName = name,
        Description = "Four-course menu.",
        PricePerPax = price,
        CourseCount = 4,
        ServiceHours = 6,
        Features = "Buffet|Stewarding",
        Highlight = "Guest favorite",
        Status = status
    };

    [Fact]
    public async Task Package_Index_PagesTenAtATimeCheapestFirst()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 12; i++)
        {
            db.MenuPackages.Add(NewPackage($"Package {i:00}", "Active", 500m * i));
        }
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var first = Assert.IsType<ViewResult>(await controller.Index(null));
        var paged = Assert.IsType<PagedResult<MenuPackageModel>>(first.Model);
        Assert.Equal(12, paged.TotalItems);
        Assert.Equal(10, paged.Items.Count);
        Assert.Equal(500m, paged.Items[0].PricePerPax);
        Assert.Equal(2, paged.TotalPages);

        var second = Assert.IsType<ViewResult>(await controller.Index(2));
        Assert.Equal(2, Assert.IsType<PagedResult<MenuPackageModel>>(second.Model).Items.Count);
    }

    [Fact]
    public async Task Package_Create_AddsThePackage()
    {
        using var db = TestControllerSupport.CreateContext();

        var result = await CreateController(db).Create(NewPackage("Grand Fiesta"));

        AssertIndexRedirect(result);
        var stored = await db.MenuPackages.AsNoTracking().SingleAsync();
        Assert.Equal("Grand Fiesta", stored.PackageName);
        Assert.Equal(1500m, stored.PricePerPax);
        Assert.NotEqual(default, stored.CreatedAt);
    }

    [Fact]
    public async Task Package_Create_WithInvalidModel_RedisplaystheForm()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateController(db);
        var package = NewPackage("Broken", price: -5m);
        controller.ModelState.AddModelError(nameof(package.PricePerPax), "Price must be positive.");

        var view = Assert.IsType<ViewResult>(await controller.Create(package));

        Assert.Same(package, view.Model);
        Assert.Empty(await db.MenuPackages.ToListAsync());
    }

    [Fact]
    public async Task Package_Details_ReturnsThePackage()
    {
        using var db = TestControllerSupport.CreateContext();
        var package = NewPackage("Grand Fiesta");
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await CreateController(db).Details(package.Id));

        Assert.Equal("Grand Fiesta", Assert.IsType<MenuPackageModel>(view.Model).PackageName);
    }

    [Fact]
    public async Task Package_Details_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateController(db).Details(88));
        Assert.IsType<NotFoundResult>(await CreateController(db).Details(null));
    }

    [Fact]
    public async Task Package_Edit_UpdatesThePackage()
    {
        using var db = TestControllerSupport.CreateContext();
        var package = NewPackage("Grand Fiesta");
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        var edited = NewPackage("Grand Fiesta Deluxe", "Inactive", 2200m);
        edited.Id = package.Id;

        AssertIndexRedirect(await CreateController(db).Edit(package.Id, edited));

        var stored = await db.MenuPackages.AsNoTracking().SingleAsync();
        Assert.Equal("Grand Fiesta Deluxe", stored.PackageName);
        Assert.Equal(2200m, stored.PricePerPax);
        Assert.Equal("Inactive", stored.Status);
    }

    [Fact]
    public async Task Package_Edit_WithMismatchedId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();
        var package = NewPackage("Grand Fiesta");
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        Assert.IsType<NotFoundResult>(await CreateController(db).Edit(package.Id + 5, NewPackage("Other")));
    }

    [Fact]
    public async Task Package_Edit_WithInvalidModel_RedisplaystheForm()
    {
        using var db = TestControllerSupport.CreateContext();
        var package = NewPackage("Grand Fiesta");
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var edited = NewPackage("Grand Fiesta Deluxe");
        edited.Id = package.Id;
        controller.ModelState.AddModelError(nameof(edited.PackageName), "Name is required.");

        var view = Assert.IsType<ViewResult>(await controller.Edit(package.Id, edited));

        Assert.Same(edited, view.Model);
        Assert.Equal("Grand Fiesta", (await db.MenuPackages.AsNoTracking().SingleAsync()).PackageName);
    }

    [Fact]
    public async Task Package_Delete_RemovesThePackage()
    {
        using var db = TestControllerSupport.CreateContext();
        var package = NewPackage("Grand Fiesta");
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreateController(db).Delete(package.Id));
        Assert.Empty(await db.MenuPackages.ToListAsync());
    }

    [Fact]
    public async Task Package_Delete_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateController(db).Delete(91));
    }

    [Fact]
    public async Task Package_Delete_RedirectsToALocalReturnUrl()
    {
        using var db = TestControllerSupport.CreateContext();
        var package = NewPackage("Grand Fiesta");
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        var result = await CreateController(db).Delete(package.Id, "/SuperAdmin/MenuPackages?page=2");

        Assert.Equal("/SuperAdmin/MenuPackages?page=2", Assert.IsType<RedirectResult>(result).Url);
    }

    [Fact]
    public async Task Package_Delete_KeepsHistoryWhenEventsAlreadyUsedIt()
    {
        using var db = TestControllerSupport.CreateContext();
        var package = NewPackage("Retired Classics");
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        var customer = new CustomerModel { FullName = "Ana", Email = "ana@example.com", Type = "Individual", Status = "Active" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        db.Events.Add(new EventModel
        {
            EventName = "Historic Gala",
            CustomerId = customer.Id,
            EventType = "Corporate",
            EventDate = new DateTime(2024, 5, 5),
            PaxCount = 120,
            Status = "Completed",
            PackageId = package.Id,
            TotalAmount = 180000m
        });
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreateController(db).Delete(package.Id));

        var ev = await db.Events.AsNoTracking().SingleAsync();
        Assert.Null(ev.PackageId);
        Assert.Equal("Completed", ev.Status);
    }
}