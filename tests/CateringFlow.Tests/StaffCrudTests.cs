using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A25 - Staff and staff-assignment CRUD.
/// The operations team must be able to hire, filter, edit and remove crew,
/// and assigning crew to an event must flip their availability.
/// </summary>
public class StaffCrudTests
{
    private static StaffController CreateStaffController(CateringFlowDbContext db)
    {
        var controller = new StaffController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static StaffAssignmentController CreateAssignmentController(CateringFlowDbContext db)
    {
        var controller = new StaffAssignmentController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static StaffModel NewStaff(string name, string availability = "Available", string employmentType = "Full-time") => new()
    {
        FullName = name,
        Email = $"{name.Replace(" ", string.Empty).ToLowerInvariant()}@cateringflow.ph",
        Phone = "0917-000-0000",
        Position = "Sous Chef",
        Availability = availability,
        EmploymentType = employmentType,
        Specialty = "Filipino cuisine",
        DateHired = new DateTime(2024, 1, 15),
        CreatedAt = DateTime.Now
    };

    private static async Task<EventModel> SeedEventAsync(CateringFlowDbContext db)
    {
        var customer = new CustomerModel
        {
            FullName = "Ana Dela Cruz",
            Email = "ana@example.com",
            Type = "Individual",
            Status = "Active"
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var ev = new EventModel
        {
            EventName = "Dela Cruz Wedding",
            CustomerId = customer.Id,
            EventType = "Wedding",
            EventDate = new DateTime(2026, 10, 10),
            PaxCount = 200,
            Status = "Upcoming",
            TotalAmount = 300000m
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        return ev;
    }

    // ---------- staff CRUD ----------

    private static void AssertIndexRedirect(IActionResult result)
    {
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Staff_Index_FiltersAndCountsAvailability()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Staff.AddRange(
            NewStaff("Grace Villanueva"),
            NewStaff("Mateo Bagatsing", "Busy"),
            NewStaff("Jenny Pascual", "On Leave", "Part-time"));
        await db.SaveChangesAsync();

        var controller = CreateStaffController(db);

        var view = Assert.IsType<ViewResult>(await controller.Index(null, null, null, null));
        Assert.Equal(3, Assert.IsType<PagedResult<StaffModel>>(view.Model).TotalItems);
        Assert.Equal(3, Convert.ToInt32(controller.ViewData["TotalStaff"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalAvailable"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalBusy"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalOnLeave"]));
        Assert.Equal(2, Convert.ToInt32(controller.ViewData["TotalFullTime"]));

        var busy = Assert.IsType<ViewResult>(await controller.Index(null, "Busy", null, null));
        Assert.Single(Assert.IsType<PagedResult<StaffModel>>(busy.Model).Items);

        var partTime = Assert.IsType<ViewResult>(await controller.Index(null, null, "Part-time", null));
        Assert.Single(Assert.IsType<PagedResult<StaffModel>>(partTime.Model).Items);

        var bySearch = Assert.IsType<ViewResult>(await controller.Index("Mateo", null, null, null));
        Assert.Equal("Mateo Bagatsing", Assert.IsType<PagedResult<StaffModel>>(bySearch.Model).Items.Single().FullName);
    }

    [Fact]
    public async Task Staff_Create_AddsTheCrewMember()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateStaffController(db);

        var result = await controller.Create(NewStaff("Grace Villanueva"));

        AssertIndexRedirect(result);
        var stored = await db.Staff.AsNoTracking().SingleAsync();
        Assert.Equal("Grace Villanueva", stored.FullName);
    }

    [Fact]
    public async Task Staff_Create_WithInvalidModel_RedisplaystheForm()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateStaffController(db);
        var staff = NewStaff("Broken");
        staff.Email = "not-an-email";
        controller.ModelState.AddModelError(nameof(staff.Email), "Bad email.");

        var view = Assert.IsType<ViewResult>(await controller.Create(staff));

        Assert.Same(staff, view.Model);
        Assert.Empty(await db.Staff.ToListAsync());
    }

    [Fact]
    public async Task Staff_Details_ReturnsTheCrewMemberWithTheirAssignments()
    {
        using var db = TestControllerSupport.CreateContext();
        var staff = NewStaff("Grace Villanueva");
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await CreateStaffController(db).Details(staff.Id));

        Assert.Equal("Grace Villanueva", Assert.IsType<StaffModel>(view.Model).FullName);
    }

    [Fact]
    public async Task Staff_Details_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateStaffController(db).Details(404));
        Assert.IsType<NotFoundResult>(await CreateStaffController(db).Details(null));
    }

    [Fact]
    public async Task Staff_Edit_UpdatesTheCrewMember()
    {
        using var db = TestControllerSupport.CreateContext();
        var staff = NewStaff("Grace Villanueva");
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        var edited = NewStaff("Grace Villanueva-Reyes");
        edited.Id = staff.Id;
        edited.Position = "Executive Chef";
        edited.Availability = "Busy";
        edited.EmploymentType = "Part-time";

        var result = await CreateStaffController(db).Edit(staff.Id, edited);

        AssertIndexRedirect(result);
        var stored = await db.Staff.AsNoTracking().SingleAsync();
        Assert.Equal("Grace Villanueva-Reyes", stored.FullName);
        Assert.Equal("Executive Chef", stored.Position);
        Assert.Equal("Busy", stored.Availability);
        Assert.Equal("Part-time", stored.EmploymentType);
    }

    [Fact]
    public async Task Staff_Edit_WithMismatchedId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();
        var staff = NewStaff("Grace Villanueva");
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        Assert.IsType<NotFoundResult>(await CreateStaffController(db).Edit(staff.Id + 1, NewStaff("X")));
    }

    [Fact]
    public async Task Staff_Delete_RemovesTheCrewMember()
    {
        using var db = TestControllerSupport.CreateContext();
        var staff = NewStaff("Grace Villanueva");
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreateStaffController(db).Delete(staff.Id));
        Assert.Empty(await db.Staff.ToListAsync());
    }

    [Fact]
    public async Task Staff_Delete_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateStaffController(db).Delete(999));
    }

    [Fact]
    public async Task Staff_Delete_RedirectsToALocalReturnUrl()
    {
        using var db = TestControllerSupport.CreateContext();
        var staff = NewStaff("Grace Villanueva");
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        var result = await CreateStaffController(db).Delete(staff.Id, "/SuperAdmin/Staff?page=2");

        Assert.Equal("/SuperAdmin/Staff?page=2", Assert.IsType<RedirectResult>(result).Url);
    }

    // ---------- staff assignment CRUD ----------

    [Fact]
    public async Task Assignment_Index_CanFilterByEvent()
    {
        using var db = TestControllerSupport.CreateContext();
        var first = await SeedEventAsync(db);
        var staff = NewStaff("Grace Villanueva");
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        db.StaffAssignments.Add(new StaffAssignmentModel
        {
            StaffId = staff.Id,
            EventId = first.Id,
            RoleAtEvent = "Head Chef",
            AssignedAt = DateTime.Now
        });
        await db.SaveChangesAsync();

        var controller = CreateAssignmentController(db);

        var all = Assert.IsType<ViewResult>(await controller.Index(null));
        Assert.Single(Assert.IsType<List<StaffAssignmentModel>>(all.Model));
        Assert.True(controller.ViewData.ContainsKey("Events"));

        var filtered = Assert.IsType<ViewResult>(await controller.Index(first.Id));
        Assert.Single(Assert.IsType<List<StaffAssignmentModel>>(filtered.Model));

        var other = Assert.IsType<ViewResult>(await controller.Index(first.Id + 500));
        Assert.Empty(Assert.IsType<List<StaffAssignmentModel>>(other.Model));
    }

    [Fact]
    public async Task Assignment_Create_MarksTheCrewMemberBusy()
    {
        using var db = TestControllerSupport.CreateContext();
        var ev = await SeedEventAsync(db);
        var staff = NewStaff("Grace Villanueva");
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        var result = await CreateAssignmentController(db).Create(new StaffAssignmentModel
        {
            StaffId = staff.Id,
            EventId = ev.Id,
            RoleAtEvent = "Head Chef"
        });

        AssertIndexRedirect(result);
        var assignment = await db.StaffAssignments.AsNoTracking().SingleAsync();
        Assert.Equal("Head Chef", assignment.RoleAtEvent);
        Assert.NotEqual(default, assignment.AssignedAt);
        Assert.Equal("Busy", (await db.Staff.AsNoTracking().SingleAsync()).Availability);
    }

    [Fact]
    public async Task Assignment_Create_WithInvalidModel_RedisplaystheForm()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateAssignmentController(db);
        controller.ModelState.AddModelError("RoleAtEvent", "Role is required.");

        var assignment = new StaffAssignmentModel { StaffId = 1, EventId = 1 };
        var view = Assert.IsType<ViewResult>(await controller.Create(assignment));

        Assert.Same(assignment, view.Model);
        Assert.Empty(await db.StaffAssignments.ToListAsync());
    }

    [Fact]
    public async Task Assignment_Create_OnlyOffersAvailableCrewAndOpenEvents()
    {
        using var db = TestControllerSupport.CreateContext();
        var ev = await SeedEventAsync(db);
        db.Staff.AddRange(
            NewStaff("Grace Villanueva", "Available"),
            NewStaff("Mateo Bagatsing", "Busy"));
        var completed = new EventModel
        {
            EventName = "Old Gala",
            CustomerId = ev.CustomerId,
            EventType = "Corporate",
            EventDate = new DateTime(2024, 1, 1),
            PaxCount = 50,
            Status = "Completed"
        };
        db.Events.Add(completed);
        await db.SaveChangesAsync();

        var controller = CreateAssignmentController(db);
        await controller.Create();

        var staff = Assert.IsType<List<StaffModel>>(controller.ViewData["Staff"]);
        var events = Assert.IsType<List<EventModel>>(controller.ViewData["Events"]);
        Assert.Equal("Grace Villanueva", staff.Single().FullName);
        Assert.DoesNotContain(events, e => e.Status == "Completed");
    }

    [Fact]
    public async Task Assignment_Delete_FreesTheCrewMemberAgain()
    {
        using var db = TestControllerSupport.CreateContext();
        var ev = await SeedEventAsync(db);
        var staff = NewStaff("Grace Villanueva", "Busy");
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        var assignment = new StaffAssignmentModel
        {
            StaffId = staff.Id,
            EventId = ev.Id,
            RoleAtEvent = "Server",
            AssignedAt = DateTime.Now
        };
        db.StaffAssignments.Add(assignment);
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreateAssignmentController(db).Delete(assignment.Id));
        Assert.Empty(await db.StaffAssignments.ToListAsync());
        Assert.Equal("Available", (await db.Staff.AsNoTracking().SingleAsync()).Availability);
    }

    [Fact]
    public async Task Assignment_Delete_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateAssignmentController(db).Delete(321));
    }

    [Fact]
    public async Task Assignment_Delete_LeavesCrewWhoAreNotMarkedBusyAlone()
    {
        using var db = TestControllerSupport.CreateContext();
        var ev = await SeedEventAsync(db);
        var staff = NewStaff("Grace Villanueva", "On Leave");
        db.Staff.Add(staff);
        await db.SaveChangesAsync();

        var assignment = new StaffAssignmentModel { StaffId = staff.Id, EventId = ev.Id };
        db.StaffAssignments.Add(assignment);
        await db.SaveChangesAsync();

        await CreateAssignmentController(db).Delete(assignment.Id);

        Assert.Equal("On Leave", (await db.Staff.AsNoTracking().SingleAsync()).Availability);
    }
}
