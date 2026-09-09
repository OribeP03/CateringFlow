using System.ComponentModel.DataAnnotations;

namespace cateringflow.Models;

public class SupplierModel
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Supplier name is required.")]
    [Display(Name = "Supplier Name")]
    public string SupplierName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    public string? Address { get; set; }

    [Required]
    public string Category { get; set; } = "Meats & Poultry";

    [Required]
    public string Status { get; set; } = "Active";

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<InventoryModel>? InventoryItems { get; set; }
}
