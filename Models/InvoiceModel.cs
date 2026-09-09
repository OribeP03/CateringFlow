using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

public class InvoiceModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Invoice Number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Display(Name = "Quotation")]
    public int? QuotationId { get; set; }

    [ForeignKey("QuotationId")]
    public QuotationModel? Quotation { get; set; }

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public CustomerModel? Customer { get; set; }

    public int? EventId { get; set; }

    [ForeignKey("EventId")]
    public EventModel? Event { get; set; }

    [Required]
    [DataType(DataType.Currency)]
    [Display(Name = "Total Amount")]
    public decimal TotalAmount { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Amount Paid")]
    public decimal AmountPaid { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Issue Date")]
    public DateTime IssueDate { get; set; } = DateTime.Now;

    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; }

    [Required]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Unpaid";

    public string? Notes { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<PaymentModel>? Payments { get; set; }

    [NotMapped]
    public decimal Balance => TotalAmount - AmountPaid;
    [NotMapped]
    public string CustomerName => Customer?.FullName ?? (CustomerId > 0 ? $"Customer #{CustomerId}" : "N/A");
}
