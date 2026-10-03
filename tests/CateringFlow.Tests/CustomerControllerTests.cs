using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class CustomerControllerTests
{
    private static CustomerController CreateCustomerController(CateringFlowDbContext db)
    {
        var controller = new CustomerController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static SuperAdminController CreateSuperAdminController(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static CustomerModel NewCustomer(string suffix = "")
        => new()
        {
            FullName = suffix.Length == 0 ? "Cliente" : $"Cliente {suffix}",
            Email = $"c{suffix}@test.com",
            Type = "Individual",
            Status = "Active",
            CreatedAt = DateTime.Now
        };

    [Fact]
    public async Task Create_ValidCustomer_InsertsAndRedirectsToIndex()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateCustomerController(db);

        var result = await controller.Create(NewCustomer("One"), returnUrl: null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        var saved = await db.Customers.FirstOrDefaultAsync(c => c.FullName == "Cliente One");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task Create_FromSuperAdminPage_RedirectsToReturnUrl()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateCustomerController(db);

        var result = await controller.Create(NewCustomer("Super"), returnUrl: "/SuperAdmin/Customers");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/SuperAdmin/Customers", redirect.Url);
    }

    [Fact]
    public async Task Create_InvalidModel_ReturnsViewWithModel()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateCustomerController(db);

        var invalid = NewCustomer("Bad");
        invalid.FullName = string.Empty;
        controller.ModelState.AddModelError("FullName", "Full name is required.");
        var result = await controller.Create(invalid, returnUrl: null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<CustomerModel>(view.Model);
        Assert.Empty(await db.Customers.ToListAsync());
    }

    [Fact]
    public async Task Edit_ValidChanges_PersistAndRedirect()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Customers.Add(NewCustomer("Original"));
        await db.SaveChangesAsync();
        var customer = await db.Customers.FirstAsync(c => c.FullName == "Cliente Original");
        var controller = CreateCustomerController(db);

        var updated = NewCustomer("Renamed");
        updated.Id = customer.Id;
        updated.Type = "Corporate";
        updated.Status = "Inactive";

        var result = await controller.Edit(customer.Id, updated, returnUrl: null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        var persisted = await db.Customers.FindAsync(customer.Id);
        Assert.Equal("Cliente Renamed", persisted!.FullName);
        Assert.Equal("Corporate", persisted.Type);
        Assert.Equal("Inactive", persisted.Status);
    }

    [Fact]
    public async Task Edit_InvalidModel_ReturnsViewWithoutChanges()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Customers.Add(NewCustomer("Stable"));
        await db.SaveChangesAsync();
        var customer = await db.Customers.FirstAsync(c => c.FullName == "Cliente Stable");
        var controller = CreateCustomerController(db);

        var bad = NewCustomer("Changed");
        bad.Id = customer.Id;
        bad.FullName = string.Empty;
        controller.ModelState.AddModelError("FullName", "Full name is required.");

        var result = await controller.Edit(customer.Id, bad, returnUrl: null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<CustomerModel>(view.Model);
        var persisted = await db.Customers.FindAsync(customer.Id);
        Assert.Equal("Cliente Stable", persisted!.FullName);
    }

    [Fact]
    public async Task Delete_RemovesCustomerAndRedirects()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Customers.Add(NewCustomer("Gone"));
        await db.SaveChangesAsync();
        var customer = await db.Customers.FirstAsync(c => c.FullName == "Cliente Gone");
        var controller = CreateCustomerController(db);

        var result = await controller.Delete(customer.Id, returnUrl: "/SuperAdmin/Customers");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/SuperAdmin/Customers", redirect.Url);
        Assert.Null(await db.Customers.FindAsync(customer.Id));
    }

    // ---- SuperAdmin.Customers action (Phase 4) ----

    [Fact]
    public async Task SuperAdmin_Customers_PagesAtTen_WithTotalItems()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 25; i++)
        {
            db.Customers.Add(NewCustomer(i.ToString()));
        }
        await db.SaveChangesAsync();
        var controller = CreateSuperAdminController(db);

        var result = await controller.Customers(null, null, null, 1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<CustomerModel>>(view.Model);
        Assert.Equal(25, model.TotalItems);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(3, model.TotalPages);
    }

    [Fact]
    public async Task SuperAdmin_Customers_SearchAndTypeFiltersApply()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 30; i++)
        {
            db.Customers.Add(new CustomerModel
            {
                FullName = i % 3 == 0 ? $"Special {i}" : $"Customer {i}",
                Email = $"c{i}@test.com",
                Type = i % 2 == 0 ? "Corporate" : "Individual",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();
        var controller = CreateSuperAdminController(db);

        var searchResult = await controller.Customers("Special", null, null, null);
        var searchModel = Assert.IsType<PagedResult<CustomerModel>>(Assert.IsType<ViewResult>(searchResult).Model);
        Assert.Equal(10, searchModel.TotalItems);
        Assert.Equal(10, searchModel.Items.Count);

        var typeResult = await controller.Customers(null, "Corporate", null, null);
        var typeModel = Assert.IsType<PagedResult<CustomerModel>>(Assert.IsType<ViewResult>(typeResult).Model);
        Assert.Equal(15, typeModel.TotalItems);
    }

    [Fact]
    public async Task SuperAdmin_Customers_OutOfRangePage_ClampsToLastPage()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 12; i++)
        {
            db.Customers.Add(NewCustomer(i.ToString()));
        }
        await db.SaveChangesAsync();
        var controller = CreateSuperAdminController(db);

        var result = await controller.Customers(null, null, null, 50);

        var model = Assert.IsType<PagedResult<CustomerModel>>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(2, model.Page);
        Assert.Equal(2, model.Items.Count);
    }
}