using cateringflow.Controllers;
using cateringflow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CateringFlow.Tests;

/// <summary>
/// Test phase A23 - mirror of Phase 23 (Client services + contact/inquiry surfaces).
/// The public site must present real catering services and post a contact inquiry
/// that lands in the inquiry pipeline handled by the events team.
/// </summary>
public class ClientServicesInquiryViewTests
{
    // ---------- client services section ----------

    [Fact]
    public void HomePage_RendersTheServicesAndContactSections()
    {
        var index = TestControllerSupport.RepoFile("Views/Client/Index.cshtml");

        Assert.Contains("_Services.cshtml", index);
        Assert.Contains("_ContactInquiry.cshtml", index);
        Assert.DoesNotContain("_BuiltForPros", index);
    }

    [Theory]
    [InlineData("Wedding Catering")]
    [InlineData("Corporate & Institutional")]
    [InlineData("Galas & Municipal Feasts")]
    [InlineData("Live Stations & Food Halls")]
    [InlineData("Kitchen Management")]
    public void ServicesSection_AdvertisesTheRealServiceLine(string service)
    {
        var services = TestControllerSupport.RepoFile("Views/Client/Components/_Services.cshtml");

        Assert.Contains(service, services);
    }

    [Fact]
    public void ServicesSection_EachServiceLinksIntoTheInquiryForm()
    {
        var services = TestControllerSupport.RepoFile("Views/Client/Components/_Services.cshtml");

        Assert.Contains("openInquiryForm", services);
        Assert.Contains("Request this service", services);
    }

    [Fact]
    public void ServicesSection_NoLongerPromotesErpFeaturesToClients()
    {
        var services = TestControllerSupport.RepoFile("Views/Client/Components/_Services.cshtml");

        foreach (var erpTerm in new[] { "purchase order", "inventory", "profit margin", "supplier" })
        {
            Assert.DoesNotContain(erpTerm, services, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TheOldErpFeaturePartialIsGone()
    {
        Assert.False(TestControllerSupport.RepoFileExists("Views/Client/Components/_BuiltForPros.cshtml"));
    }

    // ---------- contact / inquiry form ----------

    [Theory]
    [InlineData("inquiryForm")]
    [InlineData("inquiryFullName")]
    [InlineData("inquiryEmail")]
    [InlineData("inquiryPhone")]
    [InlineData("inquiryCompany")]
    [InlineData("inquiryEventType")]
    [InlineData("inquiryEventDate")]
    [InlineData("inquiryPax")]
    [InlineData("inquiryVenue")]
    [InlineData("inquiryPackageId")]
    [InlineData("inquiryMessage")]
    [InlineData("inquirySubmitBtn")]
    [InlineData("inquirySuccess")]
    [InlineData("inquiryReference")]
    public void ContactSection_RendersEveryInquiryField(string elementId)
    {
        var contact = TestControllerSupport.RepoFile("Views/Client/Components/_ContactInquiry.cshtml");

        Assert.Contains($"id=\"{elementId}\"", contact);
    }

    [Fact]
    public void ContactSection_CarriesTheCompanyContactDetails()
    {
        var contact = TestControllerSupport.RepoFile("Views/Client/Components/_ContactInquiry.cshtml");

        Assert.Contains("CompanySettings", contact);
        Assert.Contains("CompanyPhone", contact);
        Assert.Contains("CompanyEmail", contact);
        Assert.Contains("CompanyAddress", contact);
    }

    [Fact]
    public void ContactSection_TellsClientsInquiriesAreHandledByTheTeam()
    {
        var contact = TestControllerSupport.RepoFile("Views/Client/Components/_ContactInquiry.cshtml");

        Assert.Contains("one business day", contact);
        Assert.Contains("handled manually by our events team", contact);
    }

    [Fact]
    public void PackagesPage_AlsoOffersTheInquiryForm()
    {
        var packages = TestControllerSupport.RepoFile("Views/Client/Packages.cshtml");

        Assert.Contains("_ContactInquiry.cshtml", packages);
        Assert.Contains("__CATERINGFLOW_INQUIRY_SOURCE__", packages);
        Assert.Contains("Packages Page", packages);
    }

    [Fact]
    public void HomePage_TagsWebsiteInquiriesAsWebsiteSourced()
    {
        var index = TestControllerSupport.RepoFile("Views/Client/Index.cshtml");

        Assert.Contains("__CATERINGFLOW_INQUIRY_SOURCE__ = 'Website'", index);
    }

    // ---------- navigation + calls to action ----------

    [Fact]
    public void Navbar_LinksToServicesAndContact()
    {
        var navbar = TestControllerSupport.RepoFile("Views/Client/Components/_Navbar.cshtml");

        Assert.Contains("href=\"/#services\"", navbar);
        Assert.Contains("href=\"/#contact\"", navbar);
    }

    [Fact]
    public void CtaBanner_AddsARequestAQuoteButton()
    {
        var cta = TestControllerSupport.RepoFile("Views/Client/Components/_CtaBanner.cshtml");

        Assert.Contains("openInquiryForm()", cta);
        Assert.Contains("Request a Quote", cta);
    }

    [Fact]
    public void Footer_LinksTheRealServiceLinesAndContact()
    {
        var footer = TestControllerSupport.RepoFile("Views/Client/Components/_Footer.cshtml");

        Assert.Contains("Wedding Catering", footer);
        Assert.Contains("Galas &amp; Municipal Feasts", footer);
        Assert.Contains("href=\"#contact\"", footer);
    }

    // ---------- client-side behavior ----------

    [Fact]
    public void ClientScript_PostsTheInquiryToTheClientInquiryEndpoint()
    {
        var js = TestControllerSupport.RepoFile("wwwroot/js/client.js");

        Assert.Contains("initInquiryForm()", js);
        Assert.Contains("'/Client/Inquiry'", js);
        Assert.Contains("RequestVerificationToken", js);
        Assert.Contains("data.reference", js);
    }

    [Fact]
    public void ClientScript_ExposesTheHelpersUsedByTheServiceCards()
    {
        var js = TestControllerSupport.RepoFile("wwwroot/js/client.js");

        Assert.Contains("window.openInquiryForm", js);
        Assert.Contains("window.resetInquiryForm", js);
        Assert.Contains("inquiryFullName", js);
        Assert.Contains("inquiryMessage", js);
    }

    // ---------- styling ----------

    [Theory]
    [InlineData(".services-grid")]
    [InlineData(".service-card")]
    [InlineData(".service-icon-box")]
    [InlineData(".service-includes")]
    [InlineData(".contact-details")]
    [InlineData(".inquiry-card")]
    [InlineData(".inquiry-input")]
    [InlineData(".inquiry-success")]
    [InlineData(".btn-inquiry-gold")]
    public void ClientStylesheet_StylesTheServicesAndInquirySurfaces(string selector)
    {
        var css = TestControllerSupport.RepoFile("wwwroot/css/client.css");

        Assert.Contains(selector, css);
    }

    [Fact]
    public void ClientStylesheet_KeepsTheServicesGridResponsive()
    {
        var css = TestControllerSupport.RepoFile("wwwroot/css/client.css");

        Assert.Contains(".services-grid", css);
        Assert.Contains("grid-template-columns: 1fr", css);
    }

    // ---------- controller wiring ----------

    [Fact]
    public async Task HomePage_SharesActivePackagesAndCompanySettingsWithTheContactForm()
    {
        using var db = TestControllerSupport.CreateContext();
        db.MenuPackages.Add(new MenuPackageModel
        {
            PackageName = "Grand Fiesta",
            PricePerPax = 1450m,
            Status = "Active"
        });
        db.MenuPackages.Add(new MenuPackageModel
        {
            PackageName = "Retired Package",
            PricePerPax = 900m,
            Status = "Inactive"
        });
        db.Settings.Add(new SettingsModel
        {
            CompanyName = "CateringFlow Events",
            CompanyPhone = "(02) 5555-1234",
            CompanyEmail = "events@cateringflow.ph",
            CompanyAddress = "1 Test Street"
        });
        await db.SaveChangesAsync();

        var controller = new ClientController(db, new FakeWebHostEnvironment());
        TestControllerSupport.InitController(controller);

        var result = await controller.Index();

        Assert.IsType<ViewResult>(result);
        var packages = Assert.IsType<List<MenuPackageModel>>(controller.ViewData["Packages"]);
        Assert.Single(packages);
        Assert.Equal("Grand Fiesta", packages[0].PackageName);

        var settings = Assert.IsType<SettingsModel>(controller.ViewData["CompanySettings"]);
        Assert.Equal("CateringFlow Events", settings.CompanyName);
    }

    [Fact]
    public async Task PackagesPage_SharesActivePackagesAndCompanySettingsWithTheContactForm()
    {
        using var db = TestControllerSupport.CreateContext();
        db.MenuPackages.Add(new MenuPackageModel
        {
            PackageName = "Grand Fiesta",
            PricePerPax = 1450m,
            Status = "Active"
        });
        await db.SaveChangesAsync();

        var controller = new ClientController(db, new FakeWebHostEnvironment());
        TestControllerSupport.InitController(controller);

        var result = await controller.Packages();

        Assert.IsType<ViewResult>(result);
        Assert.Single(Assert.IsType<List<MenuPackageModel>>(controller.ViewData["Packages"]));
        Assert.True(controller.ViewData.ContainsKey("CompanySettings"));
    }
}