using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace cateringflow.Models;

public class InventoryModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Item Code")]
    public string ItemCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Item name is required.")]
    [Display(Name = "Item Name")]
    public string ItemName { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = "Meats & Poultry";

    [Range(0, double.MaxValue, ErrorMessage = "Stock cannot be negative.")]
    [Display(Name = "Current Stock")]
    public double CurrentStock { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Reorder level cannot be negative.")]
    [Display(Name = "Min Reorder Level")]
    public double MinReorderLevel { get; set; }

    public string? Unit { get; set; } = "kg";

    [Range(0, double.MaxValue, ErrorMessage = "Unit cost cannot be negative.")]
    [Display(Name = "Unit Cost")]
    [DataType(DataType.Currency)]
    public decimal UnitCost { get; set; }

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [ForeignKey("SupplierId")]
    public SupplierModel? Supplier { get; set; }

    [Display(Name = "Stock Status")]
    public string StockStatus { get; set; } = "Adequate";

    [Display(Name = "Last Updated")]
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    [NotMapped]
    public string SupplierName => Supplier?.SupplierName ?? (SupplierId > 0 ? "Linked Supplier" : "N/A");
}
