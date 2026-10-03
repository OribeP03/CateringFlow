using cateringflow.Controllers;
using cateringflow.Data;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A21 - mirror of Phase 21 (Real-Life CRM).
/// Leads must never be created by hand: no Create endpoint, no "Add Lead" control,
/// and the CRM board/table must be driven by real database rows.
/// </summary>
public class CrmRealLifeTests
{
    private static CRMLeadController CreateLeadController(CateringFlowDbContext db)
    {
        var controller = new CRMLeadController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static SuperAdminController CreateAdminController(CateringFlowDbContext db)
    {
        var controller = new SuperAdminController(db);
        TestControllerSupport.InitController(controller);
        return controller;
    }

    private static async Task SeedPipelineAsync(CateringFlowDbContext db, string assignee = "Carlo Aquino")
    {
        var stages = new[] { "New", "Contacted", "Qualified", "Proposal", "Negotiation", "Won", "Lost" };
        for (var i = 0; i < stages.Length; i++)
        {
            db.CrmLeads.Add(new CRMLeadModel
            {
                LeadName = $"Lead {stages[i]}",
                Company = $"Company {i}",
                Email = $"lead{i}@example.com",
                Phone = "0917-000-0000",
                EstimatedValue = 10000m * (i + 1),
                Stage = stages[i],
                AssignedTo = assignee,
                CreatedAt = DateTime.Now.AddDays(-(i + 1))
            });
        }

        // A second assignee so the assignee filter has something to filter on.
        db.CrmLeads.Add(new CRMLeadModel
        {
            LeadName = "Lead Diana",
            EstimatedValue = 50000m,
            Stage = "Contacted",
            AssignedTo = "Diana Reyes",
            CreatedAt = DateTime.Now.AddDays(-1)
        });

        await db.SaveChangesAsync();
    }

    // ---------- No manual lead creation ----------

    [Fact]
    public void CrmLeadController_ExposesNoCreateAction()
    {
        var actions = typeof(CRMLeadController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(m => m.Name.Equals("Create", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(actions);
    }

    [Fact]
    public void CrmLeadController_StillExposesReadUpdateAndDeleteActions()
    {
        var actionNames = typeof(CRMLeadController)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToList();

        Assert.Contains("Index", actionNames);
        Assert.Contains("Details", actionNames);
        Assert.Contains("Edit", actionNames);
        Assert.Contains("UpdateStage", actionNames);
        Assert.Contains("Delete", actionNames);
    }

    [Fact]
    public void CreateLeadView_IsRemoved()
    {
        Assert.False(TestControllerSupport.RepoFileExists("Views/CRMLead/Create.cshtml"));
    }

    [Theory]
    [InlineData("Views/SuperAdmin/CRM.cshtml")]
    [InlineData("Views/CRMLead/Index.cshtml")]
    [InlineData("Views/SuperAdmin/Components/_CrmKanbanBoard.cshtml")]
    [InlineData("Views/SuperAdmin/Components/_CrmKpiCards.cshtml")]
    public void CrmViews_ContainNoAddLeadControl(string viewPath)
    {
        var markup = TestControllerSupport.RepoFile(viewPath);

        Assert.DoesNotContain("Add Lead", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("addLeadModal", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("asp-action=\"Create\"", markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddLeadModalPartial_IsRemoved()
    {
        Assert.False(TestControllerSupport.RepoFileExists("Views/SuperAdmin/Components/_AddLeadModal.cshtml"));
    }

    // ---------- Live pipeline board ----------

    [Fact]
    public async Task CrmPage_RendersOneColumnPerPipelineStage()
    {
        using var db = TestControllerSupport.CreateContext();
        await SeedPipelineAsync(db);

        var result = await CreateAdminController(db).CRM(null, null, null, null);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CrmBoardViewModel>(view.Model);

        Assert.Equal(CrmBoardViewModel.Pipeline.Length, model.Columns.Count);
        Assert.Equal(
            CrmBoardViewModel.Pipeline.Select(p => p.Stage),
            model.Columns.Select(c => c.Stage));
    }

    [Fact]
    public async Task CrmPage_PlacesEveryLeadInExactlyOneColumn()
    {
        using var db = TestControllerSupport.CreateContext();
        await SeedPipelineAsync(db);

        var view = Assert.IsType<ViewResult>(await CreateAdminController(db).CRM(null, null, null, null));
        var model = Assert.IsType<CrmBoardViewModel>(view.Model);

        var bucketed = model.Columns.SelectMany(c => c.Leads.Select(l => l.Id)).ToList();
        var allIds = db.CrmLeads.Select(l => l.Id).ToList();

        Assert.Equal(allIds.Count, bucketed.Count);
        Assert.Equal(bucketed.Count, bucketed.Distinct().Count());
        Assert.Equal(allIds.OrderBy(i => i), bucketed.OrderBy(i => i));
    }

    [Fact]
    public async Task CrmPage_ColumnCountsAndTotalsMatchTheDatabase()
    {
        using var db = TestControllerSupport.CreateContext();
        await SeedPipelineAsync(db);

        var view = Assert.IsType<ViewResult>(await CreateAdminController(db).CRM(null, null, null, null));
        var model = Assert.IsType<CrmBoardViewModel>(view.Model);

        foreach (var column in model.Columns)
        {
            var expectedCount = await db.CrmLeads.CountAsync(l => l.Stage == column.Stage);
            var expectedTotal = await db.CrmLeads
                .Where(l => l.Stage == column.Stage)
                .SumAsync(l => (decimal?)l.EstimatedValue) ?? 0m;

            Assert.Equal(expectedCount, column.Count);
            Assert.Equal(expectedTotal, column.TotalValue);
        }
    }

    [Fact]
    public async Task CrmPage_PipelineValueCountsOnlyOpenLeads()
    {
        using var db = TestControllerSupport.CreateContext();
        await SeedPipelineAsync(db);

        var view = Assert.IsType<ViewResult>(await CreateAdminController(db).CRM(null, null, null, null));
        var model = Assert.IsType<CrmBoardViewModel>(view.Model);

        var expectedOpen = await db.CrmLeads
            .Where(l => CrmBoardViewModel.OpenStages.Contains(l.Stage))
            .SumAsync(l => (decimal?)l.EstimatedValue) ?? 0m;

        Assert.Equal(expectedOpen, model.PipelineValue);
        Assert.Equal(6, model.OpenLeads);
        Assert.Equal(1, model.WonLeads);
        Assert.Equal(1, model.LostLeads);
        Assert.Equal(8, model.TotalLeads);
    }

    [Fact]
    public async Task CrmPage_EmptyDatabaseRendersZeroedBoard()
    {
        using var db = TestControllerSupport.CreateContext();

        var view = Assert.IsType<ViewResult>(await CreateAdminController(db).CRM(null, null, null, null));
        var model = Assert.IsType<CrmBoardViewModel>(view.Model);

        Assert.Equal(0, model.TotalLeads);
        Assert.Equal(0m, model.PipelineValue);
        Assert.Equal(0, model.ConversionRate);
        Assert.Equal(CrmBoardViewModel.Pipeline.Length, model.Columns.Count);
        Assert.All(model.Columns, c => Assert.Empty(c.Leads));
    }

    [Fact]
    public async Task CrmPage_KpiCardsAreOrderedAscendingByValue()
    {
        using var db = TestControllerSupport.CreateContext();
        await SeedPipelineAsync(db);

        var view = Assert.IsType<ViewResult>(await CreateAdminController(db).CRM(null, null, null, null));
        var cards = CrmBoardViewModel.BuildKpiCards(Assert.IsType<CrmBoardViewModel>(view.Model));

        Assert.NotEmpty(cards);
        Assert.Equal(cards.Select(c => c.SortValue).OrderBy(v => v), cards.Select(c => c.SortValue));
    }

    // ---------- Lead list paging / filtering ----------

    [Fact]
    public async Task CrmPage_TablePagesAtTenNewestFirst()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 0; i < 25; i++)
        {
            db.CrmLeads.Add(new CRMLeadModel
            {
                LeadName = $"Inquiry {i:00}",
                Stage = "New",
                EstimatedValue = 1000m * i,
                CreatedAt = DateTime.Now.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var controller = CreateAdminController(db);

        var firstPage = Assert.IsType<ViewResult>(await controller.CRM(null, null, null, null));
        var page1 = Assert.IsType<CrmBoardViewModel>(firstPage.Model);
        Assert.Equal(25, page1.Leads.TotalItems);
        Assert.Equal(10, page1.Leads.PageSize);

        var secondPage = Assert.IsType<ViewResult>(await controller.CRM(null, null, null, 2));
        var page2 = Assert.IsType<CrmBoardViewModel>(secondPage.Model);
        Assert.Equal(2, page2.Leads.Page);

        var newest = await db.CrmLeads.OrderByDescending(l => l.CreatedAt).FirstAsync();
        var page1Items = Assert.IsType<PagedResult<CRMLeadModel>>(page1.Leads).Items;
        Assert.Equal(newest.Id, page1Items[0].Id);
    }

    [Fact]
    public async Task CrmPage_StageFilterNarrowsTheTable()
    {
        using var db = TestControllerSupport.CreateContext();
        await SeedPipelineAsync(db);

        var controller = CreateAdminController(db);
        var view = Assert.IsType<ViewResult>(await controller.CRM(null, "New", null, null));
        var model = Assert.IsType<CrmBoardViewModel>(view.Model);

        Assert.Equal("New", model.StageFilter);
        Assert.Equal(1, model.Leads.TotalItems);

        // The board always shows the full pipeline, so the columns stay complete.
        Assert.Equal(8, model.TotalLeads);
    }

    [Fact]
    public async Task CrmPage_AssigneeFilterAndSearchNarrowTheTable()
    {
        using var db = TestControllerSupport.CreateContext();
        await SeedPipelineAsync(db);

        var controller = CreateAdminController(db);

        var byAssignee = Assert.IsType<CrmBoardViewModel>(
            Assert.IsType<ViewResult>(await controller.CRM(null, null, "Diana Reyes", null)).Model);
        Assert.Equal(1, byAssignee.Leads.TotalItems);
        Assert.Contains("Diana Reyes", byAssignee.Assignees);

        var bySearch = Assert.IsType<CrmBoardViewModel>(
            Assert.IsType<ViewResult>(await controller.CRM("Lead Proposal", null, null, null)).Model);
        Assert.Equal(1, bySearch.Leads.TotalItems);
    }

    // ---------- Lead list (CRMLead/Index) ----------

    [Fact]
    public async Task LeadIndex_PagesAtTenAndFiltersByStage()
    {
        using var db = TestControllerSupport.CreateContext();
        for (var i = 0; i < 14; i++)
        {
            db.CrmLeads.Add(new CRMLeadModel
            {
                LeadName = $"Lead {i}",
                Stage = i % 2 == 0 ? "New" : "Won",
                EstimatedValue = 1000m,
                CreatedAt = DateTime.Now.AddDays(-i)
            });
        }
        await db.SaveChangesAsync();

        var controller = CreateLeadController(db);

        var all = Assert.IsType<PagedResult<CRMLeadModel>>(Assert.IsType<ViewResult>(await controller.Index(null, null, null, null)).Model);
        Assert.Equal(14, all.TotalItems);
        Assert.Equal(10, all.Items.Count);
        Assert.Equal(2, all.TotalPages);

        var won = Assert.IsType<PagedResult<CRMLeadModel>>(
            Assert.IsType<ViewResult>(await controller.Index(null, "Won", null, null)).Model);
        Assert.Equal(7, won.TotalItems);
    }

    [Fact]
    public async Task UpdateStage_AdvancesLeadAndStampsLastContact()
    {
        using var db = TestControllerSupport.CreateContext();
        var lead = new CRMLeadModel { LeadName = "Web Inquiry", Stage = "New", EstimatedValue = 120000m };
        db.CrmLeads.Add(lead);
        await db.SaveChangesAsync();

        var controller = CreateLeadController(db);
        var result = await controller.UpdateStage(lead.Id, "Contacted", "/SuperAdmin/CRM");

        Assert.IsType<RedirectResult>(result);
        var updated = await db.CrmLeads.AsNoTracking().FirstAsync(l => l.Id == lead.Id);
        Assert.Equal("Contacted", updated.Stage);
        Assert.NotNull(updated.LastContact);
    }

    [Fact]
    public async Task UpdateStage_WonOnUnlinkedLeadCreatesTheCustomer()
    {
        using var db = TestControllerSupport.CreateContext();
        var lead = new CRMLeadModel
        {
            LeadName = "Ana Reyes",
            Email = "ana@example.com",
            Phone = "0917-111-2222",
            Stage = "Negotiation",
            EstimatedValue = 250000m
        };
        db.CrmLeads.Add(lead);
        await db.SaveChangesAsync();

        await CreateLeadController(db).UpdateStage(lead.Id, "Won");

        var customer = await db.Customers.AsNoTracking().SingleAsync();
        Assert.Equal("Ana Reyes", customer.FullName);
        Assert.Equal("ana@example.com", customer.Email);
        Assert.Contains("Auto-converted", customer.Notes);

        var updatedLead = await db.CrmLeads.AsNoTracking().FirstAsync(l => l.Id == lead.Id);
        Assert.Equal(customer.Id, updatedLead.CustomerId);
    }

    [Fact]
    public async Task Edit_PersistsQualificationAndReassignment()
    {
        using var db = TestControllerSupport.CreateContext();
        var lead = new CRMLeadModel { LeadName = "Web Inquiry", Stage = "New", EstimatedValue = 1000m };
        db.CrmLeads.Add(lead);
        await db.SaveChangesAsync();

        var controller = CreateLeadController(db);
        var result = await controller.Edit(lead.Id, new CRMLeadModel
        {
            Id = lead.Id,
            LeadName = "Web Inquiry (qualified)",
            Company = "Santos Holdings",
            Email = "web@example.com",
            Phone = "0917-000-1111",
            EstimatedValue = 275000m,
            Stage = "Qualified",
            AssignedTo = "Diana Reyes",
            Notes = "Budget confirmed.",
            LastContact = DateTime.Today
        }, "/CRMLead");

        Assert.IsType<RedirectResult>(result);
        var updated = await db.CrmLeads.AsNoTracking().FirstAsync(l => l.Id == lead.Id);
        Assert.Equal("Web Inquiry (qualified)", updated.LeadName);
        Assert.Equal("Santos Holdings", updated.Company);
        Assert.Equal(275000m, updated.EstimatedValue);
        Assert.Equal("Qualified", updated.Stage);
        Assert.Equal("Diana Reyes", updated.AssignedTo);
    }

    [Fact]
    public async Task Delete_RemovesTheLead()
    {
        using var db = TestControllerSupport.CreateContext();
        var lead = new CRMLeadModel { LeadName = "Junk Inquiry", Stage = "New" };
        db.CrmLeads.Add(lead);
        await db.SaveChangesAsync();

        var result = await CreateLeadController(db).Delete(lead.Id, "/SuperAdmin/CRM");

        Assert.IsType<RedirectResult>(result);
        Assert.Empty(await db.CrmLeads.ToListAsync());
    }

    [Fact]
    public async Task Details_And_Delete_ReturnNotFoundForUnknownLead()
    {
        using var db = TestControllerSupport.CreateContext();
        var controller = CreateLeadController(db);

        Assert.IsType<NotFoundResult>(await controller.Details(null));
        Assert.IsType<NotFoundResult>(await controller.Details(999));
        Assert.IsType<NotFoundResult>(await controller.Edit(null));
        Assert.IsType<NotFoundResult>(await controller.Delete(999));
    }

    // ---------- Stage normalization helpers ----------

    [Theory]
    [InlineData("New", CrmBoardViewModel.StageNew)]
    [InlineData("new lead", CrmBoardViewModel.StageNew)]
    [InlineData("Contacted", CrmBoardViewModel.StageContacted)]
    [InlineData("Quotation Sent", CrmBoardViewModel.StageProposal)]
    [InlineData("Proposal", CrmBoardViewModel.StageProposal)]
    [InlineData("Converted", CrmBoardViewModel.StageWon)]
    [InlineData("Locked/Lost", CrmBoardViewModel.StageLost)]
    public void NormalizeStage_MapsSynonymsOntoTheCanonicalPipeline(string input, string expected)
    {
        Assert.Equal(expected, CrmBoardViewModel.NormalizeStage(input));
    }

    [Fact]
    public void DaysWaiting_ReportsAgeInDaysAndNeverGoesNegative()
    {
        var lead = new CRMLeadModel { LeadName = "Old", CreatedAt = DateTime.Now.AddDays(-9) };
        Assert.Equal(9, CrmBoardViewModel.DaysWaiting(lead, DateTime.Now));

        var future = new CRMLeadModel { LeadName = "New", CreatedAt = DateTime.Now.AddDays(3) };
        Assert.Equal(0, CrmBoardViewModel.DaysWaiting(future, DateTime.Now));
    }
}