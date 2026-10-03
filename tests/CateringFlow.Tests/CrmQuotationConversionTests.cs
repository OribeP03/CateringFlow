using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A27 - mirror of Phase 27 (CRM-to-quotation conversion).
/// Closing the commercial loop: a live website inquiry becomes a real, linked
/// quotation, the pipeline moves forward, the client gets a customer record and
/// the audit trail proves who did it.
/// </summary>
public class CrmQuotationConversionTests
{
    private static QuotationController CreateQuotationController(CateringFlowDbContext db)
    {
        var controller = new QuotationController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static SuperAdminController CreateSuperAdminController(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<(InquiryModel Inquiry, CRMLeadModel Lead, MenuPackageModel Package)> SeedAsync(
        CateringFlowDbContext db,
        string status = InquiryStatuses.Contacted,
        string leadStage = CRMLeadModelStages.Qualified,
        int pax = 120,
        decimal pricePerPax = 450m)
    {
        var package = new MenuPackageModel
        {
            PackageName = "Signature Buffet",
            PricePerPax = pricePerPax,
            Status = "Active",
            CreatedAt = DateTime.Now
        };
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        var lead = new CRMLeadModel
        {
            LeadName = "Ana Dela Cruz",
            Email = "ana@example.com",
            Phone = "0917-123-4567",
            Stage = leadStage,
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
            Company = "Dela Cruz Household",
            EventDate = DateTime.Today.AddDays(60),
            PaxCount = pax,
            Venue = "Tagaytay Ridgeline Resort",
            PackageId = package.Id,
            Message = "Wedding buffet with live stations, please quote.",
            Source = InquirySources.Website,
            Status = status,
            CreatedAt = DateTime.Now
        };
        db.Inquiries.Add(inquiry);
        await db.SaveChangesAsync();

        inquiry.CrmLeadId = lead.Id;
        db.Inquiries.Update(inquiry);
        await db.SaveChangesAsync();

        return (inquiry, lead, package);
    }

    // ---------- one-click conversion ----------

    [Fact]
    public async Task ConvertFromInquiry_CreatesADraftQuotationPrefilledFromTheInquiry()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _, package) = await SeedAsync(db);
        var controller = CreateQuotationController(db);

        var result = await controller.ConvertFromInquiry(inquiry.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);

        var quotation = await db.Quotations.SingleAsync();
        Assert.Equal("Draft", quotation.Status);
        Assert.Equal(inquiry.Id, quotation.InquiryId);
        Assert.Equal(inquiry.EventDate!.Value.Date, quotation.EventDate.Date);
        Assert.Equal(inquiry.PaxCount, quotation.PaxCount);
        Assert.Equal(package.Id, quotation.PackageId);
        Assert.Equal(package.PricePerPax * inquiry.PaxCount, quotation.TotalAmount);
        Assert.Contains(inquiry.Reference, quotation.Notes!);
        Assert.Contains(inquiry.Venue!, quotation.Notes!);
        Assert.Equal(quotation.Id, Convert.ToInt32(redirect.RouteValues!["id"]));
    }

    [Fact]
    public async Task ConvertFromInquiry_LinksInquiryAndLeadAndWritesTheAuditTrail()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, lead, _) = await SeedAsync(db);
        var controller = CreateQuotationController(db);

        await controller.ConvertFromInquiry(inquiry.Id);

        var reloadedInquiry = await db.Inquiries.SingleAsync(i => i.Id == inquiry.Id);
        Assert.Equal(InquiryStatuses.Quoted, reloadedInquiry.Status);
        Assert.NotNull(reloadedInquiry.UpdatedAt);

        var reloadedLead = await db.CrmLeads.SingleAsync(l => l.Id == lead.Id);
        Assert.Equal(CRMLeadModelStages.Proposal, reloadedLead.Stage);
        Assert.NotNull(reloadedLead.LastContact);

        var log = await db.ActivityLogs.SingleAsync(a => a.EntityType == "Quotation");
        Assert.Equal("Created", log.Action);
        Assert.Contains("generated from inquiry", log.Description);

        var notification = await db.Notifications.SingleAsync(n => n.Title == "Quotation Generated from Inquiry");
        Assert.Equal("Sales / CRM Staff", notification.TargetRole);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task ConvertFromInquiry_CreatesTheCustomerOnceAndReusesItOnASecondInquiry()
    {
        using var db = TestControllerSupport.CreateContext();
        var (first, _, _) = await SeedAsync(db);
        var controller = CreateQuotationController(db);

        await controller.ConvertFromInquiry(first.Id);

        var customer = await db.Customers.SingleAsync();
        Assert.Equal("Ana Dela Cruz", customer.FullName);
        Assert.Equal(first.Email, customer.Email);
        Assert.Equal("Corporate", customer.Type);
        Assert.Contains(first.Reference, customer.Notes!);

        var quotation = await db.Quotations.SingleAsync();
        Assert.Equal(customer.Id, quotation.CustomerId);
        Assert.Equal(customer.Id, (await db.Inquiries.SingleAsync()).CustomerId);
    }

    [Fact]
    public async Task ConvertFromInquiry_ReusesAnExistingCustomerWithTheSameEmail()
    {
        using var db = TestControllerSupport.CreateContext();
        var customer = new CustomerModel
        {
            FullName = "Ana Dela Cruz (existing record)",
            Email = "ana@example.com",
            Type = "Individual",
            Status = "Active",
            CreatedAt = DateTime.Now.AddMonths(-2)
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var (inquiry, _, _) = await SeedAsync(db);
        var controller = CreateQuotationController(db);

        await controller.ConvertFromInquiry(inquiry.Id);

        Assert.Equal(1, await db.Customers.CountAsync());
        Assert.Equal(customer.Id, (await db.Quotations.SingleAsync()).CustomerId);
    }

    [Fact]
    public async Task ConvertFromInquiry_QuotesAnInquiryOnlyOnce()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _, _) = await SeedAsync(db);
        var controller = CreateQuotationController(db);

        await controller.ConvertFromInquiry(inquiry.Id);
        var second = await controller.ConvertFromInquiry(inquiry.Id);

        var quotation = await db.Quotations.SingleAsync();
        var redirect = Assert.IsType<RedirectToActionResult>(second);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(quotation.Id, Convert.ToInt32(redirect.RouteValues!["id"]));
        Assert.Contains("already covers inquiry", controller.TempData["Success"]!.ToString());
    }

    [Fact]
    public async Task ConvertFromInquiry_ReturnsNotFoundForAnUnknownInquiry()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateQuotationController(db);

        Assert.IsType<NotFoundResult>(await controller.ConvertFromInquiry(4242));
        Assert.Empty(await db.Quotations.ToListAsync());
    }

    [Fact]
    public async Task ConvertFromInquiry_RefusesALostInquiry()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, lead, _) = await SeedAsync(db, status: InquiryStatuses.Lost, leadStage: CRMLeadModelStages.Lost);
        var controller = CreateQuotationController(db);

        var result = await controller.ConvertFromInquiry(inquiry.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(await db.Quotations.ToListAsync());
        Assert.Equal(CRMLeadModelStages.Lost, (await db.CrmLeads.SingleAsync()).Stage);
        Assert.Contains("Lost", controller.TempData["Error"]!.ToString());
    }

    [Fact]
    public async Task ConvertFromInquiry_UsesTheNextQuotationNumberAndAPlaceholderDate()
    {
        using var db = TestControllerSupport.CreateContext();
        db.Quotations.Add(new QuotationModel
        {
            QuotationNumber = "QTN-1042",
            CustomerId = 1,
            EventDate = DateTime.Today.AddDays(20),
            PaxCount = 50,
            TotalAmount = 1000m,
            Status = "Sent",
            CreatedAt = DateTime.Now.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var (inquiry, _, _) = await SeedAsync(db, pax: 0);
        inquiry.EventDate = null;
        db.Inquiries.Update(inquiry);
        await db.SaveChangesAsync();

        var controller = CreateQuotationController(db);
        await controller.ConvertFromInquiry(inquiry.Id);

        var quotation = await db.Quotations.SingleAsync(q => q.InquiryId == inquiry.Id);
        Assert.Equal("QTN-1043", quotation.QuotationNumber);
        Assert.Equal(1, quotation.PaxCount);
        Assert.True(quotation.EventDate >= DateTime.Today);
        Assert.True(quotation.ValidUntil >= DateTime.Today.AddDays(14));
    }

    // ---------- prefilled review screen ----------

    [Fact]
    public async Task CreateFromInquiry_PrefillsTheDraftWithoutWritingAnything()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _, package) = await SeedAsync(db);
        var controller = CreateQuotationController(db);

        var view = Assert.IsType<ViewResult>(await controller.CreateFromInquiry(inquiry.Id));
        var draft = Assert.IsType<QuotationModel>(view.Model);

        Assert.Equal("Create", view.ViewName);
        Assert.Equal(inquiry.Id, draft.InquiryId);
        Assert.Equal(inquiry.EventDate!.Value.Date, draft.EventDate.Date);
        Assert.Equal(inquiry.PaxCount, draft.PaxCount);
        Assert.Equal(package.Id, draft.PackageId);
        Assert.Equal("Draft", draft.Status);
        Assert.Contains(inquiry.Reference, draft.Notes!);
        Assert.Equal(inquiry.Reference, draft.InquiryReference);

        Assert.Empty(await db.Quotations.ToListAsync());
        Assert.Empty(await db.Customers.ToListAsync());
    }

    [Fact]
    public async Task CreateFromInquiry_OpensTheExistingQuotationInsteadOfDuplicatingIt()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _, _) = await SeedAsync(db);
        var controller = CreateQuotationController(db);
        await controller.ConvertFromInquiry(inquiry.Id);
        var quotation = await db.Quotations.SingleAsync();

        var result = await controller.CreateFromInquiry(inquiry.Id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(quotation.Id, Convert.ToInt32(redirect.RouteValues!["id"]));
    }

    [Fact]
    public async Task CreateFromInquiry_ReturnsNotFoundForAnUnknownInquiry()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateQuotationController(db);

        Assert.IsType<NotFoundResult>(await controller.CreateFromInquiry(999));
    }

    // ---------- saving the prefilled quotation ----------

    [Fact]
    public async Task Create_SavingAConvertedQuotationLinksTheInquiryAndMovesTheLead()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, lead, package) = await SeedAsync(db, leadStage: CRMLeadModelStages.Contacted);
        var controller = CreateQuotationController(db);

        var result = await controller.Create(new QuotationModel
        {
            QuotationNumber = "QTN-2001",
            CustomerId = 0,
            EventDate = inquiry.EventDate!.Value,
            PaxCount = inquiry.PaxCount,
            PackageId = package.Id,
            TotalAmount = 1m,
            Status = "Draft",
            InquiryId = inquiry.Id
        });

        Assert.IsType<RedirectToActionResult>(result);
        var quotation = await db.Quotations.SingleAsync();
        Assert.Equal(package.PricePerPax * inquiry.PaxCount, quotation.TotalAmount);
        Assert.Equal(inquiry.Id, quotation.InquiryId);
        Assert.True(quotation.CustomerId > 0);
        Assert.Equal(InquiryStatuses.Quoted, (await db.Inquiries.SingleAsync()).Status);
        Assert.Equal(CRMLeadModelStages.Proposal, (await db.CrmLeads.SingleAsync(l => l.Id == lead.Id)).Stage);
        Assert.Contains(inquiry.Reference, controller.TempData["Success"]!.ToString());
    }

    [Fact]
    public async Task Create_NeverMovesAWonLeadBackToProposal()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, lead, package) = await SeedAsync(db, leadStage: CRMLeadModelStages.Won);
        var controller = CreateQuotationController(db);

        await controller.Create(new QuotationModel
        {
            QuotationNumber = "QTN-2002",
            CustomerId = 1,
            EventDate = inquiry.EventDate!.Value,
            PaxCount = 10,
            PackageId = package.Id,
            Status = "Draft",
            InquiryId = inquiry.Id
        });

        Assert.Equal(CRMLeadModelStages.Won, (await db.CrmLeads.SingleAsync(l => l.Id == lead.Id)).Stage);
        Assert.Equal(InquiryStatuses.Quoted, (await db.Inquiries.SingleAsync()).Status);
    }

    [Fact]
    public async Task Create_RejectsAQuotationSourcedFromALostInquiry()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _, package) = await SeedAsync(db, status: InquiryStatuses.Lost, leadStage: CRMLeadModelStages.Lost);
        var controller = CreateQuotationController(db);

        var result = await controller.Create(new QuotationModel
        {
            QuotationNumber = "QTN-2003",
            CustomerId = 1,
            EventDate = inquiry.EventDate!.Value,
            PaxCount = 10,
            PackageId = package.Id,
            Status = "Draft",
            InquiryId = inquiry.Id
        });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Create", view.ViewName);
        Assert.Contains(controller.ModelState, e => e.Key == "InquiryId");
        Assert.Empty(await db.Quotations.ToListAsync());
    }

    [Fact]
    public async Task Create_WithoutAnInquiryKeepsThePlainWorkflow()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateQuotationController(db);

        var result = await controller.Create(new QuotationModel
        {
            QuotationNumber = "QTN-2004",
            CustomerId = 7,
            EventDate = DateTime.Today.AddDays(10),
            PaxCount = 25,
            Status = "Draft"
        });

        Assert.IsType<RedirectToActionResult>(result);
        var quotation = await db.Quotations.SingleAsync();
        Assert.Null(quotation.InquiryId);
        Assert.Equal(0m, quotation.TotalAmount);
        Assert.Equal("Quotation QTN-2004 created successfully.", controller.TempData["Success"]!.ToString());
    }

    // ---------- stage rules ----------

    [Theory]
    [InlineData(CRMLeadModelStages.New, CRMLeadModelStages.Proposal)]
    [InlineData(CRMLeadModelStages.Contacted, CRMLeadModelStages.Proposal)]
    [InlineData(CRMLeadModelStages.Qualified, CRMLeadModelStages.Proposal)]
    [InlineData(CRMLeadModelStages.Proposal, null)]
    [InlineData(CRMLeadModelStages.Negotiation, null)]
    [InlineData(CRMLeadModelStages.Won, null)]
    [InlineData(CRMLeadModelStages.Lost, null)]
    public void StageForQuotation_OnlyMovesLeadsForwardAndNeverTouchesClosedOnes(string current, string? expected)
    {
        Assert.Equal(expected, CrmBoardViewModel.StageForQuotation(current));
    }

    // ---------- CRM page + RBAC + views ----------

    [Fact]
    public async Task CrmPage_ReportsHowManyLeadsWereQuotedAndWhichInquiryToQuote()
    {
        using var db = TestControllerSupport.CreateContext();
        var (quotedInquiry, quotedLead, package) = await SeedAsync(db);
        var (pending, pendingLead, _) = await SeedAsync(db, status: InquiryStatuses.New, leadStage: CRMLeadModelStages.New);

        var controller = CreateQuotationController(db);
        await controller.ConvertFromInquiry(quotedInquiry.Id);

        var superAdmin = CreateSuperAdminController(db);
        var view = Assert.IsType<ViewResult>(await superAdmin.CRM(null, null, null, null));
        var board = Assert.IsType<CrmBoardViewModel>(view.Model);

        Assert.Equal(1, board.QuotedLeads);
        Assert.Equal(package.PricePerPax * quotedInquiry.PaxCount, board.QuotedValue);
        Assert.Equal(50d, board.QuotationRate);
        Assert.Equal(quotedInquiry.Id, board.InquiryIdByLead[quotedLead.Id]);
        Assert.Equal(pending.Id, board.InquiryIdByLead[pendingLead.Id]);
    }

    [Fact]
    public async Task InquiryInbox_ExposesQuotableAndQuotedCounts()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, _, _) = await SeedAsync(db);
        var (lost, _, _) = await SeedAsync(db, status: InquiryStatuses.Lost, leadStage: CRMLeadModelStages.Lost);

        var quotationController = CreateQuotationController(db);
        await quotationController.ConvertFromInquiry(inquiry.Id);

        var superAdmin = CreateSuperAdminController(db);
        await superAdmin.Inquiries(null, null, null, null);

        Assert.Equal(1, Convert.ToInt32(superAdmin.ViewData["TotalQuotable"]));
        Assert.Equal(1, Convert.ToInt32(superAdmin.ViewData["TotalWithQuotation"]));

        var view = Assert.IsType<ViewResult>(await superAdmin.Inquiries(null, null, null, null));
        var paged = Assert.IsType<PagedResult<InquiryModel>>(view.Model);
        var quotedRow = paged.Items.Single(i => i.Id == inquiry.Id);
        Assert.NotNull(quotedRow.Quotation);
        var lostRow = paged.Items.Single(i => i.Id == lost.Id);
        Assert.Null(lostRow.Quotation);
        Assert.False(lostRow.IsQuotable);
    }

    [Theory]
    [InlineData(UserRoles.SuperAdmin)]
    [InlineData(UserRoles.SalesCrm)]
    public void Rbac_OnlySalesCanConvertAnInquiryIntoAQuotation(string role)
    {
        var rbac = new RbacService();

        Assert.True(rbac.CanAccessPage(role, "ConvertInquiryToQuotation"));
        Assert.True(rbac.CanAccessPage(role, "Inquiries"));
    }

    [Theory]
    [InlineData(UserRoles.StaffCrew)]
    [InlineData(UserRoles.InventoryStaff)]
    [InlineData(UserRoles.KitchenManager)]
    [InlineData(UserRoles.EventCoordinator)]
    [InlineData(UserRoles.FinanceStaff)]
    [InlineData(UserRoles.Customer)]
    public void Rbac_NobodyElseCanConvertAnInquiryIntoAQuotation(string role)
    {
        var rbac = new RbacService();

        Assert.False(rbac.CanAccessPage(role, "ConvertInquiryToQuotation"));
    }

    [Fact]
    public void Views_OfferTheConversionControlInTheInboxAndOnThePipeline()
    {
        var inbox = TestControllerSupport.RepoFile("Views/SuperAdmin/Inquiries.cshtml");
        var crm = TestControllerSupport.RepoFile("Views/SuperAdmin/CRM.cshtml");
        var create = TestControllerSupport.RepoFile("Views/Quotation/Create.cshtml");

        Assert.Contains("CreateFromInquiry", inbox);
        Assert.Contains("ConvertFromInquiry", inbox);
        Assert.Contains("<th>Quotation</th>", inbox);
        Assert.DoesNotContain("Add Inquiry", inbox);

        Assert.Contains("ConvertFromInquiry", crm);
        Assert.Contains("converted to quotations", crm);
        Assert.Contains("InquiryIdByLead", crm);

        Assert.Contains("asp-for=\"InquiryId\"", create);
        Assert.Contains("Converted from website inquiry", create);
    }
}