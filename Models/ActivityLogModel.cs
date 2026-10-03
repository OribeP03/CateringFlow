using System.ComponentModel.DataAnnotations;

namespace cateringflow.Models;

public class ActivityLogModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Action")]
    public string Action { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Entity Type")]
    public string EntityType { get; set; } = string.Empty;

    [Display(Name = "Entity Id")]
    public int? EntityId { get; set; }

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Performed By")]
    public string? PerformedBy { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}