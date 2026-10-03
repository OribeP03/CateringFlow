using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A22 - mirror of Phase 22 (Inquiry Pipeline backend).
/// A public inquiry must be persisted, routed into the CRM as a "New" lead,
/// notified to the team that handles inquiries, and written to the audit trail.
/// </summary>
public class ClientInquiryTests
{
    private static ClientController CreateClientController(CateringFlowDbContext db)
    {
        var controller = new ClientController(db, new FakeWebHostEnvironment());
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static InquiryRequest ValidRequest(string email = "client@example.com") => new()
    {
        FullName = "Ana Dela Cruz",
        Email = email,
        Phone = "0917-123-4567",
        EventType = "Wedding",
        Company = "Dela Cruz Family",
        EventDate = new DateTime(2026, 12, 12),
        PaxCount = 150,
        Venue = "Taal Vista Pavilion",
        Message = "We would like a wedding package with a grand buffet and a tasting."
    };

    private static OkObjectResult AssertJsonOk(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        return ok;
    }

    private static string? ReadString(IActionResult result, string property)
    {
        var json = AssertJsonOk(result);
        var type = json.Value!.GetType();
        return type.GetProperty(property)?.GetValue(json.Value) as string;
    }

    [Fact]
    public async Task Inquiry_WithValidRequest_PersistsTheInquiry()
    {
        using var db = TestControllerSupport.CreateContext();

        var json = AssertJsonOk(await CreateClientController(db).Inquiry(ValidRequest()));
        var success = json.Value!.GetType().GetProperty("success")?.GetValue(json.Value);
        Assert.Equal(true, success);

        var inquiry = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal("Ana Dela Cruz", inquiry.FullName);
        Assert.Equal("client@example.com", inquiry.Email);
        Assert.Equal("0917-123-4567", inquiry.Phone);
        Assert.Equal("Wedding", inquiry.EventType);
        Assert.Equal("Dela Cruz Family", inquiry.Company);
        Assert.Equal(new DateTime(2026, 12, 12), inquiry.EventDate);
        Assert.Equal(150, inquiry.PaxCount);
        Assert.Equal("Taal Vista Pavilion", inquiry.Venue);
        Assert.Contains("wedding package", inquiry.Message);
        Assert.Equal(InquiryStatuses.New, inquiry.Status);
        Assert.Equal(InquirySources.Website, inquiry.Source);
        Assert.Null(inquiry.AssignedTo);
    }

    [Fact]
    public async Task Inquiry_ReturnsAReferenceTheClientCanQuote()
    {
        using var db = TestControllerSupport.CreateContext();

        var reference = ReadString(await CreateClientController(db).Inquiry(ValidRequest()), "reference");

        Assert.NotNull(reference);
        var inquiry = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal($"INQ-{inquiry.CreatedAt.Year}-{inquiry.Id:000}", reference);
    }

    [Fact]
    public async Task Inquiry_CreatesACrmLeadAtTheNewStage()
    {
        using var db = TestControllerSupport.CreateContext();

        await CreateClientController(db).Inquiry(ValidRequest());

        var lead = await db.CrmLeads.AsNoTracking().SingleAsync();
        Assert.Equal("Ana Dela Cruz", lead.LeadName);
        Assert.Equal("Dela Cruz Family", lead.Company);
        Assert.Equal("client@example.com", lead.Email);
        Assert.Equal("0917-123-4567", lead.Phone);
        Assert.Equal(CRMLeadModelStages.New, lead.Stage);
        Assert.Null(lead.AssignedTo);
        Assert.True(lead.EstimatedValue > 0m);
        Assert.Contains("Auto-created from Website inquiry", lead.Notes);
    }

    [Fact]
    public async Task Inquiry_LinksTheLeadBackToTheInquiry()
    {
        using var db = TestControllerSupport.CreateContext();

        await CreateClientController(db).Inquiry(ValidRequest());

        var inquiry = await db.Inquiries.AsNoTracking().SingleAsync();
        var lead = await db.CrmLeads.AsNoTracking().SingleAsync();
        Assert.NotNull(inquiry.CrmLeadId);
        Assert.Equal(lead.Id, inquiry.CrmLeadId);
    }

    [Fact]
    public async Task Inquiry_EstimatesValueFromTheSelectedPackage()
    {
        using var db = TestControllerSupport.CreateContext();
        var package = new MenuPackageModel
        {
            PackageName = "Grand Buffet",
            Description = "Buffet",
            PricePerPax = 1500m,
            CourseCount = 4,
            ServiceHours = 6,
            Status = "Active"
        };
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        var request = ValidRequest();
        request.PackageId = package.Id;
        request.PaxCount = 100;

        await CreateClientController(db).Inquiry(request);

        var lead = await db.CrmLeads.AsNoTracking().SingleAsync();
        Assert.Equal(150000m, lead.EstimatedValue);

        var inquiry = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal(package.Id, inquiry.PackageId);
    }

    [Fact]
    public async Task Inquiry_WithoutPackage_UsesTheDefaultPerPaxEstimate()
    {
        using var db = TestControllerSupport.CreateContext();
        var request = ValidRequest();
        request.PaxCount = 20;

        await CreateClientController(db).Inquiry(request);

        var lead = await db.CrmLeads.AsNoTracking().SingleAsync();
        Assert.Equal(ClientController.DefaultPricePerPax * 20m, lead.EstimatedValue);
    }

    [Fact]
    public async Task Inquiry_NotifiesTheStaffWhoHandleInquiries()
    {
        using var db = TestControllerSupport.CreateContext();

        await CreateClientController(db).Inquiry(ValidRequest());

        var notification = await db.Notifications.AsNoTracking().SingleAsync();
        Assert.Equal("New Website Inquiry", notification.Title);
        Assert.Equal("Sales / CRM Staff", notification.TargetRole);
        Assert.False(notification.IsRead);
        Assert.Contains("Ana Dela Cruz", notification.Message);
        Assert.Contains("Wedding", notification.Message);
    }

    [Fact]
    public async Task Inquiry_WritesAnActivityLogEntry()
    {
        using var db = TestControllerSupport.CreateContext();

        await CreateClientController(db).Inquiry(ValidRequest());

        var inquiry = await db.Inquiries.AsNoTracking().SingleAsync();
        var log = await db.ActivityLogs.AsNoTracking().SingleAsync();
        Assert.Equal("Created", log.Action);
        Assert.Equal("Inquiry", log.EntityType);
        Assert.Equal(inquiry.Id, log.EntityId);
        Assert.Contains("Website inquiry", log.Description);
        Assert.Contains("Routed to CRM lead", log.Description);
    }

    [Fact]
    public async Task Inquiry_StoresBlankOptionalFieldsAsNull()
    {
        using var db = TestControllerSupport.CreateContext();
        var request = ValidRequest();
        request.Phone = "   ";
        request.Venue = null;
        request.Company = null;
        request.EventType = null;
        request.EventDate = null;

        await CreateClientController(db).Inquiry(request);

        var inquiry = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Null(inquiry.Phone);
        Assert.Null(inquiry.Venue);
        Assert.Null(inquiry.Company);
        Assert.Null(inquiry.EventType);
        Assert.Null(inquiry.EventDate);
    }

    [Fact]
    public async Task Inquiry_WithUnknownPackage_StillStoresTheInquiry()
    {
        using var db = TestControllerSupport.CreateContext();
        var request = ValidRequest();
        request.PackageId = 9999;

        var result = await CreateClientController(db).Inquiry(request);

        AssertJsonOk(result);
        var inquiry = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Null(inquiry.PackageId);
    }

    [Fact]
    public async Task Inquiry_TrimsWhitespaceAroundTheSubmittedFields()
    {
        using var db = TestControllerSupport.CreateContext();
        var request = ValidRequest();
        request.FullName = "  Ana Dela Cruz  ";
        request.Message = "  Please send the menu.  ";
        request.Phone = " 0917-123-4567 ";

        await CreateClientController(db).Inquiry(request);

        var inquiry = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal("Ana Dela Cruz", inquiry.FullName);
        Assert.Equal("Please send the menu.", inquiry.Message);
        Assert.Equal("0917-123-4567", inquiry.Phone);
    }

    [Fact]
    public async Task Inquiry_KeepsTheSubmittedSource()
    {
        using var db = TestControllerSupport.CreateContext();
        var request = ValidRequest();
        request.Source = InquirySources.PackagesPage;

        await CreateClientController(db).Inquiry(request);

        var inquiry = await db.Inquiries.AsNoTracking().SingleAsync();
        Assert.Equal(InquirySources.PackagesPage, inquiry.Source);

        var lead = await db.CrmLeads.AsNoTracking().SingleAsync();
        Assert.Contains("Packages Page inquiry", lead.Notes);
    }

    // ---------- validation ----------

    [Fact]
    public async Task Inquiry_WithoutName_IsRejectedAndWritesNothing()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateClientController(db);
        var request = ValidRequest();
        request.FullName = "";
        controller.ModelState.AddModelError(nameof(request.FullName), "Please enter your full name.");

        var result = await controller.Inquiry(request);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Inquiries.ToListAsync());
        Assert.Empty(await db.CrmLeads.ToListAsync());
    }

    [Fact]
    public async Task Inquiry_WithInvalidEmail_IsRejectedAndWritesNothing()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateClientController(db);
        var request = ValidRequest();
        request.Email = "not-an-email";
        controller.ModelState.AddModelError(nameof(request.Email), "Please enter a valid email address.");

        var result = await controller.Inquiry(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
        Assert.Empty(await db.Inquiries.ToListAsync());
        Assert.Empty(await db.CrmLeads.ToListAsync());
    }

    [Fact]
    public async Task Inquiry_WithoutMessage_IsRejectedAndWritesNothing()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateClientController(db);
        controller.ModelState.AddModelError("Message", "Please tell us about your event.");

        var result = await controller.Inquiry(ValidRequest());

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Inquiries.ToListAsync());
    }

    [Fact]
    public async Task Inquiry_WithNegativePax_IsRejectedByModelValidation()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateClientController(db);
        var request = ValidRequest();
        request.PaxCount = -5;
        controller.ModelState.AddModelError(nameof(request.PaxCount), "Guest count looks invalid.");

        Assert.IsType<BadRequestObjectResult>(await controller.Inquiry(request));
        Assert.Empty(await db.Inquiries.ToListAsync());
    }

    [Fact]
    public async Task Inquiry_WithNoPax_StillCreatesTheLeadUsingOneGuestMinimum()
    {
        using var db = TestControllerSupport.CreateContext();
        var request = ValidRequest();
        request.PaxCount = 0;

        await CreateClientController(db).Inquiry(request);

        var lead = await db.CrmLeads.AsNoTracking().SingleAsync();
        Assert.Equal(ClientController.DefaultPricePerPax, lead.EstimatedValue);
    }

    [Fact]
    public async Task Inquiry_WithoutRequest_IsRejectedWithoutThrowing()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateClientController(db);

        Assert.IsType<BadRequestObjectResult>(await controller.Inquiry(null!));
        Assert.Empty(await db.Inquiries.ToListAsync());
    }

    // ---------- status -> CRM stage mapping ----------

    [Theory]
    [InlineData(InquiryStatuses.New, CRMLeadModelStages.New)]
    [InlineData(InquiryStatuses.Contacted, CRMLeadModelStages.Contacted)]
    [InlineData(InquiryStatuses.Quoted, CRMLeadModelStages.Proposal)]
    [InlineData(InquiryStatuses.Won, CRMLeadModelStages.Won)]
    [InlineData(InquiryStatuses.Lost, CRMLeadModelStages.Lost)]
    public void InquiryStatus_MapsOntoTheCrmPipelineStage(string status, string expectedStage)
    {
        Assert.Equal(expectedStage, InquiryStatuses.ToLeadStage(status));
    }

    [Fact]
    public void InquiryStatuses_ExposeTheFullWorkflow()
    {
        Assert.Equal(
            new[] { "New", "Contacted", "Quoted", "Won", "Lost" },
            InquiryStatuses.All);
    }
}