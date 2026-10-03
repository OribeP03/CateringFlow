using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class SupplierControllerTests
{
    private static SupplierController CreateSupplierController(CateringFlowDbContext db)
    {
        var controller = new SupplierController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static SuperAdminController CreateSuperAdmin(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<SupplierModel> SeedSupplier(CateringFlowDbContext db, string name, string category = "Produce")
    {
        var supplier = new SupplierModel
        {
            SupplierName = name,
            ContactPerson = $"Contact {name}",
            Email = $"{name.ToLower().Replace(" ", string.Empty)}@supplier.ph",
            Phone = "0917 000 0000",
            Address = "Makati",
            Category = category,
            Status = "Active",
            CreatedAt = DateTime.Now
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier;
    }

    [Fact]
    public async Task Create_ValidSupplier_InsertsAndRedirectsToReturnUrl()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateSupplierController(db);

        var supplier = new SupplierModel
        {
            SupplierName = "Fresh Catch Seafoods",
            ContactPerson = "Mrs. Reyes",
            Email = "sales@freshcatch.ph",
            Phone = "0917 555 4321",
            Address = "Navotas",
            Category = "Seafood",
            Status = "Active"
        };

        var result = await controller.Create(supplier, returnUrl: "/SuperAdmin/Suppliers");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/SuperAdmin/Suppliers", redirect.Url);
        Assert.NotNull(await db.Suppliers.FirstOrDefaultAsync(s => s.SupplierName == "Fresh Catch Seafoods"));
    }

    [Fact]
    public async Task Create_InvalidModel_ReturnsViewWithoutSaving()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateSupplierController(db);

        var invalid = new SupplierModel { SupplierName = string.Empty, Email = "x@y.com", Category = "Grains", Status = "Active" };
        controller.ModelState.AddModelError("SupplierName", "Supplier name is required.");

        var result = await controller.Create(invalid, returnUrl: null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<SupplierModel>(view.Model);
        Assert.Empty(await db.Suppliers.ToListAsync());
    }

    [Fact]
    public async Task Edit_PersistsChangesAndRedirects()
    {
        using var db = TestControllerSupport.CreateContext();
        var supplier = await SeedSupplier(db, "Orig Farm");
        var controller = CreateSupplierController(db);

        var updated = new SupplierModel
        {
            Id = supplier.Id,
            SupplierName = "Orig Farm Co.",
            ContactPerson = "New Rep",
            Email = "new@origfarm.ph",
            Phone = "0917 999 1111",
            Address = "Laguna",
            Category = "Produce",
            Status = "Inactive"
        };

        var result = await controller.Edit(supplier.Id, updated, returnUrl: null);

        Assert.IsType<RedirectToActionResult>(result);
        var persisted = await db.Suppliers.FindAsync(supplier.Id);
        Assert.Equal("Orig Farm Co.", persisted!.SupplierName);
        Assert.Equal("Inactive", persisted.Status);
    }

    [Fact]
    public async Task Delete_RemovesSupplierAndRedirects()
    {
        using var db = TestControllerSupport.CreateContext();
        var supplier = await SeedSupplier(db, "Gone Farm");
        var controller = CreateSupplierController(db);

        var result = await controller.Delete(supplier.Id, returnUrl: null);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(await db.Suppliers.FindAsync(supplier.Id));
    }

    // ---- SuperAdmin.Suppliers page (Phase 9) ----

    [Fact]
    public async Task SuperAdmin_Suppliers_PagesAtTen_WithTotalItems()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 25; i++)
        {
            await SeedSupplier(db, $"Supplier {i}");
        }
        var controller = CreateSuperAdmin(db);

        var result = await controller.Suppliers(null, null, 1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<SupplierModel>>(view.Model);
        Assert.Equal(25, model.TotalItems);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(3, model.TotalPages);
    }

    [Fact]
    public async Task SuperAdmin_Suppliers_SearchAndCategoryFiltersApply()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 30; i++)
        {
            await SeedSupplier(db, i % 3 == 0 ? $"Seafood King {i}" : $"Supplier {i}", i % 2 == 0 ? "Meats & Poultry" : "Produce");
        }
        var controller = CreateSuperAdmin(db);

        var searchResult = await controller.Suppliers("Seafood King", null, null);
        var searchModel = Assert.IsType<PagedResult<SupplierModel>>(Assert.IsType<ViewResult>(searchResult).Model);
        Assert.Equal(10, searchModel.TotalItems);

        var categoryResult = await controller.Suppliers(null, "Meats & Poultry", null);
        var categoryModel = Assert.IsType<PagedResult<SupplierModel>>(Assert.IsType<ViewResult>(categoryResult).Model);
        Assert.Equal(15, categoryModel.TotalItems);
    }
}