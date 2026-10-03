using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

/// <summary>
/// A website inquiry submitted through the public Contact / Request-a-Quote form.
/// Inquiries are never typed in by staff - they arrive from the client site and are
/// turned into a CRM lead (Stage = "New") plus a notification for the team that
/// handles inquiries.
/// </summary>
public class InquiryModel
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(120)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(40)]
    public string? Phone { get; set; }

    [Display(Name = "Event Type")]
    [StringLength(60)]
    public string? EventType { get; set; }

    [StringLength(160)]
    [Display(Name = "Company")]
    public string? Company { get; set; }

    [Display(Name = "Event Date")]
    [DataType(DataType.Date)]
    public DateTime? EventDate { get; set; }

    [Range(0, 100000, ErrorMessage = "Guest count looks invalid.")]
    [Display(Name = "Guest Count")]
    public int PaxCount { get; set; }

    [StringLength(200)]
    public string? Venue { get; set; }

    [Display(Name = "Package")]
    public int? PackageId { get; set; }

    [ForeignKey("PackageId")]
    public MenuPackageModel? Package { get; set; }

    [Required(ErrorMessage = "Please tell us about your event.")]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Source { get; set; } = InquirySources.Website;

    [Required]
    [StringLength(40)]
    public string Status { get; set; } = InquiryStatuses.New;

    [Display(Name = "Assigned To")]
    [StringLength(120)]
    public string? AssignedTo { get; set; }

    [Display(Name = "CRM Lead")]
    public int? CrmLeadId { get; set; }

    [ForeignKey("CrmLeadId")]
    public CRMLeadModel? CrmLead { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public CustomerModel? Customer { get; set; }

    /// <summary>
    /// The quotation generated from this inquiry, if the sales team already quoted it.
    /// One quotation per inquiry (Phase 27).
    /// </summary>
    public QuotationModel? Quotation { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Last Updated")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Human-friendly inquiry reference (INQ-YYYY-NNN), shown to the client.</summary>
    [NotMapped]
    public string Reference => $"INQ-{CreatedAt.Year}-{Id:000}";

    /// <summary>A lost inquiry is closed for business - staff cannot quote it anymore.</summary>
    [NotMapped]
    public bool IsQuotable => Status != InquiryStatuses.Lost;
}

/// <summary>Where an inquiry came from.</summary>
public static class InquirySources
{
    public const string Website = "Website";
    public const string PackagesPage = "Packages Page";
    public const string Booking = "Booking";
}

/// <summary>Inquiry workflow states handled by the staff member who owns inquiries.</summary>
public static class InquiryStatuses
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Quoted = "Quoted";
    public const string Won = "Won";
    public const string Lost = "Lost";

    public static readonly string[] All = { New, Contacted, Quoted, Won, Lost };

    /// <summary>CRM lead stage an inquiry status maps to when it is forwarded to the pipeline.</summary>
    public static string? ToLeadStage(string? status) => status switch
    {
        New => CRMLeadModelStages.New,
        Contacted => CRMLeadModelStages.Contacted,
        Quoted => CRMLeadModelStages.Proposal,
        Won => CRMLeadModelStages.Won,
        Lost => CRMLeadModelStages.Lost,
        _ => null
    };
}

/// <summary>
/// Team members who may own an inquiry. Sales / CRM staff handle the inbox by hand,
/// so the assignee list is fixed and shared by the CRM pipeline and the inquiry inbox.
/// </summary>
public static class InquiryAssignees
{
    public static readonly string[] All = { "Carlo Aquino", "Diana Cruz" };
}

/// <summary>
/// Canonical CRM stage strings, kept next to the inquiry statuses so the two
/// vocabularies cannot drift apart.
/// </summary>
public static class CRMLeadModelStages
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Qualified = "Qualified";
    public const string Proposal = "Proposal";
    public const string Negotiation = "Negotiation";
    public const string Won = "Won";
    public const string Lost = "Lost";
}