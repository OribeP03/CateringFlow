using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

public class CRMLeadModel
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public CustomerModel? Customer { get; set; }

    [Required(ErrorMessage = "Lead name is required.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    public string? Company { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Value must be non-negative.")]
    [DataType(DataType.Currency)]
    [Display(Name = "Estimated Value")]
    public decimal EstimatedValue { get; set; }

    [Required]
    [Display(Name = "Pipeline Stage")]
    public string Stage { get; set; } = "New";

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public string? Notes { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Last Contact")]
    [DataType(DataType.Date)]
    public DateTime? LastContact { get; set; }

    [NotMapped]
    public string CustomerName => Customer?.FullName ?? "Unlinked Lead";
}
