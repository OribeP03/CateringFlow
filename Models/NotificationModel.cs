using System.ComponentModel.DataAnnotations;

namespace cateringflow.Models;

public class NotificationModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = "Info";

    [Display(Name = "Is Read")]
    public bool IsRead { get; set; }

    [Display(Name = "Target Role")]
    public string? TargetRole { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
