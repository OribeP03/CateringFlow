using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class InventoryControllerTests
{
    private static InventoryController CreateInventoryController(CateringFlowDbContext db)
    {
        var controller = new InventoryController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static SuperAdminController CreateSuperAdmin(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static InventoryModel NewItem(string code, int stock, int reorder = 10, string category = "Produce")
        => new()
        {
            ItemCode = code,
            ItemName = $"Item {code}",
            Category = category,
            CurrentStock = stock,
            MinReorderLevel = reorder,
            Unit = "kg",
            UnitCost = 120m,
            LastUpdated = DateTime.Now
        };

    private static async Task<InventoryModel> SeedItem(CateringFlowDbContext db, string code, int stock, int reorder = 10)
    {
        var item = NewItem(code, stock, reorder);
        item.StockStatus = stock <= 0 ? "Out of Stock" : stock < reorder ? "Low Stock" : "Adequate";
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    [Theory]
    [InlineData(20, 10, "Adequate")]
    [InlineData(5, 10, "Low Stock")]
    [InlineData(0, 10, "Out of Stock")]
    public async Task Create_ComputesAutoStockStatus(int stock, int reorder, string expected)
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateInventoryController(db);

        var result = await controller.Create(NewItem($"INV-{stock}", stock, reorder), returnUrl: null);

        Assert.IsType<RedirectToActionResult>(result);
        var saved = await db.InventoryItems.SingleAsync();
        Assert.Equal(expected, saved.StockStatus);
        Assert.Equal($"INV-{stock}", saved.ItemCode);
    }

    [Fact]
    public async Task Create_InvalidModel_ReturnsViewWithoutSaving()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateInventoryController(db);

        var invalid = NewItem("INV-999", 5);
        invalid.ItemName = string.Empty;
        controller.ModelState.AddModelError("ItemName", "Item name is required.");

        var result = await controller.Create(invalid, returnUrl: null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<InventoryModel>(view.Model);
        Assert.Empty(await db.InventoryItems.ToListAsync());
    }

    [Fact]
    public async Task Edit_RecomputesStockStatusAndRedirects()
    {
        using var db = TestControllerSupport.CreateContext();
        var existing = await SeedItem(db, "INV-010", 20, 10);

        var updated = NewItem("INV-010", 0);
        updated.Id = existing.Id;

        var controller = CreateInventoryController(db);
        var result = await controller.Edit(existing.Id, updated, returnUrl: "/SuperAdmin/Inventory");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/SuperAdmin/Inventory", redirect.Url);
        var persisted = await db.InventoryItems.FindAsync(existing.Id);
        Assert.Equal("Out of Stock", persisted!.StockStatus);
        Assert.Equal(0, persisted.CurrentStock);
    }

    [Fact]
    public async Task Delete_RemovesItemAndRedirects()
    {
        using var db = TestControllerSupport.CreateContext();
        var existing = await SeedItem(db, "INV-011", 5, 10);
        var controller = CreateInventoryController(db);

        var result = await controller.Delete(existing.Id, returnUrl: null);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Null(await db.InventoryItems.FindAsync(existing.Id));
    }

    // ---- SuperAdmin.Inventory page (Phase 8) ----

    [Fact]
    public async Task SuperAdmin_Inventory_PagesAtTen_WithTotalItems()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 25; i++)
        {
            await SeedItem(db, $"INV-{i:D3}", 10 + i);
        }
        var controller = CreateSuperAdmin(db);

        var result = await controller.Inventory(null, null, null, 1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<InventoryModel>>(view.Model);
        Assert.Equal(25, model.TotalItems);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(3, model.TotalPages);
    }

    [Fact]
    public async Task SuperAdmin_Inventory_CategoryAndStockStatusFiltersApply()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 30; i++)
        {
            var item = NewItem($"INV-{i:D3}", i % 2 == 0 ? 3 : 20, 10, i % 2 == 0 ? "Seafood" : "Produce");
            item.StockStatus = item.CurrentStock < item.MinReorderLevel ? "Low Stock" : "Adequate";
            db.InventoryItems.Add(item);
        }
        await db.SaveChangesAsync();
        var controller = CreateSuperAdmin(db);

        var categoryResult = await controller.Inventory(null, "Seafood", null, null);
        var categoryModel = Assert.IsType<PagedResult<InventoryModel>>(Assert.IsType<ViewResult>(categoryResult).Model);
        Assert.Equal(15, categoryModel.TotalItems);

        var stockResult = await controller.Inventory(null, null, "Low Stock", null);
        var stockModel = Assert.IsType<PagedResult<InventoryModel>>(Assert.IsType<ViewResult>(stockResult).Model);
        Assert.Equal(15, stockModel.TotalItems);
    }
}