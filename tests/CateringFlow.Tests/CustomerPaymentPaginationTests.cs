using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class CustomerPaymentPaginationTests
{
    private static CateringFlowDbContext CreateContext()
        => new CateringFlowDbContext(
            new DbContextOptionsBuilder<CateringFlowDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

    private static async Task SeedPayments(CateringFlowDbContext db, int count, string method = "Cash")
    {
        var customer = new CustomerModel
        {
            FullName = "Seeder",
            Email = "seeder@test.com",
            Type = "Individual",
            Status = "Active"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        for (var i = 1; i <= count; i++)
        {
            var invoice = new InvoiceModel
            {
                InvoiceNumber = $"INV-{Guid.NewGuid():N}-{i}",
                CustomerId = customer.Id,
                TotalAmount = 100m * i,
                DueDate = DateTime.Now.AddDays(7),
                Status = "Unpaid"
            };
            db.Invoices.Add(invoice);
            db.Payments.Add(new PaymentModel
            {
                Invoice = invoice,
                Customer = customer,
                Amount = 100m * i,
                PaymentMethod = method,
                PaymentDate = DateTime.Now.AddDays(-i),
                CreatedAt = DateTime.Now.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Customer_Index_FirstPage_RendersFirstTenRows()
    {
        using var db = CreateContext();
        for (var i = 1; i <= 25; i++)
        {
            db.Customers.Add(new CustomerModel
            {
                FullName = $"Customer {i}",
                Email = $"c{i}@test.com",
                Type = "Individual",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var controller = new CustomerController(db);
        var result = await controller.Index(search: null, type: null, status: null, page: 1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<CustomerModel>>(view.Model);
        Assert.Equal(25, model.TotalItems);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(1, model.Page);
    }

    [Fact]
    public async Task Customer_Index_SecondPage_RowsElevenToTwenty()
    {
        using var db = CreateContext();
        for (var i = 1; i <= 25; i++)
        {
            db.Customers.Add(new CustomerModel
            {
                FullName = $"Customer {i}",
                Email = $"c{i}@test.com",
                Type = "Individual",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var controller = new CustomerController(db);
        var result = await controller.Index(search: null, type: null, status: null, page: 2);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<CustomerModel>>(view.Model);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(11, model.FirstItem);
        Assert.Equal(20, model.LastItem);
    }

    [Fact]
    public async Task Customer_Index_OutOfRangePage_ClampsToLastPage()
    {
        using var db = CreateContext();
        for (var i = 1; i <= 25; i++)
        {
            db.Customers.Add(new CustomerModel
            {
                FullName = $"Customer {i}",
                Email = $"c{i}@test.com",
                Type = "Individual",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var controller = new CustomerController(db);
        var result = await controller.Index(search: null, type: null, status: null, page: 50);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<CustomerModel>>(view.Model);
        Assert.Equal(3, model.Page);
        Assert.Equal(5, model.Items.Count);
    }

    [Fact]
    public async Task Customer_Index_FilterCountsFromFullDataset_NotPageRows()
    {
        using var db = CreateContext();
        for (var i = 1; i <= 30; i++)
        {
            db.Customers.Add(new CustomerModel
            {
                FullName = i % 3 == 0 ? $"Special {i}" : $"Customer {i}",
                Email = $"c{i}@test.com",
                Type = "Individual",
                Status = "Active",
                CreatedAt = DateTime.Now.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var controller = new CustomerController(db);
        var result = await controller.Index(search: "Special", type: null, status: null, page: null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<CustomerModel>>(view.Model);
        Assert.Equal(10, model.TotalItems);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(1, model.TotalPages);
    }

    [Fact]
    public async Task Payment_Index_FirstPage_RendersFirstTenAndTotals()
    {
        using var db = CreateContext();
        await SeedPayments(db, 12);

        var controller = new PaymentController(db);
        var result = await controller.Index(page: 1);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<PaymentModel>>(view.Model);
        Assert.Equal(12, model.TotalItems);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(1, model.Page);
        Assert.Equal(2, model.TotalPages);
        Assert.Equal(7800m, Assert.IsType<decimal>(view.ViewData["TotalCollected"]));
    }

    [Fact]
    public async Task Payment_Index_SecondPage_RendersRemainingRows()
    {
        using var db = CreateContext();
        await SeedPayments(db, 12);

        var controller = new PaymentController(db);
        var result = await controller.Index(page: 2);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedResult<PaymentModel>>(view.Model);
        Assert.Equal(2, model.Items.Count);
        Assert.Equal(11, model.FirstItem);
        Assert.Equal(12, model.LastItem);
        Assert.False(model.HasNext);
    }

    [Fact]
    public async Task Payment_Index_ComputesMostUsedMethod_AcrossAllRows()
    {
        using var db = CreateContext();
        for (var i = 1; i <= 12; i++)
        {
            await SeedPayments(db, 1, i <= 8 ? "GCash" : "Cash");
        }

        var controller = new PaymentController(db);
        var result = await controller.Index(page: null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("GCash", view.ViewData["MostUsedMethod"]);
        Assert.Equal(8, view.ViewData["MostUsedMethodCount"]);

        var model = Assert.IsType<PagedResult<PaymentModel>>(view.Model);
        Assert.Equal(12, model.TotalItems);
        Assert.Equal(10, model.Items.Count);
    }
}