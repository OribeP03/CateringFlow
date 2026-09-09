using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

public class EventModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Event Name")]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public CustomerModel? Customer { get; set; }

    [Required]
    [Display(Name = "Event Type")]
    public string EventType { get; set; } = "Wedding";

    [Required]
    [Display(Name = "Event Date")]
    [DataType(DataType.Date)]
    public DateTime EventDate { get; set; } = DateTime.Now;

    public string? Venue { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please enter a valid number of pax.")]
    [Display(Name = "No. of Pax")]
    public int PaxCount { get; set; }

    [Required]
    public string Status { get; set; } = "Upcoming";

    [Display(Name = "Menu Package")]
    public int? PackageId { get; set; }

    [ForeignKey("PackageId")]
    public MenuPackageModel? Package { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Total Amount")]
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<QuotationModel> Quotations { get; set; } = new List<QuotationModel>();
    public ICollection<InvoiceModel> Invoices { get; set; } = new List<InvoiceModel>();
    public ICollection<StaffAssignmentModel> StaffAssignments { get; set; } = new List<StaffAssignmentModel>();

    [NotMapped]
    public string CustomerName => Customer?.FullName ?? (CustomerId > 0 ? $"Customer #{CustomerId}" : "N/A");
}
