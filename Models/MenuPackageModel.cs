using System.ComponentModel.DataAnnotations;

namespace cateringflow.Models;

public class MenuPackageModel
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Package name is required.")]
    [Display(Name = "Package Name")]
    public string PackageName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value.")]
    [Display(Name = "Price Per Pax")]
    [DataType(DataType.Currency)]
    public decimal PricePerPax { get; set; }

    [Display(Name = "Course Count")]
    public int CourseCount { get; set; } = 4;

    [Display(Name = "Service Hours")]
    public int ServiceHours { get; set; } = 4;

    // Pipe "|"-separated list of inclusions shown on the client "View Details" modal.
    public string? Features { get; set; }

    public string? ImageName { get; set; }

    [Display(Name = "Highlight")]
    public string? Highlight { get; set; }

    [Required]
    public string Status { get; set; } = "Active";

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<EventModel> Events { get; set; } = new List<EventModel>();
    public ICollection<QuotationModel> Quotations { get; set; } = new List<QuotationModel>();
}
