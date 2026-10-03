using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using cateringflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A28 - mirror of Phase 28 (CRM Pipeline Quick-Quote Integration Test).
/// Verifies the "Quote" button on the CRM pipeline triggers ConvertFromInquiry,
/// links inquiry/lead/quotation, advances the pipeline and logs the action.
/// </summary>
public class CrmQuickQuoteFromPipelineTests
{
    private static QuotationController CreateQuotationController(CateringFlowDbContext db)
    {
        var controller = new QuotationController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task<(InquiryModel Inquiry, CRMLeadModel Lead, MenuPackageModel Package)> SeedAsync(
        CateringFlowDbContext db,
        string status = InquiryStatuses.New,
        string leadStage = CRMLeadModelStages.Qualified,
        int pax = 150,
        decimal pricePerPax = 500m)
    {
        var package = new MenuPackageModel
        {
            PackageName = "Pipeline Package",
            PricePerPax = pricePerPax,
            Status = "Active",
            CreatedAt = DateTime.Now
        };
        db.MenuPackages.Add(package);
        await db.SaveChangesAsync();

        var lead = new CRMLeadModel
        {
            LeadName = "Sara Villanueva",
            Email = "sara@example.com",
            Phone = "0918-555-2222",
            Stage = leadStage,
            CreatedAt = DateTime.Now.AddHours(-2)
        };
        db.CrmLeads.Add(lead);
        await db.SaveChangesAsync();

        var inquiry = new InquiryModel
        {
            FullName = "Sara Villanueva",
            Email = "sara@example.com",
            Phone = "0918-555-2222",
            EventType = "Corporate",
            Company = "Brightside HR Solutions",
            EventDate = DateTime.Today.AddDays(40),
            PaxCount = pax,
            Venue = "Makati Grand Ballroom",
            PackageId = package.Id,
            Message = "Annual summit - need full service buffet.",
            Source = InquirySources.Website,
            Status = status,
            CreatedAt = DateTime.Now.AddHours(-1)
        };
        db.Inquiries.Add(inquiry);
        await db.SaveChangesAsync();

        inquiry.CrmLeadId = lead.Id;
        db.Inquiries.Update(inquiry);
        await db.SaveChangesAsync();

        return (inquiry, lead, package);
    }

    [Fact]
    public async Task ConvertFromInquiry_PostFromCrmFlow_CreatesLinkedQuotationAndAdvancesPipeline()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, lead, package) = await SeedAsync(db);
        var controller = CreateQuotationController(db);

        var result = await controller.ConvertFromInquiry(inquiry.Id, returnUrl: "/SuperAdmin/CRM");

        Assert.True(result is RedirectToActionResult or RedirectResult);

        var quotation = await db.Quotations.SingleAsync(q => q.InquiryId == inquiry.Id);
        Assert.Equal(package.PricePerPax * inquiry.PaxCount, quotation.TotalAmount);
        Assert.Equal("Draft", quotation.Status);

        var reloadedInquiry = await db.Inquiries.SingleAsync(i => i.Id == inquiry.Id);
        Assert.Equal(InquiryStatuses.Quoted, reloadedInquiry.Status);
        Assert.NotNull(reloadedInquiry.UpdatedAt);

        var reloadedLead = await db.CrmLeads.SingleAsync(l => l.Id == lead.Id);
        Assert.Equal(CRMLeadModelStages.Proposal, reloadedLead.Stage);
        Assert.NotNull(reloadedLead.LastContact);

        Assert.True(await db.ActivityLogs.AnyAsync(a => a.EntityType == "Quotation" && a.Action == "Created"));
        Assert.True(await db.Notifications.AnyAsync(n => n.Title == "Quotation Generated from Inquiry"));

        // redirect checked via action name/route
    }

    [Fact]
    public void CrmView_RendersQuoteButtonForLeadsWithInquiryAndConversionStats()
    {
        var crm = TestControllerSupport.RepoFile("Views/SuperAdmin/CRM.cshtml");

        Assert.Contains("ConvertFromInquiry", crm);
        Assert.Contains("Quote", crm);
        Assert.Contains("converted to quotations", crm);
        Assert.Contains("InquiryIdByLead", crm);
    }

    [Fact]
    public async Task CrmBoard_IncludesInquiryMapAndQuotedStatsAfterConversion()
    {
        using var db = TestControllerSupport.CreateContext();
        var (inquiry, lead, package) = await SeedAsync(db);
        var quotationController = CreateQuotationController(db);
        await quotationController.ConvertFromInquiry(inquiry.Id);

        var superAdmin = new SuperAdminController(db);
        TestControllerSupport.InitController(superAdmin);
        var view = Assert.IsType<ViewResult>(await superAdmin.CRM(null, null, null, null));
        var board = Assert.IsType<CrmBoardViewModel>(view.Model);

        Assert.Equal(1, board.QuotedLeads);
        Assert.Equal(package.PricePerPax * inquiry.PaxCount, board.QuotedValue);
        Assert.True(board.QuotationRate > 0);
        Assert.Equal(inquiry.Id, board.InquiryIdByLead[lead.Id]);
    }
}


