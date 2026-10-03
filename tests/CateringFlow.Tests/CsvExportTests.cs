using System.Text;
using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class CsvExportTests
{
    private static SuperAdminController CreateSuperAdmin(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static string ReadCsv(FileContentResult result)
        => Encoding.UTF8.GetString(result.FileContents);

    [Fact]
    public async Task ExportCustomers_ReturnsCsvWithHeaderAndRows()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Customers.AddRange(
            new CustomerModel { FullName = "Ana Reyes", Email = "ana@test.com", Type = "Individual", Status = "Active" },
            new CustomerModel { FullName = "TechCorp", Email = "tech@test.com", Type = "Company", Status = "Active" });
        await db.SaveChangesAsync();

        var controller = CreateSuperAdmin(db);
        var result = await controller.ExportCustomers(null, null, null);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.StartsWith("customers-", file.FileDownloadName);
        Assert.EndsWith(".csv", file.FileDownloadName);

        var csv = ReadCsv(file);
        Assert.Contains("FullName", csv);
        Assert.Contains("Ana Reyes", csv);
        Assert.Contains("TechCorp", csv);
    }

    [Fact]
    public async Task ExportCustomers_AppliesSearchFilter()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Customers.AddRange(
            new CustomerModel { FullName = "Ana Reyes", Email = "ana@test.com", Type = "Individual", Status = "Active" },
            new CustomerModel { FullName = "BPO Global", Email = "bpo@test.com", Type = "Company", Status = "Active" });
        await db.SaveChangesAsync();

        var controller = CreateSuperAdmin(db);
        var file = Assert.IsType<FileContentResult>(await controller.ExportCustomers("Ana", null, null));
        var csv = ReadCsv(file);

        Assert.Contains("Ana Reyes", csv);
        Assert.DoesNotContain("BPO Global", csv);
    }

    [Fact]
    public async Task ExportPayments_IncludesInvoiceCustomerAndAmount()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = new CustomerModel { FullName = "Maria Santos", Email = "maria@test.com", Type = "Individual", Status = "Active" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var invoice = new InvoiceModel
        {
            InvoiceNumber = "INV-2026-001",
            CustomerId = customer.Id,
            TotalAmount = 50000m,
            AmountPaid = 50000m,
            Status = "Paid",
            DueDate = DateTime.Today.AddDays(5)
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        db.Payments.Add(new PaymentModel
        {
            InvoiceId = invoice.Id,
            CustomerId = customer.Id,
            Amount = 50000m,
            PaymentMethod = "Bank Transfer",
            ReferenceNumber = "REF-123",
            PaymentDate = DateTime.Today.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var controller = CreateSuperAdmin(db);
        var file = Assert.IsType<FileContentResult>(await controller.ExportPayments(null, null));
        var csv = ReadCsv(file);

        Assert.Contains("InvoiceNumber", csv);
        Assert.Contains("INV-2026-001", csv);
        Assert.Contains("Maria Santos", csv);
        Assert.Contains("50000.00", csv);
        Assert.Contains("Bank Transfer", csv);
    }

    [Fact]
    public async Task ExportInventory_AppliesCategoryFilter_AndEscapesCommas()
    {
        using var db = TestControllerSupport.CreateContext();
        db.InventoryItems.AddRange(
            new InventoryModel { ItemCode = "A-1", ItemName = "Pork, Cuts", Category = "Meats & Poultry", CurrentStock = 9, MinReorderLevel = 10, Unit = "kg", StockStatus = "Low Stock" },
            new InventoryModel { ItemCode = "B-1", ItemName = "Rice", Category = "Grains", CurrentStock = 50, MinReorderLevel = 10, Unit = "kg", StockStatus = "Adequate" });
        await db.SaveChangesAsync();

        var controller = CreateSuperAdmin(db);
        var file = Assert.IsType<FileContentResult>(await controller.ExportInventory("Meats & Poultry", null));
        var csv = ReadCsv(file);

        Assert.Contains("Meats & Poultry", csv);
        Assert.DoesNotContain("Rice", csv);
        // CSV escaping: name containing a comma must be quoted with doubled quotes preserved.
        Assert.Contains("\"Pork, Cuts\"", csv);
    }

    [Fact]
    public async Task ExportInventory_UsesStockStatusFilter()
    {
        using var db = TestControllerSupport.CreateContext();
        db.InventoryItems.AddRange(
            new InventoryModel { ItemCode = "A-1", ItemName = "Beef", Category = "Meats & Poultry", CurrentStock = 0, MinReorderLevel = 10, Unit = "kg", StockStatus = "Out of Stock" },
            new InventoryModel { ItemCode = "B-1", ItemName = "Chicken", Category = "Meats & Poultry", CurrentStock = 40, MinReorderLevel = 10, Unit = "kg", StockStatus = "Adequate" });
        await db.SaveChangesAsync();

        var controller = CreateSuperAdmin(db);
        var file = Assert.IsType<FileContentResult>(await controller.ExportInventory(null, "Out of Stock"));
        var csv = ReadCsv(file);

        Assert.Contains("Beef", csv);
        Assert.DoesNotContain("Chicken", csv);
    }
}