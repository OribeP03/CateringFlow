using System.ComponentModel.DataAnnotations;

namespace cateringflow.Models;

public class CustomerModel
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    public string? Address { get; set; }

    [Required]
    [Display(Name = "Customer Type")]
    public string Type { get; set; } = "Individual";

    [Required]
    public string Status { get; set; } = "Active";

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    public ICollection<EventModel>? Events { get; set; }
    public ICollection<QuotationModel>? Quotations { get; set; }
    public ICollection<InvoiceModel>? Invoices { get; set; }
    public ICollection<PaymentModel>? Payments { get; set; }
    public ICollection<CRMLeadModel>? CrmLeads { get; set; }
    public ICollection<InquiryModel>? Inquiries { get; set; }
    public ICollection<PaymentProofModel>? PaymentProofs { get; set; }
}
