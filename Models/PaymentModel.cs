using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

public class PaymentModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Invoice")]
    public int InvoiceId { get; set; }

    [ForeignKey("InvoiceId")]
    public InvoiceModel? Invoice { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public CustomerModel? Customer { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "Cash";

    [Display(Name = "Reference Number")]
    public string? ReferenceNumber { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Payment Date")]
    public DateTime PaymentDate { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [NotMapped]
    public string InvoiceNumber => Invoice?.InvoiceNumber ?? (InvoiceId > 0 ? $"Invoice #{InvoiceId}" : "N/A");
}
