using System.Security.Claims;
using cateringflow.Controllers;
using cateringflow.Models;
using cateringflow.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

public class CustomerBookingVisibilityTests
{
    private static ClaimsPrincipal CustomerUser(string name, string email)
        => new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, email)
        }, "test"));

    [Fact]
    public async Task Book_FromClient_CreatesUpcomingEvent_Invoice_Notification_AndIsVisibleToSuperAdmin()
    {
        using var db = TestControllerSupport.CreateContext();
        var http = new DefaultHttpContext { User = CustomerUser("Juan Dela Cruz", "juan@example.com") };
        var client = new ClientController(db, new FakeWebHostEnvironment());
        TestControllerSupport.InitController(client, http);

        var result = await client.Book(new BookingRequest
        {
            FullName = "Juan Dela Cruz",
            EventType = "Wedding",
            EventDate = new DateTime(2026, 12, 5),
            PaxCount = 150,
            Venue = "Grand Hyatt",
            Notes = "Vegetarian platter"
        });

        Assert.IsType<OkObjectResult>(result);

        var booking = await db.Events.SingleAsync();
        Assert.Equal("Juan Dela Cruz - Wedding", booking.EventName);
        Assert.Equal("Upcoming", booking.Status);
        Assert.Equal(150, booking.PaxCount);
        Assert.Equal(new DateTime(2026, 12, 5), booking.EventDate);

        Assert.NotNull(await db.Invoices.FirstOrDefaultAsync(i => i.EventId == booking.Id));
        Assert.NotNull(await db.Notifications.FirstOrDefaultAsync(n => n.Title == "New Client Booking" && n.TargetRole == "Super Admin"));

        var super = new SuperAdminController(db);
        TestControllerSupport.InitController(super);
        var listResult = await super.Events(null, null, null, null);
        var model = Assert.IsType<PagedResult<EventModel>>(Assert.IsType<ViewResult>(listResult).Model);
        Assert.Contains(model.Items, e => e.Id == booking.Id);
    }

    [Fact]
    public async Task Book_WithInvalidPax_RejectedAndNoBookingCreated()
    {
        using var db = TestControllerSupport.CreateContext();
        var http = new DefaultHttpContext { User = CustomerUser("Ana Santos", "ana@example.com") };
        var client = new ClientController(db, new FakeWebHostEnvironment());
        TestControllerSupport.InitController(client, http);

        var result = await client.Book(new BookingRequest
        {
            FullName = "Ana Santos",
            PaxCount = 0
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Events.ToListAsync());
    }

    [Theory]
    [InlineData(UserRoles.SuperAdmin)]
    [InlineData(UserRoles.SalesCrm)]
    [InlineData(UserRoles.EventCoordinator)]
    public void EventsManagement_AllowedForRolesThatManageTheEventsPage(string role)
    {
        var rbac = new RbacService();
        Assert.True(rbac.HasAccess(role, "Events", out var permission));
        Assert.Contains("Manage", permission);
    }

    [Fact]
    public void EventsManagement_NotAllowedForRolesWithoutEventAccess()
    {
        var rbac = new RbacService();
        Assert.False(rbac.HasAccess(UserRoles.StaffCrew, "Events", out _));
    }
}