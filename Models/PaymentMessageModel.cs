using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

public class PaymentMessageModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Payment Proof")]
    public int PaymentProofId { get; set; }

    [ForeignKey("PaymentProofId")]
    public PaymentProofModel? PaymentProof { get; set; }

    [Required]
    [Display(Name = "Sender Role")]
    public string SenderRole { get; set; } = "Customer";

    [Required]
    [Display(Name = "Sender Name")]
    public string SenderName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "Image Path")]
    public string? ImagePath { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
