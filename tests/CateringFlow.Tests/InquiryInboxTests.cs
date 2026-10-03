using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A24 - mirror of Phase 24 (Admin Inquiry Inbox).
/// The events team works inquiries by hand: read the inbox, move the status,
/// assign an owner and delete spam — while the CRM lead stays in sync and the
/// audit trail records every manual action.
/// </summary>
public class InquiryInboxTests
{
    private static InquiryController CreateController(CateringFlowDbContext db)
    {
        var controller = new InquiryController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<(InquiryModel Inquiry, CRMLeadModel Lead)> SeedInquiryAsync(
        CateringFlowDbContext db,
        string status = InquiryStatuses.New,
        string? assignedTo = null)
    {
        var lead = new CRMLeadModel
        {
            LeadName = "Ana Dela Cruz",
            Email = "ana@example.com",
            Stage = CRMLeadModelStages.New,
            AssignedTo = assignedTo,
            CreatedAt = DateTime.Now
        };
        db.CrmLeads.Add(lead);
        await db.SaveChangesAsync();

        var inquiry = new InquiryModel
        {
            FullName = "Ana Dela Cruz",
            Email = "ana@example.com",
            Phone = "0917-123-4567",
            EventType = "Wedding",
            PaxCount = 150,
            Message = "Wedding buffet for 150 guests.",
            Source = InquirySources.Website,
            Status = status,
            AssignedTo = assignedTo,
            CrmLeadId = lead.Id,
            CreatedAt = DateTime.Now
        };
        db.Inquiries.Add(inquiry);
        await db.SaveChangesAsync();

        return (inquiry, lead);
    }

    // ---------- inbox listing ----------

    [Fact]
    public async Task Inquiries_ReturnsTheInboxPagedNewestFirst()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 1; i <= 11; i++)
        {
            db.Inquiries.Add(new InquiryModel
            {
                FullName = $"Client {i:00}",
                Email = $"client{i}@example.com",
                Message = "Please quote.",
                Source = InquirySources.Website,
                Status = InquiryStatuses.New,
                CreatedAt = new DateTime(2026, 1, 1).AddDays(i)
            });
        }
        await db.SaveChangesAsync();

        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);

        var view = Assert.IsType<ViewResult>(await controller.Inquiries(null, null, null, null));
        var paged = Assert.IsType<PagedResult<InquiryModel>>(view.Model);

        Assert.Equal(11, paged.TotalItems);
        Assert.Equal(10, paged.Items.Count);
        Assert.Equal("Client 11", paged.Items[0].FullName);
        Assert.Equal(2, paged.TotalPages);
    }

    [Fact]
    public async Task Inquiries_FiltersBySearchStatusAndAssignee()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Inquiries.AddRange(
            new InquiryModel
            {
                FullName = "Ana Dela Cruz",
                Email = "ana@example.com",
                Company = "Dela Cruz Foods",
                Message = "Wedding.",
                Source = InquirySources.Website,
                Status = InquiryStatuses.Contacted,
                AssignedTo = "Carlo Aquino",
                CreatedAt = DateTime.Now
            },
            new InquiryModel
            {
                FullName = "Bing Corp",
                Email = "events@bingcorp.ph",
                Company = "Bing Corp",
                Message = "Town hall.",
                Source = InquirySources.PackagesPage,
                Status = InquiryStatuses.New,
                CreatedAt = DateTime.Now
            });
        await db.SaveChangesAsync();

        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);

        var bySearch = Assert.IsType<ViewResult>(await controller.Inquiries("bingcorp", null, null, null));
        Assert.Single(Assert.IsType<PagedResult<InquiryModel>>(bySearch.Model).Items);

        var byStatus = Assert.IsType<ViewResult>(await controller.Inquiries(null, InquiryStatuses.Contacted, null, null));
        Assert.Single(Assert.IsType<PagedResult<InquiryModel>>(byStatus.Model).Items);

        var byAssignee = Assert.IsType<ViewResult>(await controller.Inquiries(null, null, "Carlo Aquino", null));
        Assert.Single(Assert.IsType<PagedResult<InquiryModel>>(byAssignee.Model).Items);
    }

    [Fact]
    public async Task Inquiries_ReportsTheStatusBreakdown()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Inquiries.AddRange(
            NewInquiry("New one", InquiryStatuses.New),
            NewInquiry("Contacted one", InquiryStatuses.Contacted),
            NewInquiry("Quoted one", InquiryStatuses.Quoted),
            NewInquiry("Won one", InquiryStatuses.Won, "Carlo Aquino"),
            NewInquiry("Unassigned won", InquiryStatuses.Won));
        await db.SaveChangesAsync();

        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);

        await controller.Inquiries(null, null, null, null);

        Assert.Equal(5, Convert.ToInt32(controller.ViewData["TotalInquiries"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalNew"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalContacted"]));
        Assert.Equal(1, Convert.ToInt32(controller.ViewData["TotalQuoted"]));
        Assert.Equal(2, Convert.ToInt32(controller.ViewData["TotalWon"]));
        Assert.Equal(4, Convert.ToInt32(controller.ViewData["TotalUnassigned"]));
    }

    // ---------- manual status handling ----------

    [Theory]
    [InlineData(InquiryStatuses.Contacted, CRMLeadModelStages.Contacted)]
    [InlineData(InquiryStatuses.Quoted, CRMLeadModelStages.Proposal)]
    [InlineData(InquiryStatuses.Won, CRMLeadModelStages.Won)]
    [InlineData(InquiryStatuses.Lost, CRMLeadModelStages.Lost)]
    public async Task UpdateStatus_MovesTheInquiryAndItsCrmLeadTogether(string status, string expectedStage)
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _) = await SeedInquiryAsync(db);

        var result = await CreateController(db).UpdateStatus(inquiry.Id, status);

        Assert.Equal("/SuperAdmin/Inquiries", Assert.IsType<RedirectResult>(result).Url);

        var stored = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal(status, stored.Status);
        Assert.NotNull(stored.UpdatedAt);

        var lead = await db.CrmLeads.AsNoTracking().SingleAsync();
        Assert.Equal(expectedStage, lead.Stage);
        Assert.NotNull(lead.LastContact);
    }

    [Fact]
    public async Task UpdateStatus_RejectsAnUnknownStatus()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _) = await SeedInquiryAsync(db);

        await CreateController(db).UpdateStatus(inquiry.Id, "Teleported");

        var stored = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal(InquiryStatuses.New, stored.Status);
    }

    [Fact]
    public async Task UpdateStatus_OnAMissingInquiry_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateController(db).UpdateStatus(999, InquiryStatuses.Contacted));
    }

    [Fact]
    public async Task UpdateStatus_NotifiesTheTeamAndWritesToTheActivityLog()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _) = await SeedInquiryAsync(db);

        await CreateController(db).UpdateStatus(inquiry.Id, InquiryStatuses.Quoted);

        var notification = await db.Notifications.AsNoTracking().SingleAsync();
        Assert.Contains("Quoted", notification.Title);
        Assert.Equal("Sales / CRM Staff", notification.TargetRole);

        var log = await db.ActivityLogs.AsNoTracking()
            .SingleAsync(l => l.EntityType == "Inquiry" && l.EntityId == inquiry.Id);
        Assert.Equal("Updated", log.Action);
    }

    [Fact]
    public async Task UpdateStatus_RedirectsBackToALocalReturnUrl()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _) = await SeedInquiryAsync(db);

        var result = await CreateController(db).UpdateStatus(
            inquiry.Id,
            InquiryStatuses.Contacted,
            "/SuperAdmin/Inquiries?status=New");

        Assert.Equal("/SuperAdmin/Inquiries?status=New", Assert.IsType<RedirectResult>(result).Url);
    }

    // ---------- assignment ----------

    [Fact]
    public async Task Assign_ClaimsTheInquiryAndTheLeadForATeamMember()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _) = await SeedInquiryAsync(db);

        var result = await CreateController(db).Assign(inquiry.Id, "Diana Cruz");

        Assert.Equal("/SuperAdmin/Inquiries", Assert.IsType<RedirectResult>(result).Url);

        var stored = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal("Diana Cruz", stored.AssignedTo);

        var lead = await db.CrmLeads.AsNoTracking().SingleAsync();
        Assert.Equal("Diana Cruz", lead.AssignedTo);
    }

    [Fact]
    public async Task Assign_WithoutAnOwner_ReleasesTheInquiry()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _) = await SeedInquiryAsync(db, InquiryStatuses.Contacted, "Carlo Aquino");

        await CreateController(db).Assign(inquiry.Id, "  ");

        Assert.Null((await db.Inquiries.AsNoTracking().SingleAsync()).AssignedTo);
        Assert.Null((await db.CrmLeads.AsNoTracking().SingleAsync()).AssignedTo);
    }

    [Fact]
    public async Task Assign_OnAMissingInquiry_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateController(db).Assign(1234, "Carlo Aquino"));
    }

    [Fact]
    public async Task Assign_IsRecordedInTheActivityLog()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _) = await SeedInquiryAsync(db);

        await CreateController(db).Assign(inquiry.Id, "Carlo Aquino");

        var log = await db.ActivityLogs.AsNoTracking()
            .SingleAsync(l => l.EntityType == "Inquiry" && l.EntityId == inquiry.Id);
        Assert.Contains("Carlo Aquino", log.Description);
    }

    // ---------- deletion keeps the pipeline ----------

    [Fact]
    public async Task Delete_RemovesTheInquiryButKeepsTheCrmLead()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, lead) = await SeedInquiryAsync(db);

        var result = await CreateController(db).Delete(inquiry.Id);

        Assert.Equal("/SuperAdmin/Inquiries", Assert.IsType<RedirectResult>(result).Url);
        Assert.Empty(await db.Inquiries.ToListAsync());

        var keptLead = await db.CrmLeads.AsNoTracking().SingleAsync();
        Assert.Equal(lead.Id, keptLead.Id);
        Assert.Equal(CRMLeadModelStages.New, keptLead.Stage);
    }

    [Fact]
    public async Task Delete_OnAMissingInquiry_IsNotFound()
    {
        using var db = TestControllerSupport.CreateContext();

        Assert.IsType<NotFoundResult>(await CreateController(db).Delete(77));
    }

    [Fact]
    public async Task Delete_IsRecordedInTheActivityLog()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _) = await SeedInquiryAsync(db);

        await CreateController(db).Delete(inquiry.Id);

        var log = await db.ActivityLogs.AsNoTracking().SingleAsync(l => l.EntityType == "Inquiry");
        Assert.Equal("Deleted", log.Action);
    }

    // ---------- there is no manual inquiry creation ----------

    [Fact]
    public void InquiryController_ExposesNoCreateAction()
    {
        var actions = typeof(InquiryController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => m.ReturnType == typeof(IActionResult)
                        || typeof(Task<IActionResult>).IsAssignableFrom(m.ReturnType))
            .Select(m => m.Name)
            .ToList();

        Assert.DoesNotContain("Create", actions);
        Assert.DoesNotContain("Add", actions);
        Assert.Contains("UpdateStatus", actions);
        Assert.Contains("Assign", actions);
        Assert.Contains("Delete", actions);
    }

    [Fact]
    public async Task TheInboxRouteIsReachableFromTheCrmPage()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _) = await SeedInquiryAsync(db);

        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        var view = Assert.IsType<ViewResult>(await controller.Inquiries(null, null, null, null));
        var paged = Assert.IsType<PagedResult<InquiryModel>>(view.Model);

        Assert.Equal(inquiry.Id, paged.Items.Single().Id);
    }

    // ---------- view, RBAC and navigation ----------

    [Fact]
    public void InboxView_HasNoAddInquiryControl()
    {
        var view = TestControllerSupport.RepoFile("Views/SuperAdmin/Inquiries.cshtml");

        Assert.DoesNotContain("Add Inquiry", view);
        Assert.DoesNotContain("/Inquiry/Create", view);
    }

    [Theory]
    [InlineData("/Inquiry/Assign")]
    [InlineData("/Inquiry/UpdateStatus")]
    [InlineData("/Inquiry/Delete")]
    public void InboxView_PostsToTheManualHandlingActions(string action)
    {
        Assert.Contains(action, TestControllerSupport.RepoFile("Views/SuperAdmin/Inquiries.cshtml"));
    }

    [Fact]
    public void InboxView_CarriesAntiForgeryTokensAndTheReturnUrl()
    {
        var view = TestControllerSupport.RepoFile("Views/SuperAdmin/Inquiries.cshtml");

        Assert.Contains("Html.AntiForgeryToken()", view);
        Assert.Contains("name=\"returnUrl\"", view);
    }

    [Fact]
    public void InboxView_ShowsTheClientMessageAndTheLinkedLead()
    {
        var view = TestControllerSupport.RepoFile("Views/SuperAdmin/Inquiries.cshtml");

        Assert.Contains("@inquiry.Message", view);
        Assert.Contains("/CRMLead/Details/@inquiry.CrmLeadId", view);
    }

    [Theory]
    [InlineData(UserRoles.SuperAdmin)]
    [InlineData(UserRoles.SalesCrm)]
    public void Inquiries_AreRbacGatedForTheTeamsThatHandleInquiries(string role)
    {
        var rbac = new RbacService();

        Assert.True(rbac.CanAccessPage(role, "Inquiries"));
        Assert.True(rbac.CanAccessPage(role, "UpdateInquiryStatus"));
        Assert.True(rbac.CanAccessPage(role, "AssignInquiry"));
        Assert.True(rbac.CanAccessPage(role, "DeleteInquiry"));
    }

    [Theory]
    [InlineData(UserRoles.InventoryStaff)]
    [InlineData(UserRoles.KitchenManager)]
    [InlineData(UserRoles.FinanceStaff)]
    [InlineData(UserRoles.StaffCrew)]
    [InlineData(UserRoles.EventCoordinator)]
    public void Inquiries_AreHiddenFromTeamsThatDoNotHandleThem(string role)
    {
        var rbac = new RbacService();

        Assert.False(rbac.CanAccessPage(role, "Inquiries"));
    }

    [Fact]
    public void TheAdminSidebar_LinksTheInquiryInbox()
    {
        var sidebar = TestControllerSupport.RepoFile("Views/SuperAdmin/Components/_AdminSidebar.cshtml");

        Assert.Contains("allowedPages.ContainsKey(\"Inquiries\")", sidebar);
        Assert.Contains("/SuperAdmin/Inquiries", sidebar);
        Assert.Contains("Inquiry Inbox", sidebar);
    }

    [Fact]
    public void TheCrmPage_LinksToTheInquiryInbox()
    {
        var crm = TestControllerSupport.RepoFile("Views/SuperAdmin/CRM.cshtml");

        Assert.Contains("/SuperAdmin/Inquiries", crm);
    }

    [Fact]
    public void TheInboxStylesAreWiredUp()
    {
        var css = TestControllerSupport.RepoFile("wwwroot/css/superadmin.css");

        Assert.Contains(".inquiry-kpi-card", css);
        Assert.Contains(".inquiry-message", css);
        Assert.Contains(".inquiry-assign-form", css);
    }

    private static InquiryModel NewInquiry(string name, string status, string? assignedTo = null) => new()
    {
        FullName = name,
        Email = $"{name.Replace(" ", string.Empty).ToLowerInvariant()}@example.com",
        Message = "Please send a quotation.",
        Source = InquirySources.Website,
        Status = status,
        AssignedTo = assignedTo,
        CreatedAt = DateTime.Now
    };
}
