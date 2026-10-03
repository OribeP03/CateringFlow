using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class CalendarTests
{
    private static SuperAdminController CreateSuperAdmin(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<CustomerModel> SeedCustomer(CateringFlowDbContext db, string name)
    {
        var customer = new CustomerModel
        {
            FullName = name,
            Email = $"{name.ToLower().Replace(" ", string.Empty)}@test.com",
            Type = "Individual",
            Status = "Active"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    private static async Task<EventModel> SeedEvent(CateringFlowDbContext db, CustomerModel customer, string name, DateTime date, decimal amount = 50000m, string status = "Upcoming", string eventType = "Wedding")
    {
        var item = new EventModel
        {
            EventName = name,
            CustomerId = customer.Id,
            EventType = eventType,
            EventDate = date,
            PaxCount = 80,
            Status = status,
            TotalAmount = amount
        };
        db.Events.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    [Fact]
    public async Task Calendar_DefaultsToCurrentMonth_WithGrid()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateSuperAdmin(db);

        var result = await controller.Calendar(null, null);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CalendarViewModel>(view.Model);
        Assert.Equal(DateTime.Now.Year, model.Year);
        Assert.Equal(DateTime.Now.Month, model.Month);
        Assert.Equal(42, model.Days.Count);
        Assert.Equal(DateTime.Now.ToString("MMMM yyyy"), model.MonthName);
    }

    [Fact]
    public async Task Calendar_UsesProvidedYearMonth()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateSuperAdmin(db);

        var result = await controller.Calendar(2026, 6);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CalendarViewModel>(view.Model);
        Assert.Equal(2026, model.Year);
        Assert.Equal(6, model.Month);
        Assert.Equal(30, model.TotalDaysInMonth);
        Assert.Equal("June 2026", model.MonthName);
        Assert.Equal(1, model.Days.Count(d => d.InMonth && d.Date.Day == 1));
        Assert.Equal(30, model.Days.Count(d => d.InMonth));
        Assert.Equal(42, model.Days.Count);
    }

    [Fact]
    public async Task Calendar_InvalidMonth_FallsBackToCurrent()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateSuperAdmin(db);

        var result = await controller.Calendar(2026, 13);

        var model = Assert.IsType<CalendarViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(DateTime.Now.Month, model.Month);

        var result2 = await controller.Calendar(1990, 5);
        var model2 = Assert.IsType<CalendarViewModel>(Assert.IsType<ViewResult>(result2).Model);
        Assert.Equal(DateTime.Now.Year, model2.Year);
        Assert.Equal(5, model2.Month);
    }

    [Fact]
    public async Task Calendar_EventsPlacedInMatchingDayCells()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Cal Tester");
        var targetDate = new DateTime(2026, 6, 15, 14, 0, 0);
        var otherDate = new DateTime(2026, 7, 3, 9, 0, 0); // outside June but inside a muted cell
        var e1 = await SeedEvent(db, customer, "June Wedding", targetDate);
        await SeedEvent(db, customer, "July Party", otherDate);

        var controller = CreateSuperAdmin(db);
        var result = await controller.Calendar(2026, 6);
        var model = Assert.IsType<CalendarViewModel>(Assert.IsType<ViewResult>(result).Model);

        var targetCell = model.Days.Single(d => d.Date == targetDate.Date);
        Assert.True(targetCell.InMonth);
        Assert.Contains(targetCell.Events, e => e.Id == e1.Id);

        var otherCell = model.Days.SingleOrDefault(d => d.Date == otherDate.Date);
        Assert.NotNull(otherCell);
        Assert.False(otherCell.InMonth);
        Assert.Empty(otherCell.Events);
    }

    [Fact]
    public async Task Calendar_Stats_UpcomingCountAndRevenue()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = await SeedCustomer(db, "Stats Tester");
        var june15 = new DateTime(2026, 6, 15);
        var june20 = new DateTime(2026, 6, 20);
        var june25 = new DateTime(2026, 6, 25);
        await SeedEvent(db, customer, "Catered A", june15, amount: 100000m);
        await SeedEvent(db, customer, "Catered B", june20, amount: 50000m, status: "In Progress");
        await SeedEvent(db, customer, "Cancelled C", june25, amount: 75000m, status: "Cancelled");

        var controller = CreateSuperAdmin(db);
        var result = await controller.Calendar(2026, 6);
        var model = Assert.IsType<CalendarViewModel>(Assert.IsType<ViewResult>(result).Model);

        Assert.Equal(2, model.UpcomingCount);
        Assert.Equal(225000m, model.MonthRevenue);
    }
}