using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A25 - Notification and company settings CRUD.
/// Inquiry notifications are the hand-off point of the inquiry pipeline, and the
/// company settings feed the public contact section of the client site.
/// </summary>
public class NotificationSettingsCrudTests
{
    private static NotificationController CreateNotificationController(CateringFlowDbContext db)
    {
        var controller = new NotificationController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static SettingsController CreateSettingsController(CateringFlowDbContext db)
    {
        var controller = new SettingsController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static void AssertIndexRedirect(IActionResult result)
    {
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    private static NotificationModel NewNotification(string title, bool isRead = false) => new()
    {
        Title = title,
        Message = $"{title} details",
        Type = "Info",
        IsRead = isRead,
        TargetRole = "Sales / CRM Staff",
        CreatedAt = DateTime.Now
    };

    // ---------- notifications ----------

    [Fact]
    public async Task Notification_Index_CountsUnreadAndPagesTenAtATime()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 12; i++)
        {
            db.Notifications.Add(NewNotification($"Inquiry {i:00}", isRead: i > 3));
        }
        await db.SaveChangesAsync();

        var controller = CreateNotificationController(db);

        var view = Assert.IsType<ViewResult>(await controller.Index(null));
        var paged = Assert.IsType<PagedResult<NotificationModel>>(view.Model);
        Assert.Equal(12, paged.TotalItems);
        Assert.Equal(10, paged.Items.Count);
        Assert.Equal(3, Convert.ToInt32(controller.ViewData["UnreadCount"]));
    }

    [Fact]
    public async Task Notification_MarkAsRead_FlipsASingleNotification()
    {
        using var db = TestControllerSupport.CreateContext();
        var notification = NewNotification("New Website Inquiry");
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreateNotificationController(db).MarkAsRead(notification.Id));

        Assert.True((await db.Notifications.AsNoTracking().SingleAsync()).IsRead);
    }

    [Fact]
    public async Task Notification_MarkAsRead_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateNotificationController(db).MarkAsRead(404));
    }

    [Fact]
    public async Task Notification_MarkAllRead_ClearsEveryUnreadNotification()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Notifications.AddRange(
            NewNotification("One"),
            NewNotification("Two", isRead: true),
            NewNotification("Three"));
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreateNotificationController(db).MarkAllRead());

        var all = await db.Notifications.AsNoTracking().ToListAsync();
        Assert.All(all, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task Notification_Delete_RemovesTheNotification()
    {
        using var db = TestControllerSupport.CreateContext();
        var notification = NewNotification("Spam");
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        AssertIndexRedirect(await CreateNotificationController(db).Delete(notification.Id));
        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task Notification_Delete_ForAMissingId_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateNotificationController(db).Delete(405));
    }

    [Fact]
    public async Task Notification_Delete_RedirectsToALocalReturnUrl()
    {
        using var db = TestControllerSupport.CreateContext();
        var notification = NewNotification("Spam");
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var result = await CreateNotificationController(db).Delete(notification.Id, "/SuperAdmin/Notifications");

        Assert.Equal("/SuperAdmin/Notifications", Assert.IsType<RedirectResult>(result).Url);
    }

    // ---------- settings ----------

    [Fact]
    public async Task Settings_Index_FallsBackToDefaultsWhenEmpty()
    {
        using var db = TestControllerSupport.CreateContext();

        var view = Assert.IsType<ViewResult>(await CreateSettingsController(db).Index());

        var settings = Assert.IsType<SettingsModel>(view.Model);
        Assert.Equal("CateringFlow", settings.CompanyName);
    }

    [Fact]
    public async Task Settings_Index_ReturnsTheStoredCompany()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Settings.Add(new SettingsModel
        {
            CompanyName = "CateringFlow Events",
            CompanyEmail = "events@cateringflow.ph",
            CompanyPhone = "(02) 5555-1234",
            CompanyAddress = "1 Test Street",
            Tagline = "Flawless catering"
        });
        await db.SaveChangesAsync();

        var view = Assert.IsType<ViewResult>(await CreateSettingsController(db).Index());

        Assert.Equal("CateringFlow Events", Assert.IsType<SettingsModel>(view.Model).CompanyName);
    }

    [Fact]
    public async Task Settings_Save_InsertsTheFirstCompanyRecord()
    {
        using var db = TestControllerSupport.CreateContext();

        AssertIndexRedirect(await CreateSettingsController(db).Index(new SettingsModel
        {
            CompanyName = "CateringFlow Events",
            CompanyEmail = "events@cateringflow.ph",
            CompanyPhone = "(02) 5555-1234",
            CompanyAddress = "1 Test Street",
            Tagline = "Flawless catering"
        }));

        var stored = await db.Settings.AsNoTracking().SingleAsync();
        Assert.Equal("CateringFlow Events", stored.CompanyName);
        Assert.NotEqual(default, stored.UpdatedAt);
    }

    [Fact]
    public async Task Settings_Save_UpdatesTheExistingCompanyInPlace()
    {
        using var db = TestControllerSupport.CreateContext();
        var settings = new SettingsModel { CompanyName = "CateringFlow", CompanyEmail = "old@cateringflow.ph" };
        db.Settings.Add(settings);
        await db.SaveChangesAsync();

        var edited = new SettingsModel
        {
            Id = settings.Id,
            CompanyName = "CateringFlow Events",
            CompanyEmail = "events@cateringflow.ph",
            CompanyPhone = "(02) 5555-1234",
            CompanyAddress = "1 Test Street",
            Tagline = "Flawless catering"
        };

        AssertIndexRedirect(await CreateSettingsController(db).Index(edited));

        var all = await db.Settings.AsNoTracking().ToListAsync();
        Assert.Single(all);
        Assert.Equal("CateringFlow Events", all[0].CompanyName);
        Assert.Equal("events@cateringflow.ph", all[0].CompanyEmail);
    }

    [Fact]
    public async Task Settings_Save_LeavesThePublicContactFieldsUsable()
    {
        using var db = TestControllerSupport.CreateContext();

        await CreateSettingsController(db).Index(new SettingsModel
        {
            CompanyName = "CateringFlow Events",
            CompanyPhone = "(02) 5555-1234",
            CompanyEmail = "events@cateringflow.ph",
            CompanyAddress = "1 Test Street"
        });

        var stored = await db.Settings.AsNoTracking().SingleAsync();
        Assert.False(string.IsNullOrWhiteSpace(stored.CompanyPhone));
        Assert.False(string.IsNullOrWhiteSpace(stored.CompanyEmail));
        Assert.False(string.IsNullOrWhiteSpace(stored.CompanyAddress));
    }
}