using cateringflow.Data;
using cateringflow.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class PagedResultTests
{
    private static CateringFlowDbContext CreateContext(int customers)
    {
        var options = new DbContextOptionsBuilder<CateringFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CateringFlowDbContext(options);
        for (var i = 1; i <= customers; i++)
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
        db.SaveChanges();
        return db;
    }

    private static IQueryable<CustomerModel> OrderedCustomers(CateringFlowDbContext db)
        => db.Customers.OrderByDescending(c => c.CreatedAt);

    [Fact]
    public async Task CreateAsync_FirstPage_ReturnsFirstTenRows()
    {
        using var db = CreateContext(25);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 1);

        Assert.Equal(25, result.TotalItems);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(1, result.FirstItem);
        Assert.Equal(10, result.LastItem);
        Assert.Equal("Customer 1", result.Items[0].FullName);
    }

    [Fact]
    public async Task CreateAsync_SecondPage_ReturnsRowsElevenToTwenty()
    {
        using var db = CreateContext(25);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 2);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(11, result.FirstItem);
        Assert.Equal(20, result.LastItem);
    }

    [Fact]
    public async Task CreateAsync_LastPage_ReturnsRemainingRows()
    {
        using var db = CreateContext(25);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 3);

        Assert.Equal(3, result.Page);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(21, result.FirstItem);
        Assert.Equal(25, result.LastItem);
    }

    [Fact]
    public async Task CreateAsync_EmptySource_ReturnsEmptyFirstPage()
    {
        using var db = CreateContext(0);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 1);

        Assert.Equal(0, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.FirstItem);
        Assert.Equal(0, result.LastItem);
        Assert.False(result.HasNext);
        Assert.False(result.HasPrevious);
    }

    [Fact]
    public async Task CreateAsync_OutOfRangePage_ClampsToLastPage()
    {
        using var db = CreateContext(25);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 99);

        Assert.Equal(3, result.Page);
        Assert.Equal(5, result.Items.Count);
        Assert.False(result.HasNext);
    }

    [Fact]
    public async Task CreateAsync_NegativePage_DefaultsToFirstPage()
    {
        using var db = CreateContext(25);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), -5);

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task CreateAsync_TotalPages_CalculatesCeiling()
    {
        using var db = CreateContext(25);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 1);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNext);
    }

    [Fact]
    public async Task CreateAsync_CustomPageSize_IsHonored()
    {
        using var db = CreateContext(25);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 1, 5);

        Assert.Equal(5, result.PageSize);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public async Task GetPageWindow_FewPages_ReturnsAllPages()
    {
        using var db = CreateContext(25);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 2);

        Assert.Equal(new[] { 1, 2, 3 }, result.GetPageWindow());
    }

    [Fact]
    public async Task GetPageWindow_ManyPages_BoundsAroundCurrentPage()
    {
        using var db = CreateContext(60);
        var result = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 10);

        Assert.Equal(6, result.TotalPages);
        Assert.Equal(new[] { 2, 3, 4, 5, 6 }, result.GetPageWindow());

        var first = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 1);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, first.GetPageWindow());

        var mid = await PagedResult<CustomerModel>.CreateAsync(OrderedCustomers(db), 10, 2);
        Assert.Equal(30, mid.TotalPages);
        Assert.Equal(new[] { 8, 9, 10, 11, 12 }, mid.GetPageWindow());
    }
}