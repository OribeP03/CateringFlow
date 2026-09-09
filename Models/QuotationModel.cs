using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

public class QuotationModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Quotation Number")]
    public string QuotationNumber { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public CustomerModel? Customer { get; set; }

    public int? EventId { get; set; }

    [ForeignKey("EventId")]
    public EventModel? Event { get; set; }

    [Required]
    [Display(Name = "Event Date")]
    [DataType(DataType.Date)]
    public DateTime EventDate { get; set; } = DateTime.Now;

    [Range(1, int.MaxValue, ErrorMessage = "Please enter a valid number of pax.")]
    [Display(Name = "No. of Pax")]
    public int PaxCount { get; set; }

    [Display(Name = "Menu Package")]
    public int? PackageId { get; set; }

    [ForeignKey("PackageId")]
    public MenuPackageModel? Package { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Total Amount")]
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    [Required]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Draft";

    [Display(Name = "Valid Until")]
    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public InvoiceModel? Invoice { get; set; }

    [NotMapped]
    public string CustomerName => Customer?.FullName ?? (CustomerId > 0 ? $"Customer #{CustomerId}" : "N/A");
    [NotMapped]
    public string PackageName => Package?.PackageName ?? "N/A";
}
