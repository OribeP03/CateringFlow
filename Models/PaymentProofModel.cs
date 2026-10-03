using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

public class PaymentProofModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Event")]
    public int EventId { get; set; }

    [ForeignKey("EventId")]
    public EventModel? Event { get; set; }

    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public CustomerModel? Customer { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = string.Empty;

    [Display(Name = "Reference Number")]
    public string? ReferenceNumber { get; set; }

    [Display(Name = "Proof Image Path")]
    public string? ProofImagePath { get; set; }

    [Required]
    public string Status { get; set; } = "Pending";

    [Display(Name = "Amount")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    public string? AdminNotes { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<PaymentMessageModel> Messages { get; set; } = new List<PaymentMessageModel>();

    [NotMapped]
    public string CustomerName => Customer?.FullName ?? (CustomerId > 0 ? $"Customer #{CustomerId}" : "N/A");
}
